using System;
using System.ComponentModel;

namespace GameBox.Utils
{
    public class ScoreManager : INotifyPropertyChanged
    {
        private static ScoreManager? _instance;
        private int _multiplayerWins = 0;
        private int _gamesPlayed = 0;

        public static ScoreManager Instance => _instance ??= new ScoreManager();

        public int MultiplayerWins
        {
            get => _multiplayerWins;
            private set
            {
                if (_multiplayerWins != value)
                {
                    _multiplayerWins = value;
                    OnPropertyChanged(nameof(MultiplayerWins));
                    OnPropertyChanged(nameof(WinPercentage));
                }
            }
        }

        public int GamesPlayed
        {
            get => _gamesPlayed;
            private set
            {
                if (_gamesPlayed != value)
                {
                    _gamesPlayed = value;
                    OnPropertyChanged(nameof(GamesPlayed));
                    OnPropertyChanged(nameof(WinPercentage));
                }
            }
        }

        public double WinPercentage => GamesPlayed > 0 ? (double)MultiplayerWins / GamesPlayed * 100 : 0;

        public void RecordWin()
        {
            MultiplayerWins++;
            GamesPlayed++;
        }

        public void RecordLoss()
        {
            GamesPlayed++;
        }

        public void Reset()
        {
            MultiplayerWins = 0;
            GamesPlayed = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}