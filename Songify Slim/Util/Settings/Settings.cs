using System;

namespace Songify_Slim
{
    /// <summary>
    ///     This class is a getter and setter for Settings
    /// </summary>
    internal class Settings
    {
        public static string AccessToken
        {
            get => GetAccessToken();
            set => SetAccessToken(value);
        }

        public static string ArtistBlacklist
        {
            get => GetArtistBlacklist();
            set => SetArtistBlacklist(value);
        }

        public static bool AutoClearQueue
        {
            get => GetAutoClearQueue();
            set => SetAutoClearQueue(value);
        }

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

        public static bool DownloadCover
        {
            get => GetDownloadCover();
            set => SetDownloadCover(value);
        }

        public static bool MsgLoggingEnabled
        {
            get => GetMsgLoggingEnabled();
            set => SetMsgLoggingEnabled(value);
        }

        public static string NbUser
        {
            get => GetNbUser();
            set => SetNbUser(value);
        }

        public static string NbUserId
        {
            get => GetNbUserId();
            set => SetNbUserId(value);
        }

        public static string OutputString
        {
            get => GetOutputString();
            set => SetOutputString(value);
        }

        public static double PosX
        {
            get => GetPosX();
            set => SetPosX(value);
        }

        public static double PosY
        {
            get => GetPosY();
            set => SetPosY(value);
        }

        public static string RefreshToken
        {
            get => GetRefreshToken();
            set => SetRefreshToken(value);
        }

        public static bool SaveHistory
        {
            get => GetSaveHistory();
            set => SetSaveHistory(value);
        }

        public static string Source
        {
            get => GetSource();
            set => SetSource(value);
        }

        public static bool SplitOutput
        {
            get => GetSplitOutput();
            set => SetSplitOutput(value);
        }

        public static bool Systray
        {
            get => GetSystray();
            set => SetSystray(value);
        }

        public static bool Telemetry
        {
            get => GetTelemetry();
            set => SetTelemetry(value);
        }

        public static string Theme
        {
            get => GetTheme();
            set => SetTheme(value);
        }

        public static string TwAcc
        {
            get => GetTwAcc();
            set => SetTwAcc(value);
        }

        public static bool TwAutoConnect
        {
            get => GetTwAutoConnect();
            set => SetTwAutoConnect(value);
        }

        public static string TwChannel
        {
            get => GetTwChannel();
            set => SetTwChannel(value);
        }

        public static string TwOAuth
        {
            get => GetTwOAuth();
            set => SetTwOAuth(value);
        }

        public static string TwRewardID
        {
            get => GetTwRewardID();
            set => SetTwRewardID(value);
        }

        public static bool TwSRCommand
        {
            get => GetTwSRCommand();
            set => SetTwSRCommand(value);
        }

        public static int TwSRCooldown
        {
            get => GetTwSRCooldown();
            set => SetTwSRCooldown(value);
        }

        public static int TwSRMaxReq
        {
            get => GetTwSRMaxReq();
            set => SetTwSRMaxReq(value);
        }

        public static bool TwSRReward
        {
            get => GetTwSRReward();
            set => SetTwSRReward(value);
        }

        public static bool Upload
        {
            get => GetUpload();
            set => SetUpload(value);
        }

        public static bool UploadHistory
        {
            get => GetUploadHistory();
            set => SetUploadHistory(value);
        }

        public static string Uuid
        {
            get => GetUuid();
            set => SetUuid(value);
        }

        public static string Webua => GetWebua();

        private static string GetAccessToken()
        {
            return Properties.Settings.Default.AccessToken;
        }

        private static string GetArtistBlacklist()
        {
            return Properties.Settings.Default.ArtistBlacklist;
        }

        private static bool GetAutoClearQueue()
        {
            return Properties.Settings.Default.AutoClearQueue;
        }

        private static bool GetAutostart()
        {
            return Properties.Settings.Default.autostart;
        }

        private static int GetChromeFetchRate()
        {
            return Properties.Settings.Default.ChromeFetchRate;
        }

        private static string GetColor()
        {
            return Properties.Settings.Default.color;
        }

        private static string GetCustomPauseText()
        {
            return Properties.Settings.Default.customPauseText;
        }

        private static bool GetCustomPauseTextEnabled()
        {
            return Properties.Settings.Default.customPause;
        }

        private static string GetDirectory()
        {
            return Properties.Settings.Default.directory;
        }

        private static bool GetDownloadCover()
        {
            return Properties.Settings.Default.SaveAlbumCover;
        }

        private static bool GetMsgLoggingEnabled()
        {
            return Properties.Settings.Default.MsgLoggingEnabled;
        }

        private static string GetNbUser()
        {
            return Properties.Settings.Default.NBUser;
        }

        private static string GetNbUserId()
        {
            return Properties.Settings.Default.NBUserID;
        }

        private static string GetOutputString()
        {
            return Properties.Settings.Default.outputString;
        }

        private static double GetPosX()
        {
            return Properties.Settings.Default.PosX;
        }

        private static double GetPosY()
        {
            return Properties.Settings.Default.PosY;
        }

        private static string GetRefreshToken()
        {
            return Properties.Settings.Default.RefreshToken;
        }

        private static bool GetSaveHistory()
        {
            return Properties.Settings.Default.SaveHistory;
        }

        private static string GetSource()
        {
            return Properties.Settings.Default.Source;
        }

        private static bool GetSplitOutput()
        {
            return Properties.Settings.Default.SplitString;
        }

        private static bool GetSystray()
        {
            return Properties.Settings.Default.systray;
        }

        private static bool GetTelemetry()
        {
            return Properties.Settings.Default.telemetry;
        }

        private static string GetTheme()
        {
            return Properties.Settings.Default.theme;
        }

        private static string GetTwAcc()
        {
            return Properties.Settings.Default.TwAcc;
        }

        private static bool GetTwAutoConnect()
        {
            return Properties.Settings.Default.TwAutoConnect;
        }

        private static string GetTwChannel()
        {
            return Properties.Settings.Default.TwChannel;
        }

        private static string GetTwOAuth()
        {
            return Properties.Settings.Default.TwOauth;
        }

        private static string GetTwRewardID()
        {
            return Properties.Settings.Default.TwRewardID;
        }

        private static bool GetTwSRCommand()
        {
            return Properties.Settings.Default.TwSRCommand;
        }

        private static int GetTwSRCooldown()
        {
            return Properties.Settings.Default.TwSRCooldown;
        }

        private static int GetTwSRMaxReq()
        {
            return Properties.Settings.Default.TwSRMaxReq;
        }

        private static bool GetTwSRReward()
        {
            return Properties.Settings.Default.TwSRReward;
        }

        private static bool GetUpload()
        {
            return Properties.Settings.Default.uploadSonginfo;
        }

        private static bool GetUploadHistory()
        {
            return Properties.Settings.Default.UploadHistory;
        }

        private static string GetUuid()
        {
            return Properties.Settings.Default.uuid;
        }

        private static string GetWebua()
        {
            return Properties.Settings.Default.webua;
        }

        private static void SetAccessToken(string value)
        {
            Properties.Settings.Default.AccessToken = value;
            Properties.Settings.Default.Save();
        }

        private static void SetArtistBlacklist(string value)
        {
            Properties.Settings.Default.ArtistBlacklist = value;
            Properties.Settings.Default.Save();
        }

        private static void SetAutoClearQueue(bool value)
        {
            Properties.Settings.Default.AutoClearQueue = value;
            Properties.Settings.Default.Save();

        }
        private static void SetAutostart(bool autostart)
        {
            Properties.Settings.Default.autostart = autostart;
            Properties.Settings.Default.Save();
        }

        private static void SetChromeFetchRate(int rate)
        {
            Properties.Settings.Default.ChromeFetchRate = rate;
            Properties.Settings.Default.Save();
        }

        private static void SetColor(string color)
        {
            Properties.Settings.Default.color = color;
            Properties.Settings.Default.Save();
        }

        private static void SetCustomPauseText(string customtext)
        {
            Properties.Settings.Default.customPauseText = customtext;
            Properties.Settings.Default.Save();
        }

        private static void SetCustomPauseTextEnabled(bool custompause)
        {
            Properties.Settings.Default.customPause = custompause;
            Properties.Settings.Default.Save();
        }

        private static void SetDirectory(string directory)
        {
            Properties.Settings.Default.directory = directory;
            Properties.Settings.Default.Save();
        }

        private static void SetDownloadCover(bool value)
        {
            Properties.Settings.Default.SaveAlbumCover = value;
            Properties.Settings.Default.Save();
        }

        private static void SetMsgLoggingEnabled(bool value)
        {
            Properties.Settings.Default.MsgLoggingEnabled = value;
            Properties.Settings.Default.Save();
        }

        private static void SetNbUser(string nbuser)
        {
            Properties.Settings.Default.NBUser = nbuser;
            Properties.Settings.Default.Save();
        }

        private static void SetNbUserId(string nbuserId)
        {
            Properties.Settings.Default.NBUserID = nbuserId;
            Properties.Settings.Default.Save();
        }

        private static void SetOutputString(string outputstring)
        {
            Properties.Settings.Default.outputString = outputstring;
            Properties.Settings.Default.Save();
        }

        private static void SetPosX(double value)
        {
            Properties.Settings.Default.PosX = value;
            Properties.Settings.Default.Save();
        }

        private static void SetPosY(double value)
        {
            Properties.Settings.Default.PosY = value;
            Properties.Settings.Default.Save();
        }
        private static void SetRefreshToken(string value)
        {
            Properties.Settings.Default.RefreshToken = value;
            Properties.Settings.Default.Save();
        }

        private static void SetSaveHistory(bool savehistory)
        {
            Properties.Settings.Default.SaveHistory = savehistory;
            Properties.Settings.Default.Save();
        }

        private static void SetSource(string source)
        {
            Properties.Settings.Default.Source = source;
            Properties.Settings.Default.Save();
        }

        private static void SetSplitOutput(bool value)
        {
            Properties.Settings.Default.SplitString = value;
            Properties.Settings.Default.Save();
        }

        private static void SetSystray(bool systray)
        {
            Properties.Settings.Default.systray = systray;
            Properties.Settings.Default.Save();
        }

        private static void SetTelemetry(bool telemetry)
        {
            Properties.Settings.Default.telemetry = telemetry;
            Properties.Settings.Default.Save();
        }

        private static void SetTheme(string theme)
        {
            Properties.Settings.Default.theme = theme;
            Properties.Settings.Default.Save();
        }

        private static void SetTwAcc(string value)
        {
            Properties.Settings.Default.TwAcc = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwAutoConnect(bool value)
        {
            Properties.Settings.Default.TwAutoConnect = value;
            Properties.Settings.Default.Save();
        }
        private static void SetTwChannel(string value)
        {
            Properties.Settings.Default.TwChannel = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwOAuth(string value)
        {
            Properties.Settings.Default.TwOauth = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwRewardID(string value)
        {
            Properties.Settings.Default.TwRewardID = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSRCommand(bool value)
        {
            Properties.Settings.Default.TwSRCommand = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSRCooldown(int value)
        {
            Properties.Settings.Default.TwSRCooldown = value;
            Properties.Settings.Default.Save();
        }
        private static void SetTwSRMaxReq(int value)
        {
            Properties.Settings.Default.TwSRMaxReq = value;
            Properties.Settings.Default.Save();
        }
        private static void SetTwSRReward(bool value)
        {
            Properties.Settings.Default.TwSRReward = value;
            Properties.Settings.Default.Save();
        }
        private static void SetUpload(bool uploadsong)
        {
            Properties.Settings.Default.uploadSonginfo = uploadsong;
            Properties.Settings.Default.Save();
        }

        private static void SetUploadHistory(bool history)
        {
            Properties.Settings.Default.UploadHistory = history;
            Properties.Settings.Default.Save();
        }
        private static void SetUuid(string uuid)
        {
            Properties.Settings.Default.uuid = uuid;
            Properties.Settings.Default.Save();
        }
    }
}