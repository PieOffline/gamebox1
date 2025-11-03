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
    public partial class RockPaperScissorsGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool gameActive = false;
        
        private string? myChoice = null;
        private string? opponentChoice = null;
        private int myScore = 0;
        private int opponentScore = 0;

        public RockPaperScissorsGame()
        {
            InitializeComponent();
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
            if (!gameActive || myChoice != null) return;
            
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
            // Reset for new round
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
