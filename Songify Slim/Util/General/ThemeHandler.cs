using System.Windows;
using ControlzEx.Theming;

namespace Songify_Slim.Util.General
{
    internal class ThemeHandler
    {
        public static void ApplyTheme()
        {
            if (string.IsNullOrEmpty(Settings.Settings.Theme))
                Settings.Settings.Theme = "Light";
            if (string.IsNullOrEmpty(Settings.Settings.Color))
                Settings.Settings.Color = "Blue";
            if (string.IsNullOrEmpty(Settings.Settings.Color))
                Settings.Settings.Color = Settings.Settings.Theme + "." + Settings.Settings.Color;

            //changes the theme
            string theme = Settings.Settings.Theme.Replace("Base", "");
            string color = Settings.Settings.Color;
            string themeName = theme + "." + color;
            ThemeManager.Current.ChangeTheme(Application.Current, themeName);
        }
    }
}