using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

using MahApps.Metro.Controls;

using gtavmm_metro.Tabs;

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

        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            await this.Init();   // initialize once the window is rendered
        }

        private async Task Init()
        {
            // perform all tasks (check files, load mods, etc.) here while the user is shown a progress ring
            await Task.Delay(1000);     // temp substitute for work delay
            await Task.Run(() => AssignUCToTabs());

            await Task.Run(() => this.UserInteractionStartNow());    // enable the UI for the user when tasks finished.
        }

        private void AssignUCToTabs()
        {
            this.Dispatcher.Invoke(() => // needed as window control is being modified from a non-main thread
            {
                // Assigning ScriptMods UserControl to Script Mods tab
                this.ScriptModsUserControl = new ScriptModsUC();
                this.ScriptModsTabItem.Content = this.ScriptModsUserControl;

                // Assigning About UserControl to About tab
                this.AboutUserControl = new AboutUC();
                this.AboutTabItem.Content = this.AboutUserControl;
            });
        }

        private void UserInteractionStartNow()
        {
            this.Dispatcher.Invoke(() =>    // needed as window element is being modified from a non-main thread
            {
                this.MainTabControl.IsEnabled = true;

                DoubleAnimation smooth_fadein = new DoubleAnimation(0.3, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                this.MainTabControl.BeginAnimation(OpacityProperty, smooth_fadein);

                this.MainProgressRing.IsActive = false;
            });
        }
    }
}
