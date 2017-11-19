using System;
using System.IO;
using System.Linq;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace gtavmm_metro.Setup.Pages
{
    /// <summary>
    /// Interaction logic for ModsDirectory.xaml
    /// </summary>
    public partial class ModsDirectory : UserControl
    {
        private SetupMainWindow ParentWindow { get; set; }
        public DirectoryInfo ModsDirectoryConfirmedLocation { get; set; }

        public ModsDirectory(SetupMainWindow parent)
        {
            this.ParentWindow = parent;
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

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.SetupContainer.Content = this.ParentWindow.GTAVDirectoryPage;
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.FinishSetup();
        }
    }
}
