using System;
using System.IO;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;

using System.Windows.Media;
using System.Collections.Generic;

using Microsoft.WindowsAPICodePack.Dialogs;

using gtavmm_metro.Models;

namespace gtavmm_metro.Setup.Pages
{
    /// <summary>
    /// Interaction logic for GTAVDirectory.xaml
    /// </summary>
    public partial class GTAVDirectory : UserControl
    {
        private SetupMainWindow ParentWindow { get; set; }
        public DirectoryInfo GTAVDirectoryConfirmedLocation { get; set; }
        public bool IsSteamDRM { get; set; }
        private bool FirstLoad = true;

        public GTAVDirectory(SetupMainWindow parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }

        private async void GTAVDirectorySelect_Loaded(object sender, RoutedEventArgs e)
        {
            if (FirstLoad)
            {
                await Task.Run(() => AttemptAutoDetectGTADir());
                FirstLoad = false;
            }
        }

        private void AttemptAutoDetectGTADir()
        {
            List<string> gtavDirectories;
            string gtavExePath;

            gtavDirectories = GTAVSteam.GetExpectedLocationDirectories();
            foreach (string expectedSteamLocationDir in gtavDirectories)
            {
                gtavExePath = Path.Combine(expectedSteamLocationDir, "GTA5.exe");
                if (File.Exists(gtavExePath))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.GTAVDirectoryTextBlock.Text = expectedSteamLocationDir;
                        this.GTAVDirectoryConfirmedLocation = new DirectoryInfo(expectedSteamLocationDir);
                        this.GTAVDirectoryTextBlock.BorderBrush = Brushes.Green;

                        this.AttemptAutoDetectGTADRM();
                        this.DRMChooserPanel.IsEnabled = true;

                        this.GoForward.IsEnabled = true;
                    });

                    return;
                }
            }

            gtavDirectories = GTAVRockstar.GetExpectedLocationDirectories();
            foreach (string expectedRockstarLocationDir in gtavDirectories)
            {
                gtavExePath = Path.Combine(expectedRockstarLocationDir, "GTA5.exe");
                if (File.Exists(gtavExePath))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.GTAVDirectoryTextBlock.Text = expectedRockstarLocationDir;
                        this.GTAVDirectoryConfirmedLocation = new DirectoryInfo(expectedRockstarLocationDir);
                        this.GTAVDirectoryTextBlock.BorderBrush = Brushes.Green;

                        this.AttemptAutoDetectGTADRM();
                        this.DRMChooserPanel.IsEnabled = true;

                        this.GoForward.IsEnabled = true;
                    });

                    return;
                }
            }
        }

        private void AttemptAutoDetectGTADRM()
        {
            string gtavDir = this.GTAVDirectoryConfirmedLocation.FullName;

            string steamAttempt = Path.Combine(gtavDir, GTAVSteam.DRMIdentifierFile);
            if (File.Exists(steamAttempt))
            {
                this.IsSteamDRM = true;
                this.SteamDRM_Radio.IsChecked = true;
                return;
            }

            string rockstarAttempt = Path.Combine(gtavDir, GTAVRockstar.DRMIdentifierFile);
            if (File.Exists(rockstarAttempt))
            {
                this.IsSteamDRM = false;
                this.RockstarDRM_Radio.IsChecked = true;
                return;
            }

            this.IsSteamDRM = false;
            this.SteamDRM_Radio.IsChecked = false;
            this.RockstarDRM_Radio.IsChecked = false;
            return;
        }

        private void BrowseGTAVDirectory_Click(object sender, RoutedEventArgs e)
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
                    this.GTAVDirectoryTextBlock.Text = folderSelectDialog.FileName;
                     
                    string gtavExePath = Path.Combine(folderSelectDialog.FileName, "GTA5.exe");
                    if (File.Exists(gtavExePath))
                    {
                        this.GTAVDirectoryTextBlock.BorderBrush = Brushes.Green;
                        this.GTAVDirectoryConfirmedLocation = new DirectoryInfo(folderSelectDialog.FileName);

                        this.AttemptAutoDetectGTADRM();
                        this.DRMChooserPanel.IsEnabled = true;

                        this.GoForward.IsEnabled = true;
                    }
                    else
                    {
                        this.GTAVDirectoryTextBlock.BorderBrush = Brushes.Red;

                        this.DRMChooserPanel.IsEnabled = false;
                        this.GoForward.IsEnabled = false;
                    }
                }
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.SetupContainer.Content = this.ParentWindow.WelcomePage;
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
           this.ParentWindow.SetupContainer.Content = this.ParentWindow.ModsDirectoryPage;
        }
    }
}
