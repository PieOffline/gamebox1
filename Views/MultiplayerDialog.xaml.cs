using System;
using System.Threading.Tasks;
using System.Windows;
using GameBox.Utils;

namespace GameBox
{
    public partial class MultiplayerDialog : Window
    {
        private readonly string _gameName;
        private readonly Func<Window> _gameFactory;

        public MultiplayerDialog(string gameName, Func<Window> gameFactory)
        {
            InitializeComponent();
            _gameName = gameName;
            _gameFactory = gameFactory;
            
            GameNameText.Text = gameName;
            DataContext = this;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var opponentCode = OpponentCodeTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(opponentCode))
            {
                MessageBox.Show("Please enter your opponent's fruit code.", "Missing Code", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable UI while connecting
            ConnectButton.IsEnabled = false;
            ConnectButton.Content = "Sending Request...";
            StatusText.Text = "Sending game request...";

            try
            {
                // First try parsing as custom format (supports 20.Apple, 5.102.Coffee, 1.1.1.Apple, etc.)
                var opponentIp = NetworkUtils.ParseCustomIpFormat(opponentCode);
                
                if (string.IsNullOrEmpty(opponentIp))
                {
                    MessageBox.Show($"Invalid fruit code or IP format: {opponentCode}\n\n" +
                                  "Valid formats:\n" +
                                  "  - Fruit code (e.g., Apple)\n" +
                                  "  - Last two octets (e.g., 20.Apple for x.x.x.20.1)\n" +
                                  "  - Last three octets (e.g., 5.102.Coffee for x.5.102.3)\n" +
                                  "  - Full IP (e.g., 1.1.1.Apple for 1.1.1.1)", 
                        "Invalid Code", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusText.Text = $"Sending request to {opponentCode} ({opponentIp})...";
                
                // Send game request using new network manager
                var networkManager = NetworkManager.Instance;
                var localCode = NetworkUtils.IpToFruitCode(NetworkUtils.GetLocalIPAddress());
                
                var response = await networkManager.SendGameRequestAsync(opponentIp, _gameName, localCode);
                
                if (!response.Success)
                {
                    MessageBox.Show($"{opponentCode} declined or could not respond:\n{response.Message}", 
                                  "Request Declined", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                StatusText.Text = "Request accepted! Starting game...";
                
                // Mark as in-game
                networkManager.SetInGameStatus(true, _gameName);
                
                // Create and show the game window
                var gameWindow = _gameFactory();
                
                // If the game supports multiplayer, pass the opponent IP
                if (gameWindow is IMultiplayerGame multiplayerGame)
                {
                    multiplayerGame.SetOpponent(opponentIp, isHost: true);
                }
                
                // When game window closes, mark as not in game
                void OnGameClosed(object? s, EventArgs args)
                {
                    networkManager.SetInGameStatus(false);
                    gameWindow.Closed -= OnGameClosed; // Detach to prevent memory leak
                }
                gameWindow.Closed += OnGameClosed;
                
                gameWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable UI
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Connect & Play";
                StatusText.Text = "";
            }
        }

        private void LocalMultiplayer_Click(object sender, RoutedEventArgs e)
        {
            // For local multiplayer (testing), use localhost
            try
            {
                StatusText.Text = "Starting local multiplayer game...";
                
                // Mark as in-game
                var networkManager = NetworkManager.Instance;
                networkManager.SetInGameStatus(true, _gameName);
                
                // Create and show the game window
                var gameWindow = _gameFactory();
                
                // If the game supports multiplayer, pass localhost
                if (gameWindow is IMultiplayerGame multiplayerGame)
                {
                    multiplayerGame.SetOpponent("127.0.0.1", isHost: true);
                }
                
                // When game window closes, mark as not in game
                void OnGameClosed(object? s, EventArgs args)
                {
                    networkManager.SetInGameStatus(false);
                    gameWindow.Closed -= OnGameClosed;
                }
                gameWindow.Closed += OnGameClosed;
                
                gameWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpponentCodeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Auto-capitalize fruit codes
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                var cursorPosition = textBox.SelectionStart;
                textBox.Text = CapitalizeFruitCode(textBox.Text);
                textBox.SelectionStart = cursorPosition;
            }
        }

        private string CapitalizeFruitCode(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }

    // Interface for multiplayer games
    public interface IMultiplayerGame
    {
        void SetOpponent(string opponentIp, bool isHost);
    }
}