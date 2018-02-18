using System.IO;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using MahApps.Metro.Controls;

using gtavmm_metro.Models;
using gtavmm_metro.Properties;
using gtavmm_metro.Setup.Pages;

namespace gtavmm_metro.Setup
{
    public partial class SetupMainWindow : MetroWindow
    {
        public WelcomeUC WelcomePage { get; set; }
        public GTAVDirectoryUC GTAVDirectoryPage { get; set; }
        public ModsDirectoryUC ModsDirectoryPage { get; set; }

        public SetupMainWindow()
        {
            InitializeComponent();
        }

        #region Events
        private void Setup_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadUserControls();

            this.SetupContainer.Content = WelcomePage;
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
        #endregion
        private void LoadUserControls()
        {
            this.WelcomePage = new WelcomeUC();
            this.WelcomePage.GoForwardRequested += (s, e) => NextPageRequested(s as UserControl);

            this.GTAVDirectoryPage = new GTAVDirectoryUC();
            this.GTAVDirectoryPage.GoBackRequested += (s, e) => PreviousPageRequested(s as UserControl);
            this.GTAVDirectoryPage.GoForwardRequested += (s, e) => NextPageRequested(s as UserControl);

            this.ModsDirectoryPage = new ModsDirectoryUC();
            this.ModsDirectoryPage.GoBackRequested += (s, e) => PreviousPageRequested(s as UserControl);
            this.ModsDirectoryPage.FinishSetupRequested += async (s, e) => await FinishSetup();
        }

        private void PreviousPageRequested(UserControl senderUC)
        {
            UserControl newPage = null;

            if (senderUC is GTAVDirectoryUC)
                newPage = this.WelcomePage;
            else if (senderUC is ModsDirectoryUC)
                newPage = this.GTAVDirectoryPage;

            if (newPage != null)
                this.SetupContainer.Content = newPage;
        }

        private void NextPageRequested(UserControl senderUC)
        {
            UserControl newPage = null;

            if (senderUC is WelcomeUC)
                newPage = this.GTAVDirectoryPage;
            else if (senderUC is GTAVDirectoryUC)
                newPage = this.ModsDirectoryPage;

            if (newPage != null)
                this.SetupContainer.Content = newPage;
        }

        public async Task FinishSetup()
        {
            Settings.Default.IsFirstLaunch = false;
            Settings.Default.GTAVDirectory = this.GTAVDirectoryPage.GTAVDirectoryConfirmedLocation.FullName;
            Settings.Default.IsSteamDRM = this.GTAVDirectoryPage.IsSteamDRM;
            Settings.Default.ModsDirectory = this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName;
            Settings.Default.Save();

            DBInstance modsDbConnection = new DBInstance(this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName);
            await modsDbConnection.VerifyTablesState();

            ScriptModAPI scriptModAPI = new ScriptModAPI(Path.Combine(this.ModsDirectoryPage.ModsDirectoryConfirmedLocation.FullName, "Script Mods"),
                modsDbConnection);
            if (await scriptModAPI.GetAllScriptMods() == null)
            {
                await scriptModAPI.CreateScriptMod("Script Hook V + ASI Loader", 0, "Script Hook V + ASI Loader © - not included, please download yourself.\nRequired to load most modifications. Should be up-to-date as new GTAV updates are released to ensure compatibility and avoid crashes.", false);
                await scriptModAPI.CreateScriptMod("OpenIV.ASI", 1, "OpenIV.ASI © - not included, please download yourself (usually included with OpenIV ©.)\nRequired to load asset mods (the modified .rpf packages).", false);
            }

            MainWindow mainWindow = new MainWindow(modsDbConnection);
            mainWindow.Show();
            this.Close();
        }
    }
}
