using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;

using System.Threading.Tasks;

using System.Windows;
using System.Windows.Resources;

using gtavmm_metro.Properties;

namespace gtavmm_metro.Models
{
    public enum GTAVDRM { Steam, Rockstar }
    public enum GTAVMode { Offline, Online }

    public class GTAVOptions
    {
        public bool OfflineMode { get; set; }
    }
    public class GTAOOptions
    {
        public bool StraightToFreemode { get; set; }
    }

    public class GTAV
    {
        private static readonly string SteamDRMIdentifier = "steam_api64.dll";
        private static readonly string RockstarDRMIdentifier = "PlayGTAV.exe";

        private static readonly string GTAVSteamProtocolURL = "steam://run/271590";
        private static readonly string GTAVRockstarEntryExe = "PlayGTAV.exe";

        private string GamePath;
        private GTAVDRM TargetDRM;
        private GTAVMode GameMode;

        private Process GTAVLauncherProcess;
        private Timer GTAVLauncherDiscoveryTimer;
        public event EventHandler GTAVStarted;
        public event EventHandler GTAVExited;

        public GTAV(string gamePath, GTAVMode gameMode, GTAVDRM targetDRM)
        {
            this.GamePath = gamePath;
            this.GameMode = gameMode;
            this.TargetDRM = targetDRM;
        }

        public async Task<bool> SetOptions(GTAVOptions gameOptions)
        {
            List<string> optionStrings = this.GetGameOptionsAsStrings(gameOptions);
            if (optionStrings.Any()) { return await this.CreateCommandLineTxtFile(optionStrings); }

            return true;
        }
        public async Task<bool> SetOptions(GTAOOptions gameOptions)
        {
            List<string> optionStrings = this.GetGameOptionsAsStrings(gameOptions);
            if (optionStrings.Any()) { return await this.CreateCommandLineTxtFile(optionStrings); }

            return true;
        }
        private List<string> GetGameOptionsAsStrings(GTAVOptions gameOptions)
        {
            List<string> optionStrings = new List<string>();
            if (gameOptions.OfflineMode) { optionStrings.Add("-scOfflineOnly"); }

            return optionStrings;
        }
        private List<string> GetGameOptionsAsStrings(GTAOOptions gameOptions)
        {
            List<string> optionStrings = new List<string>();
            if (gameOptions.StraightToFreemode) { optionStrings.Add("-StraightIntoFreemode"); }

            return optionStrings;
        }
        private async Task<bool> CreateCommandLineTxtFile(List<string> optionStrings)
        {
            string outputFilePath = Path.Combine(this.GamePath, "commandline.txt");

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(File.Open(outputFilePath, FileMode.Create)))
                {
                    foreach (string optionString in optionStrings)
                    {
                        await streamWriter.WriteLineAsync(optionString);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                {
                    return false;
                }

                throw;
            }

            return true;
        }
        public bool DeleteOptionsFile()
        {
            try { File.Delete(Path.Combine(this.GamePath, "commandline.txt")); }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException) { return false; }

                throw;
            }

            return true;
        }

        public bool InsertScriptMod(ScriptMod scriptMod)
        {
            scriptMod.FilesWithPath = new List<string>();

            string modsPath = Path.Combine(Settings.Default.ModsDirectory, "Script Mods");
            string scriptModDir = scriptMod.Name;
            DirectoryInfo fullModificationPath = new DirectoryInfo(Path.Combine(modsPath, scriptModDir));

            FileInfo[] allModFiles = fullModificationPath.GetFiles("*", SearchOption.AllDirectories);
            foreach (FileInfo modFile in allModFiles)
            {
                string fullPath = modFile.FullName;
                string relativePathWithFile = fullPath.Split(new[] { fullModificationPath + @"\" },
                    StringSplitOptions.RemoveEmptyEntries)[0];
                scriptMod.FilesWithPath.Add(relativePathWithFile);

                string relativePathWithoutFile = Path.GetDirectoryName(relativePathWithFile);
                if (!String.IsNullOrWhiteSpace(relativePathWithoutFile))
                { Directory.CreateDirectory(Path.Combine(this.GamePath, relativePathWithoutFile)); }

                try
                {
                    string destinationPath = Path.Combine(this.GamePath, relativePathWithFile);
                    if (File.Exists(destinationPath)) { File.Delete(destinationPath); }

                    File.Move(fullPath, destinationPath);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is PathTooLongException) { return false; }

                    throw;
                }
            }

            return true;
        }
        /// <summary>
        /// Move all script modifications back to the relevant owner modification.
        /// </summary>
        public bool MoveScriptModBack(ScriptMod scriptMod)
        {
            string modRootFullPath = Path.Combine(Settings.Default.ModsDirectory, "Script Mods", scriptMod.Name);

            foreach (string modFilePath in scriptMod.FilesWithPath)
            {
                string fileFullPath = Path.Combine(this.GamePath, modFilePath);
                if (File.Exists(fileFullPath))
                {
                    try { File.Move(Path.Combine(this.GamePath, modFilePath), Path.Combine(modRootFullPath, modFilePath)); }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is UnauthorizedAccessException || ex is PathTooLongException) { return false; }

                        throw;
                    }
                }
            }

            return true;
        }
        public static bool MoveScriptModBack(ScriptMod scriptMod, string gamePath)
        {
            string modRootFullPath = Path.Combine(Settings.Default.ModsDirectory, "Script Mods", scriptMod.Name);

            foreach (string modFilePath in scriptMod.FilesWithPath)
            {
                string fileFullPath = Path.Combine(gamePath, modFilePath);
                if (File.Exists(fileFullPath))
                {
                    try { File.Move(Path.Combine(gamePath, modFilePath), Path.Combine(modRootFullPath, modFilePath)); }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is UnauthorizedAccessException || ex is PathTooLongException) { return false; }

                        throw;
                    }
                }
            }

            return true;
        }
        public async Task DeleteRemainingRootFoldersWithNoFiles()
        {
            List<string> coreGameFolders = await GetCoreGameFolders();

            await Task.Run(() =>
            {
                foreach (string directory in Directory.GetDirectories(this.GamePath))
                {
                    if (!coreGameFolders.Contains(new DirectoryInfo(directory).Name))
                    {
                        bool hasNoFiles = !Directory.EnumerateFiles(directory).Any();
                        if (hasNoFiles) { Directory.Delete(directory, true); }
                    }
                }
            });
        }
        public async Task<List<FileInfo>> DiscoverUnknownLeftoverFiles()
        {
            List<FileInfo> unknownLeftoverFiles = new List<FileInfo>();
            List<string> coreGameFiles = await GetCoreGameFiles();

            await Task.Run(() =>
            {
                foreach (string file in Directory.GetFiles(this.GamePath))
                {
                    FileInfo fileInfo = new FileInfo(file);

                    if (!coreGameFiles.Contains(fileInfo.Name))
                    {
                        unknownLeftoverFiles.Add(fileInfo);
                    }
                }
            });

            return unknownLeftoverFiles;
        }
        public async Task<List<DirectoryInfo>> DiscoverUnknownLeftoverNonEmptyFolders()
        {
            List<DirectoryInfo> unknownLeftoverFolders = new List<DirectoryInfo>();
            List<string> coreGameFolders = await GetCoreGameFolders();

            await Task.Run(() =>
            {
                foreach (string dir in Directory.GetDirectories(this.GamePath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dir);

                    if (!coreGameFolders.Contains(dirInfo.Name))
                    {
                        unknownLeftoverFolders.Add(dirInfo);
                    }
                }
            });

            return unknownLeftoverFolders;
        }

        public bool InsertAssetMod(AssetMod assetMod)
        {
            string modsPath = Path.Combine(Settings.Default.ModsDirectory, "Asset Mods");

            Directory.CreateDirectory(Path.Combine(this.GamePath, "mods"));

            string targetRelativePath = assetMod.TargetRPF;
            targetRelativePath = targetRelativePath.Substring(1);   // substring the prefix slash out for Path.Combine()

            string relativePathWithoutFile = Path.GetDirectoryName(targetRelativePath);
            if (!String.IsNullOrWhiteSpace(relativePathWithoutFile))
            { Directory.CreateDirectory(Path.Combine(this.GamePath, "mods", relativePathWithoutFile)); }

            string targetFullPath = Path.Combine(this.GamePath, "mods", targetRelativePath);
            try
            {
                if (File.Exists(targetFullPath)) { File.Delete(targetFullPath); }

                File.Move(Path.Combine(Settings.Default.ModsDirectory, "Asset Mods", targetRelativePath), targetFullPath);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is PathTooLongException) { return false; }

                throw;
            }

            return true;
        }
        public bool MoveAssetModBack(AssetMod assetMod)
        {
            string modRootFullPath = Path.Combine(Settings.Default.ModsDirectory, "Asset Mods");

            string fullRpfPath = Path.Combine(this.GamePath, "mods", assetMod.TargetRPF.Substring(1));
            if (!File.Exists(fullRpfPath)) { return false; }

            try
            {
                File.Move(fullRpfPath, Path.Combine(modRootFullPath, assetMod.TargetRPF.Substring(1)));
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is PathTooLongException) { return false; }

                throw;
            }

            return true;
        }
        public static bool MoveAssetModBack(AssetMod assetMod, string gamePath)
        {
            string modRootFullPath = Path.Combine(Settings.Default.ModsDirectory, "Asset Mods");

            string fullRpfPath = Path.Combine(gamePath, "mods", assetMod.TargetRPF.Substring(1));
            if (!File.Exists(fullRpfPath)) { return false; }

            try
            {
                File.Move(fullRpfPath, Path.Combine(modRootFullPath, assetMod.TargetRPF.Substring(1)));
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException || ex is PathTooLongException) { return false; }

                throw;
            }

            return true;
        }
        public bool DeleteModsFolderInGTAVDirectory()
        {
            string targetPath = Path.Combine(this.GamePath, "mods");
            if (!Directory.Exists(targetPath)) { return true; }

            string modsFolderInGTAVDirPath = Path.Combine(this.GamePath, "mods");
            if (!Directory.EnumerateFiles(modsFolderInGTAVDirPath).Any())
            {
                try { Directory.Delete(modsFolderInGTAVDirPath, true); }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is IOException) { return false; }

                    throw;
                }
                
                return true;
            }

            return false;
        }
        public static bool DeleteModsFolderInGTAVDirectory(string gamePath)
        {
            string targetPath = Path.Combine(gamePath, "mods");
            if (!Directory.Exists(targetPath)) { return true; }

            string modsFolderInGTAVDirPath = Path.Combine(gamePath, "mods");
            if (!Directory.EnumerateFiles(modsFolderInGTAVDirPath).Any())
            {
                try { Directory.Delete(modsFolderInGTAVDirPath, true); }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is IOException) { return false; }

                    throw;
                }

                return true;
            }

            return false;
        }

        public bool StartGTAV(GTAVOptions gameOptions)
        {
            bool procLaunchResult = this.StartGTAVProcess();
            if (!procLaunchResult) { return false; }

            return true;
        }
        public bool StartGTAO(GTAOOptions gameOptions)
        {
            bool procLaunchResult = this.StartGTAVProcess();
            if (!procLaunchResult) { return false; }

            return true;
        }

        public void CancelGTALaunch()
        {
            this.GTAVLauncherDiscoveryTimer.Stop();
            this.GTAVLauncherDiscoveryTimer.Enabled = false;
            this.GTAVLauncherDiscoveryTimer = null;
        }

        private bool StartGTAVProcess()
        {
            try
            {
                Process initialProc = new Process();

                if (this.TargetDRM == GTAVDRM.Steam) { initialProc.StartInfo.FileName = GTAVSteamProtocolURL; }
                else { initialProc.StartInfo.FileName = Path.Combine(this.GamePath, GTAVRockstarEntryExe); }

                initialProc.EnableRaisingEvents = true;
                initialProc.Exited += (sender, e) => this.SetupGTAVLauncherProcessTimer();

                initialProc.Start();
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException || ex is Win32Exception || ex is ObjectDisposedException)
                {
                    return false;
                }

                throw;
            }

            return true;
        }
        private void SetupGTAVLauncherProcessTimer()
        {
            this.GTAVLauncherDiscoveryTimer = new Timer();
            this.GTAVLauncherDiscoveryTimer.Elapsed += GTAVLauncherProcessTimerElapsed;
            this.GTAVLauncherDiscoveryTimer.Interval = 500;
            this.GTAVLauncherDiscoveryTimer.AutoReset = false;
            this.GTAVLauncherDiscoveryTimer.Enabled = true;
        }
        private void GTAVLauncherProcessTimerElapsed(object sender, EventArgs e)
        {
            Process[] gtavProcs = Process.GetProcessesByName("GTAVLauncher");
            if (gtavProcs.Length != 0)
            {
                this.GTAVLauncherProcess = gtavProcs.First();
                this.GTAVLauncherProcess.EnableRaisingEvents = true;
                this.GTAVLauncherProcess.Exited += this.GTAVExited;

                if (this.GTAVStarted != null)
                    this.GTAVStarted(this, null);
            }
            else
            {
                this.GTAVLauncherDiscoveryTimer.Start();
            }
        }

        public static string GetDRMIdentifier(GTAVDRM targetDRM)
        {
            if (targetDRM == GTAVDRM.Steam) { return SteamDRMIdentifier; }
            else if (targetDRM == GTAVDRM.Rockstar) { return RockstarDRMIdentifier; }

            return null;
        }
        public string GetDRMIdentifier()
        {
            return GetDRMIdentifier(this.TargetDRM);
        }

        public static List<string> GetExpectedLocationDirectories(GTAVDRM targetDRM)
        {
            if (targetDRM == GTAVDRM.Steam)
            {
                return new List<string>()
                {
                    @"C:\Program Files\Steam\steamapps\common\Grand Theft Auto V",
                    @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V"
                };
            }
            else if (targetDRM == GTAVDRM.Rockstar)
            {
                return new List<string>
                {
                    @"C:\Program Files\Rockstar Games\Grand Theft Auto V",
                    @"C:\Program Files (x86)\Rockstar Games\Grand Theft Auto V",
                };
            }

            return null;
        }
        public List<string> GetExpectedLocationDirectories()
        {
            return GetExpectedLocationDirectories(this.TargetDRM);
        }

        public async static Task<List<string>> GetCoreGameFiles()
        {
            List<string> gtavCoreGameFiles = new List<string>();

            StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Data/GTAVCoreGameFiles.txt"));
            using (StreamReader reader = new StreamReader(streamInfo.Stream))
            {
                while (!reader.EndOfStream)
                {
                    gtavCoreGameFiles.Add(await reader.ReadLineAsync());
                }
            }

            return gtavCoreGameFiles;
        }
        public async static Task<List<string>> GetCoreGameFolders()
        {
            List<string> gtavCoreGameFolders = new List<string>();

            StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Data/GTAVCoreGameFolders.txt"));
            using (StreamReader reader = new StreamReader(streamInfo.Stream))
            {
                while (!reader.EndOfStream)
                {
                    gtavCoreGameFolders.Add(await reader.ReadLineAsync());
                }
            }

            return gtavCoreGameFolders;
        }

        public static List<string> GetAllRPFsInsideGTAVDirectory(string GTAVPath)
        {
            DirectoryInfo directory = new DirectoryInfo(GTAVPath);
            FileInfo[] allRPFTopLevel = directory.GetFiles("*.rpf", SearchOption.TopDirectoryOnly);

            List<string> RPFNames = new List<string>();
            foreach (FileInfo RPF in allRPFTopLevel)
            {
                string fullPath = RPF.FullName;
                RPFNames.Add(fullPath.Split(new string[] { Settings.Default.GTAVDirectory }, StringSplitOptions.RemoveEmptyEntries)[0]);
            }

            DirectoryInfo[] allSubDirs = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach(DirectoryInfo subDir in allSubDirs)
            {
                if (subDir.Name.ToLower() == "mods") { continue; }

                FileInfo[] allRPFWithinSubDirRecursive = subDir.GetFiles("*.rpf", SearchOption.AllDirectories);
                foreach(FileInfo RPF in allRPFWithinSubDirRecursive)
                {
                    string fullPath = RPF.FullName;
                    RPFNames.Add(fullPath.Split(new string[] { Settings.Default.GTAVDirectory }, StringSplitOptions.RemoveEmptyEntries)[0]);
                }
            }

            return RPFNames;
        }
    }
}
