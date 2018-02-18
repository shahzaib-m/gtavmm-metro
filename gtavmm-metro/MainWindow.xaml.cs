using System;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

using MahApps.Metro;
using MahApps.Metro.IconPacks;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Tabs;
using gtavmm_metro.Models;
using gtavmm_metro.Common;
using gtavmm_metro.Properties;

namespace gtavmm_metro
{
    public partial class MainWindow : MetroWindow
    {
        private HomeUC HomeUserControl;
        private ScriptModsUC ScriptModsUserControl;
        private AssetModsUC AssetModsUserControl;
        private AboutUC AboutUserControl;

        private DBInstance ModsDbConnection = null;

        private UpdateHandler UpdateHandler;
        private bool FailedToUpdate = false;
        private bool FailedToCleanup = false;
        
        public MainWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow = this;

            this.SetThemeAndIcon();
        }
        public MainWindow(DBInstance existingModsDbConnection)
        {
            InitializeComponent();

            Application.Current.MainWindow = this;
            this.ModsDbConnection = existingModsDbConnection;

            this.SetThemeAndIcon();
        }
        public MainWindow(bool failedToUpdate, bool failedToCleanup)
        {
            InitializeComponent();
            Application.Current.MainWindow = this;

            this.FailedToUpdate = failedToUpdate;
            this.FailedToCleanup = failedToCleanup;

            this.SetThemeAndIcon();
        }
        

        #region MainWindow Events
        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            await this.Init();   // initialize once the window is rendered
        }

        private void _this_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            this.MainTabControl.SelectedIndex = 4;
        }
        #endregion

        private void SetThemeAndIcon()
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);        // temp
            this.Icon = new BitmapImage(new Uri(String.Format("pack://application:,,,/Assets/Icons/{0}.ico", appStyle.Item2.Name)));   // temp
        }

        private async Task Init()
        {
            // prereqs (dir checks) TODO

            if (this.ModsDbConnection == null)
            {
                this.ModsDbConnection = new DBInstance(Settings.Default.ModsDirectory);
                await this.ModsDbConnection.VerifyTablesState();
            }
            this.UpdateHandler = new UpdateHandler(Utils.GetExecutingAssemblyVersion(),
                Utils.GetExecutingAssemblyDirectory().FullName);

            await this.AssignUCToTabs();
            this.PreloadIntensiveTabs();

            await this.CheckForInsertedScriptMods();
            await this.CheckForInsertedAssetMods();

            this.EnableUserInteraction();    // enable the UI for the user when tasks finished.

            if (this.FailedToUpdate)
            {
                await this.NotifyUserOfUpdateFailure();
            }
            else if (this.FailedToCleanup)
            {
                await this.NotifyUserOfCleanupFailure();
            }
            await this.CheckForUpdates();
        }

        /// <summary>
        /// This method assigns the appropriate usercontrols for all the tabitems in this window.
        /// </summary>
        private async Task AssignUCToTabs()
        {
            // Assigning ScriptMods UserControl to Script Mods tab
            this.ScriptModsUserControl = new ScriptModsUC();
            await this.ScriptModsUserControl.LoadScriptMods(this.ModsDbConnection);
            this.ScriptModsTabItem.Content = this.ScriptModsUserControl;

            // Assigning AssetMods UserControl to Asset Mods tab
            this.AssetModsUserControl = new AssetModsUC();
            await this.AssetModsUserControl.LoadAssetMods(this.ModsDbConnection);
            this.AssetModsTabItem.Content = this.AssetModsUserControl;

            // Assigning Home UserControl to Home tab
            this.HomeUserControl = new HomeUC(this.ScriptModsUserControl, this.AssetModsUserControl);
            this.HomeTabItem.Content = this.HomeUserControl;

            // Assigning About UserControl to About tab
            this.AboutUserControl = new AboutUC(this.UpdateHandler);
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

        private async Task CheckForInsertedScriptMods()
        {
            IList<ScriptMod> insertedScriptMods = this.ScriptModsUserControl.ScriptMods.Where(scriptMod => scriptMod.IsInserted).ToList();
            if (insertedScriptMods.Any())
            {
                string dialogTitle = "Found inserted script modifications";
                string dialogMessage = "Found script modifications that were inserted during a launch. These modifications could been left in GTAV's directory after this application exited/crashed while it was waiting for GTAV to exit. Attempt to move these files back to their relevant modifications? You cannot do this later.";

                MessageDialogResult res = await this.ShowMessageAsync(dialogTitle, dialogMessage,
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (res == MessageDialogResult.Affirmative)
                {
                    string progressTitle = "Moving script modifications back";
                    string progressMessage = "...";

                    ProgressDialogController prog = await this.ShowProgressAsync(progressTitle, progressMessage);
                    prog.SetIndeterminate();

                    foreach (ScriptMod scriptMod in insertedScriptMods)
                    {
                        prog.SetMessage(scriptMod.Name + " - moving back...");
                        bool currScriptModMoveBackSuccess = GTAV.MoveScriptModBack(scriptMod, Settings.Default.GTAVDirectory);
                        if (!currScriptModMoveBackSuccess)
                        {
                            await this.ShowMessageAsync("Failed to move back script modification files",
                                "Couldn't move back some files belonging to '" + scriptMod.Name + "' from GTAV directory" +
                                " You may have to manually go to GTAV's folder to find these files and put them back in this modification.");
                        }

                        scriptMod.IsInserted = false;
                        await this.ScriptModsUserControl.ScriptModAPI.UpdateScriptModIsInserted(scriptMod.Id, false);
                    }

                    await prog.CloseAsync();
                }
                else
                {
                    foreach (ScriptMod scriptMod in insertedScriptMods)
                    {
                        scriptMod.IsInserted = false;
                        await this.ScriptModsUserControl.ScriptModAPI.UpdateScriptModIsInserted(scriptMod.Id, false);
                    }
                }
            }
        }
        private async Task CheckForInsertedAssetMods()
        {
            IList<AssetMod> insertedAssetMods = this.AssetModsUserControl.AssetMods.Where(assetMod => assetMod.IsInserted && assetMod.IsUsableAssetMod).ToList();
            if (insertedAssetMods.Any())
            {
                string dialogTitle = "Found inserted asset modifications";
                string dialogMessage = "Found asset mod packages that were inserted during a launch. These modifications could been left in GTAV's directory after this application exited/crashed while it was waiting for GTAV to exit. Attempt to move these asset mod packages back? You cannot do this later and the asset mods will be removed from this application.";

                MessageDialogResult res = await this.ShowMessageAsync(dialogTitle, dialogMessage,
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (res == MessageDialogResult.Affirmative)
                {
                    string progressTitle = "Moving asset modifications back";
                    string progressMessage = "...";

                    ProgressDialogController prog = await this.ShowProgressAsync(progressTitle, progressMessage);
                    prog.SetIndeterminate();

                    foreach (AssetMod assetMod in insertedAssetMods)
                    {
                        prog.SetMessage(assetMod.Name + " - moving back...");
                        bool currAssetModMoveBackSuccess = GTAV.MoveAssetModBack(assetMod, Settings.Default.GTAVDirectory);
                        if (!currAssetModMoveBackSuccess)
                        {
                            await this.ShowMessageAsync("Failed to move back asset modification",
                                "Couldn't automatically move back '" + assetMod.Name + "' (target: '" + assetMod.TargetRPF + "') from GTAV directory. It may not longer exist or some other error may have occured.");

                            this.AssetModsUserControl.AssetMods.Remove(assetMod);
                            await this.AssetModsUserControl.AssetModAPI.RemoveAndDeleteAssetMod(assetMod.Id, false);
                        }
                        else
                        {
                            assetMod.IsInserted = false;
                            await this.AssetModsUserControl.AssetModAPI.UpdateAssetModIsInserted(assetMod.Id, false);
                        }
                    }

                    prog.SetMessage("Deleting 'mods' folder inside GTAV directory...");
                    bool delModsFolderInGTAVDirSuccess = await Task.Run(() => GTAV.DeleteModsFolderInGTAVDirectory(Settings.Default.GTAVDirectory));
                    if (!delModsFolderInGTAVDirSuccess)
                    {
                        await this.Dispatcher.Invoke(async () => await this.ShowMessageAsync("Failed to delete 'mods' folder in GTAV directory",
                            "Couldn't delete 'mods' folder inside GTAV directory. Some files are remaining and preventing deletion."));
                    }

                    await prog.CloseAsync();
                }
                else
                {
                    foreach (AssetMod assetMod in insertedAssetMods)
                    {
                        this.AssetModsUserControl.AssetMods.Remove(assetMod);
                        await this.AssetModsUserControl.AssetModAPI.RemoveAndDeleteAssetMod(assetMod.Id, false);
                    }
                }
            }
        }

        private async Task CheckForUpdates()
        {
            await this.UpdateHandler.CheckForUpdateAsync();
            bool updateAvailable = this.UpdateHandler.IsUpdateAvailable();
            if (updateAvailable)
            {
                this.UpdateButton.IsEnabled = true;
                this.UpdateIcon.Kind = PackIconMaterialKind.Update;
                this.UpdateIcon.Foreground = Brushes.Orange;
                this.UpdateTextBlock.Text = "Update available";
                this.UpdateTextBlock.FontWeight = FontWeights.Bold;

                DoubleAnimation fadeIn = new DoubleAnimation(0.0, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 500)));
                this.UpdateButton.BeginAnimation(OpacityProperty, fadeIn);

                this.AboutUserControl.SetUpdateDetails();
            }
        }

        private async Task NotifyUserOfUpdateFailure()
        {
            string dialogTitle = "Updating failed";
            string dialogMessage = "Failed to update this application executable.";

            MessageDialogResult res = await this.ShowMessageAsync(dialogTitle, dialogMessage);
        }
        private async Task NotifyUserOfCleanupFailure()
        {
            string dialogTitle = "Cleanup failed";
            string dialogMessage = "Failed to cleanup the temporary directory after update process. You may delete it manually (located in the directory of this application).";

            MessageDialogResult res = await this.ShowMessageAsync(dialogTitle, dialogMessage);
        }

        /// <summary>
        /// This method should be called after all initial tasks are done and user interaction should now begin.
        /// </summary>
        private void EnableUserInteraction()
        {
            this.MainTabControl.IsEnabled = true;

            DoubleAnimation smoothFadeIn = new DoubleAnimation(0.0, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
            this.MainTabControl.BeginAnimation(OpacityProperty, smoothFadeIn);

            this.MainProgressRing.IsActive = false;
        }
    }
}
