using System;
using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Models;
using gtavmm_metro.AppSettings;

namespace gtavmm_metro.Tabs.HomeUCTabs
{
    public partial class OnlineUC : UserControl
    {
        public event EventHandler TabCollapseRequested;

        private GTAV GTAV;
        private ProgressDialogController GTAVLaunchProgress;

        public OnlineUC()
        {
            InitializeComponent();

            this.LoadStateSettings();
        }

        private void LoadStateSettings()
        {
            this.OptionsToggleButton_StraightToFreemode.IsChecked = SettingsHandler.GTAOOptionsStraightIntoFreemode_IsChecked;
            this.OptionsToggleButton_StraightToFreemode_Click(this, null);
        }

        private void CollapseGTAOTabSection_Click(object sender, RoutedEventArgs e) => TabCollapseRequested?.Invoke(this, null);

        private void OptionsToggleButton_StraightToFreemode_Click(object sender, RoutedEventArgs e)
        {
            if (this.OptionsToggleButton_StraightToFreemode.IsChecked == true)
            {
                OptionsToggleButton_StraightToFreemode.Content = "Enabled";
                SettingsHandler.GTAOOptionsStraightIntoFreemode_IsChecked = true;
            }
            else
            {
                this.OptionsToggleButton_StraightToFreemode.Content = "Disabled";
                SettingsHandler.GTAOOptionsStraightIntoFreemode_IsChecked = false;
            }
        }

        private async void GTAOLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);
            this.GTAVLaunchProgress = await metroWindow.ShowProgressAsync("Launching Grand Theft Auto V", "Launching GTA Online with selected options. Please wait...", true);
            this.GTAVLaunchProgress.SetIndeterminate();

            string gamePath = SettingsHandler.GTAVDirectory;
            GTAVDRM targetDRM = SettingsHandler.IsSteamDRM ? GTAVDRM.Steam : GTAVDRM.Rockstar;

            this.GTAV = new GTAV(gamePath, GTAVMode.Online, targetDRM);
            this.GTAV.GTAVExited += this.GTAVProcessExited;


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
                this.GTAV.GTAVStarted += this.GTAVProcessStarted;

                this.GTAVLaunchProgress.SetTitle("Waiting for Grand Theft Auto V to launch");
                this.GTAVLaunchProgress.SetMessage("Waiting for Grand Theft Auto V to launch...");

                this.GTAVLaunchProgress.SetCancelable(true);
                this.GTAVLaunchProgress.Canceled += this.GTAVCancelWaitLaunch;
            }
            else
            {
                await this.GTAVLaunchProgress.CloseAsync();
                await metroWindow.ShowMessageAsync("Failed to launch Grand Theft Auto V", "Couldn't launch GTA Online. Please double check GTAV's directory/copy under Settings.", MessageDialogStyle.Affirmative);
            }
        }

        private void GTAVProcessStarted(object sender, EventArgs e)
        {
            this.GTAV.GTAVExited += this.GTAVProcessExited;
            this.GTAVLaunchProgress.SetCancelable(false);

            this.GTAVLaunchProgress.SetTitle("Grand Theft Auto V Running");
            this.GTAVLaunchProgress.SetMessage("Waiting for GTAV to exit...");

        }
        private void GTAVCancelWaitLaunch(object sender, EventArgs e)
        {
            this.GTAVLaunchProgress.SetCancelable(false);
            this.GTAV.GTAVStarted -= this.GTAVProcessExited;
            this.GTAV.GTAVExited -= this.GTAVProcessExited;

            this.GTAVLaunchProgress.SetTitle("Cancelling Grand Theft Auto V launch");
            this.GTAVLaunchProgress.SetMessage("Cancelling GTAV launch and cleaning up...");

            this.GTAV.CancelGTALaunch();
            this.GTAVProcessExited(this, null);
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
