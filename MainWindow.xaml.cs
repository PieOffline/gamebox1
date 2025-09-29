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
    }

    public ScoreManager ScoreManagerInstance { get; }
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
}