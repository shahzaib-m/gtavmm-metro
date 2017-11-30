using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
    /// <summary>
    /// Interaction logic for AssetModsUC.xaml
    /// </summary>
    public partial class AssetModsUC : UserControl
    {
        private ObservableCollection<AssetMod> AssetMods { get; set; }
        private ObservableCollection<string> TargetRPFList { get; set; }
        private AssetModAPI AssetModAPI { get; set; }
        public string ModsRootFolder { get; set; }
        private bool FirstLoad { get; set; } = true;

        public AssetModsUC()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        #region UserControl Events
        private void ViewAssetModFolder_Click(object sender, RoutedEventArgs e)
        {
            string chosenModName = (((FrameworkElement)sender).DataContext as AssetMod).Name; // the sender AssetMod object from the datagrid

            if (File.Exists(Path.Combine(this.ModsRootFolder, chosenModName + ".rpf")))
            {
                Process explorerRPF = new Process();
                explorerRPF.StartInfo.FileName = "explorer.exe";
                explorerRPF.StartInfo.Arguments = String.Format("/select,\"{0}\"", Path.Combine(this.ModsRootFolder, chosenModName + ".rpf"));
                explorerRPF.Start();
            }
        }

        private async void DeleteAssetModButton_Click(object sender, RoutedEventArgs e)
        {
            List<AssetMod> selectedAssetMods = this.AssetModsDataGrid.SelectedItems.Cast<AssetMod>().ToList();
            bool multiSelection = (selectedAssetMods.Count > 1);

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

        private void MoveAssetModUpButton_Click(object sender, RoutedEventArgs e)
        {
            AssetMod chosenAssetMod = ((FrameworkElement)sender).DataContext as AssetMod;
            if (this.AssetMods.ElementAt(0).Id == chosenAssetMod.Id)
                return;

            int oldIndex = this.AssetMods.IndexOf(chosenAssetMod);
            int newIndex = oldIndex - 1;

            this.AssetMods.Move(oldIndex, newIndex);
            Task.Run(() => this.AssetModAPI.UpdateAssetModOrderIndexes(this.AssetMods));
        }

        private void MoveAssetModDownButton_Click(object sender, RoutedEventArgs e)
        {
            AssetMod chosenAssetMod = ((FrameworkElement)sender).DataContext as AssetMod;
            if (this.AssetMods.ElementAt(this.AssetMods.Count - 1).Id == chosenAssetMod.Id)
                return;

            int oldIndex = this.AssetMods.IndexOf(chosenAssetMod);
            int newIndex = oldIndex + 1;

            this.AssetMods.Move(oldIndex, newIndex);
            Task.Run(() => this.AssetModAPI.UpdateAssetModOrderIndexes(this.AssetMods));
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
                else
                {
                    Regex invalidFileNameCharsRegex = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");
                    if (invalidFileNameCharsRegex.IsMatch(editedAssetMod.Name))
                    {
                        editedAssetMod.Name = await this.AssetModAPI.GetOldNameBeforeIllegalEdit(editedAssetMod.Id);
                        return;
                    }
                }

                bool changeNameIsSuccess = await this.AssetModAPI.UpdateAssetModName(editedAssetMod.Id, editedAssetMod.Name);
                if (!changeNameIsSuccess)
                {
                    editedAssetMod.Name = await this.AssetModAPI.GetOldNameBeforeIllegalEdit(editedAssetMod.Id);

                    MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);  // needed to access ShowMessageAsync() method in MetroWindow

                    string errorDialogTitle = "Unable to rename modification package";
                    string errorDialogMessage = "Unable to rename this modification package. The file may be in use by an application (or alternatively the name has unknown invalid characters or is too long).";

                    await metroWindow.ShowMessageAsync(errorDialogTitle, errorDialogMessage, MessageDialogStyle.Affirmative);
                }
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

        //// --- Bottom bar buttons
        // View Modifications Folder Behaviour
        private void ViewModsRootFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(this.ModsRootFolder);
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
            if (assetModsWithMatchingTargetRPF.Count == 1)
            {
                string dialogTitle = "Target RPF exists";
                string dialogMessage = String.Format("An existing modification package ({0}) was found with this target RPF. Are you sure you want to continue creating a new mod with this target RPF?\n\n" +
                    "You should use the existing modification package if you intend to add assets from another GTAV mod to this RPF, since adding a new modification package will only allow you to enable one of them (one modification per target RPF).",
                        assetModsWithMatchingTargetRPF[0].Name);

                MetroDialogSettings dialogSettings = new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" };
                MessageDialogResult result = await metroWindow.ShowMessageAsync(dialogTitle, dialogMessage, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
                if (result == MessageDialogResult.Affirmative)
                {
                    ProgressDialogController controller = await metroWindow.ShowProgressAsync("Copying file", "Please wait...", false);
                    controller.SetIndeterminate();

                    this.AssetMods.Add(await Task.Run(() => this.AssetModAPI.CreateAssetMod("New Mod Package Name", targetRPFString,
                        this.AssetMods.Count, "New Mod Package Description", false, true)));

                    this.TargetRPFComboxBox.SelectedIndex = -1;
                    await controller.CloseAsync();
                }
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

        #region My Methods
        public async void LoadAssetMods(DBInstance modsDbConnection)
        {
            await this.Dispatcher.Invoke(async () =>
            {
                this.ModsRootFolder = Settings.Default.ModsDirectory;
                this.AssetModAPI = new AssetModAPI(this.ModsRootFolder, modsDbConnection, Settings.Default.GTAVDirectory);
                //this.AssetModAPI.AssignStaticRPFValuesToModel(Settings.Default.GTAVDirectory);
                this.TargetRPFList = new ObservableCollection<string>(this.AssetModAPI.GetAllRPFsInsideGTAVDirectory());
                this.TargetRPFComboxBox.ItemsSource = this.TargetRPFList;

                List<AssetMod> assetModsFromDb = await this.AssetModAPI.GetAllAssetMods();
                if (assetModsFromDb == null)
                {
                    this.AssetMods = new ObservableCollection<AssetMod>();
                    this.AssetMods.Add(await this.AssetModAPI.CreateAssetMod("Example mod package",
                       @"\update\sample.rpf", 0, "This is an example description of a mod package",
                        false, false));
                }
                else
                {
                    this.AssetMods = new ObservableCollection<AssetMod>(assetModsFromDb);
                }

                this.AssetModsDataGrid.ItemsSource = this.AssetMods;
            });
        }
        #endregion
    }
}
