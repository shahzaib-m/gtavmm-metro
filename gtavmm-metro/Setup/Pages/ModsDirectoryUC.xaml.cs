using System;
using System.IO;
using System.Linq;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace gtavmm_metro.Setup.Pages
{
    public partial class ModsDirectoryUC : UserControl
    {
        public event EventHandler GoBackRequested;
        public event EventHandler FinishSetupRequested;

        public DirectoryInfo ModsDirectoryConfirmedLocation { get; set; }

        public ModsDirectoryUC()
        {
            InitializeComponent();
        }

        private void BrowseModsDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog folderSelectDialog = new CommonOpenFileDialog
            {
                Title = "Modifications Directory",
                IsFolderPicker = true,
                InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString(),
                DefaultDirectory = Environment.SpecialFolder.MyDocuments.ToString(),
                EnsurePathExists = true
            })
            {
                if (folderSelectDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    this.ModsDirectoryTextBlock.Text = folderSelectDialog.FileName;

                    if (this.IsDirectoryEmpty(folderSelectDialog.FileName) || File.Exists(Path.Combine(folderSelectDialog.FileName, "data.gtavmm-metro")))
                    {
                        this.ModsDirectoryTextBlock.BorderBrush = Brushes.Green;
                        this.ModsDirectoryConfirmedLocation = new DirectoryInfo(folderSelectDialog.FileName);

                        this.Finish.IsEnabled = true;
                    }
                    else
                    {
                        this.ModsDirectoryTextBlock.BorderBrush = Brushes.Red;

                        this.Finish.IsEnabled = false;
                    }
                }
            }
        }

        private bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        private void GoBack_Click(object sender, RoutedEventArgs e) => GoBackRequested?.Invoke(this, null);
        private void Finish_Click(object sender, RoutedEventArgs e) => FinishSetupRequested?.Invoke(this, null);
    }
}
