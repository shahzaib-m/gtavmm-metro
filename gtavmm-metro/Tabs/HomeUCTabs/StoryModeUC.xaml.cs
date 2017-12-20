using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Models;
using gtavmm_metro.Properties;

namespace gtavmm_metro.Tabs.HomeUCTabs
{
    public partial class StoryModeUC : UserControl
    {
        private HomeUC ParentWindow;
        private ScriptModsUC ScriptModsUserControl;
        private AssetModsUC AssetModsUserControl;

        private GTAV GTAV;
        private ProgressDialogController GTAVLaunchProgress;

        public StoryModeUC(HomeUC parent, ScriptModsUC scriptModsUC, AssetModsUC assetModsUC)
        {
            this.ParentWindow = parent;
            this.ScriptModsUserControl = scriptModsUC;
            this.AssetModsUserControl = assetModsUC;
            InitializeComponent();
            this.DataContext = this;
        }

        private void CollapseGTAVTabSection_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.BlankTabItem.IsSelected = true;
        }

        private void ModsToggleButton_ScriptMods_Click(object sender, RoutedEventArgs e)
        {
            if (ModsToggleButton_ScriptMods.IsChecked == true)
            {
                ModsToggleButton_ScriptMods.Content = "Enabled";
            }
            else
            {
                ModsToggleButton_ScriptMods.Content = "Disabled";
                ModsToggleButton_AssetMods.Content = "Disabled";
                ModsToggleButton_AssetMods.IsChecked = false;
            }
        }

        private void ModsToggleButton_AssetMods_Click(object sender, RoutedEventArgs e)
        {
            if (ModsToggleButton_AssetMods.IsChecked == true)
            {
                ModsToggleButton_AssetMods.Content = "Enabled";
            }
            else
            {
                ModsToggleButton_AssetMods.Content = "Disabled";
            }
        }

        private void OptionsToggleButton_OfflineMode_Click(object sender, RoutedEventArgs e)
        {
            if (OptionsToggleButton_OfflineMode.IsChecked == true)
            {
                OptionsToggleButton_OfflineMode.Content = "Enabled";
            }
            else
            {
                OptionsToggleButton_OfflineMode.Content = "Disabled";
            }
        }

        private async void GTAVLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);
            this.GTAVLaunchProgress = await metroWindow.ShowProgressAsync("Launching Grand Theft Auto V",
                "Launching GTAV Story with selected options. Please wait...");
            this.GTAVLaunchProgress.SetIndeterminate();

            string gamePath = Settings.Default.GTAVDirectory;
            GTAVDRM targetDRM = Settings.Default.IsSteamDRM ? GTAVDRM.Steam : GTAVDRM.Rockstar;

            this.GTAV = new GTAV(gamePath, GTAVMode.Offline, targetDRM);
            this.GTAV.GTAVProcessExited += this.GTAVProcessExited;

            if (this.ModsToggleButton_ScriptMods.IsChecked == true)
            {
                this.GTAVLaunchProgress.SetMessage("Inserting script mods into GTAV Directory...");

                bool insertScriptModSuccess = await Task.Run(() => this.GTAV.InsertScriptMods(
                    this.ScriptModsUserControl.ScriptMods.Where(scriptMod => scriptMod.IsEnabled).ToList()));
                if (!insertScriptModSuccess)
                {
                    MessageDialogResult res = await metroWindow.ShowMessageAsync("Unable to insert a modification into GTAV directory",
                        "Could not move files of a modification over to GTAV directory. Stopping script mod insertion. Continue launch?",
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (res == MessageDialogResult.Negative)
                    {
                        await this.MoveAssetModsBack();
                        await this.GTAVLaunchProgress.CloseAsync();
                        return;
                    }
                }
            }

            if (this.ModsToggleButton_AssetMods.IsChecked == true)
            {
                this.GTAVLaunchProgress.SetMessage("Inserting asset mods into GTAV Directory...");

                bool insertAssetModSuccess = await Task.Run(() => this.GTAV.InsertAssetMods(
                    this.AssetModsUserControl.AssetMods.Where(assetMod => assetMod.IsEnabled).ToList()));
                if (!insertAssetModSuccess)
                {
                    MessageDialogResult res = await metroWindow.ShowMessageAsync("Unable to insert a modification into GTAV directory",
                        "Could not move files of a modification over to GTAV directory. Stopping asset mod insertion. Continue launch?",
                        MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (res == MessageDialogResult.Negative)
                    {
                        await this.MoveScriptModsBack();
                        await this.MoveAssetModsBack();
                        await this.GTAVLaunchProgress.CloseAsync();
                        return;
                    }
                }
            }

            GTAVOptions gameOptions = new GTAVOptions
            {
                OfflineMode = (bool)this.OptionsToggleButton_OfflineMode.IsChecked
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

            bool launchSuccess = this.GTAV.StartGTAV(gameOptions);
            if (launchSuccess)
            {
                this.GTAVLaunchProgress.SetTitle("Grand Theft Auto V Running");
                this.GTAVLaunchProgress.SetMessage("Waiting for GTAV to exit...");
            }
            else
            {
                await this.GTAVLaunchProgress.CloseAsync();
                await metroWindow.ShowMessageAsync("Failed to launch Grand Theft Auto V", "Couldn't launch GTAV Story.", MessageDialogStyle.Affirmative);
            }
        }

        private async void GTAVProcessExited(object sender, EventArgs e)
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetMessage("Grand Theft Auto V Exited - Cleaning up");
            await this.DeleteOptionsFile();

            if (this.Dispatcher.Invoke(() => this.ModsToggleButton_ScriptMods.IsChecked == true))
            {
                await this.MoveScriptModsBack();
            };
            if (this.Dispatcher.Invoke(() => this.ModsToggleButton_AssetMods.IsChecked == true))
            {
                await this.MoveAssetModsBack();
            }

            await this.GTAVLaunchProgress.CloseAsync();
        }

        private async Task DeleteOptionsFile()
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetMessage("Removing options file...");

            bool optionsDelSuccess = this.GTAV.DeleteOptionsFile();
            if (!optionsDelSuccess)
            {
                await metroWindow.ShowMessageAsync("Failed to delete 'commandline.txt'",
                    "Couldn't remove the options file (commandline.txt) from GTAV directory during clean-up");
            }
        }
        private async Task MoveScriptModsBack()
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetMessage("Moving script modifications back...");
            bool moveScriptModsBackSuccess = await Task.Run(() => this.GTAV.MoveScriptModsBack());
            if (!moveScriptModsBackSuccess)
            {
                await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to move back script modifications",
                    "Couldn't move back some of the script modifications from GTAV directory during clean-up"));
            }

            this.GTAVLaunchProgress.SetMessage("Deleting remaining non-game folders with no files...");
            await this.GTAV.DeleteRemainingRootFoldersWithNoFiles();

            this.GTAVLaunchProgress.SetMessage("Checking for non-game unknown remaining and non-empty folders...");
            List<DirectoryInfo> unknownFolders = await this.GTAV.DiscoverUnknownLeftoverNonEmptyFolders();

            if (unknownFolders.Any())
            {
                ScriptMod scriptModForUnknownFolders = await this.ScriptModsUserControl.ScriptModAPI.CreateScriptMod(
                    "Leftover Mod Folders - Review",
                    this.ScriptModsUserControl.ScriptMods.Count - 1,
                    "* The folders inside this modification are leftover non-empty (non-game) folders after GTAV Story with script mods was launched. " +
                        "They have been preserved for review as they may contain files generated by a modification that you would like to keep (by moving these files to the belonging modification). *",
                    false);
                this.Dispatcher.Invoke(() => this.ScriptModsUserControl.ScriptMods.Add(scriptModForUnknownFolders));

                foreach (DirectoryInfo dir in unknownFolders)
                {
                    Directory.Move(dir.FullName,
                        Path.Combine(Settings.Default.ModsDirectory, "Script Mods", scriptModForUnknownFolders.Name, dir.Name));
                }
            }


            this.GTAVLaunchProgress.SetMessage("Checking for non-game unknown remaining files...");
            List<FileInfo> unknownFiles = await this.GTAV.DiscoverUnknownLeftoverFiles();

            if (unknownFiles.Any())
            {
                ScriptMod scriptModForUnknownFiles = await this.ScriptModsUserControl.ScriptModAPI.CreateScriptMod(
                    "Leftover Mod Files - Review",
                    this.ScriptModsUserControl.ScriptMods.Count - 1,
                    "* The files inside this modification are leftover (non-game) files after GTAV Story with script mods was launched. " +
                        "They have been preserved for review as they may contain files generated by a modification that you would like to keep (by moving these folders to the belonging modification). *",
                    false);
                this.Dispatcher.Invoke(() => this.ScriptModsUserControl.ScriptMods.Add(scriptModForUnknownFiles));

                foreach (FileInfo file in unknownFiles)
                {
                    File.Move(file.FullName,
                        Path.Combine(Settings.Default.ModsDirectory, "Script Mods", scriptModForUnknownFiles.Name, file.Name));
                }
            }
        }
        private async Task MoveAssetModsBack()
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetMessage("Moving asset modifications back...");
            bool moveAssetModsBackSuccess = await Task.Run(() => this.GTAV.MoveAssetModsBack());
            if (!moveAssetModsBackSuccess)
            {
                await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to move back asset modifications",
                    "Couldn't move back some of the asset modifications from GTAV directory during clean-up"));
            }

            this.GTAVLaunchProgress.SetMessage("Deleting 'mods' folder inside GTAV directory...");
            bool delModsFolderInGTAVDirSuccess = await Task.Run(() => this.GTAV.DeleteModsFolderInGTAVDirectory());
            if (!delModsFolderInGTAVDirSuccess)
            {
                await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to delete 'mods' folder in GTAV directory",
                    "Couldn't delete 'mods' folder inside GTAV directory. Some files are remaining and preventing deletion"));
            }
        }
    }
}
