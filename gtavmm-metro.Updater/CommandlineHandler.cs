using System.IO;
using System.Text;
using System.Diagnostics;

namespace gtavmm_metro.Updater
{
    public class CommandlineHandler
    {
        private FileInfo _oldExecutable;
        private FileInfo _newExecutable;
        private DirectoryInfo _tempDirectory;

        public void ProcessArgs(string[] args)
        {
            string oldExecutablePath = "";
            string newExecutablePath = "";
            string tempDirectory = "";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-oldExecutablePath":
                        oldExecutablePath = args[++i];
                        break;
                    case "-newExecutablePath":
                        newExecutablePath = args[++i];
                        break;
                    case "-tempDirectory":
                        tempDirectory = args[++i];
                        break;
                }
            }

            _oldExecutable = new FileInfo(oldExecutablePath);
            _newExecutable = new FileInfo(newExecutablePath);
            _tempDirectory = new DirectoryInfo(tempDirectory);
        }

        public void CloseOldExecutableProcess()
        {
            Process[] oldProcesses = Process.GetProcessesByName(_oldExecutable.Name);
            if (oldProcesses.Length == 0) { return; }

            oldProcesses[0].CloseMainWindow();
        }

        public bool ReplaceOldExecutableWithNew()
        {
            string backupFilePath = Path.Combine(_oldExecutable.Directory.FullName, _oldExecutable.Name + ".bak");

            try
            {
                if (File.Exists(_oldExecutable.FullName))
                {
                    File.Move(_oldExecutable.FullName, backupFilePath);
                }

                File.Move(_newExecutable.FullName, _oldExecutable.FullName);
            }
            catch
            {
                // log to file?
                if (File.Exists(backupFilePath))
                {
                    File.Move(backupFilePath, _oldExecutable.FullName);
                }

                return false;
            }

            if (File.Exists(backupFilePath)) { File.Delete(backupFilePath); }
            return true;
        }

        public bool TryDeleteTempDirectory()
        {
            try { _tempDirectory.Delete(true); } catch { return false; }

            return true;
        }

        public bool StartOldProcess(bool updateSuccess, bool cleanupSuccess)
        {
            try
            {
                StringBuilder procArgs = new StringBuilder();

                if (!updateSuccess) { procArgs.Append("--updateFail "); }
                if (!cleanupSuccess) { procArgs.Append("--cleanupFail"); }

                ProcessStartInfo procInfo = new ProcessStartInfo(_oldExecutable.FullName);
                procInfo.Arguments = procArgs.ToString();
                Process.Start(procInfo);
            }
            catch { return false; }

            return true;
        }
    }
}
