using System.Windows;
using GameBox.Utils;

namespace GameBox.Views
{
    public partial class GameRequestDialog : Window
    {
        private readonly GameRequest _request;

        public GameRequestDialog(GameRequest request)
        {
            InitializeComponent();
            _request = request;
            
            MessageText.Text = $"{_request.SenderCode} ({_request.SenderIp})\n" +
                             $"wants to play {_request.GameName} with you!";
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
