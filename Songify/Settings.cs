namespace Songify
{
    internal class Settings
    {
        #region ProxySettings

        public static void SetProxyPass(string pass)
        {
            Properties.Settings.Default.proxyPass = pass;
            Properties.Settings.Default.Save();
        }

        public static string GetProxyPass()
        {
            return Properties.Settings.Default.proxyPass;
        }

        public static void SetProxyUser(string username)
        {
            Properties.Settings.Default.proxyUser = username;
            Properties.Settings.Default.Save();
        }

        public static string GetProxyUser()
        {
            return Properties.Settings.Default.proxyUser;
        }

        public static void SetProxyPort(string port)
        {
            Properties.Settings.Default.proxyPort = port;
            Properties.Settings.Default.Save();
        }

        public static string GetProxyPort()
        {
            return Properties.Settings.Default.proxyPort;
        }

        public static void SetProxyHost(string host)
        {
            Properties.Settings.Default.proxyHost = host;
            Properties.Settings.Default.Save();
        }

        public static string GetProxyHost()
        {
            return Properties.Settings.Default.proxyHost;
        }

        #endregion

        public static void SetDeleteAlbumArtOnpause(bool deleteAlbumArtOnpause)
        {
            Properties.Settings.Default.deleteAlbumArtOnpause = deleteAlbumArtOnpause;
            Properties.Settings.Default.Save();
        }

        public static bool GetDeleteAlbumArtOnpause()
        {
            return Properties.Settings.Default.deleteAlbumArtOnpause;
        }

        public static bool GetDownloadAlbumArt()
        {
            return Properties.Settings.Default.downloadAlbumArt;
        }

        public static void SetDownloadAlbumArt(bool downloadAlbumArt)
        {
            Properties.Settings.Default.downloadAlbumArt = (bool)downloadAlbumArt;
            Properties.Settings.Default.Save();
        }

        public static bool GetCustomPauseEnabled()
        {
            return Properties.Settings.Default.customPauseEnabled;
        }

        public static void SetCustomPauseEnabled(bool customPauseEnabled)
        {
            Properties.Settings.Default.customPauseEnabled = (bool)customPauseEnabled;
            Properties.Settings.Default.Save();
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

        public static bool GetShowAlbumArt()
        {
            return Properties.Settings.Default.showAlbumArt;
        }

        public static void SetShowAlbumArt(bool showAlbumArt)
        {
            Properties.Settings.Default.showAlbumArt = (bool)showAlbumArt;
            Properties.Settings.Default.Save();
        }

        public static string GetCustomPauseText()
        {
            return Properties.Settings.Default.customPauseText;
        }

        public static void SetCustomPauseText(string customPauseText)
        {
            Properties.Settings.Default.customPauseText = customPauseText;
            Properties.Settings.Default.Save();
        }

        public static string GetCustomOutput()
        {
            return Properties.Settings.Default.customOutput;
        }

        public static void SetCustomOutputy(string customOutput)
        {
            Properties.Settings.Default.customOutput = customOutput;
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

        public static string GetTheme()
        {
            return Properties.Settings.Default.theme;
        }

        public static void SetTheme(string theme)
        {
            Properties.Settings.Default.theme = theme;
            Properties.Settings.Default.Save();
        }
    }
}