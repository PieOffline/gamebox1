using System.Windows;

namespace GameBox.Games
{
    // Placeholder stub classes for remaining single-player games
    public partial class TetrisGame : Window
    {
        public TetrisGame() 
        { 
            InitializeComponent();
            Title = "Tetris Game";
            Width = 500;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🧩 Tetris Game\n\nComing Soon!\n\nThis will be the classic block-falling puzzle game.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }

    public partial class MemoryMatchGame : Window
    {
        public MemoryMatchGame() 
        { 
            InitializeComponent();
            Title = "Memory Match Game";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🃏 Memory Match Game\n\nComing Soon!\n\nFlip cards to find matching pairs.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }

    public partial class BreakoutGame : Window
    {
        public BreakoutGame() 
        { 
            InitializeComponent();
            Title = "Breakout Game";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🧱 Breakout Game\n\nComing Soon!\n\nBreak bricks with your ball and paddle.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }

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

    public partial class SimonGame : Window
    {
        public SimonGame() 
        { 
            InitializeComponent();
            Title = "Simon Says Game";
            Width = 500;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🎵 Simon Says Game\n\nComing Soon!\n\nRepeat the sequence of colors and sounds.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }

    public partial class Game2048 : Window
    {
        public Game2048() 
        { 
            InitializeComponent();
            Title = "2048 Game";
            Width = 500;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🔢 2048 Game\n\nComing Soon!\n\nCombine numbered tiles to reach 2048.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }
    }
}