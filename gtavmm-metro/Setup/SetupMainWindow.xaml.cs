using System;
using System.Data.SQLite;

using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

using MahApps.Metro.Controls;

using gtavmm_metro.Models;
using gtavmm_metro.Properties;
using gtavmm_metro.Setup.Pages;


namespace gtavmm_metro.Setup
{
    /// <summary>
    /// Interaction logic for SetupMainWindow.xaml
    /// </summary>
    public partial class SetupMainWindow : MetroWindow
    {
        public Welcome WelcomePage { get; set; }
        public GTAVDirectory GTAVDirectoryPage { get; set; }
        public ModsDirectory ModsDirectoryPage { get; set; }

        public SetupMainWindow()
        { 
            InitializeComponent();
        }

        private async void Setup_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => LoadUserControls());

            this.SetupContainer.Content = WelcomePage;
        }

        private void LoadUserControls()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.WelcomePage = new Welcome(this);
                this.GTAVDirectoryPage = new GTAVDirectory(this);
                this.ModsDirectoryPage = new ModsDirectory(this);
            });
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        public async void FinishSetup()
        {
            Settings.Default.IsFirstLaunch = false;
            Settings.Default.GTAVDirectory = this.GTAVDirectoryPage.GTAVDirectoryConfirmedLocation.FullName;
            Settings.Default.IsSteamDRM = this.GTAVDirectoryPage.IsSteamDRM;
            Settings.Default.ModsDirectory = this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName;
            Settings.Default.Save();

            ScriptModAPI scriptModAPI = new ScriptModAPI(this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName, new DBInstance(this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName));
            if (await scriptModAPI.GetAllScriptMods() == null)
            {
                await scriptModAPI.CreateScriptMod("Script Hook V + ASI Loader", 0, "Script Hook V + ASI Loader © - not included, please download yourself. Required to load most modifications (NOT for GTA Online). Should be up-to-date as new GTAV updates are released to ensure compatibility and avoid crashes.", false);
                await scriptModAPI.CreateScriptMod("OpenIV.ASI", 1, "OpenIV.ASI © - not included, please download yourself (usually included with OpenIV ©.) Required to load asset mods (NOT for GTA Online), the modified .rpf files that go inside the \"mods\" folder in the GTAV directory instead of the original files.", false);
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
