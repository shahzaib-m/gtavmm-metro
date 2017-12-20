using System;
using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Models;
using gtavmm_metro.Properties;

namespace gtavmm_metro.Tabs.HomeUCTabs
{
    public partial class OnlineUC : UserControl
    {
        private HomeUC ParentWindow;

        private GTAV GTAV;
        private ProgressDialogController GTAVLaunchProgress;

        public OnlineUC(HomeUC parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }

        private void CollapseGTAOTabSection_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.BlankTabItem.IsSelected = true;
        }

        private void OptionsToggleButton_StraightToFreemode_Click(object sender, RoutedEventArgs e)
        {
            if (this.OptionsToggleButton_StraightToFreemode.IsChecked == true)
            {
                OptionsToggleButton_StraightToFreemode.Content = "Enabled";
            }
            else
            {
                this.OptionsToggleButton_StraightToFreemode.Content = "Disabled";
            }
        }

        private async void GTAOLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);
            this.GTAVLaunchProgress = await metroWindow.ShowProgressAsync("Launching Grand Theft Auto V", "Launching GTA Online with selected options. Please wait...", true);
            this.GTAVLaunchProgress.SetIndeterminate();

            string gamePath = Settings.Default.GTAVDirectory;
            GTAVDRM targetDRM = Settings.Default.IsSteamDRM ? GTAVDRM.Steam : GTAVDRM.Rockstar;

            this.GTAV = new GTAV(gamePath, GTAVMode.Online, targetDRM);
            this.GTAV.GTAVProcessExited += this.GTAVProcessExited;


            GTAOOptions gameOptions = new GTAOOptions
            {
                StraightToFreemode = (bool)this.OptionsToggleButton_StraightToFreemode.IsChecked
            };
            bool optionSetSuccess = await this.GTAV.SetOptions(gameOptions);
            if (!optionSetSuccess)
            {
                MessageDialogResult res = await metroWindow.ShowMessageAsync("Unable to set options",
                    "Could not set launch options. Continue with launch?",
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });

                if (res == MessageDialogResult.Negative)
                {
                    await this.GTAVLaunchProgress.CloseAsync();
                    return;
                }
            }

            bool launchSuccess = this.GTAV.StartGTAO(gameOptions);
            if (launchSuccess)
            {
                this.GTAVLaunchProgress.SetTitle("Grand Theft Auto V Running");
                this.GTAVLaunchProgress.SetMessage("Waiting for GTAV to exit...");
                this.GTAVLaunchProgress.SetCancelable(false);
            }
            else
            {
                await this.GTAVLaunchProgress.CloseAsync();
                await metroWindow.ShowMessageAsync("Failed to launch Grand Theft Auto V", "Couldn't launch GTA Online.", MessageDialogStyle.Affirmative);
            }
        }

        private async void GTAVProcessExited(object sender, EventArgs e)
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetMessage("Grand Theft Auto V Exited - Cleaning up");
            this.GTAVLaunchProgress.SetMessage("Removing options file...");

            bool optionsDelSuccess = this.GTAV.DeleteOptionsFile();
            if (!optionsDelSuccess)
            {
                await metroWindow.ShowMessageAsync("Failed to delete 'commandline.txt'",
                    "Couldn't remove the options file (commandline.txt) from GTAV directory during clean-up");
            }

            await this.GTAVLaunchProgress.CloseAsync();
        }
    }
}
