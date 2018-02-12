using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Net;
using System.Net.Http;

using Newtonsoft.Json;

namespace gtavmm_metro.Common
{
    public class UpdateHandler
    {
        private const string GITHUB_REPO_URL = "https://api.github.com/repos/shahzaib-m/gtavmm-metro";
        private const string GITHUB_REPO_LATEST_RELEASE_URL = "https://api.github.com/repos/shahzaib-m/gtavmm-metro/releases/latest";

        private DirectoryInfo WorkingDirectory;

        public Version CurrentVersion { get; private set; }
        public Version LatestVersion { get; private set; } = null;
        public string ReleaseUrl { get; private set; }
        public string UpdateZipName { get; set; }
        public int UpdateZipByteSize { get; private set; }
        public string UpdateZipUrl { get; private set; }

        public event DownloadProgressChangedEventHandler UpdateDownloadProgressChanged;

        public UpdateHandler(Version currentVersion, string appWorkingDirectory)
        {
            CurrentVersion = currentVersion;
            WorkingDirectory = new DirectoryInfo(appWorkingDirectory);
        }

        public async Task<bool> CheckForUpdateAsync()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "GTAV Mod Manager Metro/" + CurrentVersion);

                try
                {
                    string response = await httpClient.GetStringAsync(GITHUB_REPO_LATEST_RELEASE_URL);
                    GithubReleaseModel latestRelease = JsonConvert.DeserializeObject<GithubReleaseModel>(response);

                    LatestVersion = new Version(latestRelease.Tag_name.Substring(1));
                    ReleaseUrl = latestRelease.Html_url;
                    UpdateZipName = latestRelease.Assets[0].Name;
                    UpdateZipByteSize = latestRelease.Assets[0].Size;
                    UpdateZipUrl = latestRelease.Assets[0].Browser_download_url;
                }
                catch { return false; }

                return true;
            }
        }

        public bool IsUpdateAvailable()
        {
            if (LatestVersion == null) { return false; }
            else
            {
                if (CurrentVersion.CompareTo(LatestVersion) < 0)
                    return true;
            }

            return false;
        }

        public async Task<bool> DownloadUpdateAsync()
        {
            try
            {
                FileInfo outputZipFile = new FileInfo(Path.Combine(WorkingDirectory.FullName, UpdateZipName));

                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += this.UpdateDownloadProgressChanged;
                    await client.DownloadFileTaskAsync(UpdateZipUrl, Path.Combine(WorkingDirectory.FullName, UpdateZipName));
                }

                bool tryExtract = Archive.TryExtractZipFile(outputZipFile, WorkingDirectory.FullName);
                if (!tryExtract) { return false; }
            }
            catch (Exception ex)
            {
                if (ex is WebException) { return false; }

                throw;
            }

            return true;
        }

        public bool UpdateAllNonCoreUpdateFiles(string coreExecutableName)
        {
            try
            {
                string updateFolderPath = Path.Combine(WorkingDirectory.FullName, Path.GetFileNameWithoutExtension(UpdateZipName));
                FileInfo[] updateFiles = new DirectoryInfo(updateFolderPath).GetFiles();

                foreach (FileInfo updateFile in updateFiles)
                {
                    if (updateFile.Name != coreExecutableName)
                    {
                        string fileNewPath = Path.Combine(WorkingDirectory.FullName, updateFile.Name);
                        if (File.Exists(fileNewPath))
                        {
                            string backupFilePath = fileNewPath + ".bak";
                            if (File.Exists(backupFilePath)) { File.Delete(backupFilePath); } 

                            File.Move(fileNewPath, backupFilePath);
                        }

                        File.Move(updateFile.FullName, fileNewPath);
                    }
                }
            }
            catch
            {
                foreach (FileInfo file in WorkingDirectory.GetFiles())
                {
                    if (file.Name.EndsWith(".bak"))
                        File.Move(file.FullName, file.FullName.Substring(0, file.FullName.Length - 4));
                }

                return false;
            }

            foreach (FileInfo file in WorkingDirectory.GetFiles())
                if (file.Name.EndsWith(".bak")) { File.Delete(file.FullName); }

            return true;
        }
    }

    internal class GithubReleaseModel
    {
        public string Tag_name { get; set; }
        public string Html_url { get; set; }
        public List<GithubAssetModel> Assets { get; set; }
    }

    internal class GithubAssetModel
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public string Browser_download_url { get; set; }
    }
}