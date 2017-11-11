using System;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

using MahApps.Metro;
using MahApps.Metro.Controls;

using gtavmm_metro.Tabs;
using gtavmm_metro.Properties;

namespace gtavmm_metro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private HomeUC HomeUserControl;
        private ScriptModsUC ScriptModsUserControl;
        private AboutUC AboutUserControl;

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow = this;

            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);        // temp
            this.Icon = new BitmapImage(new Uri(String.Format("pack://application:,,,/Assets/Icons/{0}.ico", appStyle.Item2.Name)));   // temp
        }


        #region MainWindow Events
        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            await this.Init();   // initialize once the window is rendered
        }

        private void _this_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cleanup, application is being closed, implement
            Settings.Default.Save();
        }
        #endregion

        #region My Methods
        /// <summary>
        /// This method performs the initial tasks after the window is loaded and the user is presented with a ProgressRing.
        /// </summary>
        /// <returns></returns>
        private async Task Init()
        {
            await Task.Delay(1000);     // temp substitute for work delay
            await Task.Run(() => this.AssignUCToTabs());

            await Task.Run(() => this.UserInteractionStartNow());    // enable the UI for the user when tasks finished.
        }

        /// <summary>
        /// This method assigns the appropriate usercontrols for all the tabitems in this window.
        /// </summary>
        private void AssignUCToTabs()
        {
            this.Dispatcher.Invoke(() => // needed as window elements are being modified from a non-main thread
            {
                // Assigning ScriptMods UserControl to Script Mods tab
                this.HomeUserControl = new HomeUC();
                this.HomeTabItem.Content = this.HomeUserControl;

                // Assigning ScriptMods UserControl to Script Mods tab
                this.ScriptModsUserControl = new ScriptModsUC();
                this.ScriptModsTabItem.Content = this.ScriptModsUserControl;

                // Assigning About UserControl to About tab
                this.AboutUserControl = new AboutUC();
                this.AboutTabItem.Content = this.AboutUserControl;
            });
        }

        /// <summary>
        /// This method should be called after all initial tasks are done and user interaction should now begin.
        /// </summary>
        private void UserInteractionStartNow()
        {
            this.Dispatcher.Invoke(() =>    // needed as window elements are being modified from a non-main thread
            {
                this.MainTabControl.IsEnabled = true;

                DoubleAnimation smoothFadeIn = new DoubleAnimation(0.3, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                this.MainTabControl.BeginAnimation(OpacityProperty, smoothFadeIn);

                this.MainProgressRing.IsActive = false;
            });
        }
        #endregion
    }
}
