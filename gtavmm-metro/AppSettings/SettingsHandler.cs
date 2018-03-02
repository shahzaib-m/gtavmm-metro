using System;

using gtavmm_metro.Properties;

namespace gtavmm_metro.AppSettings
{
    public static class SettingsHandler
    {
        public static event EventHandler GTAVDirectoryChanged;
        public static event EventHandler ModsDirectoryChanged;

        public static bool IsFirstLaunch
        {
            get { return Settings.Default.IsFirstLaunch; }
            set { Settings.Default.IsFirstLaunch = value; }
        }

        public static string GTAVDirectory
        {
            get { return Settings.Default.GTAVDirectory; }
            set { Settings.Default.GTAVDirectory = value; GTAVDirectoryChanged?.Invoke(typeof(SettingsHandler), EventArgs.Empty); }
        }
        public static bool IsSteamDRM
        {
            get { return Settings.Default.IsSteamDRM; }
            set { Settings.Default.IsSteamDRM = value; }
        }

        public static string ModsDirectory
        {
            get { return Settings.Default.ModsDirectory; }
            set { Settings.Default.ModsDirectory = value; ModsDirectoryChanged?.Invoke(typeof(SettingsHandler), EventArgs.Empty); }
        }

        public static bool GTAVModsScriptMods_IsChecked
        {
            get { return Settings.Default.GTAVModsScriptMods_IsChecked; }
            set { Settings.Default.GTAVModsScriptMods_IsChecked = value; }
        }
        public static bool GTAVModsAssetMods_IsChecked
        {
            get { return Settings.Default.GTAVModsAssetMods_IsChecked; }
            set { Settings.Default.GTAVModsAssetMods_IsChecked = value; }
        }
        public static bool GTAVOptionsOfflineMode_IsChecked
        {
            get { return Settings.Default.GTAVOptionsOfflineMode_IsChecked; }
            set { Settings.Default.GTAVOptionsOfflineMode_IsChecked = value; }
        }

        public static bool GTAOOptionsStraightIntoFreemode_IsChecked
        {
            get { return Settings.Default.GTAOOptionsStraightIntoFreemode_IsChecked; }
            set { Settings.Default.GTAOOptionsStraightIntoFreemode_IsChecked = value; }
        }

        public static AppTheme AppTheme
        {
            get
            {
                string appThemeString = Settings.Default.AppTheme;
                switch (appThemeString)
                {
                    case "BaseLight":
                        return AppTheme.Light;
                    case "BaseDark":
                    default:
                        return AppTheme.Dark;
                }
            }
            set
            {
                string appThemeString = null;

                switch (value)
                {
                    case AppTheme.Light:
                        appThemeString = "BaseLight";
                        break;
                    case AppTheme.Dark:
                        appThemeString = "BaseDark";
                        break;
                }

                Settings.Default.AppTheme = appThemeString;
            }
        }
        public static string GetAppThemeAsString()
        {
            return Settings.Default.AppTheme;
        }
        public static string CovertAppThemeToString(AppTheme appTheme)
        {
            return "Base" + appTheme.ToString();
        }

        public static AppAccent AppAccent
        {
            get
            {
                string appAccentString = Settings.Default.AppAccent;
                switch (appAccentString)
                {
                    case "Crimson":
                        return AppAccent.Crimson;
                    case "Purple":
                    default:
                        return AppAccent.Purple;
                }
            }
            set
            {
                string appAccentString = null;
                switch (value)
                {
                    case AppAccent.Purple:
                        appAccentString = "Purple";
                        break;
                    case AppAccent.Crimson:
                        appAccentString = "Crimson";
                        break;
                }

                Settings.Default.AppAccent = appAccentString;
            }
        }
        public static string GetAppAccentAsString()
        {
            return Settings.Default.AppAccent;
        }
        public static string ConvertAppAccentToString(AppAccent appAccent)
        {
            return appAccent.ToString();
        }

        public static void SaveAllSettings()
        {
            Settings.Default.Save();
        }
    }
}