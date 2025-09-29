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
    public partial class TetrisGame : Window
    {
        private const int BoardWidth = 10;
        private const int BoardHeight = 20;
        private const int BlockSize = 30;
        
        private int[,] gameBoard = new int[BoardHeight, BoardWidth];
        private Rectangle[,] boardRectangles = new Rectangle[BoardHeight, BoardWidth];
        private List<Rectangle> currentPieceBlocks = new List<Rectangle>();
        private List<Rectangle> nextPieceBlocks = new List<Rectangle>();
        
        private DispatcherTimer gameTimer = null!;
        private DispatcherTimer dropTimer = null!;
        private Random random = new Random();
        
        private int currentPieceType = 0;
        private int nextPieceType = 0;
        private int currentX = 0;
        private int currentY = 0;
        private int currentRotation = 0;
        
        private int score = 0;
        private int level = 1;
        private int linesCleared = 0;
        private bool gameActive = false;
        private bool paused = false;
        
        // Tetromino shapes (I, O, T, S, Z, J, L)
        private readonly int[,,,] tetrominoes = new int[7, 4, 4, 4]
        {
            // I-piece
            {
                {{0,0,0,0},{1,1,1,1},{0,0,0,0},{0,0,0,0}},
                {{0,0,1,0},{0,0,1,0},{0,0,1,0},{0,0,1,0}},
                {{0,0,0,0},{0,0,0,0},{1,1,1,1},{0,0,0,0}},
                {{0,1,0,0},{0,1,0,0},{0,1,0,0},{0,1,0,0}}
            },
            // O-piece
            {
                {{0,0,0,0},{0,1,1,0},{0,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,1,0},{0,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,1,0},{0,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,1,0},{0,1,1,0},{0,0,0,0}}
            },
            // T-piece
            {
                {{0,0,0,0},{0,1,0,0},{1,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,0,0},{0,1,1,0},{0,1,0,0}},
                {{0,0,0,0},{0,0,0,0},{1,1,1,0},{0,1,0,0}},
                {{0,0,0,0},{0,1,0,0},{1,1,0,0},{0,1,0,0}}
            },
            // S-piece
            {
                {{0,0,0,0},{0,1,1,0},{1,1,0,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,0,0},{0,1,1,0},{0,0,1,0}},
                {{0,0,0,0},{0,0,0,0},{0,1,1,0},{1,1,0,0}},
                {{0,0,0,0},{1,0,0,0},{1,1,0,0},{0,1,0,0}}
            },
            // Z-piece
            {
                {{0,0,0,0},{1,1,0,0},{0,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,0,1,0},{0,1,1,0},{0,1,0,0}},
                {{0,0,0,0},{0,0,0,0},{1,1,0,0},{0,1,1,0}},
                {{0,0,0,0},{0,1,0,0},{1,1,0,0},{1,0,0,0}}
            },
            // J-piece
            {
                {{0,0,0,0},{1,0,0,0},{1,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,1,0},{0,1,0,0},{0,1,0,0}},
                {{0,0,0,0},{0,0,0,0},{1,1,1,0},{0,0,1,0}},
                {{0,0,0,0},{0,1,0,0},{0,1,0,0},{1,1,0,0}}
            },
            // L-piece
            {
                {{0,0,0,0},{0,0,1,0},{1,1,1,0},{0,0,0,0}},
                {{0,0,0,0},{0,1,0,0},{0,1,0,0},{0,1,1,0}},
                {{0,0,0,0},{0,0,0,0},{1,1,1,0},{1,0,0,0}},
                {{0,0,0,0},{1,1,0,0},{0,1,0,0},{0,1,0,0}}
            }
        };
        
        // Tetromino colors
        private readonly Brush[] tetrominoColors = {
            Brushes.Cyan,      // I-piece
            Brushes.Yellow,    // O-piece
            Brushes.Purple,    // T-piece
            Brushes.Lime,      // S-piece
            Brushes.Red,       // Z-piece
            Brushes.Blue,      // J-piece
            Brushes.Orange     // L-piece
        };

        public TetrisGame()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            CreateBoard();
            
            // Setup game timer
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(50);
            gameTimer.Tick += GameLoop;
            
            // Setup drop timer
            dropTimer = new DispatcherTimer();
            dropTimer.Interval = TimeSpan.FromMilliseconds(800);
            dropTimer.Tick += DropTimer_Tick;
            
            NewGame();
        }

        private void CreateBoard()
        {
            GameCanvas.Children.Clear();
            
            for (int row = 0; row < BoardHeight; row++)
            {
                for (int col = 0; col < BoardWidth; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = BlockSize,
                        Height = BlockSize,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 0.5,
                        Fill = Brushes.Transparent
                    };
                    
                    Canvas.SetLeft(rect, col * BlockSize);
                    Canvas.SetTop(rect, row * BlockSize);
                    GameCanvas.Children.Add(rect);
                    boardRectangles[row, col] = rect;
                    gameBoard[row, col] = 0;
                }
            }
        }

        private void NewGame()
        {
            // Reset game state
            score = 0;
            level = 1;
            linesCleared = 0;
            paused = false;
            
            // Clear board
            for (int row = 0; row < BoardHeight; row++)
            {
                for (int col = 0; col < BoardWidth; col++)
                {
                    gameBoard[row, col] = 0;
                    boardRectangles[row, col].Fill = Brushes.Transparent;
                }
            }
            
            // Generate first pieces
            currentPieceType = random.Next(7);
            nextPieceType = random.Next(7);
            
            SpawnNewPiece();
            DrawNextPiece();
            UpdateUI();
            
            gameActive = true;
            StatusText.Text = "Game in progress!";
            
            gameTimer.Start();
            dropTimer.Start();
        }

        private void SpawnNewPiece()
        {
            currentPieceType = nextPieceType;
            nextPieceType = random.Next(7);
            currentX = BoardWidth / 2 - 2;
            currentY = 0;
            currentRotation = 0;
            
            // Check game over
            if (!CanPlacePiece(currentPieceType, currentX, currentY, currentRotation))
            {
                GameOver();
                return;
            }
            
            DrawCurrentPiece();
            DrawNextPiece();
        }

        private bool CanPlacePiece(int pieceType, int x, int y, int rotation)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (tetrominoes[pieceType, rotation, row, col] == 1)
                    {
                        int boardX = x + col;
                        int boardY = y + row;
                        
                        if (boardX < 0 || boardX >= BoardWidth || 
                            boardY >= BoardHeight || 
                            (boardY >= 0 && gameBoard[boardY, boardX] != 0))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void PlacePiece()
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (tetrominoes[currentPieceType, currentRotation, row, col] == 1)
                    {
                        int boardX = currentX + col;
                        int boardY = currentY + row;
                        
                        if (boardY >= 0 && boardY < BoardHeight && boardX >= 0 && boardX < BoardWidth)
                        {
                            gameBoard[boardY, boardX] = currentPieceType + 1;
                            boardRectangles[boardY, boardX].Fill = tetrominoColors[currentPieceType];
                        }
                    }
                }
            }
            
            // Clear current piece visualization
            foreach (var block in currentPieceBlocks)
            {
                GameCanvas.Children.Remove(block);
            }
            currentPieceBlocks.Clear();
            
            CheckLines();
            SpawnNewPiece();
        }

        private void DrawCurrentPiece()
        {
            // Clear previous piece
            foreach (var block in currentPieceBlocks)
            {
                GameCanvas.Children.Remove(block);
            }
            currentPieceBlocks.Clear();
            
            // Draw current piece
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (tetrominoes[currentPieceType, currentRotation, row, col] == 1)
                    {
                        int boardX = currentX + col;
                        int boardY = currentY + row;
                        
                        if (boardY >= 0 && boardY < BoardHeight && boardX >= 0 && boardX < BoardWidth)
                        {
                            var block = new Rectangle
                            {
                                Width = BlockSize,
                                Height = BlockSize,
                                Fill = tetrominoColors[currentPieceType],
                                Stroke = Brushes.White,
                                StrokeThickness = 2
                            };
                            
                            Canvas.SetLeft(block, boardX * BlockSize);
                            Canvas.SetTop(block, boardY * BlockSize);
                            GameCanvas.Children.Add(block);
                            currentPieceBlocks.Add(block);
                        }
                    }
                }
            }
        }

        private void DrawNextPiece()
        {
            // Clear previous next piece
            foreach (var block in nextPieceBlocks)
            {
                NextPieceCanvas.Children.Remove(block);
            }
            nextPieceBlocks.Clear();
            
            // Draw next piece in preview
            const int previewBlockSize = 15;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (tetrominoes[nextPieceType, 0, row, col] == 1)
                    {
                        var block = new Rectangle
                        {
                            Width = previewBlockSize,
                            Height = previewBlockSize,
                            Fill = tetrominoColors[nextPieceType],
                            Stroke = Brushes.White,
                            StrokeThickness = 1
                        };
                        
                        Canvas.SetLeft(block, col * previewBlockSize + 10);
                        Canvas.SetTop(block, row * previewBlockSize + 10);
                        NextPieceCanvas.Children.Add(block);
                        nextPieceBlocks.Add(block);
                    }
                }
            }
        }

        private void CheckLines()
        {
            int linesCleared = 0;
            
            for (int row = BoardHeight - 1; row >= 0; row--)
            {
                bool isFullLine = true;
                for (int col = 0; col < BoardWidth; col++)
                {
                    if (gameBoard[row, col] == 0)
                    {
                        isFullLine = false;
                        break;
                    }
                }
                
                if (isFullLine)
                {
                    // Clear the line
                    for (int moveRow = row; moveRow > 0; moveRow--)
                    {
                        for (int col = 0; col < BoardWidth; col++)
                        {
                            gameBoard[moveRow, col] = gameBoard[moveRow - 1, col];
                            boardRectangles[moveRow, col].Fill = boardRectangles[moveRow - 1, col].Fill;
                        }
                    }
                    
                    // Clear top row
                    for (int col = 0; col < BoardWidth; col++)
                    {
                        gameBoard[0, col] = 0;
                        boardRectangles[0, col].Fill = Brushes.Transparent;
                    }
                    
                    linesCleared++;
                    row++; // Check the same row again
                }
            }
            
            if (linesCleared > 0)
            {
                // Update score based on lines cleared
                int points = linesCleared switch
                {
                    1 => 100 * level,
                    2 => 300 * level,
                    3 => 500 * level,
                    4 => 800 * level, // Tetris!
                    _ => 0
                };
                
                score += points;
                this.linesCleared += linesCleared;
                
                // Level up every 10 lines
                level = (this.linesCleared / 10) + 1;
                
                // Increase speed
                UpdateDropSpeed();
                UpdateUI();
            }
        }

        private void UpdateDropSpeed()
        {
            double speed = Math.Max(50, 800 - (level - 1) * 50);
            dropTimer.Interval = TimeSpan.FromMilliseconds(speed);
        }

        private void UpdateUI()
        {
            ScoreText.Text = $"Score: {score:N0}";
            LevelText.Text = $"Level: {level}";
            LinesText.Text = $"Lines: {linesCleared}";
        }

        private void GameOver()
        {
            gameActive = false;
            gameTimer.Stop();
            dropTimer.Stop();
            
            StatusText.Text = "Game Over! Press 'New Game' to play again";
            
            MessageBox.Show($"Game Over!\n\nFinal Score: {score:N0}\nLevel Reached: {level}\nLines Cleared: {linesCleared}", 
                "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameActive || paused) return;
            
            DrawCurrentPiece();
        }

        private void DropTimer_Tick(object? sender, EventArgs e)
        {
            if (!gameActive || paused) return;
            
            if (CanPlacePiece(currentPieceType, currentX, currentY + 1, currentRotation))
            {
                currentY++;
            }
            else
            {
                PlacePiece();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!gameActive) return;
            
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    if (CanPlacePiece(currentPieceType, currentX - 1, currentY, currentRotation))
                        currentX--;
                    break;
                    
                case Key.Right:
                case Key.D:
                    if (CanPlacePiece(currentPieceType, currentX + 1, currentY, currentRotation))
                        currentX++;
                    break;
                    
                case Key.Down:
                case Key.S:
                    if (CanPlacePiece(currentPieceType, currentX, currentY + 1, currentRotation))
                        currentY++;
                    break;
                    
                case Key.Up:
                case Key.W:
                    int newRotation = (currentRotation + 1) % 4;
                    if (CanPlacePiece(currentPieceType, currentX, currentY, newRotation))
                        currentRotation = newRotation;
                    break;
                    
                case Key.Space:
                    // Hard drop
                    while (CanPlacePiece(currentPieceType, currentX, currentY + 1, currentRotation))
                    {
                        currentY++;
                    }
                    PlacePiece();
                    break;
                    
                case Key.P:
                    PauseResume_Click(this, new RoutedEventArgs());
                    break;
                    
                case Key.Escape:
                    this.Close();
                    break;
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            gameTimer?.Stop();
            dropTimer?.Stop();
            NewGame();
        }

        private void PauseResume_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive) return;
            
            paused = !paused;
            StatusText.Text = paused ? "Game Paused - Press P to resume" : "Game in progress!";
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            gameTimer?.Stop();
            dropTimer?.Stop();
        }
    }
}