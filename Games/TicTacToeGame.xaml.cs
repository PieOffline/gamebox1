using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameBox.Utils;

namespace GameBox.Games
{
    public partial class TicTacToeGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool isMyTurn = false;
        private string mySymbol = "X";
        private Button[,] gameBoard = new Button[3, 3];
        private bool gameActive = false;

        public TicTacToeGame()
        {
            InitializeComponent();
            InitializeGameBoard();
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
            
            if (isHost)
            {
                mySymbol = "X";
                isMyTurn = true;
                StatusText.Text = "Waiting for opponent to connect...";
                StartServer();
            }
            else
            {
                mySymbol = "O";
                isMyTurn = false;
                StatusText.Text = "Connecting to opponent...";
                ConnectToServer();
            }
        }

        private void InitializeGameBoard()
        {
            GameGrid.Children.Clear();
            
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var button = new Button
                    {
                        FontSize = 48,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(2),
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.DarkBlue,
                        BorderThickness = new Thickness(2)
                    };
                    
                    int currentRow = row;
                    int currentCol = col;
                    button.Click += (sender, e) => OnCellClick(currentRow, currentCol);
                    
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    GameGrid.Children.Add(button);
                    
                    gameBoard[row, col] = button;
                }
            }
        }

        private async void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 12345);
                listener.Start();
                
                StatusText.Text = "Waiting for opponent...";
                
                client = await listener.AcceptTcpClientAsync();
                stream = client.GetStream();
                
                StatusText.Text = "Opponent connected! Your turn (X)";
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
                await client.ConnectAsync(opponentIp, 12345);
                stream = client.GetStream();
                
                StatusText.Text = "Connected! Waiting for opponent's turn (X)";
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
                        var gameMove = JsonSerializer.Deserialize<GameMove>(message);
                        
                        if (gameMove != null)
                        {
                            Dispatcher.Invoke(() => ProcessOpponentMove(gameMove));
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

        private void OnCellClick(int row, int col)
        {
            if (!gameActive || !isMyTurn || !string.IsNullOrEmpty(gameBoard[row, col].Content?.ToString()))
                return;
            
            // Make move
            gameBoard[row, col].Content = mySymbol;
            gameBoard[row, col].Foreground = mySymbol == "X" ? Brushes.Blue : Brushes.Red;
            
            // Send move to opponent
            var move = new GameMove { Row = row, Col = col, Symbol = mySymbol };
            SendMove(move);
            
            // Check for win
            if (CheckWin(mySymbol))
            {
                StatusText.Text = "You win! 🎉";
                gameActive = false;
                ScoreManager.Instance.RecordWin();
                MessageBox.Show("Congratulations! You won!", "Victory!", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Check for draw
            if (CheckDraw())
            {
                StatusText.Text = "It's a draw!";
                gameActive = false;
                MessageBox.Show("Game ended in a draw!", "Draw", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Switch turns
            isMyTurn = false;
            StatusText.Text = "Opponent's turn...";
        }

        private void ProcessOpponentMove(GameMove move)
        {
            // Apply opponent's move
            gameBoard[move.Row, move.Col].Content = move.Symbol;
            gameBoard[move.Row, move.Col].Foreground = move.Symbol == "X" ? Brushes.Blue : Brushes.Red;
            
            // Check for opponent win
            if (CheckWin(move.Symbol))
            {
                StatusText.Text = "Opponent wins!";
                gameActive = false;
                ScoreManager.Instance.RecordLoss();
                MessageBox.Show("Opponent won this round!", "Game Over", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Check for draw
            if (CheckDraw())
            {
                StatusText.Text = "It's a draw!";
                gameActive = false;
                MessageBox.Show("Game ended in a draw!", "Draw", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Switch turns
            isMyTurn = true;
            StatusText.Text = $"Your turn ({mySymbol})";
        }

        private async void SendMove(GameMove move)
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

        private bool CheckWin(string symbol)
        {
            // Check rows
            for (int row = 0; row < 3; row++)
            {
                if (gameBoard[row, 0].Content?.ToString() == symbol &&
                    gameBoard[row, 1].Content?.ToString() == symbol &&
                    gameBoard[row, 2].Content?.ToString() == symbol)
                    return true;
            }
            
            // Check columns
            for (int col = 0; col < 3; col++)
            {
                if (gameBoard[0, col].Content?.ToString() == symbol &&
                    gameBoard[1, col].Content?.ToString() == symbol &&
                    gameBoard[2, col].Content?.ToString() == symbol)
                    return true;
            }
            
            // Check diagonals
            if (gameBoard[0, 0].Content?.ToString() == symbol &&
                gameBoard[1, 1].Content?.ToString() == symbol &&
                gameBoard[2, 2].Content?.ToString() == symbol)
                return true;
                
            if (gameBoard[0, 2].Content?.ToString() == symbol &&
                gameBoard[1, 1].Content?.ToString() == symbol &&
                gameBoard[2, 0].Content?.ToString() == symbol)
                return true;
            
            return false;
        }

        private bool CheckDraw()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (string.IsNullOrEmpty(gameBoard[row, col].Content?.ToString()))
                        return false;
                }
            }
            return true;
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            InitializeGameBoard();
            if (isHost)
            {
                isMyTurn = true;
                StatusText.Text = $"New game started! Your turn ({mySymbol})";
            }
            else
            {
                isMyTurn = false;
                StatusText.Text = "New game started! Waiting for opponent's turn";
            }
            gameActive = true;
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

    public class GameMove
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string Symbol { get; set; } = "";
    }
}