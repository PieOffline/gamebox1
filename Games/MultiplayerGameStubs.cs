using System.Windows;

namespace GameBox.Games
{
    // Placeholder stub classes for multiplayer games
    public partial class RockPaperScissorsGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;

        public RockPaperScissorsGame() 
        { 
            InitializeComponent();
            Title = "Rock Paper Scissors Online";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "✂️ Rock Paper Scissors Online\n\nComing Soon!\n\nPlay RPS with a friend over LAN.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
        }
    }

    public partial class CheckersGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;

        public CheckersGame() 
        { 
            InitializeComponent();
            Title = "Checkers Online";
            Width = 600;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "♔ Checkers Online\n\nComing Soon!\n\nPlay Checkers with a friend over LAN.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
        }
    }

    public partial class TankBattleGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;

        public TankBattleGame() 
        { 
            InitializeComponent();
            Title = "Tank Battle Online";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🚗 Tank Battle Online\n\nComing Soon!\n\nBattle tanks with a friend over LAN.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
        }
    }

    public partial class RacingGame : Window, IMultiplayerGame
    {
        private string opponentIp = "";
        private bool isHost = false;

        public RacingGame() 
        { 
            InitializeComponent();
            Title = "Racing Game Online";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content = new System.Windows.Controls.TextBlock 
            { 
                Text = "🏁 Racing Game Online\n\nComing Soon!\n\nRace cars with a friend over LAN.", 
                FontSize = 18, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
        }

        public void SetOpponent(string opponentIp, bool isHost)
        {
            this.opponentIp = opponentIp;
            this.isHost = isHost;
        }
    }
}