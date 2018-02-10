using System.IO;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Input;

using MahApps.Metro.Controls;

using gtavmm_metro.Models;
using gtavmm_metro.Properties;
using gtavmm_metro.Setup.Pages;

namespace gtavmm_metro.Setup
{
    public partial class SetupMainWindow : MetroWindow
    {
        public Welcome WelcomePage { get; set; }
        public GTAVDirectory GTAVDirectoryPage { get; set; }
        public ModsDirectory ModsDirectoryPage { get; set; }

        public SetupMainWindow()
        {
            InitializeComponent();
        }

        private void Setup_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadUserControls();

            this.SetupContainer.Content = WelcomePage;
        }

        private void LoadUserControls()
        {
            this.WelcomePage = new Welcome(this);
            this.GTAVDirectoryPage = new GTAVDirectory(this);
            this.ModsDirectoryPage = new ModsDirectory(this);
        }

        /// <summary>
        /// Event to move around window (no border);
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) { this.DragMove(); }
        }

        public async Task FinishSetup()
        {
            Settings.Default.IsFirstLaunch = false;
            Settings.Default.GTAVDirectory = this.GTAVDirectoryPage.GTAVDirectoryConfirmedLocation.FullName;
            Settings.Default.IsSteamDRM = this.GTAVDirectoryPage.IsSteamDRM;
            Settings.Default.ModsDirectory = this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName;
            Settings.Default.Save();

            DBInstance modsDbConnection = new DBInstance(this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName);
            ScriptModAPI scriptModAPI = new ScriptModAPI(Path.Combine(this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName, "Script Mods"),
                modsDbConnection);
            if (await scriptModAPI.GetAllScriptMods() == null)
            {
                await scriptModAPI.CreateScriptMod("Script Hook V + ASI Loader", 0, "Script Hook V + ASI Loader © - not included, please download yourself.\nRequired to load most modifications. Should be up-to-date as new GTAV updates are released to ensure compatibility and avoid crashes.", false);
                await scriptModAPI.CreateScriptMod("OpenIV.ASI", 1, "OpenIV.ASI © - not included, please download yourself (usually included with OpenIV ©.)\nRequired to load asset mods, the modified .rpf files that go inside the \"mods\" folder in the GTAV directory.", false);
            }

            MainWindow mainWindow = new MainWindow(modsDbConnection);
            mainWindow.Show();
            this.Close();
        }
    }
}
