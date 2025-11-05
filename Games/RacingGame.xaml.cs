using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GameBox.Utils;

namespace GameBox.Games
{
    public partial class RacingGame : Window, IMultiplayerGame, ILocalMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool gameActive = false;
        private bool isLocalMultiplayer = false;
        
        private DispatcherTimer gameTimer = new DispatcherTimer();
        
        // Player 1 car
        private Rectangle playerCar;
        private double playerX = 250;
        private double playerY = 500;
        private double playerPosition = 0; // Track position in meters
        
        // Player 2 car (opponent in local mode)
        private Rectangle opponentCar;
        private double opponentX = 300;
        private double opponentY = 500;
        private double opponentPosition = 0;
        
        // Movement for Player 1 (AD keys)
        private bool leftPressed, rightPressed;
        
        // Movement for Player 2 (Arrow keys)
        private bool arrowLeftPressed, arrowRightPressed;
        
        private double carSpeed = 5;
        
        private const double CarWidth = 40;
        private const double CarHeight = 60;
        private const double FinishLine = 10000; // meters

        public RacingGame()
        {
            InitializeComponent();
            
            gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            gameTimer.Tick += GameLoop;
            
            this.Loaded += (s, e) => 
            {
                GameCanvas.Focus();
                DrawTrack();
            };
        }

        public void SetLocalMultiplayerMode(bool enabled)
        {
            isLocalMultiplayer = enabled;
            gameActive = true;
            
            // Set up cars for local multiplayer
            playerX = 230;
            opponentX = 300;
            
            InitializeCars();
            DrawTrack();
            gameTimer.Start();
            
            StatusText.Text = "Local Multiplayer: Player 1 (A/D) vs Player 2 (Left/Right Arrows)";
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
            
            // Adjust starting positions
            if (isHost)
            {
                playerX = 230;
                opponentX = 300;
            }
            else
            {
                playerX = 300;
                opponentX = 230;
            }
            
            InitializeCars();
            
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

        private void DrawTrack()
        {
            // Draw road lanes
            for (int i = 0; i < 10; i++)
            {
                var lane = new Rectangle
                {
                    Width = 10,
                    Height = 80,
                    Fill = Brushes.Yellow
                };
                System.Windows.Controls.Canvas.SetLeft(lane, GameCanvas.ActualWidth / 2 - 5);
                System.Windows.Controls.Canvas.SetTop(lane, i * 100);
                GameCanvas.Children.Add(lane);
            }
        }

        private void InitializeCars()
        {
            // Player car
            playerCar = new Rectangle
            {
                Width = CarWidth,
                Height = CarHeight,
                Fill = Brushes.Blue,
                Stroke = Brushes.Navy,
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };
            System.Windows.Controls.Canvas.SetLeft(playerCar, playerX);
            System.Windows.Controls.Canvas.SetTop(playerCar, playerY);
            GameCanvas.Children.Add(playerCar);
            
            // Opponent car
            opponentCar = new Rectangle
            {
                Width = CarWidth,
                Height = CarHeight,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };
            System.Windows.Controls.Canvas.SetLeft(opponentCar, opponentX);
            System.Windows.Controls.Canvas.SetTop(opponentCar, opponentY);
            GameCanvas.Children.Add(opponentCar);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameActive) return;
            
            // Move player 1 car
            if (leftPressed && playerX > 150)
            {
                playerX -= 3;
            }
            if (rightPressed && playerX < 400)
            {
                playerX += 3;
            }
            
            // Update player 1 forward position
            playerPosition += carSpeed;
            
            // Update player 1 display
            System.Windows.Controls.Canvas.SetLeft(playerCar, playerX);
            PlayerPositionText.Text = $"{(int)playerPosition}m";
            
            if (isLocalMultiplayer)
            {
                // Move player 2 car
                if (arrowLeftPressed && opponentX > 150)
                {
                    opponentX -= 3;
                }
                if (arrowRightPressed && opponentX < 400)
                {
                    opponentX += 3;
                }
                
                // Update player 2 forward position
                opponentPosition += carSpeed;
                
                // Update player 2 display
                System.Windows.Controls.Canvas.SetLeft(opponentCar, opponentX);
                
                // Check for wins in local multiplayer
                if (playerPosition >= FinishLine && opponentPosition < FinishLine)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "Player 1 wins! 🎉";
                    MessageBox.Show("Player 1 finished first!", "Victory!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else if (opponentPosition >= FinishLine && playerPosition < FinishLine)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "Player 2 wins! 🎉";
                    MessageBox.Show("Player 2 finished first!", "Victory!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else if (playerPosition >= FinishLine && opponentPosition >= FinishLine)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "It's a tie!";
                    MessageBox.Show("Both players finished at the same time!", "Tie!", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                // Check for win in network mode
                if (playerPosition >= FinishLine)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "You win! 🎉";
                    ScoreManager.Instance.RecordWin();
                    MessageBox.Show($"Congratulations! You finished first!\nYour time: {playerPosition / carSpeed / 60:F2}s", 
                        "Victory!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                // Send position update to opponent
                SendPositionUpdate();
            }
            
            // Animate track movement
            AnimateTrack();
        }

        private void AnimateTrack()
        {
            // Move lane markers to create illusion of movement
            foreach (var child in GameCanvas.Children)
            {
                if (child is Rectangle rect && rect.Fill == Brushes.Yellow)
                {
                    double top = System.Windows.Controls.Canvas.GetTop(rect);
                    top += carSpeed;
                    if (top > GameCanvas.ActualHeight)
                    {
                        top = -80;
                    }
                    System.Windows.Controls.Canvas.SetTop(rect, top);
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameActive) return;
            
            if (isLocalMultiplayer)
            {
                // Player 1 controls (A/D)
                if (e.Key == Key.A) leftPressed = true;
                if (e.Key == Key.D) rightPressed = true;
                
                // Player 2 controls (Arrow Keys)
                if (e.Key == Key.Left) arrowLeftPressed = true;
                if (e.Key == Key.Right) arrowRightPressed = true;
            }
            else
            {
                // Network multiplayer - use arrow keys for single player
                if (e.Key == Key.Left) leftPressed = true;
                if (e.Key == Key.Right) rightPressed = true;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (isLocalMultiplayer)
            {
                // Player 1 controls (A/D)
                if (e.Key == Key.A) leftPressed = false;
                if (e.Key == Key.D) rightPressed = false;
                
                // Player 2 controls (Arrow Keys)
                if (e.Key == Key.Left) arrowLeftPressed = false;
                if (e.Key == Key.Right) arrowRightPressed = false;
            }
            else
            {
                // Network multiplayer - use arrow keys for single player
                if (e.Key == Key.Left) leftPressed = false;
                if (e.Key == Key.Right) rightPressed = false;
            }
        }

        private void SendPositionUpdate()
        {
            var update = new RaceUpdate 
            { 
                Type = "position",
                X = playerX,
                Position = playerPosition
            };
            SendMessage(update);
        }

        private void ProcessOpponentUpdate(RaceUpdate update)
        {
            if (update.Type == "position")
            {
                opponentX = update.X;
                opponentPosition = update.Position;
                
                System.Windows.Controls.Canvas.SetLeft(opponentCar, opponentX);
                
                // Check if opponent finished
                if (opponentPosition >= FinishLine && gameActive)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "Opponent wins!";
                    ScoreManager.Instance.RecordLoss();
                    MessageBox.Show("Opponent finished first!", "Game Over", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, NetworkManager.Instance.GetGamePort());
                listener.Start();
                
                client = await listener.AcceptTcpClientAsync();
                stream = client.GetStream();
                
                StatusText.Text = "Opponent connected! Race started!";
                gameActive = true;
                gameTimer.Start();
                
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
                
                StatusText.Text = "Connected! Race started!";
                gameActive = true;
                gameTimer.Start();
                
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
                        var update = JsonSerializer.Deserialize<RaceUpdate>(message);
                        
                        if (update != null)
                        {
                            Dispatcher.Invoke(() => ProcessOpponentUpdate(update));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => 
                {
                    if (gameActive)
                    {
                        MessageBox.Show($"Connection lost: {ex.Message}", "Connection Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        Close();
                    }
                });
            }
        }

        private async void SendMessage(RaceUpdate update)
        {
            try
            {
                if (stream != null)
                {
                    string json = JsonSerializer.Serialize(update);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch { }
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            gameTimer.Stop();
            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            gameTimer.Stop();
            try
            {
                stream?.Close();
                client?.Close();
                listener?.Stop();
            }
            catch { }
        }
    }

    public class RaceUpdate
    {
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Position { get; set; }
    }
}
