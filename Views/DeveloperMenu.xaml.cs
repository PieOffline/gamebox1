using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            
            OnlinePlayersList.ItemsSource = onlinePlayers;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class OnlinePlayer
    {
        public string IpAddress { get; set; } = "";
        public string FruitCode { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
