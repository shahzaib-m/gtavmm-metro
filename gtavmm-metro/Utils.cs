using System;
using System.IO;
using System.Windows;
using System.Reflection;

namespace gtavmm_metro
{
    public static class Utils
    {
        public static string GetExecutingAssemblyName()
        {
            return GetExecutingAssemblyFile().Name;
        }

        public static Version GetExecutingAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public static FileInfo GetExecutingAssemblyFile()
        {
            return new FileInfo(Assembly.GetExecutingAssembly().Location);
        }

        public static DirectoryInfo GetExecutingAssemblyDirectory()
        {
            return GetExecutingAssemblyFile().Directory;
        }

        public static void MoveFile(string sourceFilePath, string destinationFilePath)
        {
            File.Copy(sourceFilePath, destinationFilePath, true);
            File.Delete(sourceFilePath);
        }

        public static void CopyDirectoryWithContents(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            foreach (string dir in Directory.GetDirectories(sourceDirectoryPath, "*", SearchOption.AllDirectories))
            {
                string newDirectoryPath = Path.Combine(destinationDirectoryPath, dir.Substring(sourceDirectoryPath.Length + 1));
                Directory.CreateDirectory(newDirectoryPath);
            }

            foreach (string file in Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories))
            {
                string newFilePath = Path.Combine(destinationDirectoryPath, file.Substring(sourceDirectoryPath.Length + 1));
                File.Copy(file, newFilePath, true);
            }
        }
    }
}
