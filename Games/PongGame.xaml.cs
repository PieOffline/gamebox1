using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameBox.Games
{
    public partial class PongGame : Window
    {
        private Rectangle paddle = null!;
        private Ellipse ball = null!;
        private double ballSpeedX = 3;
        private double ballSpeedY = 3;
        private double paddleSpeed = 8;
        private bool upPressed = false;
        private bool downPressed = false;
        private DispatcherTimer gameTimer = null!;
        private int score = 0;

        public PongGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Create paddle
            paddle = new Rectangle
            {
                Width = 20,
                Height = 80,
                Fill = Brushes.White
            };
            Canvas.SetLeft(paddle, 20);
            Canvas.SetTop(paddle, (GameCanvas.Height - paddle.Height) / 2);
            GameCanvas.Children.Add(paddle);

            // Create ball
            ball = new Ellipse
            {
                Width = 15,
                Height = 15,
                Fill = Brushes.White
            };
            Canvas.SetLeft(ball, GameCanvas.Width / 2);
            Canvas.SetTop(ball, GameCanvas.Height / 2);
            GameCanvas.Children.Add(ball);

            // Setup game timer
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            // Update score display
            UpdateScore();
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            // Move paddle
            if (upPressed && Canvas.GetTop(paddle) > 0)
            {
                Canvas.SetTop(paddle, Canvas.GetTop(paddle) - paddleSpeed);
            }
            if (downPressed && Canvas.GetTop(paddle) < GameCanvas.Height - paddle.Height)
            {
                Canvas.SetTop(paddle, Canvas.GetTop(paddle) + paddleSpeed);
            }

            // Move ball
            double ballX = Canvas.GetLeft(ball) + ballSpeedX;
            double ballY = Canvas.GetTop(ball) + ballSpeedY;

            // Ball collision with top and bottom walls
            if (ballY <= 0 || ballY >= GameCanvas.Height - ball.Height)
            {
                ballSpeedY = -ballSpeedY;
            }

            // Ball collision with paddle
            if (ballX <= Canvas.GetLeft(paddle) + paddle.Width &&
                ballX >= Canvas.GetLeft(paddle) &&
                ballY + ball.Height >= Canvas.GetTop(paddle) &&
                ballY <= Canvas.GetTop(paddle) + paddle.Height)
            {
                ballSpeedX = -ballSpeedX;
                score += 10;
                UpdateScore();
                
                // Increase difficulty slightly
                if (Math.Abs(ballSpeedX) < 6)
                {
                    ballSpeedX *= 1.05;
                    ballSpeedY *= 1.05;
                }
            }

            // Ball goes off left side - reset
            if (ballX < 0)
            {
                ResetBall();
            }

            // Ball goes off right side - point scored
            if (ballX > GameCanvas.Width)
            {
                score += 5;
                UpdateScore();
                ResetBall();
            }

            Canvas.SetLeft(ball, ballX);
            Canvas.SetTop(ball, ballY);
        }

        private void ResetBall()
        {
            Canvas.SetLeft(ball, GameCanvas.Width / 2);
            Canvas.SetTop(ball, GameCanvas.Height / 2);
            ballSpeedX = 3 * (ballSpeedX > 0 ? -1 : 1); // Reverse direction
            ballSpeedY = 3 * (new Random().NextDouble() > 0.5 ? 1 : -1);
        }

        private void UpdateScore()
        {
            ScoreText.Text = $"Score: {score}";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    upPressed = true;
                    break;
                case Key.Down:
                case Key.S:
                    downPressed = true;
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
                case Key.Up:
                case Key.W:
                    upPressed = false;
                    break;
                case Key.Down:
                case Key.S:
                    downPressed = false;
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            gameTimer?.Stop();
        }
    }
}