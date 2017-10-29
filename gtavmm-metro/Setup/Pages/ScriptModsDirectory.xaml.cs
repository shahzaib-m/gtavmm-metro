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
    /// Interaction logic for ScriptModsDirectory.xaml
    /// </summary>
    public partial class ScriptModsDirectory : UserControl
    {
        private SetupMainWindow ParentWindow { get; set; }
        public DirectoryInfo ScriptModsDirectoryConfirmedLocation { get; set; }

        public ScriptModsDirectory(SetupMainWindow parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }

        private void BrowseScriptModsDirectory_Click(object sender, RoutedEventArgs e)
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
                    this.ScriptModsDirectoryTextBlock.Text = folderSelectDialog.FileName;

                    if (this.IsDirectoryEmpty(folderSelectDialog.FileName))
                    {
                        this.ScriptModsDirectoryTextBlock.BorderBrush = Brushes.Green;
                        this.ScriptModsDirectoryConfirmedLocation = new DirectoryInfo(folderSelectDialog.FileName);

                        this.GoForward.IsEnabled = true;
                    }
                    else
                    {
                        this.ScriptModsDirectoryTextBlock.BorderBrush = Brushes.Red;

                        this.GoForward.IsEnabled = false;
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

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.SetupContainer.Content = this.ParentWindow.AssetModsDirectoryPage;
        }
    }
}
