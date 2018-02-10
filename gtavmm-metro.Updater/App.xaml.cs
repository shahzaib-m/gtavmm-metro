using System.Windows;

namespace gtavmm_metro.Updater
{
    public partial class App : Application
    {
        private void Updater_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length != 0)
            {
                CommandlineHandler cmdHandler = new CommandlineHandler();
                cmdHandler.ProcessArgs(e.Args);
                cmdHandler.CloseOldExecutableProcess();

                bool replaceOldExeResult = cmdHandler.ReplaceOldExecutableWithNew();
                bool tryDeleteTempDir = cmdHandler.TryDeleteTempDirectory();
                bool processStarted = cmdHandler.StartOldProcess(replaceOldExeResult, tryDeleteTempDir);
                if (!processStarted)
                {
                    MessageBox.Show("Couldn't automatically start GTAV Mod Manager Metro after update. Please start it manually.", "Sorry");
                }

                Current.Shutdown();
            }
            else
            {
                // 
                Current.Shutdown();
            }
        }
    }
}
