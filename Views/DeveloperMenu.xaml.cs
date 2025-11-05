using System;
using System.Collections.Generic;
using System.Globalization;
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

        private void TerminateGames_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will immediately close all open game windows. Are you sure?",
                "Terminate All Games",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Find and close all game windows
                var windows = Application.Current.Windows;
                var gamesToClose = new List<Window>();

                foreach (Window window in windows)
                {
                    // Don't close main window or this developer menu
                    if (window.GetType().Namespace == "GameBox.Games")
                    {
                        gamesToClose.Add(window);
                    }
                }

                foreach (var game in gamesToClose)
                {
                    game.Close();
                }

                MessageBox.Show($"Terminated {gamesToClose.Count} game window(s).", "Games Terminated",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void LiveUpdate_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will:\n" +
                "1. Stop all running games\n" +
                "2. Pull the latest code from the main branch\n" +
                "3. Rebuild and restart the application\n\n" +
                "Continue?",
                "Live Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show(
                    "Live updates are not yet implemented in this build.\n\n" +
                    "To update, please:\n" +
                    "1. Close the application\n" +
                    "2. Pull the latest changes from git\n" +
                    "3. Rebuild and restart",
                    "Feature Not Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void InstallBeta_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will:\n" +
                "1. Stop all running games\n" +
                "2. Switch to the beta branch\n" +
                "3. Pull the latest beta code\n" +
                "4. Rebuild and restart the application\n\n" +
                "Continue?",
                "Install Beta",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show(
                    "Beta installation is not yet implemented in this build.\n\n" +
                    "To install beta, please:\n" +
                    "1. Close the application\n" +
                    "2. Switch to the beta branch: git checkout beta\n" +
                    "3. Pull the latest changes\n" +
                    "4. Rebuild and restart",
                    "Feature Not Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void SelfDestruct_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ WARNING ⚠️\n\n" +
                "This will IMMEDIATELY close the entire application without any further prompts.\n\n" +
                "Are you absolutely sure?",
                "Self Destruct",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (result == MessageBoxResult.Yes)
            {
                // Close the application immediately
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
