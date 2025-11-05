using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GameBox.Utils;

namespace GameBox.Views
{
    public partial class DeveloperMenu : Window
    {
        public DeveloperMenu()
        {
            InitializeComponent();
            LoadNetworkInfo();
            LoadPlayerStats();
            RefreshOnlinePlayers();
        }

        private void LoadNetworkInfo()
        {
            var localIp = NetworkUtils.GetLocalIPAddress();
            LocalIpText.Text = localIp;
            FruitCodeText.Text = NetworkUtils.IpToFruitCode(localIp);
        }

        private void LoadPlayerStats()
        {
            var scoreManager = ScoreManager.Instance;
            TotalGamesText.Text = scoreManager.GamesPlayed.ToString();
            WinsText.Text = scoreManager.MultiplayerWins.ToString();
            WinRateText.Text = $"{scoreManager.WinPercentage:F1}%";
        }

        private async void RefreshOnlinePlayers_Click(object sender, RoutedEventArgs e)
        {
            await RefreshOnlinePlayers();
        }

        private async Task RefreshOnlinePlayers()
        {
            var onlinePlayers = new List<OnlinePlayer>();
            
            // Get network base
            var networkBase = NetworkUtils.GetNetworkBase();
            var localIp = NetworkUtils.GetLocalIPAddress();
            
            // Scan common IP range (1-254) for players with GameBox app open
            await Task.Run(async () =>
            {
                // Limit concurrent connections to avoid overwhelming the network
                using var semaphore = new SemaphoreSlim(20);
                var tasks = new List<Task>();
                var playersLock = new object();
                
                for (int i = 1; i <= 254; i++)
                {
                    var ip = $"{networkBase}.{i}";
                    
                    // Skip own IP
                    if (ip == localIp) continue;
                    
                    var checkTask = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            // Check if GameBox app is running on this IP
                            var presence = await PresenceService.CheckPlayerPresenceAsync(ip, 500);
                            
                            if (presence != null && presence.IsOnline)
                            {
                                var fruitCode = NetworkUtils.IpToFruitCode(ip);
                                var status = presence.Status == PlayerStatus.InGame ? "In Game" : "Available";
                                
                                lock (playersLock)
                                {
                                    onlinePlayers.Add(new OnlinePlayer
                                    {
                                        IpAddress = ip,
                                        FruitCode = fruitCode,
                                        Status = status
                                    });
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    tasks.Add(checkTask);
                }
                
                await Task.WhenAll(tasks);
            });
            
            // Always add self to the list first
            onlinePlayers.Insert(0, new OnlinePlayer
            {
                IpAddress = localIp,
                FruitCode = NetworkUtils.IpToFruitCode(localIp),
                Status = "You",
                IsSelf = true
            });
            
            OnlinePlayersList.ItemsSource = onlinePlayers;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void TerminateGames_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ NETWORK-WIDE COMMAND ⚠️\n\n" +
                "This will close all game windows on ALL PCs running GameBox on your network!\n\n" +
                "This includes:\n" +
                "• Your own PC\n" +
                "• All connected players\n" +
                "• Any active games in progress\n\n" +
                "Are you sure?",
                "Terminate All Games (Network-Wide)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Terminate games on this PC first
                var localCount = StopAllGames();
                
                // Broadcast command to network
                var command = new DevCommand
                {
                    Type = DevCommandType.TerminateAllGames,
                    Sender = NetworkUtils.IpToFruitCode(NetworkUtils.GetLocalIPAddress())
                };

                var progressWindow = new Window
                {
                    Title = "Broadcasting Command...",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = "📡 Broadcasting terminate command to all PCs on network...\n\nPlease wait...",
                        TextAlignment = System.Windows.TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20),
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                progressWindow.Show();

                var broadcastResult = await DevCommandManager.Instance.BroadcastCommandAsync(command);
                progressWindow.Close();

                var message = $"✓ Terminated {localCount} game(s) locally\n" +
                             $"✓ Command sent to {broadcastResult.SuccessCount} PC(s) on network\n\n";
                
                if (broadcastResult.Details.Count > 0)
                {
                    message += "Reached:\n" + string.Join("\n", broadcastResult.Details.Take(10));
                    if (broadcastResult.Details.Count > 10)
                    {
                        message += $"\n... and {broadcastResult.Details.Count - 10} more";
                    }
                }

                MessageBox.Show(message, "Command Broadcast Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private int StopAllGames()
        {
            // Find and close all game windows using interface detection (more robust than namespace check)
            var windows = Application.Current.Windows;
            var gamesToClose = new List<Window>();

            foreach (Window window in windows)
            {
                // Check if window is a game by checking for multiplayer interfaces
                // or if it's in the Games namespace (fallback)
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
                catch
                {
                    // Ignore errors when closing games
                }
            }

            return gamesToClose.Count;
        }

        private async void LiveUpdate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ NETWORK-WIDE LIVE UPDATE ⚠️\n\n" +
                "This will update ALL PCs running GameBox on your network!\n\n" +
                "Actions performed on all PCs:\n" +
                "1. Stop all running games\n" +
                "2. Pull latest code from main branch\n" +
                "3. Rebuild application\n" +
                "4. Restart with new code\n\n" +
                "Continue?",
                "Live Update (Network-Wide)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Broadcast update command to network first
                var command = new DevCommand
                {
                    Type = DevCommandType.LiveUpdate,
                    Sender = NetworkUtils.IpToFruitCode(NetworkUtils.GetLocalIPAddress()),
                    Payload = "main"
                };

                var progressWindow = new Window
                {
                    Title = "Broadcasting Update...",
                    Width = 450,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = "📡 Broadcasting live update command to all PCs...\n\n" +
                               "All connected PCs will pull, rebuild, and restart.\n\n" +
                               "Please wait...",
                        TextAlignment = System.Windows.TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20),
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                progressWindow.Show();

                var broadcastResult = await DevCommandManager.Instance.BroadcastCommandAsync(command);
                
                // Now perform update on local machine
                try
                {
                    var updateResult = await UpdateManager.PullAndRestartAsync("main");
                    progressWindow.Close();

                    var message = $"✓ Update command sent to {broadcastResult.SuccessCount} PC(s)\n\n";
                    message += updateResult.Message;

                    MessageBox.Show(message, "Update Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (updateResult.Success && updateResult.RequiresRestart)
                    {
                        UpdateManager.RestartApplication();
                    }
                }
                catch (Exception ex)
                {
                    progressWindow.Close();
                    MessageBox.Show($"Update failed with error:\n{ex.Message}", "Update Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void InstallBeta_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ NETWORK-WIDE BETA INSTALLATION ⚠️\n\n" +
                "This will install beta on ALL PCs running GameBox!\n\n" +
                "Actions performed on all PCs:\n" +
                "1. Stop all running games\n" +
                "2. Switch to beta branch\n" +
                "3. Pull latest beta code\n" +
                "4. Rebuild and restart\n\n" +
                "⚠️ Beta may contain bugs and experimental features!\n\n" +
                "Continue?",
                "Install Beta (Network-Wide)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Broadcast beta installation to network first
                var command = new DevCommand
                {
                    Type = DevCommandType.InstallBeta,
                    Sender = NetworkUtils.IpToFruitCode(NetworkUtils.GetLocalIPAddress()),
                    Payload = "beta"
                };

                var progressWindow = new Window
                {
                    Title = "Broadcasting Beta Installation...",
                    Width = 450,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = "📡 Broadcasting beta installation to all PCs...\n\n" +
                               "All connected PCs will switch to beta, rebuild, and restart.\n\n" +
                               "Please wait...",
                        TextAlignment = System.Windows.TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20),
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                progressWindow.Show();

                var broadcastResult = await DevCommandManager.Instance.BroadcastCommandAsync(command);
                
                // Now perform beta installation on local machine
                try
                {
                    var updateResult = await UpdateManager.PullAndRestartAsync("beta");
                    progressWindow.Close();

                    var message = $"✓ Beta installation command sent to {broadcastResult.SuccessCount} PC(s)\n\n";
                    message += updateResult.Message;

                    MessageBox.Show(message, "Beta Installation Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (updateResult.Success && updateResult.RequiresRestart)
                    {
                        UpdateManager.RestartApplication();
                    }
                }
                catch (Exception ex)
                {
                    progressWindow.Close();
                    MessageBox.Show($"Beta installation failed with error:\n{ex.Message}", "Installation Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SelfDestruct_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ NETWORK-WIDE SELF DESTRUCT ⚠️\n\n" +
                "This will IMMEDIATELY close GameBox on ALL PCs on your network!\n\n" +
                "This action:\n" +
                "• Affects ALL connected players\n" +
                "• Terminates ALL games immediately\n" +
                "• Closes the application everywhere\n" +
                "• Cannot be undone\n\n" +
                "Are you ABSOLUTELY sure?",
                "Self Destruct (Network-Wide)",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (result == MessageBoxResult.Yes)
            {
                // Broadcast self-destruct to network first
                var command = new DevCommand
                {
                    Type = DevCommandType.SelfDestruct,
                    Sender = NetworkUtils.IpToFruitCode(NetworkUtils.GetLocalIPAddress())
                };

                // Send command (don't wait for response)
                _ = DevCommandManager.Instance.BroadcastCommandAsync(command);

                // Give network a moment to receive command
                await Task.Delay(500);

                // Close the application on this PC
                Application.Current.Shutdown();
            }
        }
    }

    public class OnlinePlayer
    {
        public string IpAddress { get; set; } = "";
        public string FruitCode { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsSelf { get; set; } = false;
        
        public Brush DisplayColor => IsSelf 
            ? new SolidColorBrush(Color.FromRgb(33, 150, 243))  // Blue for self
            : new SolidColorBrush(Color.FromRgb(76, 175, 80));  // Green for others
    }
}
