using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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
            
            // Scan common IP range (1-254) for active players
            await Task.Run(() =>
            {
                for (int i = 1; i <= 254; i++)
                {
                    var ip = $"{networkBase}.{i}";
                    var localIp = NetworkUtils.GetLocalIPAddress();
                    
                    // Skip own IP
                    if (ip == localIp) continue;
                    
                    // Quick ping check
                    try
                    {
                        using var ping = new Ping();
                        var reply = ping.Send(ip, 100);
                        
                        if (reply.Status == IPStatus.Success)
                        {
                            var fruitCode = NetworkUtils.IpToFruitCode(ip);
                            var status = NetworkManager.Instance.IsInGame ? "In Game" : "Available";
                            
                            Dispatcher.Invoke(() =>
                            {
                                onlinePlayers.Add(new OnlinePlayer
                                {
                                    IpAddress = ip,
                                    FruitCode = fruitCode,
                                    Status = "Online"
                                });
                            });
                        }
                    }
                    catch { }
                }
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
