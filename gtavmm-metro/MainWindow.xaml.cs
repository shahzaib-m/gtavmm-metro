using System;
using System.ComponentModel;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

using MahApps.Metro;
using MahApps.Metro.Controls;

using gtavmm_metro.Tabs;
using gtavmm_metro.Properties;
using gtavmm_metro.Models;

namespace gtavmm_metro
{
    public partial class MainWindow : MetroWindow
    {
        private HomeUC HomeUserControl;
        private ScriptModsUC ScriptModsUserControl;
        private AssetModsUC AssetModsUserControl;
        private AboutUC AboutUserControl;

        private DBInstance ModsDbConnection = null;

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow = this;

            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);        // temp
            this.Icon = new BitmapImage(new Uri(String.Format("pack://application:,,,/Assets/Icons/{0}.ico", appStyle.Item2.Name)));   // temp
        }
        public MainWindow(DBInstance existingModsDbConnection)
        {
            InitializeComponent();
            Application.Current.MainWindow = this;
            this.ModsDbConnection = existingModsDbConnection;

            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);        // temp
            this.Icon = new BitmapImage(new Uri(String.Format("pack://application:,,,/Assets/Icons/{0}.ico", appStyle.Item2.Name)));   // temp
        }

        #region MainWindow Events
        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            await this.Init();   // initialize once the window is rendered
        }

        private void _this_Closing(object sender, CancelEventArgs e)
        {
            // Cleanup, application is being closed, implement
            Settings.Default.Save();
        }
        #endregion

        #region My Methods
        private async Task Init()
        {
            if (this.ModsDbConnection == null) { await this.CreatePersistentDbConnection(); }
            this.AssignUCToTabs();
            this.PreloadIntensiveTabs();

            this.UserInteractionStartNow();    // enable the UI for the user when tasks finished.
        }

        private async Task CreatePersistentDbConnection()
        {
            await Task.Run(() => this.ModsDbConnection = new DBInstance(Settings.Default.ModsDirectory));
        }

        /// <summary>
        /// This method assigns the appropriate usercontrols for all the tabitems in this window.
        /// </summary>
        private void AssignUCToTabs()
        {
            // Assigning ScriptMods UserControl to Script Mods tab
            this.ScriptModsUserControl = new ScriptModsUC();
            this.ScriptModsUserControl.LoadScriptMods(this.ModsDbConnection);
            this.ScriptModsTabItem.Content = this.ScriptModsUserControl;

            // Assigning AssetMods UserControl to Asset Mods tab
            this.AssetModsUserControl = new AssetModsUC();
            this.AssetModsUserControl.LoadAssetMods(this.ModsDbConnection);
            this.AssetModsTabItem.Content = this.AssetModsUserControl;

            // Assigning Home UserControl to Home tab
            this.HomeUserControl = new HomeUC(this.ScriptModsUserControl, this.AssetModsUserControl);
            this.HomeTabItem.Content = this.HomeUserControl;

            // Assigning About UserControl to About tab
            this.AboutUserControl = new AboutUC();
            this.AboutTabItem.Content = this.AboutUserControl;
        }

        private void PreloadIntensiveTabs()
        {
            this.MainTabControl.SelectedIndex = 1;
            this.MainTabControl.UpdateLayout();

            this.MainTabControl.SelectedIndex = 2;
            this.MainTabControl.UpdateLayout();

            this.MainTabControl.SelectedIndex = 0;
        }

        /// <summary>
        /// This method should be called after all initial tasks are done and user interaction should now begin.
        /// </summary>
        private void UserInteractionStartNow()
        {
            this.MainTabControl.IsEnabled = true;

            DoubleAnimation smoothFadeIn = new DoubleAnimation(0.0, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
            this.MainTabControl.BeginAnimation(OpacityProperty, smoothFadeIn);

            this.MainProgressRing.IsActive = false;
        }
        #endregion
    }
}
