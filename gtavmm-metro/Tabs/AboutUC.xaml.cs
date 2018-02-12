using System;
using System.IO;
using System.Diagnostics;

using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using MahApps.Metro.IconPacks;

using gtavmm_metro.Common;

namespace gtavmm_metro.Tabs
{
    public partial class AboutUC : UserControl
    {
        private const string UPDATER_ARGS_TEMPLATE = "-oldExecutablePath \"{0}\" -newExecutablePath \"{1}\" -tempDirectory \"{2}\"";

        private const string GITHUB_URL = "https://github.com/shahzaib-m/gtavmm-metro";
        private UpdateHandler UpdateHandler;

        public AboutUC(UpdateHandler updateHandler)
        {
            this.UpdateHandler = updateHandler;
            InitializeComponent();

            Version currentVer = this.UpdateHandler.CurrentVersion;
            this.VersionTextBlock.Text = String.Format("GTAV Mod Manager Metro {0}.{1}.{2}",
                currentVer.Major, currentVer.Minor, currentVer.Build);
        }

        public void SetUpdateDetails()
        {
            this.UpdateMainIcon.Kind = PackIconMaterialKind.Update;
            this.UpdateMainIcon.Foreground = Brushes.Orange;

            Version latestVer = this.UpdateHandler.LatestVersion;
            this.UpdateMainText.Text = String.Format("An update is available! ({0}.{1}.{2})",
                latestVer.Major, latestVer.Minor, latestVer.Build);

            this.UpdateDownloadButton.Visibility = Visibility.Visible;
            this.ViewReleaseOnGithubButton.Visibility = Visibility.Visible;
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(GITHUB_URL);
        }

        private void ViewReleaseOnGithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(this.UpdateHandler.ReleaseUrl);
        }

        private async void UpdateDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateMainText.Text = "Starting download...";
            this.UpdateDownloadButton.IsEnabled = false;

            this.UpdateMainIcon.Visibility = Visibility.Collapsed;
            this.UpdateMainProgress.Visibility = Visibility.Visible;

            string workingDirectory = Utils.GetExecutingAssemblyDirectory().FullName;
            this.UpdateHandler.UpdateDownloadProgressChanged += (s, eArgs) =>  
                this.UpdateMainText.Text = "Downloading... (" + eArgs.ProgressPercentage + "%)";

            bool downloadSuccess = await this.UpdateHandler.DownloadUpdateAsync();
            if (!downloadSuccess)
            {
                this.UpdateMainText.Text = "Failed to download. Please try again.";
                this.UpdateDownloadButton.IsEnabled = true;

                return;
            }

            this.UpdateMainProgress.Visibility = Visibility.Collapsed;
            this.UpdateMainIcon.Kind = PackIconMaterialKind.Download;
            this.UpdateMainIcon.Visibility = Visibility.Visible;

            this.UpdateMainText.Text = "Ready to install! (This will restart the application)";
            this.UpdateDownloadButton.Visibility = Visibility.Collapsed;
            this.UpdateInstallButton.Visibility = Visibility.Visible;
        }

        private void UpdateInstallButton_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateMainText.Text = "Installing...";
            this.UpdateInstallButton.IsEnabled = false;

            this.UpdateMainProgress.Visibility = Visibility.Visible;

            string thisExeFullPath = Utils.GetExecutingAssemblyFile().FullName;
            bool moveNonCoreSuccess = this.UpdateHandler.UpdateAllNonCoreUpdateFiles(new FileInfo(thisExeFullPath).Name);
            if (!moveNonCoreSuccess)
            {
                this.UpdateMainText.Text = "Installation failed. Please try re-downloading.";
                this.UpdateInstallButton.IsEnabled = true;
                this.UpdateInstallButton.Visibility = Visibility.Collapsed;
                this.UpdateDownloadButton.Visibility = Visibility.Visible;
                this.UpdateDownloadButton.IsEnabled = true;

                return;
            }


            string workingDirectory = Directory.GetParent(thisExeFullPath).FullName;
            string updateDirectory = Path.GetFileNameWithoutExtension(this.UpdateHandler.UpdateZipName);

            ProcessStartInfo updaterProcess = new ProcessStartInfo(Path.Combine(workingDirectory, "GTAVModManagerMetroUpdater.exe"));
            updaterProcess.Arguments = String.Format(UPDATER_ARGS_TEMPLATE,
                thisExeFullPath,
                Path.Combine(workingDirectory, updateDirectory, Utils.GetExecutingAssemblyName()),
                Path.Combine(workingDirectory, updateDirectory)
            );

            Process.Start(updaterProcess);
        }
    }
}
