using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GameBox.Utils
{
    /// <summary>
    /// Service for broadcasting player presence and status
    /// </summary>
    public class PresenceService
    {
        private static readonly Lazy<PresenceService> _instance = new Lazy<PresenceService>(() => new PresenceService());
        public static PresenceService Instance => _instance.Value;

        // Dedicated port for presence status
        private const int PresencePort = 42422;
        
        private TcpListener? presenceListener;
        private bool isRunning = false;
        private PlayerStatus currentStatus = PlayerStatus.Available;

        private PresenceService()
        {
            StartService();
        }

        /// <summary>
        /// Start the presence service
        /// </summary>
        public void StartService()
        {
            if (isRunning) return;

            try
            {
                presenceListener = new TcpListener(IPAddress.Any, PresencePort);
                presenceListener.Start();
                isRunning = true;

                // Start accepting presence requests asynchronously
                Task.Run(() => AcceptPresenceRequestsAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start presence service: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the presence service
        /// </summary>
        public void StopService()
        {
            if (!isRunning) return;

            isRunning = false;
            presenceListener?.Stop();
            presenceListener = null;
        }

        /// <summary>
        /// Update the current player status
        /// </summary>
        public void UpdateStatus(PlayerStatus status)
        {
            currentStatus = status;
        }

        /// <summary>
        /// Get the presence port number
        /// </summary>
        public static int GetPresencePort() => PresencePort;

        /// <summary>
        /// Check if a remote player is online and get their status
        /// </summary>
        public static async Task<PresenceResponse?> CheckPlayerPresenceAsync(string ipAddress, int timeoutMs = 1000)
        {
            try
            {
                using var client = new TcpClient();
                
                // Try to connect with timeout
                var connectTask = client.ConnectAsync(ipAddress, PresencePort);
                var timeoutTask = Task.Delay(timeoutMs);
                
                if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                {
                    return null; // Connection timeout
                }

                if (!client.Connected)
                {
                    return null;
                }

                using var stream = client.GetStream();
                
                // Send presence request
                var request = new PresenceRequest { RequestTime = DateTime.UtcNow };
                var requestJson = JsonSerializer.Serialize(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                // Wait for response
                var buffer = new byte[1024];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                var readTimeout = Task.Delay(timeoutMs);
                
                if (await Task.WhenAny(readTask, readTimeout) == readTimeout)
                {
                    return null;
                }

                int bytesRead = await readTask;
                if (bytesRead == 0)
                {
                    return null;
                }

                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                var response = JsonSerializer.Deserialize<PresenceResponse>(responseJson);
                
                return response;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Accept incoming presence requests
        /// </summary>
        private async Task AcceptPresenceRequestsAsync()
        {
            while (isRunning)
            {
                try
                {
                    if (presenceListener == null) break;

                    var client = await presenceListener.AcceptTcpClientAsync();
                    
                    // Handle request in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandlePresenceRequestAsync(client);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error handling presence request: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error accepting presence request: {ex.Message}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Handle an incoming presence request
        /// </summary>
        private async Task HandlePresenceRequestAsync(TcpClient client)
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
                        // Send back current status
                        var response = new PresenceResponse
                        {
                            IsOnline = true,
                            Status = currentStatus,
                            ResponseTime = DateTime.UtcNow
                        };

                        var responseJson = JsonSerializer.Serialize(response);
                        var responseBytes = Encoding.UTF8.GetBytes(responseJson + "\n");
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        await stream.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandlePresenceRequestAsync: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Player status enum
    /// </summary>
    public enum PlayerStatus
    {
        Available,
        InGame
    }

    /// <summary>
    /// Presence request
    /// </summary>
    public class PresenceRequest
    {
        public DateTime RequestTime { get; set; }
    }

    /// <summary>
    /// Presence response
    /// </summary>
    public class PresenceResponse
    {
        public bool IsOnline { get; set; }
        public PlayerStatus Status { get; set; }
        public DateTime ResponseTime { get; set; }
    }
}
