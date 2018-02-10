using System.Threading;

using System.Windows;
using System.Windows.Threading;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Setup;
using gtavmm_metro.Properties;

namespace gtavmm_metro
{
    public partial class App : Application
    {
        private Mutex AppInstanceMutex = null;

        /// <summary>
        /// Before startup, create instance Mutex and check if it already exists.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            bool isNewInstance = true;
            AppInstanceMutex = new Mutex(true, "GTAVModManagerMetroMutex", out isNewInstance);
            if (!isNewInstance)
            {
                AppInstanceMutex = null;
                MessageBox.Show("GTAV Mod Manager Metro is already running.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
            }

            base.OnStartup(e);
        }
        /// <summary>
        /// Before exit, if Mutex exists then release it.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            if (AppInstanceMutex != null)
                AppInstanceMutex.ReleaseMutex();

            base.OnExit(e);
        }

        private void gtavmm_metro_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.IsFirstLaunch)
            {
                SetupMainWindow setupWindow = new SetupMainWindow();
                setupWindow.Show();
            }
            else
            {
                MainWindow mainWindow = null;

                if (e.Args.Length != 0)
                {
                    bool updateFail = false;
                    bool cleanupFail = false;

                    for (int i = 0; i < e.Args.Length; i++)
                    {
                        switch (e.Args[i])
                        {
                            case "--updateFail":
                                updateFail = true;
                                break;
                            case "--cleanupFail":
                                cleanupFail = true;
                                break;
                        }

                        MainWindow = new MainWindow(updateFail, cleanupFail);
                    }
                }
                else { mainWindow = new MainWindow(); }

                Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
        }

        private void gtavmm_metro_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MetroWindow mainWindow = Current.MainWindow as MetroWindow;
            mainWindow.ShowMessageAsync("Unknown Exception Occured",
                "An unhandled exception has just occured: " + e.Exception.Message, MessageDialogStyle.Affirmative);
            // e.Handled = true;    allow after most exception handling and logging is implemented
        }
    }
}
