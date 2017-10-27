using System;
using System.IO;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Media.Animation;

using MahApps.Metro.Controls;

using gtavmm_metro.Tabs;
using gtavmm_metro.Models;
using gtavmm_metro.Properties;

namespace gtavmm_metro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private ScriptModsUC ScriptModsUserControl;
        private AboutUC AboutUserControl;

        public MainWindow()
        {
            InitializeComponent();
        }


        #region MainWindow Events
        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            // perform all tasks (check files, load mods, etc.) here while the user is shown a progress ring
            if (Settings.Default.IsFirstLaunch) // test check, implement proper first time setup TODO
            {
                // temp ----------------------------------- fix this
                MessageBox.Show("First launch");    
                Directory.CreateDirectory("ScriptMods");

                for (int i = 0; i <= 4; i++)
                {
                    ScriptMod.CreateScriptMod(@"ScriptMods", "GFX Enhancements", "Reshade Profile 1 (Test)"); // temp dir
                    ScriptMod.CreateScriptMod(@"ScriptMods", "Turbo Boost", "Turbo boost mod for vehicles (Test)", false); // temp dir
                }
                // ---------------------------------------- fix this

                Settings.Default.IsFirstLaunch = false;
            }

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

                DoubleAnimation smooth_fadein = new DoubleAnimation(0.3, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                this.MainTabControl.BeginAnimation(OpacityProperty, smooth_fadein);

                this.MainProgressRing.IsActive = false;
            });
        }
        #endregion
    }
}
