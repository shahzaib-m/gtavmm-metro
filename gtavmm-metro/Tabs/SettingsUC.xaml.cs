using System;
using System.IO;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using Microsoft.WindowsAPICodePack.Dialogs;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using gtavmm_metro.Models;
using gtavmm_metro.AppSettings;

namespace gtavmm_metro.Tabs
{
    public partial class SettingsUC : UserControl
    {
        private bool IsInitLaunch = true;
        private List<string> AppAccents = Appearance.GetAppAccentsList();

        private AppTheme CurrentAppTheme = SettingsHandler.AppTheme;
        private AppAccent CurrentAppAccent = SettingsHandler.AppAccent;

        public SettingsUC()
        {
            InitializeComponent();

            Init();
        }

        private void Init()
        {
            GTAVDirectoryTextBox.Text = SettingsHandler.GTAVDirectory;
            ModificationsDirectoryTextBox.Text = SettingsHandler.ModsDirectory;

            if (SettingsHandler.IsSteamDRM)
                SteamDRM_Radio.IsChecked = true;
            else
                RockstarDRM_Radio.IsChecked = true;

            if (File.Exists(Path.Combine(SettingsHandler.GTAVDirectory, "GTA5.exe")))
                GTAVDirectoryTextBox.BorderBrush = Brushes.Green;
            else
                GTAVDirectoryTextBox.BorderBrush = Brushes.Red;

            if (CurrentAppTheme == AppTheme.Dark)
            {
                AppThemeDark_Radio.IsChecked = true;
            }
            else
            {
                AppThemeLight_Radio.IsChecked = true;
            }

            AccentSplitButton.ItemsSource = AppAccents;
            AccentSplitButton.SelectedIndex = AppAccents.IndexOf(Appearance.GetCurrentAppAccentString());

            IsInitLaunch = false;
        }

        private void ChangeGTAVDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog folderSelectDialog = new CommonOpenFileDialog
            {
                Title = "GTAV Directory",
                IsFolderPicker = true,
                InitialDirectory = Environment.SpecialFolder.CommonProgramFilesX86.ToString(),
                DefaultDirectory = Environment.SpecialFolder.CommonProgramFilesX86.ToString(),
                EnsurePathExists = true
            })
            {
                if (folderSelectDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string newChosenPath = folderSelectDialog.FileName;

                    string gtavExePath = Path.Combine(newChosenPath, "GTA5.exe");
                    if (File.Exists(gtavExePath))
                    {
                        GTAVDirectoryTextBox.BorderBrush = Brushes.Green;
                        GTAVDirectoryTextBox.Text = newChosenPath;

                        SettingsHandler.GTAVDirectory = newChosenPath;

                        if (File.Exists(Path.Combine(newChosenPath, GTAV.GetDRMIdentifier(GTAVDRM.Steam))))
                        {
                            SettingsHandler.IsSteamDRM = true;
                            SteamDRM_Radio.IsChecked = true;
                        }

                        else
                        {
                            SettingsHandler.IsSteamDRM = false;
                            RockstarDRM_Radio.IsChecked = true;
                        }
                    }
                    else
                    {
                        (Application.Current.MainWindow as MetroWindow).ShowMessageAsync("Invalid Selection",
                            "The given path does not seem to be a valid GTAV directory. This setting will not be changed.");
                    }
                }
            }
        }

        private void SteamDRM_Radio_Click(object sender, RoutedEventArgs e) { SettingsHandler.IsSteamDRM = true; }
        private void RockstarDRM_Radio_Click(object sender, RoutedEventArgs e) { SettingsHandler.IsSteamDRM = false; }

        private void AccentSplitButton_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitLaunch)
                return;

            string newAccent = AccentSplitButton.SelectedItem as string;
            Enum.TryParse(newAccent, out AppAccent appAccent);

            if (appAccent == CurrentAppAccent)
                return;

            AppThemeAndAccentControlsGrid.IsEnabled = false;

            Appearance.ChangeAppAccent(appAccent);
            SettingsHandler.AppAccent = appAccent;
            CurrentAppAccent = appAccent;

            AppThemeAndAccentControlsGrid.IsEnabled = true;
        }

        private void AccentSplitButton_Click(object sender, RoutedEventArgs e)
        {
            int currentAccentIndex = AccentSplitButton.SelectedIndex;
            int newAccentIndex;
            if (currentAccentIndex == AppAccents.Count - 1) { newAccentIndex = 0; }
            else
            {
                newAccentIndex = currentAccentIndex + 1;
            }

            Enum.TryParse(AppAccents[newAccentIndex], out AppAccent appAccent);

            if (appAccent == CurrentAppAccent)
                return;

            AppThemeAndAccentControlsGrid.IsEnabled = false;

            Appearance.ChangeAppAccent(appAccent);
            SettingsHandler.AppAccent = appAccent;
            CurrentAppAccent = appAccent;
            AccentSplitButton.SelectedIndex = newAccentIndex;
            
            AppThemeAndAccentControlsGrid.IsEnabled = true;
        }

        private void AppThemeLight_Radio_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAppTheme == AppTheme.Light || IsInitLaunch )
                return;

            AppThemeAndAccentControlsGrid.IsEnabled = false;

            Appearance.ChangeAppTheme(AppTheme.Light);
            SettingsHandler.AppTheme = AppTheme.Light;
            CurrentAppTheme = AppTheme.Light;

            AppThemeAndAccentControlsGrid.IsEnabled = true;
        }

        private void AppThemeDark_Radio_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAppTheme == AppTheme.Dark || IsInitLaunch)
                return;

            AppThemeAndAccentControlsGrid.IsEnabled = false;

            Appearance.ChangeAppTheme(AppTheme.Dark);
            SettingsHandler.AppTheme = AppTheme.Dark;
            CurrentAppTheme = AppTheme.Dark;

            AppThemeAndAccentControlsGrid.IsEnabled = true;
        }
    }
}
