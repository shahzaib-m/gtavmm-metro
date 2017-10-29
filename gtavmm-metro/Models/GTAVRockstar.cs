using System;
using System.Collections.Generic;

namespace gtavmm_metro.Models
{
    public static class GTAVRockstar
    {
        public static string DRMIdentifierFile { get { return "PlayGTAV.exe"; } }

        public static List<string> GetExpectedLocationDirectories()
        {
            return new List<string>
            {
                @"C:\Program Files\Rockstar Games\Grand Theft Auto V",
                @"C:\Program Files (x86)\Rockstar Games\Grand Theft Auto V",
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
