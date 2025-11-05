using System.Configuration;
using System.Data;
using System.Windows;
using GameBox.Utils;

namespace GameBox;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize developer command manager (starts listening on port 42422)
        _ = DevCommandManager.Instance;
        
        // Create and show the main window
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }
}