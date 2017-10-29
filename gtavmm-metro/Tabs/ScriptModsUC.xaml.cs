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
        public string ScriptModsRootFolder { get; set; }
        private bool FirstLoad { get; set; } = true;
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

                await Task.Run(() => this.LoadScriptMods());

                this.ScriptModsProgressRingIsActive = false;
                DoubleAnimation smoothFadeIn = new DoubleAnimation(0.0, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                this.ScriptModsDataGrid.BeginAnimation(OpacityProperty, smoothFadeIn);

                FirstLoad = false;
            }
        }

        private void ViewScriptModFolder_Click(object sender, RoutedEventArgs e)
        {
            int chosenModId = (((FrameworkElement)sender).DataContext as ScriptMod).Id; // the sender ScriptMod object from the datagrid

            Process.Start(Path.Combine(this.ScriptModsRootFolder, chosenModId.ToString()));
        }

        private async void RemoveScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            List<ScriptMod> selectedScriptMods = this.ScriptModsDataGrid.SelectedItems.Cast<ScriptMod>().ToList();
            bool multiSelection = (selectedScriptMods.Count > 1);

            string dialogTitle;
            string dialogMessage;
            if (!multiSelection)
            {
                dialogTitle = "Remove Modification";
                dialogMessage = "Are you sure you want to remove the selected modification and preserve all files? Note that the modification folder will be marked with '.removed' inside your script modifications' directory.";
            }
            else
            {
                dialogTitle = "Remove Selected Modifications";
                dialogMessage = "Are you sure you want to remove the selected modifications and preserve all files? Note that the selected modifications' folders will be marked with '.removed' inside your script modifications' directory.";
            }

            MetroDialogSettings dialogSettings = new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" };
            MetroWindow metroWindow = (Application.Current.MainWindow as MetroWindow);  // needed to access ShowMessageAsync() method in MetroWindow

            MessageDialogResult result = await metroWindow.ShowMessageAsync(dialogTitle, dialogMessage, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
            if (result.ToString() == "Affirmative")
            {
                this.ScriptModsDataGrid.IsEnabled = false;
                this.ScriptModsProgressRingIsActive = true;

                foreach (ScriptMod scriptMod in selectedScriptMods)
                {
                    scriptMod.RemoveAndOrDeleteScriptMod(this.ScriptModsRootFolder, false);
                    this.ScriptMods.Remove(scriptMod);
                }

                this.ScriptModsDataGrid.IsEnabled = true;
                this.ScriptModsProgressRingIsActive = false;
            }
        }

        private async void RemoveAndDeleteScriptModButton_Click(object sender, RoutedEventArgs e)
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
            if (result.ToString() == "Affirmative")
            {
                this.ScriptModsDataGrid.IsEnabled = false;
                this.ScriptModsProgressRingIsActive = true;

                foreach (ScriptMod scriptMod in selectedScriptMods)
                {
                    scriptMod.RemoveAndOrDeleteScriptMod(this.ScriptModsRootFolder, true);
                    this.ScriptMods.Remove(scriptMod);
                }

                this.ScriptModsDataGrid.IsEnabled = true;
                this.ScriptModsProgressRingIsActive = false;
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
                    e.Cancel = true;
                    return;
                }
            }

            await Task.Run(() => editedScriptMod.UpdateScriptMod(this.ScriptModsRootFolder, editedScriptMod.Name,
                                                    editedScriptMod.Description, editedScriptMod.IsEnabled));
        }

        private async void ScriptModDGIsEnabled_IsCheckedChanged(object sender, EventArgs e)
        {
            if (!this.FirstLoad)
            {
                ScriptMod editedScriptMod = ((FrameworkElement)sender).DataContext as ScriptMod; // the sender ScriptMod object from the datagrid
                await Task.Run(() => editedScriptMod.UpdateScriptMod(this.ScriptModsRootFolder, editedScriptMod.Name,
                                                                        editedScriptMod.Description, editedScriptMod.IsEnabled));
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

        private void AddNewScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            this.ScriptMods.Add(ScriptMod.CreateScriptMod(this.ScriptModsRootFolder, "New Mod Name", "New Mod Description", false));
        }
        #endregion

        #region My Methods
        private void LoadScriptMods()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.ScriptMods = ScriptMod.GetAllScriptMods(this.ScriptModsRootFolder);
                this.ScriptModsDataGrid.ItemsSource = this.ScriptMods;
            });
        }
        #endregion
    }
}
