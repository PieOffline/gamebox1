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
    public partial class Game2048 : Window
    {
        private const int GridSize = 4;
        private int[,] gameBoard = new int[GridSize, GridSize];
        private int[,] previousBoard = new int[GridSize, GridSize];
        private Border[,] tileBorders = new Border[GridSize, GridSize];
        
        private int score = 0;
        private int previousScore = 0;
        private int bestScore = 0;
        private bool gameWon = false;
        private bool gameOver = false;
        private bool canUndo = false;
        
        private Random random = new Random();
        private DispatcherTimer moveAnimationTimer = null!;
        
        // Tile colors based on value
        private readonly Dictionary<int, Brush> tileColors = new Dictionary<int, Brush>
        {
            { 0, Brushes.LightGray },
            { 2, Brushes.LightBlue },
            { 4, Brushes.LightGreen },
            { 8, Brushes.Orange },
            { 16, Brushes.Coral },
            { 32, Brushes.Tomato },
            { 64, Brushes.Red },
            { 128, Brushes.Gold },
            { 256, Brushes.Yellow },
            { 512, Brushes.Lime },
            { 1024, Brushes.Cyan },
            { 2048, Brushes.Magenta },
            { 4096, Brushes.Purple },
            { 8192, Brushes.DarkViolet }
        };

        public Game2048()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            CreateGameBoard();
            
            moveAnimationTimer = new DispatcherTimer();
            moveAnimationTimer.Interval = TimeSpan.FromMilliseconds(150);
            moveAnimationTimer.Tick += MoveAnimationTimer_Tick;
            
            StartNewGame();
        }

        private void CreateGameBoard()
        {
            GameGrid.Children.Clear();
            
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    var border = new Border
                    {
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(2),
                        Margin = new Thickness(2),
                        CornerRadius = new CornerRadius(5)
                    };
                    
                    var textBlock = new TextBlock
                    {
                        Text = "",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.White
                    };
                    
                    border.Child = textBlock;
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    GameGrid.Children.Add(border);
                    
                    tileBorders[row, col] = border;
                }
            }
        }

        private void StartNewGame()
        {
            // Clear board
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    gameBoard[row, col] = 0;
                }
            }
            
            score = 0;
            gameWon = false;
            gameOver = false;
            canUndo = false;
            
            // Add two initial tiles
            AddRandomTile();
            AddRandomTile();
            
            UpdateDisplay();
            UpdateUI();
        }

        private void AddRandomTile()
        {
            var emptyCells = new List<(int row, int col)>();
            
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (gameBoard[row, col] == 0)
                    {
                        emptyCells.Add((row, col));
                    }
                }
            }
            
            if (emptyCells.Count > 0)
            {
                var (row, col) = emptyCells[random.Next(emptyCells.Count)];
                gameBoard[row, col] = random.NextDouble() < 0.9 ? 2 : 4; // 90% chance of 2, 10% chance of 4
            }
        }

        private void SaveGameState()
        {
            // Save current state for undo
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    previousBoard[row, col] = gameBoard[row, col];
                }
            }
            previousScore = score;
        }

        private bool MakeMove(Direction direction)
        {
            SaveGameState();
            bool moved = false;
            
            switch (direction)
            {
                case Direction.Up:
                    moved = MoveUp();
                    break;
                case Direction.Down:
                    moved = MoveDown();
                    break;
                case Direction.Left:
                    moved = MoveLeft();
                    break;
                case Direction.Right:
                    moved = MoveRight();
                    break;
            }
            
            if (moved)
            {
                AddRandomTile();
                UpdateDisplay();
                UpdateUI();
                canUndo = true;
                
                if (CheckWin())
                {
                    GameWin();
                }
                else if (CheckGameOver())
                {
                    GameOver();
                }
            }
            
            return moved;
        }

        private bool MoveLeft()
        {
            bool moved = false;
            
            for (int row = 0; row < GridSize; row++)
            {
                // Slide tiles left
                int writeIndex = 0;
                for (int col = 0; col < GridSize; col++)
                {
                    if (gameBoard[row, col] != 0)
                    {
                        if (writeIndex != col)
                        {
                            gameBoard[row, writeIndex] = gameBoard[row, col];
                            gameBoard[row, col] = 0;
                            moved = true;
                        }
                        writeIndex++;
                    }
                }
                
                // Merge tiles
                for (int col = 0; col < GridSize - 1; col++)
                {
                    if (gameBoard[row, col] != 0 && gameBoard[row, col] == gameBoard[row, col + 1])
                    {
                        gameBoard[row, col] *= 2;
                        score += gameBoard[row, col];
                        
                        // Shift remaining tiles left
                        for (int k = col + 1; k < GridSize - 1; k++)
                        {
                            gameBoard[row, k] = gameBoard[row, k + 1];
                        }
                        gameBoard[row, GridSize - 1] = 0;
                        moved = true;
                    }
                }
            }
            
            return moved;
        }

        private bool MoveRight()
        {
            bool moved = false;
            
            for (int row = 0; row < GridSize; row++)
            {
                // Slide tiles right
                int writeIndex = GridSize - 1;
                for (int col = GridSize - 1; col >= 0; col--)
                {
                    if (gameBoard[row, col] != 0)
                    {
                        if (writeIndex != col)
                        {
                            gameBoard[row, writeIndex] = gameBoard[row, col];
                            gameBoard[row, col] = 0;
                            moved = true;
                        }
                        writeIndex--;
                    }
                }
                
                // Merge tiles
                for (int col = GridSize - 1; col > 0; col--)
                {
                    if (gameBoard[row, col] != 0 && gameBoard[row, col] == gameBoard[row, col - 1])
                    {
                        gameBoard[row, col] *= 2;
                        score += gameBoard[row, col];
                        
                        // Shift remaining tiles right
                        for (int k = col - 1; k > 0; k--)
                        {
                            gameBoard[row, k] = gameBoard[row, k - 1];
                        }
                        gameBoard[row, 0] = 0;
                        moved = true;
                    }
                }
            }
            
            return moved;
        }

        private bool MoveUp()
        {
            bool moved = false;
            
            for (int col = 0; col < GridSize; col++)
            {
                // Slide tiles up
                int writeIndex = 0;
                for (int row = 0; row < GridSize; row++)
                {
                    if (gameBoard[row, col] != 0)
                    {
                        if (writeIndex != row)
                        {
                            gameBoard[writeIndex, col] = gameBoard[row, col];
                            gameBoard[row, col] = 0;
                            moved = true;
                        }
                        writeIndex++;
                    }
                }
                
                // Merge tiles
                for (int row = 0; row < GridSize - 1; row++)
                {
                    if (gameBoard[row, col] != 0 && gameBoard[row, col] == gameBoard[row + 1, col])
                    {
                        gameBoard[row, col] *= 2;
                        score += gameBoard[row, col];
                        
                        // Shift remaining tiles up
                        for (int k = row + 1; k < GridSize - 1; k++)
                        {
                            gameBoard[k, col] = gameBoard[k + 1, col];
                        }
                        gameBoard[GridSize - 1, col] = 0;
                        moved = true;
                    }
                }
            }
            
            return moved;
        }

        private bool MoveDown()
        {
            bool moved = false;
            
            for (int col = 0; col < GridSize; col++)
            {
                // Slide tiles down
                int writeIndex = GridSize - 1;
                for (int row = GridSize - 1; row >= 0; row--)
                {
                    if (gameBoard[row, col] != 0)
                    {
                        if (writeIndex != row)
                        {
                            gameBoard[writeIndex, col] = gameBoard[row, col];
                            gameBoard[row, col] = 0;
                            moved = true;
                        }
                        writeIndex--;
                    }
                }
                
                // Merge tiles
                for (int row = GridSize - 1; row > 0; row--)
                {
                    if (gameBoard[row, col] != 0 && gameBoard[row, col] == gameBoard[row - 1, col])
                    {
                        gameBoard[row, col] *= 2;
                        score += gameBoard[row, col];
                        
                        // Shift remaining tiles down
                        for (int k = row - 1; k > 0; k--)
                        {
                            gameBoard[k, col] = gameBoard[k - 1, col];
                        }
                        gameBoard[0, col] = 0;
                        moved = true;
                    }
                }
            }
            
            return moved;
        }

        private bool CheckWin()
        {
            if (gameWon) return false;
            
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (gameBoard[row, col] == 2048)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckGameOver()
        {
            // Check for empty cells
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (gameBoard[row, col] == 0)
                        return false;
                }
            }
            
            // Check for possible merges
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    int currentValue = gameBoard[row, col];
                    
                    // Check right
                    if (col < GridSize - 1 && gameBoard[row, col + 1] == currentValue)
                        return false;
                    
                    // Check down
                    if (row < GridSize - 1 && gameBoard[row + 1, col] == currentValue)
                        return false;
                }
            }
            
            return true;
        }

        private void UpdateDisplay()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    var border = tileBorders[row, col];
                    var textBlock = border.Child as TextBlock;
                    int value = gameBoard[row, col];
                    
                    if (value == 0)
                    {
                        textBlock!.Text = "";
                        border.Background = Brushes.LightGray;
                    }
                    else
                    {
                        textBlock!.Text = value.ToString();
                        border.Background = tileColors.GetValueOrDefault(value, Brushes.DarkSlateGray);
                        
                        // Adjust font size for larger numbers
                        textBlock.FontSize = value >= 1000 ? 18 : value >= 100 ? 20 : 24;
                        textBlock.Foreground = value >= 8 ? Brushes.White : Brushes.Black;
                    }
                }
            }
        }

        private void UpdateUI()
        {
            ScoreText.Text = $"Score: {score:N0}";
            if (score > bestScore)
            {
                bestScore = score;
            }
            BestScoreText.Text = $"Best: {bestScore:N0}";
            UndoButton.IsEnabled = canUndo && !gameOver;
        }

        private void GameWin()
        {
            gameWon = true;
            StatusText.Text = "🎉 You reached 2048! You can continue playing.";
            
            MessageBox.Show("Congratulations! 🎉\n\nYou reached 2048!\n\nYou can continue playing to reach higher numbers!", 
                "Victory!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GameOver()
        {
            gameOver = true;
            StatusText.Text = "Game Over! No more moves available.";
            
            string message = $"Game Over!\n\nFinal Score: {score:N0}";
            if (score == bestScore)
            {
                message += "\n🌟 New Best Score!";
            }
            
            MessageBox.Show(message, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MoveAnimationTimer_Tick(object? sender, EventArgs e)
        {
            moveAnimationTimer.Stop();
            // Animation complete - could add smooth tile movements here
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameOver) return;
            
            Direction? direction = e.Key switch
            {
                Key.Up or Key.W => Direction.Up,
                Key.Down or Key.S => Direction.Down,
                Key.Left or Key.A => Direction.Left,
                Key.Right or Key.D => Direction.Right,
                _ => null
            };
            
            if (direction.HasValue)
            {
                MakeMove(direction.Value);
                e.Handled = true;
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Use arrow keys or WASD to move tiles!";
            StartNewGame();
        }

        private void UndoMove_Click(object sender, RoutedEventArgs e)
        {
            if (!canUndo) return;
            
            // Restore previous state
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    gameBoard[row, col] = previousBoard[row, col];
                }
            }
            score = previousScore;
            canUndo = false;
            
            UpdateDisplay();
            UpdateUI();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            moveAnimationTimer?.Stop();
        }

        private enum Direction
        {
            Up, Down, Left, Right
        }
    }
}