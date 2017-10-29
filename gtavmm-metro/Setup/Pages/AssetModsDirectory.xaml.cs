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
    /// Interaction logic for AssetModsDirectory.xaml
    /// </summary>
    public partial class AssetModsDirectory : UserControl
    {
        private SetupMainWindow ParentWindow { get; set; }
        public DirectoryInfo AssetModsDirectoryConfirmedLocation { get; set; }

        public AssetModsDirectory(SetupMainWindow parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }

        private void BrowseAssetModsDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog folderSelectDialog = new CommonOpenFileDialog
            {
                Title = "Script Modifications Directory",
                IsFolderPicker = true,
                InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString(),
                DefaultDirectory = Environment.SpecialFolder.MyDocuments.ToString(),
                EnsurePathExists = true
            })
            {
                if (folderSelectDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    this.AssetModsDirectoryTextBlock.Text = folderSelectDialog.FileName;

                    if (this.IsDirectoryEmpty(folderSelectDialog.FileName))
                    {
                        this.AssetModsDirectoryTextBlock.BorderBrush = Brushes.Green;
                        this.AssetModsDirectoryConfirmedLocation = new DirectoryInfo(folderSelectDialog.FileName);

                        this.Finish.IsEnabled = true;
                    }
                    else
                    {
                        this.AssetModsDirectoryTextBlock.BorderBrush = Brushes.Red;

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
            this.ParentWindow.SetupContainer.Content = this.ParentWindow.ScriptModsDirectoryPage;
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.FinishSetup();
        }
    }
}
