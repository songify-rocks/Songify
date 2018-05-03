using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify
{
    internal class Settings
    {
        public static bool getCustomPauseEnabled()
        {
            return Properties.Settings.Default.customPauseEnabled;
        }

        public static void setCustomPauseEnabled(bool customPauseEnabled)
        {
            Properties.Settings.Default.customPauseEnabled = (bool)customPauseEnabled;
            Properties.Settings.Default.Save();
        }

        public static string GetDirectory()
        {
            return Properties.Settings.Default.directory;
        }

        public static void SetDirectory(string directory)
        {
            Properties.Settings.Default.directory = directory;
            Properties.Settings.Default.Save();
        }

        public static bool getShowAlbumArt()
        {
            return Properties.Settings.Default.showAlbumArt;
        }

        public static void setShowAlbumArt(bool showAlbumArt)
        {
            Properties.Settings.Default.showAlbumArt = (bool)showAlbumArt;
            Properties.Settings.Default.Save();
        }

        public static string GetCustomPauseText()
        {
            return Properties.Settings.Default.customPauseText;
        }

        public static void SetCustomPauseText(string customPauseText)
        {
            Properties.Settings.Default.customPauseText = customPauseText;
            Properties.Settings.Default.Save();
        }

        public static string GetCustomOutput()
        {
            return Properties.Settings.Default.customOutput;
        }

        public static void SetCustomOutputy(string customOutput)
        {
            Properties.Settings.Default.customOutput = customOutput;
            Properties.Settings.Default.Save();
        }

        public static string GetColor()
        {
            return Properties.Settings.Default.color;
        }

        public static void SetColor(string color)
        {
            Properties.Settings.Default.color = color;
            Properties.Settings.Default.Save();
        }

        public static string GetTheme()
        {
            return Properties.Settings.Default.theme;
        }

        public static void SetTheme(string theme)
        {
            Properties.Settings.Default.theme = theme;
            Properties.Settings.Default.Save();
        }
    }
}