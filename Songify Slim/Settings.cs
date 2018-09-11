namespace Songify_Slim
{
    internal class Settings
    {
        public static void SetSystray(bool systray)
        {
            Properties.Settings.Default.systray = systray;
            Properties.Settings.Default.Save();
        }

        public static bool GetSystray()
        {
            return Properties.Settings.Default.systray;
        }

        public static void SetAutostart(bool autostart)
        {
            Properties.Settings.Default.autostart = autostart;
            Properties.Settings.Default.Save();
        }

        public static bool GetAutostart()
        {
            return Properties.Settings.Default.autostart;
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

        public static string GetTheme()
        {
            return Properties.Settings.Default.theme;
        }

        public static void SetTheme(string theme)
        {
            Properties.Settings.Default.theme = theme;
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
    }
}