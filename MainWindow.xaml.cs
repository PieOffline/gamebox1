using System;
using System.Windows;
using System.Windows.Controls;
using GameBox.Utils;
using GameBox.Games;

namespace GameBox;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        // Set up the score manager
        ScoreManagerInstance = ScoreManager.Instance;
        
        // Display the user's fruit code
        var localIp = NetworkUtils.GetLocalIPAddress();
        UserFruitCode = NetworkUtils.IpToFruitCode(localIp);
        LocalIpAddress = localIp;
        
        // Initial scan for online players
        ScanForPlayers();
    }

    public ScoreManager ScoreManagerInstance { get; set; }
    public string UserFruitCode { get; }
    public string LocalIpAddress { get; }

    // Single Player Game Buttons
    private void PlayPong_Click(object sender, RoutedEventArgs e)
    {
        var pongWindow = new PongGame();
        pongWindow.Show();
    }

    private void PlayConnectDots_Click(object sender, RoutedEventArgs e)
    {
        var connectDotsWindow = new ConnectDotsGame();
        connectDotsWindow.Show();
    }

    private void PlaySnake_Click(object sender, RoutedEventArgs e)
    {
        var snakeWindow = new SnakeGame();
        snakeWindow.Show();
    }

    private void PlayTetris_Click(object sender, RoutedEventArgs e)
    {
        var tetrisWindow = new TetrisGame();
        tetrisWindow.Show();
    }

    private void PlayMemoryMatch_Click(object sender, RoutedEventArgs e)
    {
        var memoryWindow = new MemoryMatchGame();
        memoryWindow.Show();
    }

    private void PlayBreakout_Click(object sender, RoutedEventArgs e)
    {
        var breakoutWindow = new BreakoutGame();
        breakoutWindow.Show();
    }

    private void PlayAsteroids_Click(object sender, RoutedEventArgs e)
    {
        var asteroidsWindow = new AsteroidsGame();
        asteroidsWindow.Show();
    }

    private void PlayMaze_Click(object sender, RoutedEventArgs e)
    {
        var mazeWindow = new MazeGame();
        mazeWindow.Show();
    }

    private void PlaySimon_Click(object sender, RoutedEventArgs e)
    {
        var simonWindow = new SimonGame();
        simonWindow.Show();
    }

    private void Play2048_Click(object sender, RoutedEventArgs e)
    {
        var game2048Window = new Game2048();
        game2048Window.Show();
    }

    private void PlaySolitaire_Click(object sender, RoutedEventArgs e)
    {
        var solitaireWindow = new SolitaireGame();
        solitaireWindow.Show();
    }

    private void PlayMinesweeper_Click(object sender, RoutedEventArgs e)
    {
        var minesweeperWindow = new MinesweeperGame();
        minesweeperWindow.Show();
    }

    private void PlaySudoku_Click(object sender, RoutedEventArgs e)
    {
        var sudokuWindow = new SudokuGame();
        sudokuWindow.Show();
    }

    // Multiplayer Game Buttons
    private void PlayTicTacToe_Click(object sender, RoutedEventArgs e)
    {
        ShowMultiplayerDialog("Tic-Tac-Toe", () => new TicTacToeGame());
    }

    private void PlayRockPaperScissors_Click(object sender, RoutedEventArgs e)
    {
        ShowMultiplayerDialog("Rock Paper Scissors", () => new RockPaperScissorsGame());
    }

    private void PlayCheckers_Click(object sender, RoutedEventArgs e)
    {
        ShowMultiplayerDialog("Checkers", () => new CheckersGame());
    }

    private void PlayTankBattle_Click(object sender, RoutedEventArgs e)
    {
        ShowMultiplayerDialog("Tank Battle", () => new TankBattleGame());
    }

    private void PlayRacing_Click(object sender, RoutedEventArgs e)
    {
        ShowMultiplayerDialog("Racing Game", () => new RacingGame());
    }

    private void ShowMultiplayerDialog(string gameName, Func<Window> gameFactory)
    {
        var dialog = new MultiplayerDialog(gameName, gameFactory);
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void ResetScores_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to reset all scores?", 
            "Reset Scores", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            ScoreManager.Instance.Reset();
        }
    }

    private void DeveloperMenu_Click(object sender, RoutedEventArgs e)
    {
        // Create password dialog
        var passwordDialog = new Window
        {
            Title = "Developer Access",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new TextBlock
        {
            Text = "Enter Developer Password:",
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(label, 0);
        grid.Children.Add(label);

        var passwordBox = new System.Windows.Controls.PasswordBox
        {
            FontSize = 16,
            Padding = new Thickness(10)
        };
        Grid.SetRow(passwordBox, 1);
        grid.Children.Add(passwordBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Grid.SetRow(buttonPanel, 3);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0)
        };
        cancelButton.Click += (s, args) => passwordDialog.Close();
        buttonPanel.Children.Add(cancelButton);

        var okButton = new Button
        {
            Content = "OK",
            Width = 80
        };
        okButton.Click += (s, args) =>
        {
            if (passwordBox.Password == "B3T4")
            {
                passwordDialog.Close();
                var devMenu = new Views.DeveloperMenu();
                devMenu.ShowDialog();
            }
            else
            {
                MessageBox.Show("Incorrect password!", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        buttonPanel.Children.Add(okButton);

        grid.Children.Add(buttonPanel);
        passwordDialog.Content = grid;

        passwordBox.KeyDown += (s, args) =>
        {
            if (args.Key == System.Windows.Input.Key.Enter)
            {
                okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        };

        passwordDialog.ShowDialog();
    }

    private async void ScanForPlayers_Click(object sender, RoutedEventArgs e)
    {
        await ScanForPlayers();
    }

    private async System.Threading.Tasks.Task ScanForPlayers()
    {
        var onlinePlayers = new System.Collections.Generic.List<string>();
        
        // Quick scan - only check first 20 IPs for responsiveness
        await System.Threading.Tasks.Task.Run(() =>
        {
            var networkBase = NetworkUtils.GetNetworkBase();
            var localIp = NetworkUtils.GetLocalIPAddress();
            
            for (int i = 1; i <= 20; i++)
            {
                var ip = $"{networkBase}.{i}";
                if (ip == localIp) continue;
                
                try
                {
                    using var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send(ip, 50); // Very quick timeout
                    
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        var fruitCode = NetworkUtils.IpToFruitCode(ip);
                        Dispatcher.Invoke(() => onlinePlayers.Add(fruitCode));
                    }
                }
                catch { }
            }
        });
        
        if (onlinePlayers.Count == 0)
        {
            onlinePlayers.Add("No players detected");
        }
        
        OnlinePlayersDisplay.ItemsSource = onlinePlayers;
    }
}