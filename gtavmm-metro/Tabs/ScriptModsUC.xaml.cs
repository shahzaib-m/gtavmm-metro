﻿using gtavmm_metro.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace gtavmm_metro.Tabs
{
    /// <summary>
    /// Interaction logic for ScriptMods.xaml
    /// </summary>
    public partial class ScriptModsUC : UserControl
    {
        public ObservableCollection<ScriptMod> ScriptMods { get; set; }
        public ScriptModsUC()
        {
            InitializeComponent();
        }

        private async void ScriptModsUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => LoadScriptMods());
        }

        private void LoadScriptMods()
        {
            ScriptMods = new ObservableCollection<ScriptMod>
            {
                // Dummy data
                new ScriptMod { Id = 1, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true },
                new ScriptMod { Id = 2, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false },
                new ScriptMod { Id = 3, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true },
                new ScriptMod { Id = 4, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false },
                new ScriptMod { Id = 5, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true },
                new ScriptMod { Id = 6, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false },
                new ScriptMod { Id = 7, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true },
                new ScriptMod { Id = 8, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false }
            };

            this.Dispatcher.Invoke(() =>    // needed as MainWindow control is being modified from a non-main thread
            {
                this.ScriptModsDataGrid.ItemsSource = ScriptMods;
            });
        }

        private void ViewScriptModFolder_Click(object sender, RoutedEventArgs e)
        {
            ScriptMod chosen_mod = ((FrameworkElement)sender).DataContext as ScriptMod; // the sender Script_Mod object from the datagrid
            MessageBox.Show(String.Format("Sender Name: {0}, Enabled: {1}", chosen_mod.Name, chosen_mod.IsEnabled));
        }

        private async void DeleteScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptMod chosen_mod = ((FrameworkElement)sender).DataContext as ScriptMod; // the sender Script_Mod object from the datagrid

            string dialog_title = "Delete modification";
            string dialog_message = "Are you sure you want to delete the selected modification?";
            MetroDialogSettings dialog_settings = new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" };
            var metroWindow = (Application.Current.MainWindow as MetroWindow);
            MessageDialogResult result = await metroWindow.ShowMessageAsync(dialog_title, dialog_message, MessageDialogStyle.AffirmativeAndNegative, dialog_settings);
            if (result.ToString() == "Affirmative")
            {
                bool deleted = false;
                for (int i = 0; (i < this.ScriptMods.Count) && (!deleted); i++)
                {
                    if (this.ScriptMods[i].Id == chosen_mod.Id)
                    {
                        this.ScriptMods.RemoveAt(i);
                        deleted = true;
                    }
                }
            }
        }
    }
}
