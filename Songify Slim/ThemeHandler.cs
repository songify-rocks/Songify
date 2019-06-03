using MahApps.Metro;
using System;
using System.Windows;

namespace Songify_Slim
{
    internal class ThemeHandler
    {
        public static void ApplyTheme()
        {
            //changes the theme 
            var theme = Settings.Theme;
            var color = Settings.Color;

            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(color), ThemeManager.GetAppTheme(theme));
        }
    }
}