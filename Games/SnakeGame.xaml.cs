using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameBox.Games
{
    public partial class SnakeGame : Window
    {
        private const int GridSize = 20;
        private const int GameWidth = 600;
        private const int GameHeight = 600;
        
        private List<Point> snake = new List<Point>();
        private Point food;
        private string direction = "Right";
        private string nextDirection = "Right";
        private int score = 0;
        private DispatcherTimer gameTimer = null!;
        private Random random = new Random();
        private bool gameRunning = false;

        public SnakeGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Setup the canvas
            GameCanvas.Width = GameWidth;
            GameCanvas.Height = GameHeight;
            
            // Initialize snake in the center
            snake.Clear();
            snake.Add(new Point(10, 10));
            snake.Add(new Point(9, 10));
            snake.Add(new Point(8, 10));
            
            // Place initial food
            PlaceFood();
            
            // Setup timer
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(150);
            gameTimer.Tick += GameLoop;
            
            // Reset game state
            direction = "Right";
            nextDirection = "Right";
            score = 0;
            gameRunning = true;
            
            UpdateScore();
            DrawGame();
            gameTimer.Start();
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameRunning) return;
            
            // Update direction
            direction = nextDirection;
            
            // Move snake
            Point head = snake[0];
            Point newHead = direction switch
            {
                "Up" => new Point(head.X, head.Y - 1),
                "Down" => new Point(head.X, head.Y + 1),
                "Left" => new Point(head.X - 1, head.Y),
                "Right" => new Point(head.X + 1, head.Y),
                _ => head
            };
            
            // Check wall collision
            if (newHead.X < 0 || newHead.X >= GameWidth / GridSize ||
                newHead.Y < 0 || newHead.Y >= GameHeight / GridSize)
            {
                GameOver();
                return;
            }
            
            // Check self collision
            if (snake.Contains(newHead))
            {
                GameOver();
                return;
            }
            
            // Add new head
            snake.Insert(0, newHead);
            
            // Check food collision
            if (newHead.Equals(food))
            {
                score += 10;
                UpdateScore();
                PlaceFood();
                
                // Increase speed slightly
                if (gameTimer.Interval > TimeSpan.FromMilliseconds(80))
                {
                    gameTimer.Interval = TimeSpan.FromMilliseconds(gameTimer.Interval.TotalMilliseconds - 5);
                }
            }
            else
            {
                // Remove tail
                snake.RemoveAt(snake.Count - 1);
            }
            
            DrawGame();
        }

        private void PlaceFood()
        {
            Point newFood;
            do
            {
                newFood = new Point(
                    random.Next(0, GameWidth / GridSize),
                    random.Next(0, GameHeight / GridSize)
                );
            } while (snake.Contains(newFood));
            
            food = newFood;
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();
            
            // Draw snake
            for (int i = 0; i < snake.Count; i++)
            {
                Rectangle segment = new Rectangle
                {
                    Width = GridSize - 2,
                    Height = GridSize - 2,
                    Fill = i == 0 ? Brushes.DarkGreen : Brushes.LimeGreen,
                    Stroke = Brushes.DarkGreen,
                    StrokeThickness = 1
                };
                
                Canvas.SetLeft(segment, snake[i].X * GridSize + 1);
                Canvas.SetTop(segment, snake[i].Y * GridSize + 1);
                GameCanvas.Children.Add(segment);
            }
            
            // Draw food
            Ellipse foodShape = new Ellipse
            {
                Width = GridSize - 4,
                Height = GridSize - 4,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };
            
            Canvas.SetLeft(foodShape, food.X * GridSize + 2);
            Canvas.SetTop(foodShape, food.Y * GridSize + 2);
            GameCanvas.Children.Add(foodShape);
        }

        private void UpdateScore()
        {
            ScoreText.Text = $"Score: {score} | Length: {snake.Count}";
        }

        private void GameOver()
        {
            gameRunning = false;
            gameTimer.Stop();
            
            StatusText.Text = "Game Over! Press 'New Game' to play again";
            
            MessageBox.Show($"Game Over!\n\nFinal Score: {score}\nSnake Length: {snake.Count}", 
                "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameRunning) return;
            
            // Prevent reverse direction
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    if (direction != "Down") nextDirection = "Up";
                    break;
                case Key.Down:
                case Key.S:
                    if (direction != "Up") nextDirection = "Down";
                    break;
                case Key.Left:
                case Key.A:
                    if (direction != "Right") nextDirection = "Left";
                    break;
                case Key.Right:
                case Key.D:
                    if (direction != "Left") nextDirection = "Right";
                    break;
                case Key.Space:
                    // Pause/unpause
                    if (gameTimer.IsEnabled)
                    {
                        gameTimer.Stop();
                        StatusText.Text = "Paused - Press SPACE to continue";
                    }
                    else
                    {
                        gameTimer.Start();
                        StatusText.Text = "Use arrow keys or WASD to control the snake";
                    }
                    break;
                case Key.Escape:
                    this.Close();
                    break;
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            gameTimer?.Stop();
            StatusText.Text = "Use arrow keys or WASD to control the snake";
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