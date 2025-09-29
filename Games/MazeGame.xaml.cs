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
    public partial class MazeGame : Window
    {
        private const int CellSize = 20;
        private int mazeWidth = 31; // Should be odd
        private int mazeHeight = 21; // Should be odd
        
        private int[,] maze = null!;
        private Rectangle[,] mazeRectangles = null!;
        private List<Rectangle> solutionPath = new List<Rectangle>();
        
        private int playerX = 1;
        private int playerY = 1;
        private int exitX = 29;
        private int exitY = 19;
        private Rectangle playerRect = null!;
        private Rectangle exitRect = null!;
        
        private int level = 1;
        private DateTime startTime;
        private TimeSpan bestTime = TimeSpan.MaxValue;
        private bool gameActive = false;
        private bool solutionVisible = false;
        
        private DispatcherTimer gameTimer = null!;
        private Random random = new Random();
        
        // Maze generation
        private Stack<(int x, int y)> mazeStack = new Stack<(int x, int y)>();
        private HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>();

        public MazeGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromSeconds(1);
            gameTimer.Tick += GameTimer_Tick;
            
            maze = new int[mazeHeight, mazeWidth];
            mazeRectangles = new Rectangle[mazeHeight, mazeWidth];
            
            CreateMazeDisplay();
            StartNewGame();
        }

        private void CreateMazeDisplay()
        {
            GameCanvas.Children.Clear();
            
            for (int row = 0; row < mazeHeight; row++)
            {
                for (int col = 0; col < mazeWidth; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 0.5
                    };
                    
                    Canvas.SetLeft(rect, col * CellSize);
                    Canvas.SetTop(rect, row * CellSize);
                    GameCanvas.Children.Add(rect);
                    mazeRectangles[row, col] = rect;
                }
            }
            
            // Create player
            playerRect = new Rectangle
            {
                Width = CellSize - 4,
                Height = CellSize - 4,
                Fill = Brushes.Blue,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 2
            };
            GameCanvas.Children.Add(playerRect);
            
            // Create exit
            exitRect = new Rectangle
            {
                Width = CellSize - 2,
                Height = CellSize - 2,
                Fill = Brushes.Gold,
                Stroke = Brushes.Orange,
                StrokeThickness = 2
            };
            GameCanvas.Children.Add(exitRect);
        }

        private void StartNewGame()
        {
            GenerateMaze();
            PlacePlayerAndExit();
            UpdateDisplay();
            
            startTime = DateTime.Now;
            gameActive = true;
            solutionVisible = false;
            
            gameTimer.Start();
            UpdateUI();
            
            StatusText.Text = "Navigate to the golden exit! Use arrow keys or WASD.";
        }

        private void GenerateMaze()
        {
            // Initialize maze - all walls
            for (int row = 0; row < mazeHeight; row++)
            {
                for (int col = 0; col < mazeWidth; col++)
                {
                    maze[row, col] = 1; // Wall
                }
            }
            
            // Clear stack and visited set
            mazeStack.Clear();
            visited.Clear();
            
            // Start generation from (1, 1)
            int startX = 1;
            int startY = 1;
            maze[startY, startX] = 0; // Path
            visited.Add((startX, startY));
            mazeStack.Push((startX, startY));
            
            // Generate maze using recursive backtracking
            while (mazeStack.Count > 0)
            {
                var (currentX, currentY) = mazeStack.Peek();
                var neighbors = GetUnvisitedNeighbors(currentX, currentY);
                
                if (neighbors.Count > 0)
                {
                    // Choose random neighbor
                    var (nextX, nextY) = neighbors[random.Next(neighbors.Count)];
                    
                    // Remove wall between current cell and chosen neighbor
                    int wallX = currentX + (nextX - currentX) / 2;
                    int wallY = currentY + (nextY - currentY) / 2;
                    maze[wallY, wallX] = 0; // Path
                    maze[nextY, nextX] = 0; // Path
                    
                    visited.Add((nextX, nextY));
                    mazeStack.Push((nextX, nextY));
                }
                else
                {
                    mazeStack.Pop();
                }
            }
            
            // Ensure exit is accessible
            maze[exitY, exitX] = 0;
            if (exitX > 0) maze[exitY, exitX - 1] = 0;
        }

        private List<(int x, int y)> GetUnvisitedNeighbors(int x, int y)
        {
            var neighbors = new List<(int x, int y)>();
            
            // Check all four directions (2 cells away to ensure walls between)
            var directions = new[] { (0, -2), (2, 0), (0, 2), (-2, 0) };
            
            foreach (var (dx, dy) in directions)
            {
                int newX = x + dx;
                int newY = y + dy;
                
                if (newX >= 1 && newX < mazeWidth - 1 && 
                    newY >= 1 && newY < mazeHeight - 1 && 
                    !visited.Contains((newX, newY)))
                {
                    neighbors.Add((newX, newY));
                }
            }
            
            return neighbors;
        }

        private void PlacePlayerAndExit()
        {
            // Player starts at (1, 1)
            playerX = 1;
            playerY = 1;
            
            // Exit is at bottom right area
            exitX = mazeWidth - 2;
            exitY = mazeHeight - 2;
            
            // Ensure exit position is valid
            maze[exitY, exitX] = 0;
        }

        private void UpdateDisplay()
        {
            // Update maze display
            for (int row = 0; row < mazeHeight; row++)
            {
                for (int col = 0; col < mazeWidth; col++)
                {
                    var rect = mazeRectangles[row, col];
                    
                    if (maze[row, col] == 1) // Wall
                    {
                        rect.Fill = GetWallColor();
                    }
                    else // Path
                    {
                        rect.Fill = Brushes.LightGray;
                    }
                }
            }
            
            // Update player position
            Canvas.SetLeft(playerRect, playerX * CellSize + 2);
            Canvas.SetTop(playerRect, playerY * CellSize + 2);
            
            // Update exit position
            Canvas.SetLeft(exitRect, exitX * CellSize + 1);
            Canvas.SetTop(exitRect, exitY * CellSize + 1);
        }

        private Brush GetWallColor()
        {
            // Colorful walls based on level
            return level switch
            {
                1 => Brushes.DarkBlue,
                2 => Brushes.DarkRed,
                3 => Brushes.DarkGreen,
                4 => Brushes.Purple,
                5 => Brushes.Brown,
                _ => Brushes.Black
            };
        }

        private void MovePlayer(int deltaX, int deltaY)
        {
            if (!gameActive) return;
            
            int newX = playerX + deltaX;
            int newY = playerY + deltaY;
            
            // Check bounds and walls
            if (newX >= 0 && newX < mazeWidth && 
                newY >= 0 && newY < mazeHeight && 
                maze[newY, newX] == 0)
            {
                playerX = newX;
                playerY = newY;
                
                UpdateDisplay();
                
                // Check if player reached exit
                if (playerX == exitX && playerY == exitY)
                {
                    WinLevel();
                }
            }
        }

        private void WinLevel()
        {
            gameActive = false;
            gameTimer.Stop();
            
            var completionTime = DateTime.Now - startTime;
            
            // Update best time
            if (completionTime < bestTime)
            {
                bestTime = completionTime;
            }
            
            string message = $"Level {level} Complete! 🎉\n\nTime: {completionTime:mm\\:ss}";
            if (completionTime == bestTime)
            {
                message += "\n🌟 New Best Time!";
            }
            
            StatusText.Text = "Level complete! Starting next level...";
            
            MessageBox.Show(message, "Maze Runner - Level Complete", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Start next level
            level++;
            if (level > 6) level = 1; // Cycle through levels
            
            // Make maze more complex for higher levels
            mazeWidth = Math.Min(31 + level * 2, 41);
            mazeHeight = Math.Min(21 + level * 2, 31);
            
            // Recreate maze display for new size
            maze = new int[mazeHeight, mazeWidth];
            mazeRectangles = new Rectangle[mazeHeight, mazeWidth];
            CreateMazeDisplay();
            
            StartNewGame();
        }

        private void ShowSolutionPath()
        {
            if (solutionVisible)
            {
                HideSolutionPath();
                return;
            }
            
            var solution = FindPath(playerX, playerY, exitX, exitY);
            
            if (solution.Count > 0)
            {
                solutionPath.Clear();
                
                foreach (var (x, y) in solution.Skip(1)) // Skip current position
                {
                    var solutionRect = new Rectangle
                    {
                        Width = CellSize - 8,
                        Height = CellSize - 8,
                        Fill = Brushes.LightBlue,
                        Opacity = 0.7
                    };
                    
                    Canvas.SetLeft(solutionRect, x * CellSize + 4);
                    Canvas.SetTop(solutionRect, y * CellSize + 4);
                    GameCanvas.Children.Add(solutionRect);
                    solutionPath.Add(solutionRect);
                }
                
                solutionVisible = true;
                SolutionButton.Content = "Hide Solution";
            }
        }

        private void HideSolutionPath()
        {
            foreach (var rect in solutionPath)
            {
                GameCanvas.Children.Remove(rect);
            }
            solutionPath.Clear();
            solutionVisible = false;
            SolutionButton.Content = "Show Solution";
        }

        private List<(int x, int y)> FindPath(int startX, int startY, int endX, int endY)
        {
            var queue = new Queue<(int x, int y, List<(int x, int y)> path)>();
            var visited = new HashSet<(int x, int y)>();
            
            queue.Enqueue((startX, startY, new List<(int x, int y)> { (startX, startY) }));
            visited.Add((startX, startY));
            
            var directions = new[] { (0, -1), (1, 0), (0, 1), (-1, 0) };
            
            while (queue.Count > 0)
            {
                var (x, y, path) = queue.Dequeue();
                
                if (x == endX && y == endY)
                {
                    return path;
                }
                
                foreach (var (dx, dy) in directions)
                {
                    int newX = x + dx;
                    int newY = y + dy;
                    
                    if (newX >= 0 && newX < mazeWidth && 
                        newY >= 0 && newY < mazeHeight && 
                        maze[newY, newX] == 0 && 
                        !visited.Contains((newX, newY)))
                    {
                        visited.Add((newX, newY));
                        var newPath = new List<(int x, int y)>(path) { (newX, newY) };
                        queue.Enqueue((newX, newY, newPath));
                    }
                }
            }
            
            return new List<(int x, int y)>();
        }

        private void UpdateUI()
        {
            LevelText.Text = level.ToString();
            
            if (gameActive)
            {
                var elapsed = DateTime.Now - startTime;
                TimeText.Text = $"{elapsed:mm\\:ss}";
            }
            
            BestTimeText.Text = bestTime == TimeSpan.MaxValue ? "--:--" : $"{bestTime:mm\\:ss}";
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            UpdateUI();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    MovePlayer(0, -1);
                    break;
                case Key.Down:
                case Key.S:
                    MovePlayer(0, 1);
                    break;
                case Key.Left:
                case Key.A:
                    MovePlayer(-1, 0);
                    break;
                case Key.Right:
                case Key.D:
                    MovePlayer(1, 0);
                    break;
                case Key.H:
                    ShowSolution_Click(this, new RoutedEventArgs());
                    break;
                case Key.R:
                    ResetPosition_Click(this, new RoutedEventArgs());
                    break;
                case Key.N:
                    NewMaze_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void NewMaze_Click(object sender, RoutedEventArgs e)
        {
            gameTimer?.Stop();
            HideSolutionPath();
            StartNewGame();
        }

        private void ShowSolution_Click(object sender, RoutedEventArgs e)
        {
            ShowSolutionPath();
        }

        private void ResetPosition_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive) return;
            
            playerX = 1;
            playerY = 1;
            UpdateDisplay();
            
            // Restart timer
            startTime = DateTime.Now;
            StatusText.Text = "Position reset! Navigate to the golden exit!";
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