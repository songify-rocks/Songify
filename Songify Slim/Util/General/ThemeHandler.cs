using ControlzEx.Theming;
using System.Windows;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General
{
    internal class ThemeHandler
    {
        public static void ApplyTheme()
        {
            if (string.IsNullOrEmpty(Settings.Theme))
                Settings.Theme = "Light";
            if (string.IsNullOrEmpty(Settings.Color))
                Settings.Color = "Blue";
            if (string.IsNullOrEmpty(Settings.Color))
                Settings.Color = Settings.Theme + "." + Settings.Color;

            //changes the theme
            string theme = Settings.Theme.Replace("Base", "");
            string color = Settings.Color;
            string themeName = theme + "." + color;
            ThemeManager.Current.ChangeTheme(Application.Current, themeName);
            ThemeManager.Current.SyncTheme();
        }
    }
}