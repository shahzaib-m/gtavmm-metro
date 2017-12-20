using System;
using System.IO;
using System.Linq;

using System.Windows;
using System.Windows.Controls;

using System.Diagnostics;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Models;
using gtavmm_metro.Properties;

namespace gtavmm_metro.Tabs
{
    public partial class AssetModsUC : UserControl
    {
        public ObservableCollection<AssetMod> AssetMods { get; set; }
        private ObservableCollection<string> TargetRPFList { get; set; }
        private AssetModAPI AssetModAPI { get; set; }
        private string ModsRootFolder { get; set; }
        private bool ModIndexRearrangeAllowed { get; set; } = true;

        public AssetModsUC()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        #region UserControl Events
        private void ViewAssetModInExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            string chosenModTargetRPF = (((FrameworkElement)sender).DataContext as AssetMod).TargetRPF; // the sender AssetMod object from the datagrid

            string targetRPFPath = Path.Combine(this.ModsRootFolder, chosenModTargetRPF.Substring(1));
            if (File.Exists(targetRPFPath))
            {
                Process explorerRPF = new Process();
                explorerRPF.StartInfo.FileName = "explorer.exe";
                explorerRPF.StartInfo.Arguments = String.Format("/select,\"{0}\"", targetRPFPath);
                explorerRPF.Start();
            }
        }

        private async void DeleteAssetModButton_Click(object sender, RoutedEventArgs e)
        {
            List<AssetMod> selectedAssetMods = this.AssetModsDataGrid.SelectedItems.Cast<AssetMod>().ToList();
            bool multiSelection = (selectedAssetMods.Count > 1);

            if (!selectedAssetMods.First().IsUsableAssetMod)
            {
                this.AssetMods.Remove(selectedAssetMods.First());
                return;
            }

            string dialogTitle;
            string dialogMessage;
            if (!multiSelection)
            {
                dialogTitle = "Remove and delete modification package";
                dialogMessage = "Are you sure you want to remove the selected modification package and delete the file? Note that the file will be permanently deleted. Proceed with caution.";
            }
            else
            {
                dialogTitle = "Remove and delete selected modification packages";
                dialogMessage = "Are you sure you want to remove the selected modification packages and delete all the files? Note that the selected modification packages will be permanently deleted. Proceed with caution.";
            }

            MetroDialogSettings dialogSettings = new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" };
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);  // needed to access ShowMessageAsync() method in MetroWindow

            MessageDialogResult result = await metroWindow.ShowMessageAsync(dialogTitle, dialogMessage, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
            if (result == MessageDialogResult.Affirmative)
            {
                this.AssetModsProgressRing.IsActive = true;

                string errorDialogTitle = "{0} - Unable to delete modification package";
                string errorDialogMessage = "Unable to delete this modification package. The file may be in use by an application. Attempt to delete it again?";

                foreach (AssetMod assetMod in selectedAssetMods)
                {
                    bool deleteSuccess = await this.AssetModAPI.RemoveAndDeleteAssetMod(assetMod.Id);
                    bool userWantsToRetry = true;
                    while (!deleteSuccess && userWantsToRetry)
                    {
                        MessageDialogResult deleteFailResult = await metroWindow.ShowMessageAsync(String.Format(errorDialogTitle, assetMod.Name), errorDialogMessage, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
                        if (deleteFailResult == MessageDialogResult.Negative)
                        {
                            userWantsToRetry = false;
                        }
                        else
                        {
                            deleteSuccess = await this.AssetModAPI.RemoveAndDeleteAssetMod(assetMod.Id);
                        }
                    }

                    if (deleteSuccess)
                    {
                        this.AssetMods.Remove(assetMod);
                    }
                }

                this.AssetModsProgressRing.IsActive = false;
            }
        }

        private async void MoveAssetModUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ModIndexRearrangeAllowed)
            {
                this.ModIndexRearrangeAllowed = false;

                AssetMod chosenAssetMod = ((FrameworkElement)sender).DataContext as AssetMod;
                if (this.AssetMods.ElementAt(0).Id == chosenAssetMod.Id)
                {
                    this.ModIndexRearrangeAllowed = true;
                    return;
                }

                int oldIndex = this.AssetMods.IndexOf(chosenAssetMod);
                int newIndex = oldIndex - 1;

                this.AssetMods.Move(oldIndex, newIndex);
                await Task.Run(() => this.AssetModAPI.UpdateAssetModOrderIndexes(this.AssetMods));

                this.ModIndexRearrangeAllowed = true;
            }
        }

        private async void MoveAssetModDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ModIndexRearrangeAllowed)
            {
                this.ModIndexRearrangeAllowed = false;

                AssetMod chosenAssetMod = ((FrameworkElement)sender).DataContext as AssetMod;
                if (this.AssetMods.ElementAt(this.AssetMods.Count - 1).Id == chosenAssetMod.Id)
                {
                    this.ModIndexRearrangeAllowed = true;
                    return;
                }

                int oldIndex = this.AssetMods.IndexOf(chosenAssetMod);
                int newIndex = oldIndex + 1;

                this.AssetMods.Move(oldIndex, newIndex);
                await Task.Run(() => this.AssetModAPI.UpdateAssetModOrderIndexes(this.AssetMods));

                this.ModIndexRearrangeAllowed = true;
            }
        }

        private async void AssetModsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AssetMod editedAssetMod = e.Row.Item as AssetMod;

            string whichColumn = e.Column.Header as string;
            if (whichColumn == "Name")
            {
                if (String.IsNullOrWhiteSpace(editedAssetMod.Name))
                {
                    editedAssetMod.Name = await this.AssetModAPI.GetOldNameBeforeIllegalEdit(editedAssetMod.Id);
                    return;
                }
                else if (editedAssetMod.Name == await this.AssetModAPI.GetOldNameBeforeIllegalEdit(editedAssetMod.Id))
                    return;

                await this.AssetModAPI.UpdateAssetModName(editedAssetMod.Id, editedAssetMod.Name);
            }
            else if (whichColumn == "Description")
            {
                await this.AssetModAPI.UpdateAssetModDescription(editedAssetMod.Id, editedAssetMod.Description);
            }
        }

        private async void AssetModDGIsEnabled_Click(object sender, EventArgs e)
        {
            AssetMod editedAssetMod = ((FrameworkElement)sender).DataContext as AssetMod; // the sender AssetMod object from the datagrid
            if (editedAssetMod.IsEnabled)
            {
                AssetMod duplicateActiveAssetMod = this.AssetMods.SingleOrDefault(assetMod =>
                    assetMod.TargetRPF == editedAssetMod.TargetRPF && assetMod.IsEnabled == editedAssetMod.IsEnabled && assetMod.Id != editedAssetMod.Id);

                if (duplicateActiveAssetMod != null)
                {
                    ((ToggleSwitch)sender).IsChecked = false;
                    editedAssetMod.IsEnabled = false;

                    MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);  // needed to access ShowMessageAsync() method in MetroWindow

                    string errorDialogTitle = "Unable to enable this modification package.";
                    string errorDialogMessage = String.Format("The target .rpf of this modification \"{0}\" is in use by another modification ({1}). Please disable that modification to enable this modification.",
                        editedAssetMod.TargetRPF, duplicateActiveAssetMod.Name);

                    await metroWindow.ShowMessageAsync(errorDialogTitle, errorDialogMessage, MessageDialogStyle.Affirmative);
                }
            }

            await this.AssetModAPI.UpdateAssetModIsEnabled(editedAssetMod.Id, editedAssetMod.IsEnabled);
        }

        # region Bottom bar buttons
        // View Modifications Folder Behaviour
        private void ViewModsRootFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(this.ModsRootFolder, "Asset Mods");
        }

        // Add New Asset Modification Behaviour (ChildWindow)
        private void AddNewAssetModButton_Click(object sender, RoutedEventArgs e)
        {
            this.NewAssetModChildWindow.IsOpen = true;
        }

        private void AddNewAssetModButton_CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.NewAssetModChildWindow.IsOpen = false;
        }

        private void TargetRPFComboxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetRPFComboxBox.SelectedIndex == -1)
            {
                AddNewAssetModButton_CreateButton.IsEnabled = false;
            }
            else
            {
                AddNewAssetModButton_CreateButton.IsEnabled = true;
            }
        }

        private async void AddNewAssetModButton_CreateButton_Click(object sender, RoutedEventArgs e)
        {
            this.NewAssetModChildWindow.IsOpen = false;

            string targetRPFString = this.TargetRPFComboxBox.SelectedItem as string;
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);
            List<AssetMod> assetModsWithMatchingTargetRPF = new List<AssetMod>(
                this.AssetMods.Where(assetMod => assetMod.TargetRPF == targetRPFString));
            if (assetModsWithMatchingTargetRPF.Any())
            {
                string dialogTitle = "Target RPF exists";
                string dialogMessage = String.Format("An existing modification package ({0}) was found with this target RPF. Please delete that modification if you'd like to add a new clean version of that RPF.",
                        assetModsWithMatchingTargetRPF[0].Name);

                await metroWindow.ShowMessageAsync(dialogTitle, dialogMessage, MessageDialogStyle.Affirmative);
            }
            else
            {
                ProgressDialogController controller = await metroWindow.ShowProgressAsync("Copying file", "Please wait...", false);
                controller.SetIndeterminate();

                this.AssetMods.Add(await Task.Run(() => this.AssetModAPI.CreateAssetMod("New Mod Package Name", targetRPFString,
                    this.AssetMods.Count, "New Mod Package Description", false, true)));

                this.TargetRPFComboxBox.SelectedIndex = -1;
                await controller.CloseAsync();
            }
        }
        // ---------------------------------------------------------------------------------------------------------------------------------
        #endregion
        #endregion

        #region My Methods
        public async void LoadAssetMods(DBInstance modsDbConnection)
        {
            await this.Dispatcher.Invoke(async () =>
            {
                this.ModsRootFolder = Path.Combine(Settings.Default.ModsDirectory, "Asset Mods");
                this.AssetModAPI = new AssetModAPI(this.ModsRootFolder, modsDbConnection, Settings.Default.GTAVDirectory);

                this.TargetRPFList = new ObservableCollection<string>(GTAV.GetAllRPFsInsideGTAVDirectory(Settings.Default.GTAVDirectory));
                this.TargetRPFComboxBox.ItemsSource = this.TargetRPFList;

                if (!Directory.Exists(this.ModsRootFolder))
                {
                    Directory.CreateDirectory(this.ModsRootFolder);

                    this.AssetMods = new ObservableCollection<AssetMod>();
                    this.AssetMods.Add(await this.AssetModAPI.CreateAssetMod("Example mod package (can delete)",
                       @"\update\sample.rpf", 0, "This is an example description of a mod package - Not usable. Only here as an example.",
                        false, false));
                }
                else
                {
                    List<AssetMod> assetModsFromDb = await this.AssetModAPI.GetAllAssetMods();
                    if (assetModsFromDb == null)
                    {
                        this.AssetMods = new ObservableCollection<AssetMod>();
                        this.AssetMods.Add(await this.AssetModAPI.CreateAssetMod("Example mod package (can delete)",
                           @"\update\sample.rpf", 0, "This is an example description of a mod package - Not usable. Only here as an example",
                            false, false));
                    }
                    else
                    {
                        this.AssetMods = new ObservableCollection<AssetMod>(assetModsFromDb);
                    }
                }

                this.AssetModsDataGrid.ItemsSource = this.AssetMods;
            });
        }
        #endregion
    }
}
