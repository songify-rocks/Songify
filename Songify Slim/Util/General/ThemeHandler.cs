using ControlzEx.Theming;
using Songify_Slim.Util.Settings;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

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

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}