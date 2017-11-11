using System.Windows;

using gtavmm_metro.Setup;
using gtavmm_metro.Properties;

namespace gtavmm_metro
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void gtavmm_metro_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.IsFirstLaunch)
            {
                SetupMainWindow setupWindow = new SetupMainWindow();
                setupWindow.Show();
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
        }
    }
}
