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
    public partial class AsteroidsGame : Window
    {
        private DispatcherTimer gameTimer = null!;
        private Random random = new Random();
        
        // Game state
        private int score = 0;
        private int lives = 3;
        private int level = 1;
        private bool gameActive = false;
        private bool paused = false;
        
        // Ship properties
        private Polygon ship = null!;
        private double shipX = 400;
        private double shipY = 300;
        private double shipAngle = 0;
        private double shipVelocityX = 0;
        private double shipVelocityY = 0;
        private bool shipVisible = true;
        private int invulnerabilityTime = 0;
        
        // Input state
        private bool leftPressed = false;
        private bool rightPressed = false;
        private bool thrustPressed = false;
        private int shootCooldown = 0;
        
        // Game objects
        private List<Bullet> bullets = new List<Bullet>();
        private List<Asteroid> asteroids = new List<Asteroid>();
        private List<Explosion> explosions = new List<Explosion>();
        
        // Constants
        private const double ShipTurnSpeed = 5;
        private const double ShipThrust = 0.5;
        private const double MaxVelocity = 8;
        private const double Friction = 0.98;
        private const int BulletSpeed = 10;
        private const int BulletLifetime = 60;
        private const int MaxBullets = 5;

        public AsteroidsGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            CreateShip();
            
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            gameTimer.Tick += GameLoop;
            
            StartNewGame();
        }

        private void CreateShip()
        {
            ship = new Polygon
            {
                Fill = Brushes.White,
                Stroke = Brushes.Cyan,
                StrokeThickness = 2,
                Points = new PointCollection
                {
                    new Point(0, -15),   // Top point
                    new Point(-10, 10),  // Bottom left
                    new Point(0, 5),     // Bottom center
                    new Point(10, 10)    // Bottom right
                }
            };
            
            Canvas.SetLeft(ship, shipX);
            Canvas.SetTop(ship, shipY);
            GameCanvas.Children.Add(ship);
        }

        private void StartNewGame()
        {
            score = 0;
            lives = 3;
            level = 1;
            gameActive = true;
            paused = false;
            
            // Reset ship
            shipX = GameCanvas.ActualWidth / 2;
            shipY = GameCanvas.ActualHeight / 2;
            shipAngle = 0;
            shipVelocityX = 0;
            shipVelocityY = 0;
            shipVisible = true;
            invulnerabilityTime = 0;
            
            // Clear game objects
            ClearAllObjects();
            
            // Create initial asteroids
            CreateAsteroids();
            
            UpdateUI();
            StatusText.Text = "Use WASD to move, Space to shoot, Q/E to rotate!";
            
            gameTimer.Start();
        }

        private void CreateAsteroids()
        {
            asteroids.Clear();
            int asteroidCount = 4 + level; // More asteroids each level
            
            for (int i = 0; i < asteroidCount; i++)
            {
                CreateAsteroid(AsteroidSize.Large, 0, 0, true);
            }
        }

        private void CreateAsteroid(AsteroidSize size, double x, double y, bool randomPosition = false)
        {
            var asteroid = new Asteroid();
            
            if (randomPosition)
            {
                // Place asteroid away from ship
                do
                {
                    asteroid.X = random.NextDouble() * GameCanvas.ActualWidth;
                    asteroid.Y = random.NextDouble() * GameCanvas.ActualHeight;
                }
                while (GetDistance(asteroid.X, asteroid.Y, shipX, shipY) < 100);
            }
            else
            {
                asteroid.X = x;
                asteroid.Y = y;
            }
            
            asteroid.Size = size;
            asteroid.VelocityX = (random.NextDouble() - 0.5) * 4;
            asteroid.VelocityY = (random.NextDouble() - 0.5) * 4;
            asteroid.RotationSpeed = (random.NextDouble() - 0.5) * 10;
            
            // Create visual representation
            var polygon = new Polygon
            {
                Fill = Brushes.Transparent,
                Stroke = GetAsteroidColor(size),
                StrokeThickness = 2,
                Points = GenerateAsteroidShape(size)
            };
            
            asteroid.Shape = polygon;
            Canvas.SetLeft(polygon, asteroid.X);
            Canvas.SetTop(polygon, asteroid.Y);
            GameCanvas.Children.Add(polygon);
            
            asteroids.Add(asteroid);
        }

        private PointCollection GenerateAsteroidShape(AsteroidSize size)
        {
            var points = new PointCollection();
            double radius = size switch
            {
                AsteroidSize.Large => 30,
                AsteroidSize.Medium => 20,
                AsteroidSize.Small => 10,
                _ => 20
            };
            
            int vertices = 8;
            for (int i = 0; i < vertices; i++)
            {
                double angle = (2 * Math.PI * i) / vertices;
                double variation = 0.7 + random.NextDouble() * 0.6; // Random size variation
                double x = Math.Cos(angle) * radius * variation;
                double y = Math.Sin(angle) * radius * variation;
                points.Add(new Point(x, y));
            }
            
            return points;
        }

        private Brush GetAsteroidColor(AsteroidSize size)
        {
            return size switch
            {
                AsteroidSize.Large => Brushes.Brown,
                AsteroidSize.Medium => Brushes.Orange,
                AsteroidSize.Small => Brushes.Yellow,
                _ => Brushes.Gray
            };
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameActive || paused) return;
            
            UpdateShip();
            UpdateBullets();
            UpdateAsteroids();
            UpdateExplosions();
            CheckCollisions();
            
            if (invulnerabilityTime > 0)
                invulnerabilityTime--;
            
            if (shootCooldown > 0)
                shootCooldown--;
            
            // Check win condition
            if (asteroids.Count == 0)
            {
                NextLevel();
            }
        }

        private void UpdateShip()
        {
            // Handle rotation
            if (leftPressed)
                shipAngle -= ShipTurnSpeed;
            if (rightPressed)
                shipAngle += ShipTurnSpeed;
            
            // Handle thrust
            if (thrustPressed)
            {
                double radians = Math.PI * shipAngle / 180;
                shipVelocityX += Math.Sin(radians) * ShipThrust;
                shipVelocityY -= Math.Cos(radians) * ShipThrust;
                
                // Limit velocity
                double speed = Math.Sqrt(shipVelocityX * shipVelocityX + shipVelocityY * shipVelocityY);
                if (speed > MaxVelocity)
                {
                    shipVelocityX = (shipVelocityX / speed) * MaxVelocity;
                    shipVelocityY = (shipVelocityY / speed) * MaxVelocity;
                }
            }
            
            // Apply friction
            shipVelocityX *= Friction;
            shipVelocityY *= Friction;
            
            // Update position
            shipX += shipVelocityX;
            shipY += shipVelocityY;
            
            // Wrap around screen
            if (shipX < 0) shipX = GameCanvas.ActualWidth;
            if (shipX > GameCanvas.ActualWidth) shipX = 0;
            if (shipY < 0) shipY = GameCanvas.ActualHeight;
            if (shipY > GameCanvas.ActualHeight) shipY = 0;
            
            // Update visual
            ship.RenderTransform = new RotateTransform(shipAngle, 0, 0);
            Canvas.SetLeft(ship, shipX);
            Canvas.SetTop(ship, shipY);
            
            // Handle invulnerability blinking
            if (invulnerabilityTime > 0)
            {
                shipVisible = !shipVisible;
                ship.Visibility = shipVisible ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                ship.Visibility = Visibility.Visible;
            }
        }

        private void UpdateBullets()
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var bullet = bullets[i];
                bullet.X += bullet.VelocityX;
                bullet.Y += bullet.VelocityY;
                bullet.Lifetime--;
                
                // Wrap around screen
                if (bullet.X < 0) bullet.X = GameCanvas.ActualWidth;
                if (bullet.X > GameCanvas.ActualWidth) bullet.X = 0;
                if (bullet.Y < 0) bullet.Y = GameCanvas.ActualHeight;
                if (bullet.Y > GameCanvas.ActualHeight) bullet.Y = 0;
                
                Canvas.SetLeft(bullet.Shape, bullet.X);
                Canvas.SetTop(bullet.Shape, bullet.Y);
                
                if (bullet.Lifetime <= 0)
                {
                    GameCanvas.Children.Remove(bullet.Shape);
                    bullets.RemoveAt(i);
                }
            }
        }

        private void UpdateAsteroids()
        {
            foreach (var asteroid in asteroids)
            {
                asteroid.X += asteroid.VelocityX;
                asteroid.Y += asteroid.VelocityY;
                asteroid.Rotation += asteroid.RotationSpeed;
                
                // Wrap around screen
                if (asteroid.X < 0) asteroid.X = GameCanvas.ActualWidth;
                if (asteroid.X > GameCanvas.ActualWidth) asteroid.X = 0;
                if (asteroid.Y < 0) asteroid.Y = GameCanvas.ActualHeight;
                if (asteroid.Y > GameCanvas.ActualHeight) asteroid.Y = 0;
                
                asteroid.Shape.RenderTransform = new RotateTransform(asteroid.Rotation, 0, 0);
                Canvas.SetLeft(asteroid.Shape, asteroid.X);
                Canvas.SetTop(asteroid.Shape, asteroid.Y);
            }
        }

        private void UpdateExplosions()
        {
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                var explosion = explosions[i];
                explosion.Lifetime--;
                
                if (explosion.Lifetime <= 0)
                {
                    GameCanvas.Children.Remove(explosion.Shape);
                    explosions.RemoveAt(i);
                }
                else
                {
                    // Animate explosion
                    var scale = (double)(20 - explosion.Lifetime) / 20;
                    explosion.Shape.RenderTransform = new ScaleTransform(scale, scale);
                }
            }
        }

        private void CheckCollisions()
        {
            // Bullet-asteroid collisions
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                var bullet = bullets[b];
                for (int a = asteroids.Count - 1; a >= 0; a--)
                {
                    var asteroid = asteroids[a];
                    if (GetDistance(bullet.X, bullet.Y, asteroid.X, asteroid.Y) < GetAsteroidRadius(asteroid.Size))
                    {
                        // Hit!
                        CreateExplosion(asteroid.X, asteroid.Y);
                        score += GetAsteroidScore(asteroid.Size);
                        
                        // Split asteroid
                        SplitAsteroid(asteroid);
                        
                        // Remove bullet and asteroid
                        GameCanvas.Children.Remove(bullet.Shape);
                        bullets.RemoveAt(b);
                        GameCanvas.Children.Remove(asteroid.Shape);
                        asteroids.RemoveAt(a);
                        
                        UpdateUI();
                        break;
                    }
                }
            }
            
            // Ship-asteroid collisions
            if (invulnerabilityTime <= 0)
            {
                foreach (var asteroid in asteroids)
                {
                    if (GetDistance(shipX, shipY, asteroid.X, asteroid.Y) < GetAsteroidRadius(asteroid.Size) + 10)
                    {
                        // Ship hit!
                        CreateExplosion(shipX, shipY);
                        lives--;
                        invulnerabilityTime = 120; // 2 seconds at 60 FPS
                        
                        if (lives <= 0)
                        {
                            GameOver();
                        }
                        else
                        {
                            UpdateUI();
                        }
                        break;
                    }
                }
            }
        }

        private void SplitAsteroid(Asteroid asteroid)
        {
            if (asteroid.Size == AsteroidSize.Large)
            {
                CreateAsteroid(AsteroidSize.Medium, asteroid.X, asteroid.Y);
                CreateAsteroid(AsteroidSize.Medium, asteroid.X, asteroid.Y);
            }
            else if (asteroid.Size == AsteroidSize.Medium)
            {
                CreateAsteroid(AsteroidSize.Small, asteroid.X, asteroid.Y);
                CreateAsteroid(AsteroidSize.Small, asteroid.X, asteroid.Y);
            }
        }

        private void CreateExplosion(double x, double y)
        {
            var explosion = new Explosion
            {
                X = x,
                Y = y,
                Lifetime = 20,
                Shape = new Ellipse
                {
                    Fill = Brushes.Orange,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Width = 20,
                    Height = 20
                }
            };
            
            Canvas.SetLeft(explosion.Shape, x - 10);
            Canvas.SetTop(explosion.Shape, y - 10);
            GameCanvas.Children.Add(explosion.Shape);
            explosions.Add(explosion);
        }

        private void Shoot()
        {
            if (shootCooldown > 0 || bullets.Count >= MaxBullets) return;
            
            var bullet = new Bullet
            {
                X = shipX,
                Y = shipY,
                VelocityX = Math.Sin(Math.PI * shipAngle / 180) * BulletSpeed,
                VelocityY = -Math.Cos(Math.PI * shipAngle / 180) * BulletSpeed,
                Lifetime = BulletLifetime,
                Shape = new Ellipse
                {
                    Fill = Brushes.White,
                    Width = 3,
                    Height = 3
                }
            };
            
            Canvas.SetLeft(bullet.Shape, bullet.X);
            Canvas.SetTop(bullet.Shape, bullet.Y);
            GameCanvas.Children.Add(bullet.Shape);
            bullets.Add(bullet);
            
            shootCooldown = 10; // Shoot cooldown
        }

        private double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }

        private double GetAsteroidRadius(AsteroidSize size)
        {
            return size switch
            {
                AsteroidSize.Large => 30,
                AsteroidSize.Medium => 20,
                AsteroidSize.Small => 10,
                _ => 20
            };
        }

        private int GetAsteroidScore(AsteroidSize size)
        {
            return size switch
            {
                AsteroidSize.Large => 20,
                AsteroidSize.Medium => 50,
                AsteroidSize.Small => 100,
                _ => 20
            };
        }

        private void ClearAllObjects()
        {
            foreach (var bullet in bullets)
                GameCanvas.Children.Remove(bullet.Shape);
            foreach (var asteroid in asteroids)
                GameCanvas.Children.Remove(asteroid.Shape);
            foreach (var explosion in explosions)
                GameCanvas.Children.Remove(explosion.Shape);
            
            bullets.Clear();
            asteroids.Clear();
            explosions.Clear();
        }

        private void NextLevel()
        {
            level++;
            StatusText.Text = $"Level {level}! Get ready for more asteroids!";
            
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                CreateAsteroids();
                StatusText.Text = "Use WASD to move, Space to shoot, Q/E to rotate!";
            };
            timer.Start();
        }

        private void GameOver()
        {
            gameActive = false;
            gameTimer.Stop();
            
            StatusText.Text = "Game Over! Press 'New Game' to play again.";
            
            MessageBox.Show($"Game Over!\n\nFinal Score: {score:N0}\nLevel Reached: {level}", 
                "Asteroids - Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateUI()
        {
            ScoreText.Text = $"Score: {score:N0}";
            LivesText.Text = $"Lives: {lives}";
            LevelText.Text = $"Level: {level}";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameActive) return;
            
            switch (e.Key)
            {
                case Key.A:
                case Key.Q:
                    leftPressed = true;
                    break;
                case Key.D:
                case Key.E:
                    rightPressed = true;
                    break;
                case Key.W:
                    thrustPressed = true;
                    break;
                case Key.Space:
                    Shoot();
                    break;
                case Key.P:
                    Pause_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                case Key.Q:
                    leftPressed = false;
                    break;
                case Key.D:
                case Key.E:
                    rightPressed = false;
                    break;
                case Key.W:
                    thrustPressed = false;
                    break;
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            gameTimer?.Stop();
            ClearAllObjects();
            StartNewGame();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive) return;
            
            paused = !paused;
            PauseButton.Content = paused ? "Resume" : "Pause";
            StatusText.Text = paused ? "Game Paused - Click Resume to continue" : "Use WASD to move, Space to shoot, Q/E to rotate!";
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            gameTimer?.Stop();
        }

        // Game object classes
        private class Bullet
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
            public int Lifetime { get; set; }
            public Shape Shape { get; set; } = null!;
        }

        private class Asteroid
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double VelocityX { get; set; }
            public double VelocityY { get; set; }
            public double Rotation { get; set; }
            public double RotationSpeed { get; set; }
            public AsteroidSize Size { get; set; }
            public Polygon Shape { get; set; } = null!;
        }

        private class Explosion
        {
            public double X { get; set; }
            public double Y { get; set; }
            public int Lifetime { get; set; }
            public Shape Shape { get; set; } = null!;
        }

        private enum AsteroidSize
        {
            Small, Medium, Large
        }
    }
}