using MahApps.Metro;
using System;
using System.Windows;
using Songify_Slim.Util.Settings;

namespace Songify_Slim
{
    internal class ThemeHandler
    {
        public static void ApplyTheme()
        {
            //changes the theme 
            
            string theme = Settings.Theme;
            string color = Settings.Color;

            ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(color), ThemeManager.GetAppTheme(theme));
        }
    }
}