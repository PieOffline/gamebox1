using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameBox.Games
{
    public partial class SudokuGame : Window
    {
        private const int Size = 9;
        private const int CellSize = 60;
        
        private int[,] puzzle = new int[Size, Size];
        private int[,] solution = new int[Size, Size];
        private TextBox[,] cells = new TextBox[Size, Size];

        public SudokuGame()
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
            GameGrid.Children.Clear();
            GameGrid.RowDefinitions.Clear();
            GameGrid.ColumnDefinitions.Clear();
            
            // Create grid
            for (int i = 0; i < Size; i++)
            {
                GameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize) });
                GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize) });
            }
            
            // Generate puzzle
            GeneratePuzzle();
            
            // Create cells
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var cell = CreateCell(row, col);
                    cells[row, col] = cell;
                    
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    GameGrid.Children.Add(cell);
                }
            }
        }

        private TextBox CreateCell(int row, int col)
        {
            var cell = new TextBox
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderThickness = new Thickness(1),
                MaxLength = 1
            };
            
            // Thicker borders for 3x3 blocks
            var thickness = new Thickness(
                col % 3 == 0 ? 3 : 1,
                row % 3 == 0 ? 3 : 1,
                col == 8 ? 3 : 1,
                row == 8 ? 3 : 1
            );
            cell.BorderThickness = thickness;
            cell.BorderBrush = Brushes.Black;
            
            if (puzzle[row, col] != 0)
            {
                cell.Text = puzzle[row, col].ToString();
                cell.IsReadOnly = true;
                cell.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));
                cell.Foreground = Brushes.Black;
            }
            else
            {
                cell.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            }
            
            cell.PreviewTextInput += (s, e) =>
            {
                e.Handled = !char.IsDigit(e.Text, 0) || e.Text == "0";
            };
            
            cell.TextChanged += (s, e) =>
            {
                if (cell.Text.Length > 0)
                {
                    int value = int.Parse(cell.Text);
                    if (IsValidMove(row, col, value))
                    {
                        cell.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                    }
                    else
                    {
                        cell.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                    }
                    
                    CheckWin();
                }
                else
                {
                    cell.Background = Brushes.White;
                }
            };
            
            return cell;
        }

        private void GeneratePuzzle()
        {
            // Generate a complete valid Sudoku solution
            GenerateSolution();
            
            // Copy solution to puzzle
            Array.Copy(solution, puzzle, Size * Size);
            
            // Remove numbers to create puzzle (40-50 removed for medium difficulty)
            var random = new Random();
            int cellsToRemove = 45;
            
            while (cellsToRemove > 0)
            {
                int row = random.Next(Size);
                int col = random.Next(Size);
                
                if (puzzle[row, col] != 0)
                {
                    puzzle[row, col] = 0;
                    cellsToRemove--;
                }
            }
        }

        private void GenerateSolution()
        {
            // Initialize empty grid
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    solution[i, j] = 0;
            
            // Fill diagonal 3x3 boxes
            FillDiagonalBoxes();
            
            // Fill remaining cells
            SolveSudoku(solution);
        }

        private void FillDiagonalBoxes()
        {
            var random = new Random();
            for (int box = 0; box < 3; box++)
            {
                int startRow = box * 3;
                int startCol = box * 3;
                
                var numbers = Enumerable.Range(1, 9).OrderBy(x => random.Next()).ToArray();
                int index = 0;
                
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        solution[startRow + row, startCol + col] = numbers[index++];
                    }
                }
            }
        }

        private bool SolveSudoku(int[,] grid)
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (grid[row, col] == 0)
                    {
                        for (int num = 1; num <= 9; num++)
                        {
                            if (IsValidPlacement(grid, row, col, num))
                            {
                                grid[row, col] = num;
                                
                                if (SolveSudoku(grid))
                                    return true;
                                
                                grid[row, col] = 0;
                            }
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsValidPlacement(int[,] grid, int row, int col, int num)
        {
            // Check row
            for (int c = 0; c < Size; c++)
                if (grid[row, c] == num)
                    return false;
            
            // Check column
            for (int r = 0; r < Size; r++)
                if (grid[r, col] == num)
                    return false;
            
            // Check 3x3 box
            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (grid[boxRow + r, boxCol + c] == num)
                        return false;
            
            return true;
        }

        private bool IsValidMove(int row, int col, int num)
        {
            // Check row
            for (int c = 0; c < Size; c++)
            {
                if (c != col && cells[row, c].Text == num.ToString())
                    return false;
            }
            
            // Check column
            for (int r = 0; r < Size; r++)
            {
                if (r != row && cells[r, col].Text == num.ToString())
                    return false;
            }
            
            // Check 3x3 box
            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            for (int r = boxRow; r < boxRow + 3; r++)
            {
                for (int c = boxCol; c < boxCol + 3; c++)
                {
                    if ((r != row || c != col) && cells[r, c].Text == num.ToString())
                        return false;
                }
            }
            
            return true;
        }

        private void CheckWin()
        {
            bool allFilled = true;
            bool allCorrect = true;
            
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (string.IsNullOrEmpty(cells[row, col].Text))
                    {
                        allFilled = false;
                    }
                    else if (int.Parse(cells[row, col].Text) != solution[row, col])
                    {
                        allCorrect = false;
                    }
                }
            }
            
            if (allFilled && allCorrect)
            {
                MessageBox.Show("Congratulations! You solved the puzzle!", "Victory", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
