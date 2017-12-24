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

        private bool IsScriptModsInserted = false;
        private List<ScriptMod> InsertedScriptMods = new List<ScriptMod>();
        private bool IsAssetModsInserted = false;
        private List<AssetMod> InsertedAssetMods = new List<AssetMod>();

        public StoryModeUC(HomeUC parent, ScriptModsUC scriptModsUC, AssetModsUC assetModsUC)
        {
            this.ParentWindow = parent;
            this.ScriptModsUserControl = scriptModsUC;
            this.AssetModsUserControl = assetModsUC;

            InitializeComponent();
            this.DataContext = this;

            // if user has asked for these states to be saved (in Settings?)
            if (true)
            {
                this.ModsToggleButton_ScriptMods.IsChecked = Settings.Default.GTAVModsScriptMods_IsChecked;
                this.ModsToggleButton_ScriptMods_Click(this, null);

                this.ModsToggleButton_AssetMods.IsChecked = Settings.Default.GTAVModsAssetMods_IsChecked;
                this.ModsToggleButton_AssetMods_Click(this, null);
            }
            if (true)
            {
                this.OptionsToggleButton_OfflineMode.IsChecked = Settings.Default.GTAVOptionsOfflineMode_IsChecked;
                this.OptionsToggleButton_OfflineMode_Click(this, null);
            }
            //
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

            if (this.ModsToggleButton_ScriptMods.IsChecked == true)
            {
                IList<ScriptMod> enabledScriptMods = this.ScriptModsUserControl.ScriptMods.Where(scriptMod => scriptMod.IsEnabled).ToList();
                if (enabledScriptMods.Any())
                {
                    this.IsScriptModsInserted = true;

                    this.GTAVLaunchProgress.SetTitle("Inserting script mods");
                    this.GTAVLaunchProgress.SetMessage("...");

                    foreach(ScriptMod scriptMod in enabledScriptMods.Reverse()) // modifications with lower GUI index pushed to end of list,
                                                                                // higher priority file replace
                    {
                        this.GTAVLaunchProgress.SetMessage(scriptMod.Name + " - inserting...");
                        bool currScriptModInsertSuccess = await Task.Run(() => this.GTAV.InsertScriptMod(scriptMod));
                        if (!currScriptModInsertSuccess)
                        {
                            MessageDialogResult res = await metroWindow.ShowMessageAsync("Unable to insert a script mod into GTAV directory",
                                "Could not move files of '" + scriptMod.Name + "' over to GTAV directory. Skipping this mod. Continue launch?",
                                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                            if (res == MessageDialogResult.Negative)
                            {
                                await this.MoveAssetModsBack();
                                await this.GTAVLaunchProgress.CloseAsync();
                                return;
                            }
                        }
                        else
                        {
                            scriptMod.IsInserted = true;
                            await this.ScriptModsUserControl.ScriptModAPI.UpdateScriptModIsInserted(scriptMod.Id, true);
                            await this.ScriptModsUserControl.ScriptModAPI.UpdateScriptModFilesWithPath(scriptMod.Id, scriptMod.FilesWithPath);

                            this.InsertedScriptMods.Add(scriptMod);
                        }
                    }
                }
            }

            if (this.ModsToggleButton_AssetMods.IsChecked == true)
            {
                IList<AssetMod> enabledAssetMods = this.AssetModsUserControl.AssetMods.Where(assetMod => assetMod.IsEnabled).ToList();
                if (enabledAssetMods.Any())
                {
                    this.IsAssetModsInserted = true;

                    this.GTAVLaunchProgress.SetTitle("Inserting asset mods");
                    this.GTAVLaunchProgress.SetMessage("...");

                    foreach(AssetMod assetMod in enabledAssetMods)
                    {
                        this.GTAVLaunchProgress.SetMessage(assetMod.Name + " - inserting");
                        bool currAssetModInsertSuccess = await Task.Run(() => this.GTAV.InsertAssetMod(assetMod));
                        if (!currAssetModInsertSuccess)
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
                        else
                        {
                            assetMod.IsInserted = true;
                            await this.AssetModsUserControl.AssetModAPI.UpdateAssetModIsInserted(assetMod.Id, true);

                            this.InsertedAssetMods.Add(assetMod);
                        }
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
                this.GTAV.GTAVProcessStarted += this.GTAVProcessStarted;

                this.GTAVLaunchProgress.SetTitle("Waiting for Grand Theft Auto V to launch");
                this.GTAVLaunchProgress.SetMessage("Waiting for Grand Theft Auto V to launch...");

                this.GTAVLaunchProgress.SetCancelable(true);
                this.GTAVLaunchProgress.Canceled += this.GTAVCancelWaitLaunch;
            }
            else
            {
                this.GTAVProcessExited(this, null);

                await metroWindow.ShowMessageAsync("Failed to launch Grand Theft Auto V", "Couldn't launch GTAV Story.", MessageDialogStyle.Affirmative);
            }
        }

        private void GTAVProcessStarted(object sender, EventArgs e)
        {
            this.GTAV.GTAVProcessExited += this.GTAVProcessExited;
            this.GTAVLaunchProgress.SetCancelable(false);

            this.GTAVLaunchProgress.SetTitle("Grand Theft Auto V Running");
            this.GTAVLaunchProgress.SetMessage("Waiting for GTAV to exit...");
            
        }
        private void GTAVCancelWaitLaunch(object sender, EventArgs e)
        {
            this.GTAVLaunchProgress.SetCancelable(false);
            
            this.GTAVLaunchProgress.SetTitle("Cancelling Grand Theft Auto V launch");
            this.GTAVLaunchProgress.SetMessage("Cancelling GTAV launch and cleaning up...");

            this.GTAV.CancelGTALaunch();
            this.GTAVProcessExited(this, null);
        }
        private async void GTAVProcessExited(object sender, EventArgs e)
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetTitle("Grand Theft Auto V Exited - Cleaning up");
            await this.DeleteOptionsFile();

            if (this.IsScriptModsInserted) { await this.MoveScriptModsBack(); this.IsScriptModsInserted = false; };
            if (this.IsAssetModsInserted) { await this.MoveAssetModsBack(); this.IsAssetModsInserted = false; }

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
                await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to delete 'commandline.txt'",
                    "Couldn't remove the options file (commandline.txt) from GTAV directory during clean-up"));
            }
        }
        private async Task MoveScriptModsBack()
        {
            MetroWindow metroWindow = null;
            this.Dispatcher.Invoke(() => metroWindow = (Application.Current.MainWindow as MetroWindow));

            this.GTAVLaunchProgress.SetTitle("Moving script modifications back");
            this.GTAVLaunchProgress.SetMessage("...");
            this.InsertedScriptMods.Reverse();
            foreach (ScriptMod scriptMod in this.InsertedScriptMods)
            {
                this.GTAVLaunchProgress.SetMessage(scriptMod.Name + " - moving back...");
                bool currScriptModMoveBackSuccess = await Task.Run(() => this.GTAV.MoveScriptModBack(scriptMod));
                if (!currScriptModMoveBackSuccess)
                {
                    await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to move back script modification files",
                        "Couldn't move back some files belonging to '" + scriptMod.Name + "' from GTAV directory during clean-up." +
                        " You may have to manually go to GTAV's folder to find these files and put them back in this modification."));
                }

                scriptMod.IsInserted = false;
                await this.ScriptModsUserControl.ScriptModAPI.UpdateScriptModIsInserted(scriptMod.Id, false);
            }
            this.InsertedScriptMods.Clear();
            

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
                        "They have been preserved for review as they may contain files generated by a modification that you would like to keep (by moving these folders to the belonging modification). *",
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
                        "They have been preserved for review as they may contain files generated by a modification that you would like to keep (by moving these files to the belonging modification). *",
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

            this.GTAVLaunchProgress.SetTitle("Moving asset modifications back");
            this.GTAVLaunchProgress.SetMessage("...");

            foreach(AssetMod assetMod in this.InsertedAssetMods)
            {
                this.GTAVLaunchProgress.SetMessage(assetMod.Name + " - moving back...");
                bool currAssetModMoveBackSuccess = await Task.Run(() => this.GTAV.MoveAssetModBack(assetMod));
                if (!currAssetModMoveBackSuccess)
                {
                    await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to move back asset modification",
                        "Couldn't automatically move back '" + assetMod.Name + "' (target: '" + assetMod.TargetRPF + "') from GTAV directory during clean-up"));
                }

                assetMod.IsInserted = false;
                await this.AssetModsUserControl.AssetModAPI.UpdateAssetModIsInserted(assetMod.Id, false);
            }
            this.InsertedAssetMods.Clear();

            this.GTAVLaunchProgress.SetMessage("Deleting 'mods' folder inside GTAV directory...");
            bool delModsFolderInGTAVDirSuccess = await Task.Run(() => this.GTAV.DeleteModsFolderInGTAVDirectory());
            if (!delModsFolderInGTAVDirSuccess)
            {
                await this.Dispatcher.Invoke(async () => await metroWindow.ShowMessageAsync("Failed to delete 'mods' folder in GTAV directory",
                    "Couldn't delete 'mods' folder inside GTAV directory. Some files are remaining and preventing deletion"));
            }
        }

        public void SaveState()
        {
            Settings.Default.GTAVModsScriptMods_IsChecked = (bool)this.ModsToggleButton_ScriptMods.IsChecked;
            Settings.Default.GTAVModsAssetMods_IsChecked = (bool)this.ModsToggleButton_AssetMods.IsChecked;
            Settings.Default.GTAVOptionsOfflineMode_IsChecked = (bool)this.OptionsToggleButton_OfflineMode.IsChecked;
        }
    }
}
