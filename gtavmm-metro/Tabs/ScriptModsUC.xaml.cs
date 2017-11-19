using System;
using System.IO;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

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
    /// Interaction logic for ScriptMods.xaml
    /// </summary>
    public partial class ScriptModsUC : UserControl
    {
        private ObservableCollection<ScriptMod> ScriptMods { get; set; }
        private ScriptModAPI ScriptModAPI { get; set; }
        public string ScriptModsRootFolder { get; set; }
        private bool FirstLoad { get; set; } = true;
        private bool OrderIndexUpdateInProgress { get; set; } = false;
        private bool ScriptModsProgressRingIsActive { get; set; } = true;

        public ScriptModsUC()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        #region UserControl Events
        private async void ScriptModsUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (FirstLoad)
            {
                this.ScriptModsRootFolder = Settings.Default.ScriptModsDirectory;
                this.ScriptModAPI = new ScriptModAPI(this.ScriptModsRootFolder);

                await Task.Run(() => this.LoadScriptMods());

                this.ScriptModsProgressRingIsActive = false;
                DoubleAnimation smoothFadeIn = new DoubleAnimation(0.0, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                this.ScriptModsDataGrid.BeginAnimation(OpacityProperty, smoothFadeIn);

                FirstLoad = false;
            }
        }

        private void ViewScriptModFolder_Click(object sender, RoutedEventArgs e)
        {
            string chosenModName = (((FrameworkElement)sender).DataContext as ScriptMod).Name; // the sender ScriptMod object from the datagrid

            try
            {
                Process.Start(Path.Combine(this.ScriptModsRootFolder, chosenModName));
            }
            catch (Exception ex)
            {
                if (ex is DirectoryNotFoundException)
                {

                }

                throw;
            }
        }

        private async void DeleteScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            List<ScriptMod> selectedScriptMods = this.ScriptModsDataGrid.SelectedItems.Cast<ScriptMod>().ToList();
            bool multiSelection = (selectedScriptMods.Count > 1);

            string dialogTitle;
            string dialogMessage;
            if (!multiSelection)
            {
                dialogTitle = "Remove and Delete Modification";
                dialogMessage = "Are you sure you want to remove the selected modification and delete all of its files? Note that the modification folder and everything inside it will be deleted. Proceed with caution.";
            }
            else
            {
                dialogTitle = "Remove and Delete Selected Modifications";
                dialogMessage = "Are you sure you want to remove the selected modifications and delete all of their files? Note that the selected modifications' folders and everything inside them will be deleted. Proceed with caution.";
            }

            MetroDialogSettings dialogSettings = new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" };
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);  // needed to access ShowMessageAsync() method in MetroWindow

            MessageDialogResult result = await metroWindow.ShowMessageAsync(dialogTitle, dialogMessage, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
            if (result == MessageDialogResult.Affirmative)
            {
                this.ScriptModsProgressRingIsActive = true;

                string errorDialogTitle = "{0} - Unable to delete modification";
                string errorDialogMessage = "Unable to delete this modification. The folder and/or its files may be in use by an application. Attempt to delete it again?";

                foreach (ScriptMod scriptMod in selectedScriptMods)
                {
                    bool deleteSuccess = await this.ScriptModAPI.RemoveAndDeleteScriptMod(scriptMod.Id);
                    bool userWantsToRetry = true;
                    while (!deleteSuccess && userWantsToRetry)
                    {
                        MessageDialogResult deleteFailResult = await metroWindow.ShowMessageAsync(String.Format(errorDialogTitle, scriptMod.Name), errorDialogMessage, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
                        if (deleteFailResult == MessageDialogResult.Affirmative)
                        {
                            userWantsToRetry = true;
                        }
                        else
                        {
                            userWantsToRetry = false;
                        }
                    }
                    
                    if (deleteSuccess)
                    {
                        this.ScriptMods.Remove(scriptMod);
                    }
                }

                this.ScriptModsProgressRingIsActive = false;
            }
        }

        private void MoveScriptModUpButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptMod chosenScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod;
            if (this.ScriptMods.ElementAt(0).Id == chosenScriptMod.Id)
                return;

            this.OrderIndexUpdateInProgress = true;

            int oldIndex = this.ScriptMods.IndexOf(chosenScriptMod);
            int newIndex = oldIndex - 1;

            this.ScriptMods.Move(oldIndex, newIndex);
            Task.Run(() => this.ScriptModAPI.UpdateScriptModOrderIndexes(this.ScriptMods));

            this.OrderIndexUpdateInProgress = false;
        }

        private void MoveScriptModDownButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptMod chosenScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod;
            if (this.ScriptMods.ElementAt(this.ScriptMods.Count - 1).Id == chosenScriptMod.Id)
                return;

            this.OrderIndexUpdateInProgress = true;

            int oldIndex = this.ScriptMods.IndexOf(chosenScriptMod);
            int newIndex = oldIndex + 1;

            this.ScriptMods.Move(oldIndex, newIndex);
            Task.Run(() => this.ScriptModAPI.UpdateScriptModOrderIndexes(this.ScriptMods));

            this.OrderIndexUpdateInProgress = false;
        }

        private async void ScriptModsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ScriptMod editedScriptMod = e.Row.Item as ScriptMod;

            string whichColumn = e.Column.Header as string;
            if (whichColumn == "Name")
            {
                if (String.IsNullOrWhiteSpace(editedScriptMod.Name))
                {
                    editedScriptMod.Name = await this.ScriptModAPI.GetOldNameBeforeIllegalEdit(editedScriptMod.Id);
                    return;
                }

                await this.ScriptModAPI.UpdateScriptModName(editedScriptMod.Id, editedScriptMod.Name);
            }
            else if (whichColumn == "Description")
            {
                await this.ScriptModAPI.UpdateScriptModDescription(editedScriptMod.Id, editedScriptMod.Description);
            }
        }

        private async void ScriptModDGIsEnabled_Click(object sender, EventArgs e)
        {
            if (!this.FirstLoad && !this.OrderIndexUpdateInProgress)
            {
                ScriptMod editedScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod; // the sender ScriptMod object from the datagrid
                await this.ScriptModAPI.UpdateScriptModIsEnabled(editedScriptMod.Id, editedScriptMod.IsEnabled);
            }
        }

        // Bottom bar buttons
        private void ScriptModSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptModSettingsChildWindow.IsOpen = true;
        }
        private void ScriptModSettings_CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptModSettingsChildWindow.IsOpen = false;
        }

        private void ViewScriptModsRootFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(this.ScriptModsRootFolder);
        }

        private async void AddNewScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptMod newScriptMod = await this.ScriptModAPI.CreateScriptMod("New Mod Name", this.ScriptMods.Count, "New Mod Description", false);
            this.ScriptMods.Add(newScriptMod);
        }
        #endregion

        #region My Methods
        private async void LoadScriptMods()
        {
            await this.Dispatcher.Invoke(async () =>
            {
                List<ScriptMod> scriptModsFromDb = await this.ScriptModAPI.GetAllScriptMods();
                if (scriptModsFromDb == null)
                {
                    this.ScriptMods = new ObservableCollection<ScriptMod>();
                    this.ScriptMods.Add(await this.ScriptModAPI.CreateScriptMod("New Mod Name", 0, "New Mod Description", false));
                    // UI margins act odd when datagrid is empty and a new script mod is added for the first time manually. Adding one by default.
                }
                else
                {
                    this.ScriptMods = new ObservableCollection<ScriptMod>(scriptModsFromDb);
                }

                this.ScriptModsDataGrid.ItemsSource = this.ScriptMods;
            });
        }
        #endregion
    }
}
