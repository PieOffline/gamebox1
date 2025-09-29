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
            ConnectButton.Content = "Connecting...";
            StatusText.Text = "Connecting to opponent...";

            try
            {
                var opponentIp = NetworkUtils.FruitCodeToIp(opponentCode);
                
                if (string.IsNullOrEmpty(opponentIp))
                {
                    MessageBox.Show($"Invalid fruit code: {opponentCode}", "Invalid Code", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusText.Text = $"Checking connection to {opponentIp}...";
                
                // Check if opponent is reachable
                var isReachable = await Task.Run(() => NetworkUtils.IsIpReachable(opponentIp, 3000));
                
                if (!isReachable)
                {
                    MessageBox.Show($"Cannot reach opponent at {opponentCode} ({opponentIp}). " +
                                  "Make sure they are online and connected to the same network.", 
                                  "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusText.Text = "Connection successful! Starting game...";
                
                // Create and show the game window
                var gameWindow = _gameFactory();
                
                // If the game supports multiplayer, pass the opponent IP
                if (gameWindow is IMultiplayerGame multiplayerGame)
                {
                    multiplayerGame.SetOpponent(opponentIp, isHost: true);
                }
                
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