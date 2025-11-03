using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GameBox.Utils;

namespace GameBox.Games
{
    public partial class TankBattleGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool gameActive = false;
        
        private DispatcherTimer gameTimer = new DispatcherTimer();
        
        // Player tank
        private Rectangle playerTank;
        private double playerX = 100;
        private double playerY = 100;
        private double playerAngle = 0;
        private int playerHealth = 100;
        private int playerScore = 0;
        
        // Opponent tank
        private Rectangle opponentTank;
        private double opponentX = 700;
        private double opponentY = 500;
        private double opponentAngle = 180;
        private int opponentHealth = 100;
        
        // Movement keys
        private bool wPressed, aPressed, sPressed, dPressed;
        
        private const double TankSpeed = 3;
        private const double TankSize = 30;

        public TankBattleGame()
        {
            InitializeComponent();
            
            gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            gameTimer.Tick += GameLoop;
            
            this.Loaded += (s, e) => GameCanvas.Focus();
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
            
            if (isHost)
            {
                playerX = 100;
                playerY = 100;
                opponentX = 700;
                opponentY = 500;
            }
            else
            {
                playerX = 700;
                playerY = 500;
                opponentX = 100;
                opponentY = 100;
            }
            
            InitializeTanks();
            
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

        private void InitializeTanks()
        {
            // Player tank
            playerTank = new Rectangle
            {
                Width = TankSize,
                Height = TankSize,
                Fill = Brushes.Blue,
                Stroke = Brushes.Navy,
                StrokeThickness = 2
            };
            Canvas.SetLeft(playerTank, playerX);
            Canvas.SetTop(playerTank, playerY);
            GameCanvas.Children.Add(playerTank);
            
            // Opponent tank
            opponentTank = new Rectangle
            {
                Width = TankSize,
                Height = TankSize,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };
            Canvas.SetLeft(opponentTank, opponentX);
            Canvas.SetTop(opponentTank, opponentY);
            GameCanvas.Children.Add(opponentTank);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameActive) return;
            
            // Update player position based on input
            if (wPressed) playerY -= TankSpeed;
            if (sPressed) playerY += TankSpeed;
            if (aPressed) playerX -= TankSpeed;
            if (dPressed) playerX += TankSpeed;
            
            // Clamp to canvas bounds
            playerX = Math.Max(0, Math.Min(GameCanvas.ActualWidth - TankSize, playerX));
            playerY = Math.Max(0, Math.Min(GameCanvas.ActualHeight - TankSize, playerY));
            
            // Update tank position
            Canvas.SetLeft(playerTank, playerX);
            Canvas.SetTop(playerTank, playerY);
            
            // Send position update to opponent
            SendPositionUpdate();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameActive) return;
            
            switch (e.Key)
            {
                case Key.W: wPressed = true; break;
                case Key.A: aPressed = true; break;
                case Key.S: sPressed = true; break;
                case Key.D: dPressed = true; break;
                case Key.Space: FireBullet(); break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W: wPressed = false; break;
                case Key.A: aPressed = false; break;
                case Key.S: sPressed = false; break;
                case Key.D: dPressed = false; break;
            }
        }

        private void FireBullet()
        {
            // Create bullet
            var bullet = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Yellow
            };
            
            Canvas.SetLeft(bullet, playerX + TankSize / 2);
            Canvas.SetTop(bullet, playerY + TankSize / 2);
            GameCanvas.Children.Add(bullet);
            
            // Animate bullet towards opponent
            var bulletTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            bulletTimer.Tick += (s, e) =>
            {
                double dx = opponentX - Canvas.GetLeft(bullet);
                double dy = opponentY - Canvas.GetTop(bullet);
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                if (distance < 10)
                {
                    // Hit opponent
                    bulletTimer.Stop();
                    GameCanvas.Children.Remove(bullet);
                    
                    SendBulletHit();
                    
                    playerScore += 10;
                    PlayerScoreText.Text = playerScore.ToString();
                }
                else if (Canvas.GetLeft(bullet) < 0 || Canvas.GetLeft(bullet) > GameCanvas.ActualWidth ||
                         Canvas.GetTop(bullet) < 0 || Canvas.GetTop(bullet) > GameCanvas.ActualHeight)
                {
                    // Out of bounds
                    bulletTimer.Stop();
                    GameCanvas.Children.Remove(bullet);
                }
                else
                {
                    // Move bullet
                    Canvas.SetLeft(bullet, Canvas.GetLeft(bullet) + (dx / distance) * 10);
                    Canvas.SetTop(bullet, Canvas.GetTop(bullet) + (dy / distance) * 10);
                }
            };
            bulletTimer.Start();
        }

        private void SendPositionUpdate()
        {
            var update = new TankUpdate 
            { 
                Type = "position",
                X = playerX, 
                Y = playerY, 
                Angle = playerAngle 
            };
            SendMessage(update);
        }

        private void SendBulletHit()
        {
            var update = new TankUpdate 
            { 
                Type = "hit"
            };
            SendMessage(update);
        }

        private void ProcessOpponentUpdate(TankUpdate update)
        {
            if (update.Type == "position")
            {
                opponentX = update.X;
                opponentY = update.Y;
                opponentAngle = update.Angle;
                
                Canvas.SetLeft(opponentTank, opponentX);
                Canvas.SetTop(opponentTank, opponentY);
            }
            else if (update.Type == "hit")
            {
                playerHealth -= 20;
                PlayerHealthBar.Value = playerHealth;
                
                if (playerHealth <= 0)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "You were destroyed!";
                    ScoreManager.Instance.RecordLoss();
                    MessageBox.Show("Opponent wins!", "Game Over", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (update.Type == "opponent_hit")
            {
                opponentHealth -= 20;
                OpponentHealthBar.Value = opponentHealth;
                
                if (opponentHealth <= 0)
                {
                    gameActive = false;
                    gameTimer.Stop();
                    StatusText.Text = "You win! 🎉";
                    ScoreManager.Instance.RecordWin();
                    MessageBox.Show("You destroyed the opponent tank!", "Victory!", 
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
                
                StatusText.Text = "Opponent connected! Battle started!";
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
                
                StatusText.Text = "Connected! Battle started!";
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
                        var update = JsonSerializer.Deserialize<TankUpdate>(message);
                        
                        if (update != null)
                        {
                            // Convert hit to opponent_hit for the receiver
                            if (update.Type == "hit")
                            {
                                update.Type = "opponent_hit";
                            }
                            
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

        private async void SendMessage(TankUpdate update)
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

    public class TankUpdate
    {
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
    }
}
