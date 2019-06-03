namespace Songify_Slim
{

    /// <summary>
    /// This class is a getter and setter for Settings
    /// </summary>
   
    internal class Settings
    {

        public static bool Autostart
        {
            get => GetAutostart();
            set => SetAutostart(value);
        }

        public static int ChromeFetchRate
        {
            get => GetChromeFetchRate();
            set => SetChromeFetchRate(value);
        }

        public static string Color
        {
            get => GetColor();
            set => SetColor(value);
        }

        public static string CustomPauseText
        {
            get => GetCustomPauseText();
            set => SetCustomPauseText(value);
        }

        public static bool CustomPauseTextEnabled
        {
            get => GetCustomPauseTextEnabled();
            set => SetCustomPauseTextEnabled(value);
        }

        public static string Directory
        {
            get => GetDirectory();
            set => SetDirectory(value);
        }

        public static bool History
        {
            get => GetHistory();
            set => SetHistory(value);
        }

        public static string NBUser
        {
            get => GetNBUser();
            set => SetNBUser(value);
        }

        public static string NBUserID
        {
            get => GetNBUserID();
            set => SetNBUserID(value);
        }

        public static string OutputString
        {
            get => GetOutputString();
            set => SetOutputString(value);
        }

        public static int Source
        {
            get => GetSource();
            set => SetSource(value);
        }

        public static bool Systray
        {
            get => GetSystray();
            set => SetSystray(value);
        }

        public static bool Telemetry
        {
            get { return GetTelemetry(); }
            set { SetTelemetry(value); }
        }

        public static string Theme
        {
            get { return GetTheme(); }
            set { SetTheme(value); }
        }

        public static bool Upload
        {
            get { return GetUpload(); }
            set { SetUpload(value); }
        }

        public static string UUID
        {
            get { return GetUUID(); }
            set { SetUUID(value); }
        }

        public static string Webua
        {
            get { return getWebua(); }
        }

        





        private static void SetHistory(bool history)
        {
            Properties.Settings.Default.history = history;
            Properties.Settings.Default.Save();
        }

        private static bool GetHistory()
        {
            return Properties.Settings.Default.history;
        }

        private static void SetChromeFetchRate(int rate)
        {
            Properties.Settings.Default.ChromeFetchRate = rate;
            Properties.Settings.Default.Save();
        }

        private static int GetChromeFetchRate()
        {
            return Properties.Settings.Default.ChromeFetchRate;
        }

        private static void SetUpload(bool uploadsong)
        {
            Properties.Settings.Default.uploadSonginfo = uploadsong;
            Properties.Settings.Default.Save();
        }

        private static bool GetUpload()
        {
            return Properties.Settings.Default.uploadSonginfo;
        }

        private static void SetSource(int source)
        {
            Properties.Settings.Default.Source = source;
            Properties.Settings.Default.Save();
        }

        private static int GetSource()
        {
            return Properties.Settings.Default.Source;
        }

        private static void SetNBUserID(string nbuserID)
        {
            Properties.Settings.Default.NBUserID = nbuserID;
            Properties.Settings.Default.Save();
        }

        private static string GetNBUserID()
        {
            return Properties.Settings.Default.NBUserID;
        }

        private static void SetNBUser(string nbuser)
        {
            Properties.Settings.Default.NBUser = nbuser;
            Properties.Settings.Default.Save();
        }

        private static string GetNBUser()
        {
            return Properties.Settings.Default.NBUser;
        }

        private static string getWebua()
        {
            return Properties.Settings.Default.webua;
        }

        private static void SetTelemetry(bool telemetry)
        {
            Properties.Settings.Default.telemetry = telemetry;
            Properties.Settings.Default.Save();
        }

        private static bool GetTelemetry()
        {
            return Properties.Settings.Default.telemetry;
        }

        private static void SetUUID(string UUID)
        {
            Properties.Settings.Default.uuid = UUID;
            Properties.Settings.Default.Save();
        }

        private static string GetUUID()
        {
            return Properties.Settings.Default.uuid;
        }

        private static void SetOutputString(string outputstring)
        {
            Properties.Settings.Default.outputString = outputstring;
            Properties.Settings.Default.Save();
        }

        private static string GetOutputString()
        {
            return Properties.Settings.Default.outputString;
        }

        private static void SetCustomPauseText(string customtext)
        {
            Properties.Settings.Default.customPauseText = customtext;
            Properties.Settings.Default.Save();
        }

        private static string GetCustomPauseText()
        {
            return Properties.Settings.Default.customPauseText;
        }

        private static void SetCustomPauseTextEnabled(bool custompause)
        {
            Properties.Settings.Default.customPause = custompause;
            Properties.Settings.Default.Save();
        }

        private static bool GetCustomPauseTextEnabled()
        {
            return Properties.Settings.Default.customPause;
        }

        private static void SetSystray(bool systray)
        {
            Properties.Settings.Default.systray = systray;
            Properties.Settings.Default.Save();
        }

        private static bool GetSystray()
        {
            return Properties.Settings.Default.systray;
        }

        private static void SetAutostart(bool autostart)
        {
            Properties.Settings.Default.autostart = autostart;
            Properties.Settings.Default.Save();
        }

        private static bool GetAutostart()
        {
            return Properties.Settings.Default.autostart;
        }

        private static string GetDirectory()
        {
            return Properties.Settings.Default.directory;
        }

        private static void SetDirectory(string directory)
        {
            Properties.Settings.Default.directory = directory;
            Properties.Settings.Default.Save();
        }

        private static string GetTheme()
        {
            return Properties.Settings.Default.theme;
        }

        private static void SetTheme(string theme)
        {
            Properties.Settings.Default.theme = theme;
            Properties.Settings.Default.Save();
        }

        private static string GetColor()
        {
            return Properties.Settings.Default.color;
        }

        private static void SetColor(string color)
        {
            Properties.Settings.Default.color = color;
            Properties.Settings.Default.Save();
        }
    }
}