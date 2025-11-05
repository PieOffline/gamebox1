using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GameBox.Utils;

namespace GameBox.Games
{
    public partial class RockPaperScissorsGame : Window, IMultiplayerGame, ILocalMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool gameActive = false;
        private bool isLocalMultiplayer = false;
        
        private string? myChoice = null;
        private string? opponentChoice = null;
        private int myScore = 0;
        private int opponentScore = 0;
        
        // Local multiplayer specific
        private string? player1Choice = null;
        private string? player2Choice = null;
        private int player1Score = 0;
        private int player2Score = 0;
        private bool isPlayer1Turn = true;

        public RockPaperScissorsGame()
        {
            InitializeComponent();
        }

        public void SetLocalMultiplayerMode(bool enabled)
        {
            isLocalMultiplayer = enabled;
            gameActive = true;
            isPlayer1Turn = true;
            StatusText.Text = "Player 1: Make your choice (then pass to Player 2)!";
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
            
            StatusText.Text = isHost ? "Waiting for opponent to connect..." : "Connecting to opponent...";
            
            if (isHost)
            {
                StartServer();
            }
            else
            {
                ConnectToServer();
            }
        }

        private async void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, NetworkManager.Instance.GetGamePort());
                listener.Start();
                
                StatusText.Text = "Waiting for opponent...";
                
                client = await listener.AcceptTcpClientAsync();
                stream = client.GetStream();
                
                StatusText.Text = "Opponent connected! Make your choice!";
                gameActive = true;
                
                // Start listening for messages
                _ = Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start server: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(opponentIp, NetworkManager.Instance.GetGamePort());
                stream = client.GetStream();
                
                StatusText.Text = "Connected! Make your choice!";
                gameActive = true;
                
                // Start listening for messages
                _ = Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to opponent: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ListenForMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];
                
                while (client?.Connected == true && stream != null)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var rpsMove = JsonSerializer.Deserialize<RPSMove>(message);
                        
                        if (rpsMove != null)
                        {
                            Dispatcher.Invoke(() => ProcessOpponentMove(rpsMove));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => 
                {
                    MessageBox.Show($"Connection lost: {ex.Message}", "Connection Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                });
            }
        }

        private void Rock_Click(object sender, RoutedEventArgs e)
        {
            MakeChoice("Rock", "🪨");
        }

        private void Paper_Click(object sender, RoutedEventArgs e)
        {
            MakeChoice("Paper", "📄");
        }

        private void Scissors_Click(object sender, RoutedEventArgs e)
        {
            MakeChoice("Scissors", "✂️");
        }

        private void MakeChoice(string choice, string emoji)
        {
            if (!gameActive) return;

            // Local multiplayer mode - hot seat style
            if (isLocalMultiplayer)
            {
                if (isPlayer1Turn && player1Choice == null)
                {
                    player1Choice = choice;
                    YourChoiceText.Text = "✓";
                    StatusText.Text = "Player 2: Make your choice!";
                    ResultText.Text = "Player 1 has chosen. Player 2's turn...";
                    isPlayer1Turn = false;
                }
                else if (!isPlayer1Turn && player2Choice == null)
                {
                    player2Choice = choice;
                    OpponentChoiceText.Text = "✓";
                    CheckForWinnerLocal();
                }
                return;
            }

            // Network multiplayer mode
            if (myChoice != null) return;
            
            myChoice = choice;
            YourChoiceText.Text = emoji;
            
            // Disable buttons after choice
            RockButton.IsEnabled = false;
            PaperButton.IsEnabled = false;
            ScissorsButton.IsEnabled = false;
            
            StatusText.Text = "Waiting for opponent's choice...";
            ResultText.Text = "Waiting for opponent...";
            
            // Send choice to opponent
            var move = new RPSMove { Choice = choice };
            SendMove(move);
            
            // Check if we can determine winner
            CheckForWinner();
        }

        private void ProcessOpponentMove(RPSMove move)
        {
            opponentChoice = move.Choice;
            
            // Display opponent's choice
            OpponentChoiceText.Text = move.Choice switch
            {
                "Rock" => "🪨",
                "Paper" => "📄",
                "Scissors" => "✂️",
                _ => "?"
            };
            
            // Check if we can determine winner
            CheckForWinner();
        }

        private void CheckForWinnerLocal()
        {
            if (player1Choice == null || player2Choice == null) return;
            
            // Show both choices
            YourChoiceText.Text = player1Choice switch
            {
                "Rock" => "🪨",
                "Paper" => "📄",
                "Scissors" => "✂️",
                _ => "?"
            };
            
            OpponentChoiceText.Text = player2Choice switch
            {
                "Rock" => "🪨",
                "Paper" => "📄",
                "Scissors" => "✂️",
                _ => "?"
            };
            
            // Determine winner
            string result;
            if (player1Choice == player2Choice)
            {
                result = "It's a tie! 🤝";
                ResultText.Text = result;
            }
            else if ((player1Choice == "Rock" && player2Choice == "Scissors") ||
                     (player1Choice == "Paper" && player2Choice == "Rock") ||
                     (player1Choice == "Scissors" && player2Choice == "Paper"))
            {
                result = "Player 1 wins this round! 🎉";
                player1Score++;
                ResultText.Text = result;
            }
            else
            {
                result = "Player 2 wins this round! 🎉";
                player2Score++;
                ResultText.Text = result;
            }
            
            // Update scores
            YourScoreText.Text = player1Score.ToString();
            OpponentScoreText.Text = player2Score.ToString();
            
            StatusText.Text = "Round complete! Click 'New Round' to play again.";
        }

        private void CheckForWinner()
        {
            if (myChoice == null || opponentChoice == null) return;
            
            // Determine winner
            string result;
            if (myChoice == opponentChoice)
            {
                result = "It's a tie! 🤝";
                ResultText.Text = result;
            }
            else if ((myChoice == "Rock" && opponentChoice == "Scissors") ||
                     (myChoice == "Paper" && opponentChoice == "Rock") ||
                     (myChoice == "Scissors" && opponentChoice == "Paper"))
            {
                result = "You win this round! 🎉";
                myScore++;
                ResultText.Text = result;
                ScoreManager.Instance.RecordWin();
            }
            else
            {
                result = "Opponent wins this round!";
                opponentScore++;
                ResultText.Text = result;
                ScoreManager.Instance.RecordLoss();
            }
            
            // Update scores
            YourScoreText.Text = myScore.ToString();
            OpponentScoreText.Text = opponentScore.ToString();
            
            StatusText.Text = "Round complete! Click 'New Round' to play again.";
        }

        private async void SendMove(RPSMove move)
        {
            try
            {
                if (stream != null)
                {
                    string json = JsonSerializer.Serialize(move);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send move: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewRound_Click(object sender, RoutedEventArgs e)
        {
            if (isLocalMultiplayer)
            {
                // Reset for new round in local multiplayer
                player1Choice = null;
                player2Choice = null;
                isPlayer1Turn = true;
                YourChoiceText.Text = "?";
                OpponentChoiceText.Text = "?";
                ResultText.Text = "Make your choice!";
                StatusText.Text = "Player 1: Make your choice!";
            }
            else
            {
                // Reset for new round in network multiplayer
                myChoice = null;
                opponentChoice = null;
                YourChoiceText.Text = "?";
                OpponentChoiceText.Text = "?";
                ResultText.Text = "Make your choice!";
                StatusText.Text = "Choose Rock, Paper, or Scissors!";
                
                // Re-enable buttons
                RockButton.IsEnabled = true;
                PaperButton.IsEnabled = true;
                ScissorsButton.IsEnabled = true;
            }
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try
            {
                stream?.Close();
                client?.Close();
                listener?.Stop();
            }
            catch { }
        }
    }

    public class RPSMove
    {
        public string Choice { get; set; } = "";
    }
}
