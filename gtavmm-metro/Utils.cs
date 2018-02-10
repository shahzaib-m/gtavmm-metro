using System;
using System.IO;
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
    }
}
