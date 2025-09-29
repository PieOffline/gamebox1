using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameBox.Games
{
    public partial class BreakoutGame : Window
    {
        private Rectangle paddle = null!;
        private Ellipse ball = null!;
        private List<Rectangle> bricks = new List<Rectangle>();
        private double ballSpeedX = 4;
        private double ballSpeedY = -4;
        private double paddleSpeed = 12;
        private bool leftPressed = false;
        private bool rightPressed = false;
        private DispatcherTimer gameTimer = null!;
        private int score = 0;
        private int lives = 3;
        private bool gameActive = false;
        private Random random = new Random();

        // Game constants
        private const double PaddleWidth = 100;
        private const double PaddleHeight = 15;
        private const double BallSize = 12;
        private const double BrickWidth = 75;
        private const double BrickHeight = 25;
        private const int BricksPerRow = 8;
        private const int BrickRows = 6;

        public BreakoutGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            CreatePaddle();
            CreateBall();
            CreateBricks();
            ResetBall();
            
            // Setup game timer
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            gameTimer.Tick += GameLoop;
            
            gameActive = true;
            UpdateUI();
            gameTimer.Start();
        }

        private void CreatePaddle()
        {
            paddle = new Rectangle
            {
                Width = PaddleWidth,
                Height = PaddleHeight,
                Fill = Brushes.White,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(paddle, (GameCanvas.Width - PaddleWidth) / 2);
            Canvas.SetTop(paddle, GameCanvas.Height - 40);
            GameCanvas.Children.Add(paddle);
        }

        private void CreateBall()
        {
            ball = new Ellipse
            {
                Width = BallSize,
                Height = BallSize,
                Fill = Brushes.Yellow,
                Stroke = Brushes.Orange,
                StrokeThickness = 1
            };
            
            GameCanvas.Children.Add(ball);
        }

        private void CreateBricks()
        {
            bricks.Clear();
            
            // Create colorful rows of bricks
            Brush[] rowColors = { Brushes.Red, Brushes.Orange, Brushes.Yellow, 
                                 Brushes.Green, Brushes.Blue, Brushes.Purple };
            
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BricksPerRow; col++)
                {
                    var brick = new Rectangle
                    {
                        Width = BrickWidth,
                        Height = BrickHeight,
                        Fill = rowColors[row],
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };
                    
                    double x = col * (BrickWidth + 5) + 10;
                    double y = row * (BrickHeight + 3) + 50;
                    
                    Canvas.SetLeft(brick, x);
                    Canvas.SetTop(brick, y);
                    
                    GameCanvas.Children.Add(brick);
                    bricks.Add(brick);
                }
            }
        }

        private void ResetBall()
        {
            Canvas.SetLeft(ball, GameCanvas.Width / 2 - BallSize / 2);
            Canvas.SetTop(ball, GameCanvas.Height / 2);
            
            // Random starting direction
            ballSpeedX = random.NextDouble() > 0.5 ? 4 : -4;
            ballSpeedY = -4;
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameActive) return;
            
            // Move paddle
            double paddleX = Canvas.GetLeft(paddle);
            if (leftPressed && paddleX > 0)
            {
                Canvas.SetLeft(paddle, paddleX - paddleSpeed);
            }
            if (rightPressed && paddleX < GameCanvas.Width - PaddleWidth)
            {
                Canvas.SetLeft(paddle, paddleX + paddleSpeed);
            }
            
            // Move ball
            double ballX = Canvas.GetLeft(ball) + ballSpeedX;
            double ballY = Canvas.GetTop(ball) + ballSpeedY;
            
            // Ball collision with walls
            if (ballX <= 0 || ballX >= GameCanvas.Width - BallSize)
            {
                ballSpeedX = -ballSpeedX;
                ballX = Math.Max(0, Math.Min(ballX, GameCanvas.Width - BallSize));
            }
            
            if (ballY <= 0)
            {
                ballSpeedY = -ballSpeedY;
                ballY = 0;
            }
            
            // Ball collision with paddle
            paddleX = Canvas.GetLeft(paddle);
            double paddleY = Canvas.GetTop(paddle);
            
            if (ballY + BallSize >= paddleY && ballY <= paddleY + PaddleHeight &&
                ballX + BallSize >= paddleX && ballX <= paddleX + PaddleWidth)
            {
                ballSpeedY = -Math.Abs(ballSpeedY); // Always bounce up
                
                // Add spin based on where ball hits paddle
                double hitPosition = (ballX + BallSize / 2 - paddleX) / PaddleWidth;
                ballSpeedX = (hitPosition - 0.5) * 8; // Range from -4 to 4
            }
            
            // Ball collision with bricks
            CheckBrickCollisions(ballX, ballY);
            
            // Ball falls off bottom
            if (ballY > GameCanvas.Height)
            {
                lives--;
                if (lives <= 0)
                {
                    GameOver();
                }
                else
                {
                    ResetBall();
                    UpdateUI();
                }
                return;
            }
            
            Canvas.SetLeft(ball, ballX);
            Canvas.SetTop(ball, ballY);
            
            // Check win condition
            if (bricks.Count == 0)
            {
                GameWin();
            }
        }

        private void CheckBrickCollisions(double ballX, double ballY)
        {
            var ballRect = new Rect(ballX, ballY, BallSize, BallSize);
            
            for (int i = bricks.Count - 1; i >= 0; i--)
            {
                var brick = bricks[i];
                var brickX = Canvas.GetLeft(brick);
                var brickY = Canvas.GetTop(brick);
                var brickRect = new Rect(brickX, brickY, BrickWidth, BrickHeight);
                
                if (ballRect.IntersectsWith(brickRect))
                {
                    // Remove brick
                    GameCanvas.Children.Remove(brick);
                    bricks.RemoveAt(i);
                    
                    // Update score based on brick color/row
                    score += GetBrickScore(brick);
                    UpdateUI();
                    
                    // Determine bounce direction
                    double ballCenterX = ballX + BallSize / 2;
                    double ballCenterY = ballY + BallSize / 2;
                    double brickCenterX = brickX + BrickWidth / 2;
                    double brickCenterY = brickY + BrickHeight / 2;
                    
                    // Simple collision response
                    if (Math.Abs(ballCenterX - brickCenterX) > Math.Abs(ballCenterY - brickCenterY))
                    {
                        ballSpeedX = -ballSpeedX; // Hit from side
                    }
                    else
                    {
                        ballSpeedY = -ballSpeedY; // Hit from top/bottom
                    }
                    
                    break; // Only handle one collision per frame
                }
            }
        }

        private int GetBrickScore(Rectangle brick)
        {
            // Score based on brick color (top rows worth more)
            if (brick.Fill == Brushes.Red) return 60;
            if (brick.Fill == Brushes.Orange) return 50;
            if (brick.Fill == Brushes.Yellow) return 40;
            if (brick.Fill == Brushes.Green) return 30;
            if (brick.Fill == Brushes.Blue) return 20;
            if (brick.Fill == Brushes.Purple) return 10;
            return 10;
        }

        private void UpdateUI()
        {
            ScoreText.Text = $"Score: {score}";
            LivesText.Text = $"Lives: {lives}";
            BricksText.Text = $"Bricks: {bricks.Count}";
        }

        private void GameOver()
        {
            gameActive = false;
            gameTimer.Stop();
            
            StatusText.Text = "Game Over! Press 'New Game' to play again";
            
            MessageBox.Show($"Game Over!\n\nFinal Score: {score}\nYou cleared {BricksPerRow * BrickRows - bricks.Count} bricks!", 
                "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GameWin()
        {
            gameActive = false;
            gameTimer.Stop();
            
            StatusText.Text = "Congratulations! You cleared all bricks! 🎉";
            
            MessageBox.Show($"Congratulations! 🎉\n\nYou cleared all bricks!\nFinal Score: {score}", 
                "Victory!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    leftPressed = true;
                    break;
                case Key.Right:
                case Key.D:
                    rightPressed = true;
                    break;
                case Key.Space:
                    if (!gameActive && lives > 0)
                    {
                        ResetBall();
                        gameActive = true;
                        gameTimer.Start();
                    }
                    break;
                case Key.Escape:
                    this.Close();
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    leftPressed = false;
                    break;
                case Key.Right:
                case Key.D:
                    rightPressed = false;
                    break;
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            gameTimer?.Stop();
            
            // Clear game objects
            GameCanvas.Children.Clear();
            bricks.Clear();
            
            // Reset game state
            score = 0;
            lives = 3;
            ballSpeedX = 4;
            ballSpeedY = -4;
            
            StatusText.Text = "Use A/D or ←/→ to move paddle";
            
            // Reinitialize game
            InitializeGame();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            gameTimer?.Stop();
        }
    }
}