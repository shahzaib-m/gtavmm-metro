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
using gtavmm_metro.AppSettings;

namespace gtavmm_metro.Tabs
{
    public partial class ScriptModsUC : UserControl
    {
        public ObservableCollection<ScriptMod> ScriptMods { get; set; }
        public ScriptModAPI ScriptModAPI { get; set; }
        private string ModsRootFolder { get; set; }
        private bool ModIndexRearrangeAllowed { get; set; } = true;
        private bool ScriptModsProgressRingIsActive { get; set; } = false;

        public ScriptModsUC()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        #region UserControl Events
        private void ViewScriptModFolder_Click(object sender, RoutedEventArgs e)
        {
            string chosenModName = (((FrameworkElement)sender).DataContext as ScriptMod).Name; // the sender ScriptMod object from the datagrid

            if (Directory.Exists(Path.Combine(this.ModsRootFolder, chosenModName)))
            {
                Process.Start(Path.Combine(this.ModsRootFolder, chosenModName));
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
                dialogTitle = "Remove and delete modification";
                dialogMessage = "Are you sure you want to remove the selected modification and delete all of its files? Note that the modification folder and everything inside it will be deleted. Proceed with caution.";
            }
            else
            {
                dialogTitle = "Remove and delete selected modifications";
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
                        if (deleteFailResult == MessageDialogResult.Negative)
                        {
                            userWantsToRetry = false;
                        }
                        else
                        {
                            deleteSuccess = await this.ScriptModAPI.RemoveAndDeleteScriptMod(scriptMod.Id);
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

        private async void MoveScriptModUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ModIndexRearrangeAllowed)
            {
                this.ModIndexRearrangeAllowed = false;

                ScriptMod chosenScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod;
                if (this.ScriptMods.ElementAt(0).Id == chosenScriptMod.Id)
                {
                    this.ModIndexRearrangeAllowed = true;
                    return;
                }

                int oldIndex = this.ScriptMods.IndexOf(chosenScriptMod);
                int newIndex = oldIndex - 1;

                this.ScriptMods.Move(oldIndex, newIndex);
                await Task.Run(() => this.ScriptModAPI.UpdateScriptModOrderIndexes(this.ScriptMods));

                this.ModIndexRearrangeAllowed = true;
            }
        }

        private async void MoveScriptModDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ModIndexRearrangeAllowed)
            {
                this.ModIndexRearrangeAllowed = false;

                ScriptMod chosenScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod;
                if (this.ScriptMods.ElementAt(this.ScriptMods.Count - 1).Id == chosenScriptMod.Id)
                {
                    this.ModIndexRearrangeAllowed = true;
                    return;
                }

                int oldIndex = this.ScriptMods.IndexOf(chosenScriptMod);
                int newIndex = oldIndex + 1;

                this.ScriptMods.Move(oldIndex, newIndex);
                await Task.Run(() => this.ScriptModAPI.UpdateScriptModOrderIndexes(this.ScriptMods));

                this.ModIndexRearrangeAllowed = true;
            }
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
                else if (editedScriptMod.Name == await this.ScriptModAPI.GetOldNameBeforeIllegalEdit(editedScriptMod.Id))
                    return;
                else
                {
                    Regex invalidFileNameCharsRegex = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");
                    if (invalidFileNameCharsRegex.IsMatch(editedScriptMod.Name))
                    {
                        editedScriptMod.Name = await this.ScriptModAPI.GetOldNameBeforeIllegalEdit(editedScriptMod.Id);
                        return;
                    }
                }

                bool changeNameIsSuccess = await this.ScriptModAPI.UpdateScriptModName(editedScriptMod.Id, editedScriptMod.Name);
                if (!changeNameIsSuccess)
                {
                    editedScriptMod.Name = await this.ScriptModAPI.GetOldNameBeforeIllegalEdit(editedScriptMod.Id);

                    MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);  // needed to access ShowMessageAsync() method in MetroWindow

                    string errorDialogTitle = "Unable to rename modification";
                    string errorDialogMessage = "Unable to rename this modification. The folder and/or its files may be in use by an application (or alternatively the name has unknown invalid characters or is too long).";

                    MessageDialogResult result = await metroWindow.ShowMessageAsync(errorDialogTitle, errorDialogMessage, MessageDialogStyle.Affirmative);
                }
            }
            else if (whichColumn == "Description")
            {
                await this.ScriptModAPI.UpdateScriptModDescription(editedScriptMod.Id, editedScriptMod.Description);
            }
        }

        private async void ScriptModDGIsEnabled_Click(object sender, EventArgs e)
        {
            ScriptMod editedScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod; // the sender ScriptMod object from the datagrid
            await this.ScriptModAPI.UpdateScriptModIsEnabled(editedScriptMod.Id, editedScriptMod.IsEnabled);
        }

        #region Bottom bar buttons
        // Settings Button Behaviour
        private void ScriptModSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptModSettingsChildWindow.IsOpen = true;
        }
        private void ScriptModSettings_CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptModSettingsChildWindow.IsOpen = false;
        }

        // View Modifications Folder Behaviour
        private void ViewModsRootFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(this.ModsRootFolder);
        }

        // Add New Script Modification Behaviour
        private async void AddNewScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptMod newScriptMod = await this.ScriptModAPI.CreateScriptMod("New Mod Name", this.ScriptMods.Count, "New Mod Description", false);
            this.ScriptMods.Add(newScriptMod);
        }
        #endregion
        #endregion

        #region My Methods
        public async Task LoadScriptMods(DBInstance modsDbConnection)
        {
            await this.Dispatcher.Invoke(async () =>
            {
                this.ModsRootFolder = Path.Combine(SettingsHandler.ModsDirectory, "Script Mods");
                this.ScriptModAPI = new ScriptModAPI(this.ModsRootFolder, modsDbConnection);

                if (!Directory.Exists(this.ModsRootFolder))
                {
                    Directory.CreateDirectory(this.ModsRootFolder);
                    this.ScriptMods = new ObservableCollection<ScriptMod>();
                    this.ScriptMods.Add(await this.ScriptModAPI.CreateScriptMod("Your mod name - Click to edit", 0, "Your mod's brief description to help you identify it (optional).\nClick to edit.\n'Enter' key to create a new line.\n'Ctrl+Enter'/click anywhere else to confirm.", false));
                    // UI margins act odd when datagrid is empty and a new script mod is added for the first time manually. Adding one by default.
                }
                else
                {
                    List<ScriptMod> scriptModsFromDb = await this.ScriptModAPI.GetAllScriptMods();
                    if (scriptModsFromDb == null)
                    {
                        this.ScriptMods = new ObservableCollection<ScriptMod>();
                        this.ScriptMods.Add(await this.ScriptModAPI.CreateScriptMod("Your mod name - Click to edit", 0, "Your mod's brief description to help you identify it (optional).\nClick to edit.\n'Enter' key to create a new line.\n'Ctrl + Enter'/click anywhere else to confirm.", false));
                        // UI margins act odd when datagrid is empty and a new script mod is added for the first time manually. Adding one by default.
                    }
                    else
                    {
                        this.ScriptMods = new ObservableCollection<ScriptMod>(scriptModsFromDb);
                    }
                }

                this.ScriptModsDataGrid.ItemsSource = this.ScriptMods;
            });
        }
        #endregion
    }
}
