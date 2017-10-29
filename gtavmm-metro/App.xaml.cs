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
        private void gtavmmapp_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.IsFirstLaunch)   
            {
                SetupMainWindow setupWindow = new SetupMainWindow();
                setupWindow.Show();
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
        }
    }
}
