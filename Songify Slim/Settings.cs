namespace Songify_Slim
{
    internal class Settings
    {
        public static string getWebua()
        {
            return Properties.Settings.Default.webua;
        }

        public static void SetTelemetry(bool telemetry)
        {
            Properties.Settings.Default.telemetry = telemetry;
            Properties.Settings.Default.Save();
        }

        public static bool GetTelemetry()
        {
            return Properties.Settings.Default.telemetry;
        }

        public static void SetUUID(string UUID)
        {
            Properties.Settings.Default.uuid = UUID;
            Properties.Settings.Default.Save();
        }

        public static string GetUUID()
        {
            return Properties.Settings.Default.uuid;
        }

        public static void SetOutputString(string outputstring)
        {
            Properties.Settings.Default.outputString = outputstring;
            Properties.Settings.Default.Save();
        }

        public static string GetOutputString()
        {
            return Properties.Settings.Default.outputString;
        }

        public static void SetCustomPauseText(string customtext)
        {
            Properties.Settings.Default.customPauseText = customtext;
            Properties.Settings.Default.Save();
        }

        public static string GetCustomPauseText()
        {
            return Properties.Settings.Default.customPauseText;
        }

        public static void SetCustomPauseTextEnabled(bool custompause)
        {
            Properties.Settings.Default.customPause = custompause;
            Properties.Settings.Default.Save();
        }

        public static bool GetCustomPauseTextEnabled()
        {
            return Properties.Settings.Default.customPause;
        }

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