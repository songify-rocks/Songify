using System;
using System.IO;
using System.Reflection;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using YamlDotNet.Core.Tokens;

namespace Songify_Slim.Util.Settings
{
    /// <summary>
    ///     This class is a getter and setter for Settings
    /// </summary>
    internal class Settings
    {
        public static bool BotOnlyWorkWhenLive
        {
            get => GetBotOnlyWorkWhenLive();
            set => SetBotOnlyWorkWhenLive(value);
        }

        private static void SetBotOnlyWorkWhenLive(bool value)
        {
            Properties.Settings.Default.BotOnlyWorkWhenLive = value;
            Properties.Settings.Default.Save();
        }

        private static bool GetBotOnlyWorkWhenLive()
        {
            return Properties.Settings.Default.BotOnlyWorkWhenLive;
        }

        public static bool AnnounceInChat
        {
            get => GetAnnounceInChat();
            set => SetAnnounceInChat(value);
        }

        public static bool AppendSpaces
        {
            get => GetAppendSpaces();
            set => SetAppendSpaces(value);
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

        public static bool AutoStartWebServer
        {
            get => GetAutoStartWebServer();
            set => SetAutoStartWebServer(value);
        }

        public static bool BetaUpdates
        {
            get => GetBetaUpdates();
            set => SetBetaUpdates(value);
        }

        public static bool BotCmdNext
        {
            get => GetBotCmdNext();
            set => SetBotCmdNext(value);
        }

        public static bool BotCmdPos
        {
            get => GetBotCmdPos();
            set => SetBotCmdPos(value);
        }

        public static bool BotCmdSkip
        {
            get => GetBotCmdSkip();
            set => SetBotCmdSkip(value);
        }

        public static bool BotCmdSkipVote
        {
            get => GetBotCmdSkipVote();
            set => SetBotCmdSkipVote(value);
        }

        public static int BotCmdSkipVoteCount
        {
            get => GetBotCmdSkipVoteCount();
            set => SetBotCmdSkipVoteCount(value);
        }

        public static bool BotCmdSong
        {
            get => GetBotCmdSong();
            set => SetBotCmdSong(value);
        }

        public static string BotRespBlacklist
        {
            get => GetBot_Resp_Blacklist();
            set => SetBot_Resp_Blacklist(value);
        }

        public static string BotRespError
        {
            get => GetBot_Resp_Error();
            set => SetBot_Resp_Error(value);
        }

        public static string BotRespIsInQueue
        {
            get => GetBot_Resp_IsInQueue();
            set => SetBot_Resp_IsInQueue(value);
        }

        public static string BotRespLength
        {
            get => GetBot_Resp_Length();
            set => SetBot_Resp_Length(value);
        }

        public static string BotRespMaxReq
        {
            get => GetBot_Resp_MaxReq();
            set => SetBot_Resp_MaxReq(value);
        }

        public static string BotRespModSkip
        {
            get => GetBot_Resp_ModSkip();
            set => SetBot_Resp_ModSkip(value);
        }

        public static string BotRespNext
        {
            get => GetBot_Resp_Next();
            set => SetBot_Resp_Next(value);
        }

        public static string BotRespNoSong
        {
            get => GetBot_Resp_NoSong();
            set => SetBot_Resp_NoSong(value);
        }

        public static string BotRespPos
        {
            get => GetBot_Resp_Pos();
            set => SetBot_Resp_Pos(value);
        }

        public static string BotRespSuccess
        {
            get => GetBot_Resp_Success();
            set => SetBot_Resp_Success(value);
        }

        public static string BotRespVoteSkip
        {
            get => GetBot_Resp_VoteSkip();
            set => SetBot_Resp_VoteSkip(value);
        }

        public static int ChromeFetchRate
        {
            get => GetChromeFetchRate();
            set => SetChromeFetchRate(value);
        }

        public static string ClientId
        {
            get => GetClientId();
            set => SetClientId(value);
        }

        public static string ClientSecret
        {
            get => GetClientSecret();
            set => SetClientSecret(value);
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

        public static bool GuidedSetup
        {
            get => GetGuidedSetup();
            set => SetGuidedSetup(value);
        }

        public static bool IsLive { get; set; }

        public static string Language
        {
            get => GetLanguage();

            set => SetLangauge(value);
        }

        public static int MaxSongLength
        {
            get => GetMaxSongLength();
            set => SetMaxSongLength(value);
        }

        public static bool MsgLoggingEnabled
        {
            get => GetMsgLoggingEnabled();
            set => SetMsgLoggingEnabled(value);
        }

        public static bool OpenQueueOnStartup
        {
            get => GetOpenQueueOnStartup();
            set => SetOpenQueueOnStartup(value);
        }

        public static string OutputString
        {
            get => GetOutputString();
            set => SetOutputString(value);
        }

        public static string OutputString2
        {
            get => GetOutputString2();
            set => SetOutputString2(value);
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

        public static int[] RefundConditons
        {
            get => GetRefundConditons();
            set => SetRefundConditons(value);
        }

        public static bool SaveHistory
        {
            get => GetSaveHistory();
            set => SetSaveHistory(value);
        }

        public static int Source
        {
            get => GetSource();
            set => SetSource(value);
        }

        public static int SpaceCount
        {
            get => GetSpaceCount();
            set => SetSpaceCount(value);
        }

        public static bool SplitOutput
        {
            get => GetSplitOutput();
            set => SetSplitOutput(value);
        }

        public static string SpotifyAccessToken
        {
            get => GetSpotifyAccessToken();
            set => SetSpotifyAccessToken(value);
        }

        public static string SpotifyDeviceId
        {
            get => GetSpotifyDeviceId();
            set => SetSpotifyDeviceId(value);
        }

        public static string SpotifyRefreshToken
        {
            get => GetSpotifyRefreshToken();
            set => SetSpotifyRefreshToken(value);
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

        public static string TwitchAccessToken
        {
            get => GetTwitchAccessToken();
            set => SetTwitchAccessToken(value);
        }

        public static string TwitchChannelId
        {
            get => GetTwitchChannelId();
            set => SetTwitchChannelId(value);
        }

        public static User TwitchUser
        {
            get => GetTwitchUser();
            set => SetTwitchUser(value);
        }

        public static string TwOAuth
        {
            get => GetTwOAuth();
            set => SetTwOAuth(value);
        }

        public static string TwRewardId
        {
            get => GetTwRewardId();
            set => SetTwRewardId(value);
        }

        public static bool TwSrCommand
        {
            get => GetTwSrCommand();
            set => SetTwSrCommand(value);
        }

        public static int TwSrCooldown
        {
            get => GetTwSrCooldown();
            set => SetTwSrCooldown(value);
        }

        public static int TwSrMaxReq
        {
            get => GetTwSrMaxReq();
            set => SetTwSrMaxReq(value);
        }

        public static int TwSrMaxReqBroadcaster
        {
            get => GetTwSrMaxReqBroadcaster();
            set => SetTwSrMaxReqBroadcaster(value);
        }

        public static int TwSrMaxReqEveryone
        {
            get => GetTwSrMaxReqEveryone();
            set => SetTwSrMaxReqEveryone(value);
        }

        public static int TwSrMaxReqModerator
        {
            get => GetTwSrMaxReqModerator();
            set => SetTwSrMaxReqModerator(value);
        }

        public static int TwSrMaxReqSubscriber
        {
            get => GetTwSrMaxReqSubscriber();
            set => SetTwSrMaxReqSubscriber(value);
        }

        public static int TwSrMaxReqVip
        {
            get => GetTwSrMaxReqVip();
            set => SetTwSrMaxReqVip(value);
        }

        public static bool TwSrReward
        {
            get => GetTwSrReward();
            set => SetTwSrReward(value);
        }

        public static int TwSrUserLevel
        {
            get => GetTwSrUserLevel();
            set => SetTwSrUserLevel(value);
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

        public static bool UseOwnApp
        {
            get => GetUseOwnApp();
            set => SetUseOwnApp(value);
        }

        public static string UserBlacklist
        {
            get => GetUserBlacklist();
            set => SetUserBlacklist(value);
        }

        public static string Uuid
        {
            get => GetUuid();
            set => SetUuid(value);
        }

        public static int WebServerPort
        {
            get => GetWebServerPort();
            set => SetWebServerPort(value);
        }

        public static string Webua => GetWebua();

        public static Configuration Export()
        {
            SpotifyCredentials spotifyCredentials = new SpotifyCredentials
            {
                AccessToken = GetSpotifyAccessToken(),
                RefreshToken = GetSpotifyRefreshToken(),
                DeviceId = GetSpotifyDeviceId(),
                ClientId = GetClientId(),
                ClientSecret = GetClientSecret()
            };

            TwitchCredentials twitchCredentials = new TwitchCredentials
            {
                AccessToken = GetTwitchAccessToken(),
                ChannelName = GetTwChannel(),
                ChannelId = GetTwitchChannelId(),
                BotAccountName = GetTwAcc(),
                BotOAuthToken = GetTwOAuth(),
                TwitchUser = GetTwitchUser()
            };

            BotConfig botConfig = new BotConfig
            {
                BotCmdNext = GetBotCmdNext(),
                BotCmdPos = GetBotCmdPos(),
                BotCmdSkip = GetBotCmdSkip(),
                BotCmdSkipVote = GetBotCmdSkipVote(),
                BotCmdSong = GetBotCmdSong(),
                BotCmdSkipVoteCount = GetBotCmdSkipVoteCount(),
                BotRespBlacklist = GetBot_Resp_Blacklist(),
                BotRespError = GetBot_Resp_Error(),
                BotRespIsInQueue = GetBot_Resp_IsInQueue(),
                BotRespLength = GetBot_Resp_Length(),
                BotRespMaxReq = GetBot_Resp_MaxReq(),
                BotRespModSkip = GetBot_Resp_ModSkip(),
                BotRespNoSong = GetBot_Resp_NoSong(),
                BotRespSuccess = GetBot_Resp_Success(),
                BotRespVoteSkip = GetBot_Resp_VoteSkip(),
                BotRespNext = GetBot_Resp_Next(),
                BotRespPos = GetBot_Resp_Pos(),
                OnlyWorkWhenLive = GetBotOnlyWorkWhenLive(),
            };

            AppConfig appConfig = new AppConfig
            {
                AnnounceInChat = GetAnnounceInChat(),
                AppendSpaces = GetAppendSpaces(),
                AutoClearQueue = GetAutoClearQueue(),
                Autostart = GetAutostart(),
                CustomPauseTextEnabled = GetCustomPauseTextEnabled(),
                DownloadCover = GetDownloadCover(),
                MsgLoggingEnabled = GetMsgLoggingEnabled(),
                OpenQueueOnStartup = GetOpenQueueOnStartup(),
                SaveHistory = GetSaveHistory(),
                SplitOutput = GetSplitOutput(),
                Systray = GetSystray(),
                Telemetry = GetTelemetry(),
                TwAutoConnect = GetTwAutoConnect(),
                TwSrCommand = GetTwSrCommand(),
                TwSrReward = GetTwSrReward(),
                Upload = GetUpload(),
                UploadHistory = GetUploadHistory(),
                UseOwnApp = GetUseOwnApp(),
                MaxSongLength = GetMaxSongLength(),
                PosX = (int)GetPosX(),
                PosY = (int)GetPosY(),
                SpaceCount = GetSpaceCount(),
                TwSrCooldown = GetTwSrCooldown(),
                TwSrMaxReq = GetTwSrMaxReq(),
                TwSrMaxReqBroadcaster = GetTwSrMaxReqBroadcaster(),
                TwSrMaxReqEveryone = GetTwSrMaxReqEveryone(),
                TwSrMaxReqModerator = GetTwSrMaxReqModerator(),
                TwSrMaxReqSubscriber = GetTwSrMaxReqSubscriber(),
                TwSrMaxReqVip = GetTwSrMaxReqVip(),
                TwSrUserLevel = GetTwSrUserLevel(),
                TwRewardId = GetTwRewardId(),
                ArtistBlacklist = GetArtistBlacklist(),
                Color = GetColor(),
                CustomPauseText = GetCustomPauseText(),
                Directory = GetDirectory(),
                Language = GetLanguage(),
                OutputString = GetOutputString(),
                OutputString2 = GetOutputString2(),
                Theme = GetTheme(),
                UserBlacklist = GetUserBlacklist(),
                Uuid = GetUuid(),
                RefundConditons = GetRefundConditons(),
                WebServerPort = GetWebServerPort(),
                AutoStartWebServer = GetAutoStartWebServer(),
                BetaUpdates = GetBetaUpdates()
            };

            return new Configuration
            {
                AppConfig = appConfig,
                SpotifyCredentials = spotifyCredentials,
                TwitchCredentials = twitchCredentials,
                BotConfig = botConfig
            };
        }

        public static void Import(Configuration config)
        {
            #region SpotifyCredential
            SetSpotifyAccessToken(config.SpotifyCredentials.AccessToken);
            SetClientId(config.SpotifyCredentials.ClientId);
            SetClientSecret(config.SpotifyCredentials.ClientSecret);
            SetSpotifyRefreshToken(config.SpotifyCredentials.RefreshToken);
            SetSpotifyDeviceId(config.SpotifyCredentials.DeviceId);
            #endregion

            #region BotConfig
            SetBot_Resp_ModSkip(config.BotConfig.BotRespModSkip);
            SetBot_Resp_VoteSkip(config.BotConfig.BotRespVoteSkip);
            SetBot_Resp_Pos(config.BotConfig.BotRespPos);
            SetBot_Resp_Next(config.BotConfig.BotRespNext);
            SetBot_Resp_MaxReq(config.BotConfig.BotRespMaxReq);
            SetBot_Resp_IsInQueue(config.BotConfig.BotRespIsInQueue);
            SetBot_Resp_Length(config.BotConfig.BotRespLength);
            SetBot_Resp_Blacklist(config.BotConfig.BotRespBlacklist);
            SetBot_Resp_Error(config.BotConfig.BotRespError);
            SetBot_Resp_Success(config.BotConfig.BotRespSuccess);
            SetBot_Resp_NoSong(config.BotConfig.BotRespNoSong);
            SetBotCmdNext(config.BotConfig.BotCmdNext);
            SetBotCmdPos(config.BotConfig.BotCmdPos);
            SetBotCmdSkip(config.BotConfig.BotCmdSkip);
            SetBotCmdSkipVote(config.BotConfig.BotCmdSkipVote);
            SetBotCmdSkipVoteCount(config.BotConfig.BotCmdSkipVoteCount);
            SetBotCmdSong(config.BotConfig.BotCmdSong);
            #endregion

            #region TwitchCredentials
            SetTwAcc(config.TwitchCredentials.BotAccountName);
            SetTwChannel(config.TwitchCredentials.ChannelName);
            SetTwOAuth(config.TwitchCredentials.BotOAuthToken);
            #endregion

            #region AppConfig
            SetAnnounceInChat(config.AppConfig.AnnounceInChat);
            SetAppendSpaces(config.AppConfig.AppendSpaces);
            SetArtistBlacklist(config.AppConfig.ArtistBlacklist);
            SetAutoClearQueue(config.AppConfig.AutoClearQueue);
            SetAutostart(config.AppConfig.Autostart);
            SetColor(config.AppConfig.Color);
            SetCustomPauseText(config.AppConfig.CustomPauseText);
            SetCustomPauseTextEnabled(config.AppConfig.CustomPauseTextEnabled);
            SetDirectory(config.AppConfig.Directory);
            SetDownloadCover(config.AppConfig.DownloadCover);
            SetLangauge(config.AppConfig.Language);
            SetMaxSongLength(config.AppConfig.MaxSongLength);
            SetMsgLoggingEnabled(config.AppConfig.MsgLoggingEnabled);
            SetOpenQueueOnStartup(config.AppConfig.OpenQueueOnStartup);
            SetOutputString(config.AppConfig.OutputString);
            SetOutputString2(config.AppConfig.OutputString2);
            SetPosX(config.AppConfig.PosX);
            SetPosY(config.AppConfig.PosY);
            SetSaveHistory(config.AppConfig.SaveHistory);
            SetSpaceCount(config.AppConfig.SpaceCount);
            SetSplitOutput(config.AppConfig.SplitOutput);
            SetSystray(config.AppConfig.Systray);
            SetTelemetry(config.AppConfig.Telemetry);
            SetTheme(config.AppConfig.Theme);
            SetTwAutoConnect(config.AppConfig.TwAutoConnect);
            SetTwRewardId(config.AppConfig.TwRewardId);
            SetTwSrCommand(config.AppConfig.TwSrCommand);
            SetTwSrCooldown(config.AppConfig.TwSrCooldown);
            SetTwSrMaxReq(config.AppConfig.TwSrMaxReq);
            SetTwSrMaxReqBroadcaster(config.AppConfig.TwSrMaxReqBroadcaster);
            SetTwSrMaxReqEveryone(config.AppConfig.TwSrMaxReqEveryone);
            SetTwSrMaxReqModerator(config.AppConfig.TwSrMaxReqModerator);
            SetTwSrMaxReqSubscriber(config.AppConfig.TwSrMaxReqSubscriber);
            SetTwSrMaxReqVip(config.AppConfig.TwSrMaxReqVip);
            SetTwSrReward(config.AppConfig.TwSrReward);
            SetTwSrUserLevel(config.AppConfig.TwSrUserLevel);
            SetUpload(config.AppConfig.Upload);
            SetUploadHistory(config.AppConfig.UploadHistory);
            SetUseOwnApp(config.AppConfig.UseOwnApp);
            SetUserBlacklist(config.AppConfig.UserBlacklist);
            SetUuid(config.AppConfig.Uuid);
            SetRefundConditons(config.AppConfig.RefundConditons);
            SetWebServerPort(config.AppConfig.WebServerPort);
            SetBetaUpdates(config.AppConfig.BetaUpdates);
            #endregion

            ConfigHandler.WriteAllConfig(config);
        }

        private static bool GetAnnounceInChat()
        {
            return Properties.Settings.Default.AnnounceInChat;
        }

        private static bool GetAppendSpaces()
        {
            return Properties.Settings.Default.spacesEnabled;
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

        private static bool GetAutoStartWebServer()
        {
            return Properties.Settings.Default.AutoStartWebServer;
        }

        private static bool GetBetaUpdates()
        {
            return Properties.Settings.Default.BetaUpdates;
        }

        private static string GetBot_Resp_Blacklist()
        {
            return Properties.Settings.Default.bot_Resp_Blacklist;
        }

        private static string GetBot_Resp_Error()
        {
            return Properties.Settings.Default.bot_Resp_Error;
        }

        private static string GetBot_Resp_IsInQueue()
        {
            return Properties.Settings.Default.bot_Resp_IsInQueue;
        }

        private static string GetBot_Resp_Length()
        {
            return Properties.Settings.Default.bot_Resp_Length;
        }

        private static string GetBot_Resp_MaxReq()
        {
            return Properties.Settings.Default.bot_Resp_MaxReq;
        }

        private static string GetBot_Resp_ModSkip()
        {
            return Properties.Settings.Default.bot_Resp_ModSkip;
        }

        private static string GetBot_Resp_Next()
        {
            return Properties.Settings.Default.bot_Resp_Next;
        }

        private static string GetBot_Resp_NoSong()
        {
            return Properties.Settings.Default.bot_Resp_NoSong;
        }

        private static string GetBot_Resp_Pos()
        {
            return Properties.Settings.Default.bot_Resp_Pos;
        }

        private static string GetBot_Resp_Success()
        {
            return Properties.Settings.Default.bot_Resp_Success;
        }

        private static string GetBot_Resp_VoteSkip()
        {

            return Properties.Settings.Default.bot_Resp_VoteSkip;
        }

        private static bool GetBotCmdNext()
        {
            return Properties.Settings.Default.bot_cmd_next;
        }

        private static bool GetBotCmdPos()
        {
            return Properties.Settings.Default.bot_cmd_pos;
        }

        private static bool GetBotCmdSkip()
        {
            return Properties.Settings.Default.bot_cmd_skip;
        }

        private static bool GetBotCmdSkipVote()
        {
            return Properties.Settings.Default.bot_cmd_skip_vote;
        }

        private static int GetBotCmdSkipVoteCount()
        {
            return Properties.Settings.Default.bot_cmd_skip_vote_count;
        }

        private static bool GetBotCmdSong()
        {
            return Properties.Settings.Default.bot_cmd_song;
        }

        private static int GetChromeFetchRate()
        {
            return Properties.Settings.Default.ChromeFetchRate;
        }

        private static string GetClientId()
        {
            return Properties.Settings.Default.ClientID;
        }

        private static string GetClientSecret()
        {
            return Properties.Settings.Default.ClientSecret;
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

        private static bool GetGuidedSetup()
        {
            return Properties.Settings.Default.GuidedSetup;
        }

        private static string GetLanguage()
        {
            return Properties.Settings.Default.language;
        }

        private static int GetMaxSongLength()
        {
            return Properties.Settings.Default.MaxSongLength;
        }

        private static bool GetMsgLoggingEnabled()
        {
            return Properties.Settings.Default.MsgLoggingEnabled;
        }

        private static bool GetOpenQueueOnStartup()
        {
            return Properties.Settings.Default.OpenQueueOnStartup;
        }

        private static string GetOutputString()
        {
            return Properties.Settings.Default.outputString;
        }

        private static string GetOutputString2()
        {
            return Properties.Settings.Default.outputString2;
        }

        private static double GetPosX()
        {
            return Properties.Settings.Default.PosX;
        }

        private static double GetPosY()
        {
            return Properties.Settings.Default.PosY;
        }

        private static int[] GetRefundConditons()
        {
            return Properties.Settings.Default.RefundConditons;
        }

        private static bool GetSaveHistory()
        {
            return Properties.Settings.Default.SaveHistory;
        }

        private static int GetSource()
        {
            return Properties.Settings.Default.Source;
        }

        private static int GetSpaceCount()
        {
            return Properties.Settings.Default.spaces;
        }

        private static bool GetSplitOutput()
        {
            return Properties.Settings.Default.SplitString;
        }

        private static string GetSpotifyAccessToken()
        {
            return Properties.Settings.Default.SpotifyAccessToken;
        }

        private static string GetSpotifyDeviceId()
        {
            return Properties.Settings.Default.SpotifyDeviceID;
        }

        private static string GetSpotifyRefreshToken()
        {
            return Properties.Settings.Default.SpotifyRefreshToken;
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

        private static string GetTwitchAccessToken()
        {
            return Properties.Settings.Default.TwitchAccessToken;

        }

        private static string GetTwitchChannelId()
        {
            return Properties.Settings.Default.TwitchChannelId;

        }

        private static User GetTwitchUser()
        {
            return Properties.Settings.Default.TwitchUser;
        }

        private static string GetTwOAuth()
        {
            return Properties.Settings.Default.TwOauth;
        }

        private static string GetTwRewardId()
        {
            return Properties.Settings.Default.TwRewardID;
        }

        private static bool GetTwSrCommand()
        {
            return Properties.Settings.Default.TwSRCommand;
        }

        private static int GetTwSrCooldown()
        {
            return Properties.Settings.Default.TwSRCooldown;
        }

        private static int GetTwSrMaxReq()
        {
            return Properties.Settings.Default.TwSRMaxReq;
        }

        private static int GetTwSrMaxReqBroadcaster()
        {
            return Properties.Settings.Default.TwSRMaxReqBroadcaster;
        }

        private static int GetTwSrMaxReqEveryone()
        {
            return Properties.Settings.Default.TwSRMaxReqEveryone;
        }

        private static int GetTwSrMaxReqModerator()
        {
            return Properties.Settings.Default.TwSRMaxReqModerator;
        }

        private static int GetTwSrMaxReqSubscriber()
        {
            return Properties.Settings.Default.TwSRMaxReqSubscriber;

        }

        private static int GetTwSrMaxReqVip()
        {
            return Properties.Settings.Default.TwSRMaxReqVip;

        }

        private static bool GetTwSrReward()
        {
            return Properties.Settings.Default.TwSRReward;
        }

        private static int GetTwSrUserLevel()
        {
            return Properties.Settings.Default.TwSRUserLevel;
        }

        private static bool GetUpload()
        {
            return Properties.Settings.Default.uploadSonginfo;
        }

        private static bool GetUploadHistory()
        {
            return Properties.Settings.Default.UploadHistory;
        }

        private static bool GetUseOwnApp()
        {
            return Properties.Settings.Default.UseOwnAppID;
        }

        private static string GetUserBlacklist()
        {
            return Properties.Settings.Default.UserBlacklist;
        }

        private static string GetUuid()
        {
            return Properties.Settings.Default.uuid;
        }

        private static int GetWebServerPort()
        {
            return Properties.Settings.Default.WebServerPort;
        }

        private static string GetWebua()
        {
            return Properties.Settings.Default.webua;
        }

        private static void SetAnnounceInChat(bool value)
        {
            Properties.Settings.Default.AnnounceInChat = value;
            Properties.Settings.Default.Save();
        }

        private static void SetAppendSpaces(bool value)
        {
            Properties.Settings.Default.spacesEnabled = value;
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

        private static void SetAutoStartWebServer(bool value)
        {
            Properties.Settings.Default.AutoStartWebServer = value;
            Properties.Settings.Default.Save();
        }
        private static void SetBetaUpdates(bool value)
        {
            Properties.Settings.Default.BetaUpdates = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_Blacklist(string value)
        {
            Properties.Settings.Default.bot_Resp_Blacklist = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_Error(string value)
        {
            Properties.Settings.Default.bot_Resp_Error = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_IsInQueue(string value)
        {
            Properties.Settings.Default.bot_Resp_IsInQueue = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_Length(string value)
        {
            Properties.Settings.Default.bot_Resp_Length = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_MaxReq(string value)
        {
            Properties.Settings.Default.bot_Resp_MaxReq = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_ModSkip(string value)
        {
            Properties.Settings.Default.bot_Resp_ModSkip = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_Next(string value)
        {
            Properties.Settings.Default.bot_Resp_Next = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_NoSong(string value)
        {
            Properties.Settings.Default.bot_Resp_NoSong = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_Pos(string value)
        {
            Properties.Settings.Default.bot_Resp_Pos = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_Success(string value)
        {
            Properties.Settings.Default.bot_Resp_Success = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBot_Resp_VoteSkip(string value)
        {
            Properties.Settings.Default.bot_Resp_VoteSkip = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBotCmdNext(bool value)
        {
            Properties.Settings.Default.bot_cmd_next = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBotCmdPos(bool value)
        {
            Properties.Settings.Default.bot_cmd_pos = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBotCmdSkip(bool value)
        {
            Properties.Settings.Default.bot_cmd_skip = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBotCmdSkipVote(bool value)
        {
            Properties.Settings.Default.bot_cmd_skip_vote = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBotCmdSkipVoteCount(int value)
        {
            Properties.Settings.Default.bot_cmd_skip_vote_count = value;
            Properties.Settings.Default.Save();
        }

        private static void SetBotCmdSong(bool value)
        {
            Properties.Settings.Default.bot_cmd_song = value;
            Properties.Settings.Default.Save();
        }

        private static void SetChromeFetchRate(int rate)
        {
            Properties.Settings.Default.ChromeFetchRate = rate;
            Properties.Settings.Default.Save();
        }

        private static void SetClientId(string value)
        {
            Properties.Settings.Default.ClientID = value;
            Properties.Settings.Default.Save();
        }

        private static void SetClientSecret(string value)
        {
            Properties.Settings.Default.ClientSecret = value;
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

        private static void SetGuidedSetup(bool value)
        {
            Properties.Settings.Default.GuidedSetup = value;
            Properties.Settings.Default.Save();
        }

        private static void SetLangauge(string value)
        {
            Properties.Settings.Default.language = value;
            Properties.Settings.Default.Save();
        }

        private static void SetMaxSongLength(int value)
        {
            Properties.Settings.Default.MaxSongLength = value;
            Properties.Settings.Default.Save();
        }

        private static void SetMsgLoggingEnabled(bool value)
        {
            Properties.Settings.Default.MsgLoggingEnabled = value;
            Properties.Settings.Default.Save();
        }

        private static void SetOpenQueueOnStartup(bool value)
        {
            Properties.Settings.Default.OpenQueueOnStartup = value;
            Properties.Settings.Default.Save();
        }

        private static void SetOutputString(string outputstring)
        {
            Properties.Settings.Default.outputString = outputstring;
            Properties.Settings.Default.Save();
        }

        private static void SetOutputString2(string value)
        {
            Properties.Settings.Default.outputString2 = value;
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

        private static void SetRefundConditons(int[] value)
        {
            Properties.Settings.Default.RefundConditons = value;
            Properties.Settings.Default.Save();

        }

        private static void SetSaveHistory(bool savehistory)
        {
            Properties.Settings.Default.SaveHistory = savehistory;
            Properties.Settings.Default.Save();
        }

        private static void SetSource(int source)
        {
            Properties.Settings.Default.Source = source;
            Properties.Settings.Default.Save();
        }

        private static void SetSpaceCount(int value)
        {
            Properties.Settings.Default.spaces = value;
            Properties.Settings.Default.Save();
        }

        private static void SetSplitOutput(bool value)
        {
            Properties.Settings.Default.SplitString = value;
            Properties.Settings.Default.Save();
        }

        private static void SetSpotifyAccessToken(string value)
        {
            Properties.Settings.Default.SpotifyAccessToken = value;
            Properties.Settings.Default.Save();
        }

        private static void SetSpotifyDeviceId(string value)
        {
            Properties.Settings.Default.SpotifyDeviceID = value;
            Properties.Settings.Default.Save();
        }

        private static void SetSpotifyRefreshToken(string value)
        {
            Properties.Settings.Default.SpotifyRefreshToken = value;
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

        private static void SetTwitchAccessToken(string value)
        {
            Properties.Settings.Default.TwitchAccessToken = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwitchChannelId(string value)
        {
            Properties.Settings.Default.TwitchChannelId = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwitchUser(User value)
        {
            Properties.Settings.Default.TwitchUser = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwOAuth(string value)
        {
            Properties.Settings.Default.TwOauth = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwRewardId(string value)
        {
            Properties.Settings.Default.TwRewardID = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrCommand(bool value)
        {
            Properties.Settings.Default.TwSRCommand = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrCooldown(int value)
        {
            Properties.Settings.Default.TwSRCooldown = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrMaxReq(int value)
        {
            Properties.Settings.Default.TwSRMaxReq = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrMaxReqBroadcaster(int value)
        {
            Properties.Settings.Default.TwSRMaxReqBroadcaster = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrMaxReqEveryone(int value)
        {
            Properties.Settings.Default.TwSRMaxReqEveryone = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrMaxReqModerator(int value)
        {
            Properties.Settings.Default.TwSRMaxReqModerator = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrMaxReqSubscriber(int value)
        {
            Properties.Settings.Default.TwSRMaxReqSubscriber = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrMaxReqVip(int value)
        {
            Properties.Settings.Default.TwSRMaxReqVip = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrReward(bool value)
        {
            Properties.Settings.Default.TwSRReward = value;
            Properties.Settings.Default.Save();
        }

        private static void SetTwSrUserLevel(int value)
        {
            Properties.Settings.Default.TwSRUserLevel = value;
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

        private static void SetUseOwnApp(bool value)
        {
            Properties.Settings.Default.UseOwnAppID = value;
            Properties.Settings.Default.Save();
        }

        private static void SetUserBlacklist(string value)
        {
            Properties.Settings.Default.UserBlacklist = value;
            Properties.Settings.Default.Save();
        }

        private static void SetUuid(string uuid)
        {
            Properties.Settings.Default.uuid = uuid;
            Properties.Settings.Default.Save();
        }

        private static void SetWebServerPort(int value)
        {
            Properties.Settings.Default.WebServerPort = value;
            Properties.Settings.Default.Save();
        }
    }
}