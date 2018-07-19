using MahApps.Metro;
using System;
using System.Windows;

namespace Songify
{
    internal class ThemeHandler
    {
        public static void ApplyTheme()
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Settings.GetColor()), ThemeManager.GetAppTheme(Settings.GetTheme()));
        }
    }
}