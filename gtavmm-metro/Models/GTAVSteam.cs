using System.Collections.Generic;

namespace gtavmm_metro.Models
{
    public static class GTAVSteam
    {
        public static string DRMIdentifierFile { get { return "steam_api64.dll"; } }

        public static List<string> GetExpectedLocationDirectories()
        {
            return new List<string>
            {
                @"C:\Program Files\Steam\steamapps\common\Grand Theft Auto V",
                @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V",
            };
        }

        public static List<string> GetGameFiles()
        {
            return new List<string>
            {
                @"TODO",
                @"TODO"
            };
        }
    }
}
