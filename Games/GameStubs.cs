using System.Windows;

namespace GameBox.Games
{
    // Placeholder stub classes for remaining single-player games
    public partial class AsteroidsGame : Window
    {
        public AsteroidsGame() 
        { 
            InitializeComponent();
            Title = "Asteroids Game";
            Width = 700;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🌌 Asteroids Game\n\nComing Soon!\n\nFly your spaceship and destroy asteroids.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }

    public partial class MazeGame : Window
    {
        public MazeGame() 
        { 
            InitializeComponent();
            Title = "Maze Runner Game";
            Width = 600;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🌀 Maze Runner Game\n\nComing Soon!\n\nNavigate through randomly generated mazes.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }

}