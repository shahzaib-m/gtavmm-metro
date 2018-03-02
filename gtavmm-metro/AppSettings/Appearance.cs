using System;
using System.Linq;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Media.Imaging;

using MahApps.Metro;

namespace gtavmm_metro.AppSettings
{
    public enum AppTheme { Light, Dark }
    public enum AppAccent { Purple, Crimson }

    public static class Appearance
    {
        public static List<string> GetAppAccentsList()
        {
            return Enum.GetNames(typeof(AppAccent)).ToList();
        }

        public static void ChangeAppTheme(AppTheme newAppTheme)
        {
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent(SettingsHandler.GetAppAccentAsString()),
                                        ThemeManager.GetAppTheme(SettingsHandler.CovertAppThemeToString(newAppTheme)));
        }

        public static string GetCurrentAppAccentString()
        {
            return ThemeManager.DetectAppStyle().Item2.Name;
        }
        public static void ChangeAppAccent(AppAccent newAppAccent)
        {
            ThemeManager.ChangeAppStyle(Application.Current,
                                        ThemeManager.GetAccent(SettingsHandler.ConvertAppAccentToString(newAppAccent)),
                                        ThemeManager.GetAppTheme(SettingsHandler.GetAppThemeAsString()));

            Application.Current.MainWindow.Icon = new BitmapImage(
                new Uri(String.Format("pack://application:,,,/Assets/Icons/{0}.ico", newAppAccent.ToString())));
        }
    }
}
