using System.IO;
using System.IO.Compression;

namespace gtavmm_metro.Common
{
    public static class Archive
    {
        public static bool TryExtractZipFile(FileInfo zipFile, string outputDirectory, bool extractContentsToRoot = false)
        {
            try
            {
                string targetExtractionDirectory = extractContentsToRoot ? outputDirectory
                : Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(zipFile.Name));

                if (Directory.Exists(targetExtractionDirectory)) { Directory.Delete(targetExtractionDirectory, true); }

                ZipFile.ExtractToDirectory(zipFile.FullName, targetExtractionDirectory);
                if (File.Exists(zipFile.FullName)) { File.Delete(zipFile.FullName); }
            }
            catch
            {
                return false;
            }

            if (File.Exists(zipFile.FullName)) { File.Delete(zipFile.FullName); }
            return true;
        }
    }
}
