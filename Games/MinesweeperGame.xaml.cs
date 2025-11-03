using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameBox.Games
{
    public partial class MinesweeperGame : Window
    {
        private const int Rows = 12;
        private const int Cols = 12;
        private const int Mines = 15;
        private const int CellSize = 40;
        
        private Cell[,] grid = new Cell[Rows, Cols];
        private int flagsPlaced = 0;
        private int cellsRevealed = 0;
        private bool gameOver = false;

        public MinesweeperGame()
        {
            InitializeComponent();
            NewGame();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            NewGame();
        }

        private void NewGame()
        {
            gameOver = false;
            flagsPlaced = 0;
            cellsRevealed = 0;
            
            GameGrid.Children.Clear();
            GameGrid.RowDefinitions.Clear();
            GameGrid.ColumnDefinitions.Clear();
            
            // Create grid
            for (int i = 0; i < Rows; i++)
            {
                GameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize) });
            }
            for (int i = 0; i < Cols; i++)
            {
                GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize) });
            }
            
            // Initialize cells
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    grid[row, col] = new Cell();
                    var button = CreateCellButton(row, col);
                    grid[row, col].Button = button;
                    
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    GameGrid.Children.Add(button);
                }
            }
            
            // Place mines
            PlaceMines();
            
            // Calculate adjacent mines
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (!grid[row, col].IsMine)
                    {
                        grid[row, col].AdjacentMines = CountAdjacentMines(row, col);
                    }
                }
            }
            
            UpdateDisplay();
        }

        private Button CreateCellButton(int row, int col)
        {
            var button = new Button
            {
                Background = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                BorderThickness = new Thickness(1),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            
            button.Click += (s, e) => CellClick(row, col);
            button.MouseRightButtonDown += (s, e) =>
            {
                e.Handled = true;
                FlagCell(row, col);
            };
            
            return button;
        }

        private void PlaceMines()
        {
            var random = new Random();
            int minesPlaced = 0;
            
            while (minesPlaced < Mines)
            {
                int row = random.Next(Rows);
                int col = random.Next(Cols);
                
                if (!grid[row, col].IsMine)
                {
                    grid[row, col].IsMine = true;
                    minesPlaced++;
                }
            }
        }

        private int CountAdjacentMines(int row, int col)
        {
            int count = 0;
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    
                    int newRow = row + dr;
                    int newCol = col + dc;
                    
                    if (newRow >= 0 && newRow < Rows && newCol >= 0 && newCol < Cols)
                    {
                        if (grid[newRow, newCol].IsMine)
                            count++;
                    }
                }
            }
            return count;
        }

        private void CellClick(int row, int col)
        {
            if (gameOver || grid[row, col].IsRevealed || grid[row, col].IsFlagged)
                return;
            
            RevealCell(row, col);
            
            // Check win condition
            if (cellsRevealed == Rows * Cols - Mines)
            {
                gameOver = true;
                MessageBox.Show("Congratulations! You won!", "Victory", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RevealCell(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
                return;
            
            if (grid[row, col].IsRevealed || grid[row, col].IsFlagged)
                return;
            
            grid[row, col].IsRevealed = true;
            cellsRevealed++;
            
            if (grid[row, col].IsMine)
            {
                // Game over
                gameOver = true;
                RevealAllMines();
                grid[row, col].Button.Background = Brushes.Red;
                MessageBox.Show("Game Over! You hit a mine!", "Game Over", 
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            
            // Update button appearance
            grid[row, col].Button.Background = Brushes.White;
            
            if (grid[row, col].AdjacentMines > 0)
            {
                grid[row, col].Button.Content = grid[row, col].AdjacentMines.ToString();
                grid[row, col].Button.Foreground = GetNumberColor(grid[row, col].AdjacentMines);
            }
            else
            {
                // Reveal adjacent cells if no adjacent mines
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        RevealCell(row + dr, col + dc);
                    }
                }
            }
        }

        private void FlagCell(int row, int col)
        {
            if (gameOver || grid[row, col].IsRevealed)
                return;
            
            if (grid[row, col].IsFlagged)
            {
                grid[row, col].IsFlagged = false;
                grid[row, col].Button.Content = "";
                flagsPlaced--;
            }
            else
            {
                grid[row, col].IsFlagged = true;
                grid[row, col].Button.Content = "🚩";
                flagsPlaced++;
            }
            
            UpdateDisplay();
        }

        private void RevealAllMines()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (grid[row, col].IsMine)
                    {
                        grid[row, col].Button.Content = "💣";
                        grid[row, col].Button.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                    }
                }
            }
        }

        private Brush GetNumberColor(int number)
        {
            return number switch
            {
                1 => Brushes.Blue,
                2 => Brushes.Green,
                3 => Brushes.Red,
                4 => Brushes.DarkBlue,
                5 => Brushes.DarkRed,
                6 => Brushes.Cyan,
                7 => Brushes.Black,
                8 => Brushes.Gray,
                _ => Brushes.Black
            };
        }

        private void UpdateDisplay()
        {
            MinesText.Text = Mines.ToString();
            FlagsText.Text = flagsPlaced.ToString();
        }

        private class Cell
        {
            public bool IsMine { get; set; }
            public bool IsRevealed { get; set; }
            public bool IsFlagged { get; set; }
            public int AdjacentMines { get; set; }
            public Button Button { get; set; } = null!;
        }
    }
}
