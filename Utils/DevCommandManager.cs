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
    /// Manages developer commands that can be broadcast across the network
    /// </summary>
    public class DevCommandManager
    {
        private static DevCommandManager? _instance;
        public static DevCommandManager Instance => _instance ??= new DevCommandManager();

        // Dedicated port for developer commands
        private const int DevCommandPort = 42422;
        
        private TcpListener? devListener;
        private bool isListening = false;

        private DevCommandManager()
        {
            StartListening();
        }

        /// <summary>
        /// Start listening for developer commands
        /// </summary>
        public void StartListening()
        {
            if (isListening) return;

            try
            {
                devListener = new TcpListener(IPAddress.Any, DevCommandPort);
                devListener.Start();
                isListening = true;

                // Start accepting commands asynchronously
                Task.Run(() => AcceptCommandsAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start dev command listener: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop listening for developer commands
        /// </summary>
        public void StopListening()
        {
            if (!isListening) return;

            isListening = false;
            devListener?.Stop();
            devListener = null;
        }

        /// <summary>
        /// Broadcast a developer command to all PCs on the network
        /// </summary>
        public async Task<BroadcastResult> BroadcastCommandAsync(DevCommand command)
        {
            var results = new List<string>();
            var successCount = 0;
            var failureCount = 0;

            var networkBase = NetworkUtils.GetNetworkBase();
            var localIp = NetworkUtils.GetLocalIPAddress();

            // Limit concurrent connections
            using var semaphore = new SemaphoreSlim(20);
            var tasks = new List<Task>();
            var resultsLock = new object();

            for (int i = 1; i <= 254; i++)
            {
                var ip = $"{networkBase}.{i}";
                
                // Skip own IP
                if (ip == localIp) continue;

                var sendTask = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var sent = await SendCommandToIpAsync(ip, command);
                        lock (resultsLock)
                        {
                            if (sent)
                            {
                                successCount++;
                                results.Add($"✓ {NetworkUtils.IpToFruitCode(ip)} ({ip})");
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(sendTask);
            }

            await Task.WhenAll(tasks);

            return new BroadcastResult
            {
                Success = successCount > 0,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Details = results
            };
        }

        /// <summary>
        /// Send a command to a specific IP address
        /// </summary>
        private async Task<bool> SendCommandToIpAsync(string ip, DevCommand command)
        {
            try
            {
                using var client = new TcpClient();
                
                // Try to connect with short timeout
                var connectTask = client.ConnectAsync(ip, DevCommandPort);
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                {
                    return false; // Timeout
                }

                using var stream = client.GetStream();
                
                // Serialize and send command
                var commandJson = JsonSerializer.Serialize(command);
                var commandBytes = Encoding.UTF8.GetBytes(commandJson + "\n");
                await stream.WriteAsync(commandBytes, 0, commandBytes.Length);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Accept incoming developer commands
        /// </summary>
        private async Task AcceptCommandsAsync()
        {
            while (isListening)
            {
                try
                {
                    if (devListener == null) break;

                    var client = await devListener.AcceptTcpClientAsync();
                    
                    // Handle command in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleDevCommandAsync(client);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error handling dev command: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (isListening)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error accepting dev command: {ex.Message}");
                        await Task.Delay(100); // Brief delay before retry
                    }
                }
            }
        }

        /// <summary>
        /// Handle an incoming developer command
        /// </summary>
        private async Task HandleDevCommandAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        var commandJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        var command = JsonSerializer.Deserialize<DevCommand>(commandJson);

                        if (command != null)
                        {
                            await ExecuteCommandAsync(command);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleDevCommandAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute a received developer command
        /// </summary>
        private async Task ExecuteCommandAsync(DevCommand command)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (command.Type)
                    {
                        case DevCommandType.TerminateAllGames:
                            TerminateAllGames();
                            ShowNotification($"Dev Command: All games terminated by {command.Sender}");
                            break;

                        case DevCommandType.SelfDestruct:
                            ShowNotification($"Dev Command: Application shutting down by order of {command.Sender}", isWarning: true);
                            Task.Delay(2000).ContinueWith(_ =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Application.Current.Shutdown();
                                });
                            });
                            break;

                        case DevCommandType.LiveUpdate:
                            ShowNotification($"Dev Command: Live update initiated by {command.Sender}");
                            Task.Run(async () => await PerformLiveUpdateAsync(command.Payload));
                            break;

                        case DevCommandType.InstallBeta:
                            ShowNotification($"Dev Command: Beta installation initiated by {command.Sender}");
                            Task.Run(async () => await PerformLiveUpdateAsync(command.Payload));
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing command: {ex.Message}");
            }
        }

        /// <summary>
        /// Terminate all game windows
        /// </summary>
        private void TerminateAllGames()
        {
            var windows = Application.Current.Windows;
            var gamesToClose = new List<Window>();

            foreach (Window window in windows)
            {
                if (window is IMultiplayerGame || window is ILocalMultiplayerGame || 
                    window.GetType().Namespace == "GameBox.Games")
                {
                    gamesToClose.Add(window);
                }
            }

            foreach (var game in gamesToClose)
            {
                try
                {
                    game.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// Perform live update by pulling code and reloading
        /// </summary>
        private async Task PerformLiveUpdateAsync(string? branch)
        {
            try
            {
                // Show update screen
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowUpdateScreen("Receiving live update...");
                });

                // For now, perform the update and restart
                // True hot-reload would require more complex assembly reloading
                var result = await UpdateManager.PullAndRestartAsync(branch ?? "main");
                
                if (result.Success && result.RequiresRestart)
                {
                    await Task.Delay(1000);
                    UpdateManager.RestartApplication();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Live update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show a notification to the user
        /// </summary>
        private void ShowNotification(string message, bool isWarning = false)
        {
            try
            {
                var icon = isWarning ? MessageBoxImage.Warning : MessageBoxImage.Information;
                MessageBox.Show(message, "Developer Command", MessageBoxButton.OK, icon);
            }
            catch { }
        }

        /// <summary>
        /// Show an update screen
        /// </summary>
        private void ShowUpdateScreen(string message)
        {
            try
            {
                var updateWindow = new Window
                {
                    Title = "Live Update",
                    Width = 500,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Topmost = true,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = message,
                        TextAlignment = System.Windows.TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        TextWrapping = System.Windows.TextWrapping.Wrap,
                        Margin = new Thickness(20)
                    }
                };
                updateWindow.Show();
            }
            catch { }
        }
    }

    /// <summary>
    /// Types of developer commands
    /// </summary>
    public enum DevCommandType
    {
        TerminateAllGames,
        SelfDestruct,
        LiveUpdate,
        InstallBeta
    }

    /// <summary>
    /// Developer command structure
    /// </summary>
    public class DevCommand
    {
        public DevCommandType Type { get; set; }
        public string Sender { get; set; } = "";
        public string? Payload { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of broadcasting a command
    /// </summary>
    public class BroadcastResult
    {
        public bool Success { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Details { get; set; } = new List<string>();
    }
}
