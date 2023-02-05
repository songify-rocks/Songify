using System.Windows;
using ControlzEx.Theming;
using Songify_Slim.Util.Settings;

namespace Songify_Slim
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
        }
    }
}