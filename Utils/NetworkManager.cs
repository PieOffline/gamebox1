using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GameBox.Utils
{
    /// <summary>
    /// Manages multiplayer networking with request/response model
    /// Uses two ports: one for game requests, one for active game connections
    /// </summary>
    public class NetworkManager
    {
        private static NetworkManager? _instance;
        public static NetworkManager Instance => _instance ??= new NetworkManager();

        // Port for receiving game requests
        private const int RequestPort = 42420;
        
        // Port for active game connections
        private const int GamePort = 42421;
        
        private TcpListener? requestListener;
        private bool isListening = false;
        private bool isInGame = false;
        private string currentGameName = "";

        private NetworkManager()
        {
            StartListening();
            // Initialize presence service
            _ = PresenceService.Instance;
        }

        /// <summary>
        /// Start listening for game requests
        /// </summary>
        public void StartListening()
        {
            if (isListening) return;

            try
            {
                requestListener = new TcpListener(IPAddress.Any, RequestPort);
                requestListener.Start();
                isListening = true;

                // Start accepting requests asynchronously
                Task.Run(() => AcceptRequestsAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start request listener: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop listening for game requests
        /// </summary>
        public void StopListening()
        {
            if (!isListening) return;

            isListening = false;
            requestListener?.Stop();
            requestListener = null;
        }

        /// <summary>
        /// Check if player is currently in a game
        /// </summary>
        public bool IsInGame => isInGame;

        /// <summary>
        /// Set the in-game status
        /// </summary>
        public void SetInGameStatus(bool inGame, string gameName = "")
        {
            isInGame = inGame;
            currentGameName = gameName;
            
            // Update presence service status
            PresenceService.Instance.UpdateStatus(
                inGame ? PlayerStatus.InGame : PlayerStatus.Available
            );
        }

        /// <summary>
        /// Send a game request to another player
        /// </summary>
        public async Task<GameRequestResponse> SendGameRequestAsync(string opponentIp, string gameName, string hostCode)
        {
            try
            {
                using var client = new TcpClient();
                
                // Try to connect with timeout
                var connectTask = client.ConnectAsync(opponentIp, RequestPort);
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
                {
                    return new GameRequestResponse
                    {
                        Success = false,
                        Message = "Connection timeout - opponent may not be online"
                    };
                }

                using var stream = client.GetStream();
                
                // Send request
                var request = new GameRequest
                {
                    GameName = gameName,
                    SenderIp = NetworkUtils.GetLocalIPAddress(),
                    SenderCode = hostCode,
                    Timestamp = DateTime.UtcNow
                };

                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                await stream.WriteAsync(new byte[] { (byte)'\n' }, 0, 1);

                // Wait for response with timeout
                var buffer = new byte[1024];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                if (await Task.WhenAny(readTask, Task.Delay(30000)) != readTask)
                {
                    return new GameRequestResponse
                    {
                        Success = false,
                        Message = "No response from opponent"
                    };
                }

                int bytesRead = await readTask;
                if (bytesRead == 0)
                {
                    return new GameRequestResponse
                    {
                        Success = false,
                        Message = "Connection closed by opponent"
                    };
                }

                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                var response = JsonSerializer.Deserialize<GameRequestResponse>(responseJson);

                return response ?? new GameRequestResponse
                {
                    Success = false,
                    Message = "Invalid response from opponent"
                };
            }
            catch (Exception ex)
            {
                return new GameRequestResponse
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Accept incoming game requests
        /// </summary>
        private async Task AcceptRequestsAsync()
        {
            while (isListening)
            {
                try
                {
                    if (requestListener == null) break;

                    var client = await requestListener.AcceptTcpClientAsync();
                    
                    // Handle request in background with error handling
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleGameRequestAsync(client);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Unhandled error in HandleGameRequestAsync: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (isListening)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error accepting request: {ex.Message}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Handle an incoming game request
        /// </summary>
        private async Task HandleGameRequestAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    using var stream = client.GetStream();
                    var buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        var request = JsonSerializer.Deserialize<GameRequest>(requestJson);

                        if (request != null)
                        {
                            // Check if player is already in a game
                            if (isInGame)
                            {
                                var response = new GameRequestResponse
                                {
                                    Success = false,
                                    Message = $"Player is currently in a game ({currentGameName})"
                                };
                                await SendResponseAsync(stream, response);
                                return;
                            }

                            // Notify UI thread about the request and wait for user response
                            var tcs = new TaskCompletionSource<GameRequestResponse>();
                            
                            // Use BeginInvoke to avoid potential deadlocks
                            // Warning CS4014 suppressed: we handle result via TaskCompletionSource
                            _ = Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                try
                                {
                                    var dialog = new Views.GameRequestDialog(request);
                                    bool? result = dialog.ShowDialog();
                                    
                                    var response = new GameRequestResponse
                                    {
                                        Success = result == true,
                                        Message = result == true ? "Request accepted" : "Request declined"
                                    };
                                    
                                    tcs.SetResult(response);
                                }
                                catch (Exception ex)
                                {
                                    tcs.SetResult(new GameRequestResponse
                                    {
                                        Success = false,
                                        Message = $"Error: {ex.Message}"
                                    });
                                }
                            });

                            // Wait for user response
                            var userResponse = await tcs.Task;
                            await SendResponseAsync(stream, userResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling game request: {ex.Message}");
            }
        }

        /// <summary>
        /// Send response back to requester
        /// </summary>
        private async Task SendResponseAsync(NetworkStream stream, GameRequestResponse response)
        {
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.WriteAsync(new byte[] { (byte)'\n' }, 0, 1);
            await stream.FlushAsync();
        }

        /// <summary>
        /// Get the game connection port
        /// </summary>
        public int GetGamePort() => GamePort;
    }

    /// <summary>
    /// Represents a game request from one player to another
    /// </summary>
    public class GameRequest
    {
        public string GameName { get; set; } = "";
        public string SenderIp { get; set; } = "";
        public string SenderCode { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the response to a game request
    /// </summary>
    public class GameRequestResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
