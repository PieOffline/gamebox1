using System;
using System.Collections.Generic;
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
using GameBox.Utils;

namespace GameBox.Games
{
    public partial class CheckersGame : Window, IMultiplayerGame, ILocalMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private bool gameActive = false;
        private bool isMyTurn = false;
        private bool isLocalMultiplayer = false;
        
        private const int BoardSize = 8;
        private const int CellSize = 80;
        private CheckersPiece?[,] board = new CheckersPiece?[BoardSize, BoardSize];
        private CheckersPiece? selectedPiece = null;
        private List<Ellipse> highlightCircles = new List<Ellipse>();
        
        private bool isRedPlayer; // Red moves first

        public CheckersGame()
        {
            InitializeComponent();
            InitializeBoard();
            DrawBoard();
        }

        public void SetLocalMultiplayerMode(bool enabled)
        {
            isLocalMultiplayer = enabled;
            gameActive = true;
            isRedPlayer = true;
            isMyTurn = true;
            UpdateStatusText();
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
            this.isRedPlayer = isHost; // Host is red, guest is black
            this.isMyTurn = isHost; // Red goes first
            
            StatusText.Text = isHost ? "Waiting for opponent to connect..." : "Connecting to opponent...";
            StatusText.Foreground = Brushes.Gray;
            
            if (isHost)
            {
                StartServer();
            }
            else
            {
                ConnectToServer();
            }
        }

        private void UpdateStatusText()
        {
            if (isLocalMultiplayer)
            {
                // Local multiplayer - color-coded turn indicator
                if (isRedPlayer)
                {
                    StatusText.Text = "Red's turn";
                    StatusText.Foreground = Brushes.Red;
                }
                else
                {
                    StatusText.Text = "Black's turn";
                    StatusText.Foreground = Brushes.Black;
                }
            }
            else
            {
                // Network multiplayer
                if (isMyTurn)
                {
                    StatusText.Text = $"Your turn ({(isRedPlayer ? "Red" : "Black")})";
                    StatusText.Foreground = isRedPlayer ? Brushes.Red : Brushes.Black;
                }
                else
                {
                    StatusText.Text = "Opponent's turn...";
                    StatusText.Foreground = Brushes.Gray;
                }
            }
        }

        private void InitializeBoard()
        {
            // Initialize red pieces (bottom)
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        board[row, col] = new CheckersPiece { IsRed = true, IsKing = false, Row = row, Col = col };
                    }
                }
            }
            
            // Initialize black pieces (top)
            for (int row = 5; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        board[row, col] = new CheckersPiece { IsRed = false, IsKing = false, Row = row, Col = col };
                    }
                }
            }
        }

        private void DrawBoard()
        {
            GameCanvas.Children.Clear();
            
            // Draw checkerboard pattern
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Fill = (row + col) % 2 == 0 ? Brushes.BurlyWood : Brushes.SaddleBrown
                    };
                    Canvas.SetLeft(rect, col * CellSize);
                    Canvas.SetTop(rect, row * CellSize);
                    GameCanvas.Children.Add(rect);
                    
                    // Add click handler
                    rect.MouseDown += (s, e) => OnCellClick(row, col);
                }
            }
            
            // Draw pieces
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (board[row, col] != null)
                    {
                        DrawPiece(board[row, col]!);
                    }
                }
            }
        }

        private void DrawPiece(CheckersPiece piece)
        {
            var ellipse = new Ellipse
            {
                Width = CellSize - 10,
                Height = CellSize - 10,
                Fill = piece.IsRed ? Brushes.Red : Brushes.Black,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            
            Canvas.SetLeft(ellipse, piece.Col * CellSize + 5);
            Canvas.SetTop(ellipse, piece.Row * CellSize + 5);
            GameCanvas.Children.Add(ellipse);
            
            ellipse.MouseDown += (s, e) => OnPieceClick(piece);
            
            // Draw king indicator
            if (piece.IsKing)
            {
                var crownText = new TextBlock
                {
                    Text = "♔",
                    FontSize = 40,
                    Foreground = Brushes.Gold,
                    Width = CellSize,
                    Height = CellSize,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(crownText, piece.Col * CellSize);
                Canvas.SetTop(crownText, piece.Row * CellSize + 15);
                GameCanvas.Children.Add(crownText);
            }
        }

        private void OnPieceClick(CheckersPiece piece)
        {
            if (!gameActive) return;
            
            // In local multiplayer, allow any player to move their pieces
            if (isLocalMultiplayer)
            {
                // Red player can only move red pieces, black can only move black
                if ((isRedPlayer && !piece.IsRed) || (!isRedPlayer && piece.IsRed)) return;
            }
            else
            {
                // In network mode, check if it's the player's turn
                if (!isMyTurn || piece.IsRed != isRedPlayer) return;
            }
            
            selectedPiece = piece;
            HighlightValidMoves(piece);
        }

        private void OnCellClick(int row, int col)
        {
            if (!gameActive || selectedPiece == null) return;
            
            // In network mode, check if it's the player's turn
            if (!isLocalMultiplayer && !isMyTurn) return;
            
            // Check if move is valid
            if (IsValidMove(selectedPiece, row, col))
            {
                MakeMove(selectedPiece, row, col);
            }
        }

        private void HighlightValidMoves(CheckersPiece piece)
        {
            // Clear previous highlights
            foreach (var circle in highlightCircles)
            {
                GameCanvas.Children.Remove(circle);
            }
            highlightCircles.Clear();
            
            // Highlight valid moves
            var validMoves = GetValidMoves(piece);
            foreach (var (row, col) in validMoves)
            {
                var highlight = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Fill = Brushes.LightGreen,
                    Opacity = 0.6
                };
                Canvas.SetLeft(highlight, col * CellSize + CellSize / 2 - 15);
                Canvas.SetTop(highlight, row * CellSize + CellSize / 2 - 15);
                GameCanvas.Children.Add(highlight);
                highlightCircles.Add(highlight);
            }
        }

        private List<(int, int)> GetValidMoves(CheckersPiece piece)
        {
            var moves = new List<(int, int)>();
            int direction = piece.IsRed ? 1 : -1;
            
            // Regular moves (forward)
            CheckAndAddMove(piece, piece.Row + direction, piece.Col - 1, moves);
            CheckAndAddMove(piece, piece.Row + direction, piece.Col + 1, moves);
            
            // King moves (can move backward)
            if (piece.IsKing)
            {
                CheckAndAddMove(piece, piece.Row - direction, piece.Col - 1, moves);
                CheckAndAddMove(piece, piece.Row - direction, piece.Col + 1, moves);
            }
            
            // Jump moves
            CheckJumpMove(piece, piece.Row + direction * 2, piece.Col - 2, piece.Row + direction, piece.Col - 1, moves);
            CheckJumpMove(piece, piece.Row + direction * 2, piece.Col + 2, piece.Row + direction, piece.Col + 1, moves);
            
            if (piece.IsKing)
            {
                CheckJumpMove(piece, piece.Row - direction * 2, piece.Col - 2, piece.Row - direction, piece.Col - 1, moves);
                CheckJumpMove(piece, piece.Row - direction * 2, piece.Col + 2, piece.Row - direction, piece.Col + 1, moves);
            }
            
            return moves;
        }

        private void CheckAndAddMove(CheckersPiece piece, int toRow, int toCol, List<(int, int)> moves)
        {
            if (toRow >= 0 && toRow < BoardSize && toCol >= 0 && toCol < BoardSize && board[toRow, toCol] == null)
            {
                moves.Add((toRow, toCol));
            }
        }

        private void CheckJumpMove(CheckersPiece piece, int toRow, int toCol, int jumpRow, int jumpCol, List<(int, int)> moves)
        {
            if (toRow >= 0 && toRow < BoardSize && toCol >= 0 && toCol < BoardSize && 
                board[toRow, toCol] == null && board[jumpRow, jumpCol] != null && 
                board[jumpRow, jumpCol]!.IsRed != piece.IsRed)
            {
                moves.Add((toRow, toCol));
            }
        }

        private bool IsValidMove(CheckersPiece piece, int toRow, int toCol)
        {
            var validMoves = GetValidMoves(piece);
            return validMoves.Contains((toRow, toCol));
        }

        private void MakeMove(CheckersPiece piece, int toRow, int toCol)
        {
            int fromRow = piece.Row;
            int fromCol = piece.Col;
            
            // Check if it's a jump
            bool isJump = Math.Abs(toRow - fromRow) == 2;
            int capturedRow = -1, capturedCol = -1;
            
            if (isJump)
            {
                capturedRow = (fromRow + toRow) / 2;
                capturedCol = (fromCol + toCol) / 2;
                board[capturedRow, capturedCol] = null; // Remove captured piece
            }
            
            // Move piece
            board[fromRow, fromCol] = null;
            piece.Row = toRow;
            piece.Col = toCol;
            board[toRow, toCol] = piece;
            
            // Check for king promotion
            if (!piece.IsKing)
            {
                if ((piece.IsRed && toRow == BoardSize - 1) || (!piece.IsRed && toRow == 0))
                {
                    piece.IsKing = true;
                }
            }
            
            selectedPiece = null;
            DrawBoard();
            
            if (isLocalMultiplayer)
            {
                // Check for win in local multiplayer
                if (CheckWinLocal())
                {
                    string winner = isRedPlayer ? "Red" : "Black";
                    StatusText.Text = $"{winner} player wins! 🎉";
                    gameActive = false;
                    MessageBox.Show($"Congratulations! {winner} player won!", "Victory!", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Switch turns in local multiplayer
                isRedPlayer = !isRedPlayer;
                UpdateStatusText();
            }
            else
            {
                // Send move to opponent in network mode
                var move = new CheckersMove 
                { 
                    FromRow = fromRow, 
                    FromCol = fromCol, 
                    ToRow = toRow, 
                    ToCol = toCol,
                    IsJump = isJump,
                    CapturedRow = capturedRow,
                    CapturedCol = capturedCol,
                    BecameKing = piece.IsKing
                };
                SendMove(move);
                
                // Check for win
                if (CheckWin())
                {
                    StatusText.Text = "You win! 🎉";
                    StatusText.Foreground = Brushes.Green;
                    gameActive = false;
                    ScoreManager.Instance.RecordWin();
                    MessageBox.Show("Congratulations! You won!", "Victory!", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Switch turns
                isMyTurn = false;
                UpdateStatusText();
            }
        }

        private void ProcessOpponentMove(CheckersMove move)
        {
            // Apply opponent's move
            var piece = board[move.FromRow, move.FromCol];
            if (piece != null)
            {
                board[move.FromRow, move.FromCol] = null;
                piece.Row = move.ToRow;
                piece.Col = move.ToCol;
                board[move.ToRow, move.ToCol] = piece;
                
                if (move.IsJump && move.CapturedRow >= 0)
                {
                    board[move.CapturedRow, move.CapturedCol] = null;
                }
                
                if (move.BecameKing)
                {
                    piece.IsKing = true;
                }
                
                DrawBoard();
                
                // Check for opponent win
                if (CheckWin())
                {
                    StatusText.Text = "Opponent wins!";
                    StatusText.Foreground = Brushes.Red;
                    gameActive = false;
                    ScoreManager.Instance.RecordLoss();
                    MessageBox.Show("Opponent won this game!", "Game Over", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Switch turns
                isMyTurn = true;
                UpdateStatusText();
            }
        }

        private bool CheckWinLocal()
        {
            int redPieces = 0, blackPieces = 0;
            
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (board[row, col] != null)
                    {
                        if (board[row, col]!.IsRed) redPieces++;
                        else blackPieces++;
                    }
                }
            }
            
            // Return true if the current player has won (opponent has no pieces)
            return (isRedPlayer && blackPieces == 0) || (!isRedPlayer && redPieces == 0);
        }

        private bool CheckWin()
        {
            int redPieces = 0, blackPieces = 0;
            
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if (board[row, col] != null)
                    {
                        if (board[row, col]!.IsRed) redPieces++;
                        else blackPieces++;
                    }
                }
            }
            
            return (isRedPlayer && blackPieces == 0) || (!isRedPlayer && redPieces == 0);
        }

        private async void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, NetworkManager.Instance.GetGamePort());
                listener.Start();
                
                client = await listener.AcceptTcpClientAsync();
                stream = client.GetStream();
                
                gameActive = true;
                UpdateStatusText();
                
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
                
                gameActive = true;
                UpdateStatusText();
                
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
                        var move = JsonSerializer.Deserialize<CheckersMove>(message);
                        
                        if (move != null)
                        {
                            Dispatcher.Invoke(() => ProcessOpponentMove(move));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => 
                {
                    MessageBox.Show($"Connection lost: {ex.Message}", "Connection Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                });
            }
        }

        private async void SendMove(CheckersMove move)
        {
            try
            {
                if (stream != null)
                {
                    string json = JsonSerializer.Serialize(move);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send move: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            board = new CheckersPiece?[BoardSize, BoardSize];
            InitializeBoard();
            DrawBoard();
            selectedPiece = null;
            
            if (isHost)
            {
                isMyTurn = true;
            }
            else
            {
                isMyTurn = false;
            }
            gameActive = true;
            UpdateStatusText();
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try
            {
                stream?.Close();
                client?.Close();
                listener?.Stop();
            }
            catch { }
        }
    }

    public class CheckersPiece
    {
        public bool IsRed { get; set; }
        public bool IsKing { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public class CheckersMove
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public bool IsJump { get; set; }
        public int CapturedRow { get; set; }
        public int CapturedCol { get; set; }
        public bool BecameKing { get; set; }
    }
}
