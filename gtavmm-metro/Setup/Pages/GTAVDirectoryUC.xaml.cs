using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using Microsoft.WindowsAPICodePack.Dialogs;

using gtavmm_metro.Models;

namespace gtavmm_metro.Setup.Pages
{
    public partial class GTAVDirectoryUC : UserControl
    {
        public event EventHandler GoBackRequested;
        public event EventHandler GoForwardRequested;

        public DirectoryInfo GTAVDirectoryConfirmedLocation { get; set; }
        public bool IsSteamDRM { get; set; }

        private bool FirstLoad = true;

        public GTAVDirectoryUC()
        {
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

            gtavDirectories = GTAV.GetExpectedLocationDirectories(GTAVDRM.Steam);
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

                        if (this.AttemptAutoDetectGTADRM())
                        {
                            this.GoForward.IsEnabled = true;
                        }

                        this.DRMChooserPanel.IsEnabled = true;
                    });

                    return;
                }
            }

            gtavDirectories = GTAV.GetExpectedLocationDirectories(GTAVDRM.Rockstar);
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

                        if (this.AttemptAutoDetectGTADRM())
                        {
                            this.GoForward.IsEnabled = true;
                        }

                        this.DRMChooserPanel.IsEnabled = true;
                    });

                    return;
                }
            }
        }

        private bool AttemptAutoDetectGTADRM()
        {
            string gtavDir = this.GTAVDirectoryConfirmedLocation.FullName;

            string steamAttempt = Path.Combine(gtavDir, GTAV.GetDRMIdentifier(GTAVDRM.Steam));
            if (File.Exists(steamAttempt))
            {
                this.IsSteamDRM = true;
                this.SteamDRM_Radio.IsChecked = true;
                return true;
            }

            string rockstarAttempt = Path.Combine(gtavDir, GTAV.GetDRMIdentifier(GTAVDRM.Rockstar));
            if (File.Exists(rockstarAttempt))
            {
                this.IsSteamDRM = false;
                this.RockstarDRM_Radio.IsChecked = true;
                return true;
            }

            this.IsSteamDRM = false;
            this.SteamDRM_Radio.IsChecked = false;
            this.RockstarDRM_Radio.IsChecked = false;
            return false;
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

                        if (this.AttemptAutoDetectGTADRM())
                        {
                            this.GoForward.IsEnabled = true;
                        }
                        else
                        {
                            this.GoForward.IsEnabled = false;
                        }

                        this.DRMChooserPanel.IsEnabled = true;
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

        private void ManualDRMChoose_Click(object sender, RoutedEventArgs e) => this.GoForward.IsEnabled = true;

        private void GoBack_Click(object sender, RoutedEventArgs e) => GoBackRequested?.Invoke(this, null);
        private void GoForward_Click(object sender, RoutedEventArgs e) => GoForwardRequested?.Invoke(this, null);
    }
}
