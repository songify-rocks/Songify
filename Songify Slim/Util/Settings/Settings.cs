using Songify_Slim.Views;
using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Songify_Slim.Util.Settings
{
    /// <summary>
    ///     This class is a getter and setter for Settings
    /// </summary>
    internal class Settings
    {
        private static Configuration _currentConfig = new Configuration();


        public static string AccessKey
        {
            get => GetAccessKey();
            set => SetAccessKey(value);
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

        public static List<string> ArtistBlacklist
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

        public static string BotCmdNextTrigger { get => GetCmdNextTrigger(); set => SetCmdNextTrigger(value); }

        public static bool BotCmdPos
        {
            get => GetBotCmdPos();
            set => SetBotCmdPos(value);
        }

        public static string BotCmdPosTrigger { get => GetCmdPosTrigger(); set => SetCmdPosTrigger(value); }

        public static bool BotCmdRemove { get => GetBotCmdRemove(); set => SetBotCmdRemove(value); }

        public static bool BotCmdSkip
        {
            get => GetBotCmdSkip();
            set => SetBotCmdSkip(value);
        }

        public static string BotCmdSkipTrigger { get => GetCmdSkipTrigger(); set => SetCmdSkipTrigger(value); }

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

        public static string BotCmdSongTrigger { get => GetCmdSongTrigger(); set => SetCmdSongTrigger(value); }

        public static string BotCmdSsrTrigger { get => GetCmdSsrTrigger(); set => SetCmdSsrTrigger(value); }

        public static string BotCmdVoteskipTrigger { get => GetCmdVoteskipTrigger(); set => SetCmdVoteskipTrigger(value); }

        public static bool BotOnlyWorkWhenLive
        {
            get => GetBotOnlyWorkWhenLive();
            set => SetBotOnlyWorkWhenLive(value);
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

        public static string BotRespRefund { get => GetBot_Resp_Refund(); set => SetBot_Resp_Refund(value); }

        public static string BotRespSong { get => GetBot_resp_Song(); set => SetBot_Resp_Song(value); }

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

        public static bool ChatLiveStatus { get => GetChatLiveStatus(); set => SetChatLiveStatus(value); }

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

        public static int Player
        {
            get => GetSource();
            set => SetSource(value);
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

        public static int RewardGoalAmount { get => GetRewardGoalAmount(); set => SetRewardGoalAmount(value); }

        public static bool RewardGoalEnabled { get => GetRewardGoalEnabled(); set => SetRewardGoalEnabled(value); }

        public static string RewardGoalSong { get => GetRewardGoalSong(); set => SetRewardGoalSong(value); }

        public static bool SaveHistory
        {
            get => GetSaveHistory();
            set => SetSaveHistory(value);
        }

        public static List<TrackItem> SongBlacklist { get => GetSongBlacklist(); set => SetSongBlacklist(value); }

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

        public static string SpotifyPlaylistId { get => GetSpotifyPlaylistId(); set => SetSpotifyPlaylistId(value); }

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

        public static string TwitchBotToken { get => GetTwitchBotToken(); set => SetTwitchBotToken(value); }

        public static User TwitchBotUser { get => GetTwitchBotUser(); set => SetTwitchBotUser(value); }

        public static string TwitchChannelId
        {
            get => GetTwitchChannelId();
            set => SetTwitchChannelId(value);
        }

        public static int TwitchFetchPort
        {
            get => GetTwitchFetchPort();
            set => SetTwitchFetchPort(value);
        }

        public static int TwitchRedirectPort
        {
            get => GetTwitchRedirectPort();
            set => SetTwitchRedirectPort(value);
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

        public static string TwRewardGoalRewardId { get => GetTwRewardGoalRewardId(); set => SetTwRewardGoalRewardId(value); }

        public static string TwRewardId
        {
            get => GetTwRewardId();
            set => SetTwRewardId(value);
        }

        public static string TwRewardSkipId { get => GetTwRewardSkipId(); set => SetTwRewardSkipId(value); }

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

        public static bool TwSrUnlimitedSr { get => GetTwSrUnlimitedSr(); set => SetTwSrUnlimitedSr(value); }

        public static int TwSrUserLevel
        {
            get => GetTwSrUserLevel();
            set => SetTwSrUserLevel(value);
        }


        public static List<int> UserLevelsReward
        {
            get => GetUserLevelsReward();
            set => SetUserLevelsReward(value);
        }

        private static void SetUserLevelsReward(List<int> value)
        {
            _currentConfig.AppConfig.UserLevelsReward = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static List<int> GetUserLevelsReward()
        {
            return _currentConfig.AppConfig.UserLevelsReward;
        }

        public static List<int> UserLevelsCommand
        {
            get => GetUserLevelsCommand();
            set => SetUserLevelsCommand(value);
        }

        private static void SetUserLevelsCommand(List<int> value)
        {
            _currentConfig.AppConfig.UserLevelsCommand = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static List<int> GetUserLevelsCommand()
        {
            return _currentConfig.AppConfig.UserLevelsCommand;
        }

        public static bool UpdateRequired { get => GetUpdateRequired(); set => SetUpdateRequired(value); }

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

        public static List<string> UserBlacklist
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

        public static string WebUserAgent => GetWebua();
        public static string BotCmdRemoveTrigger { get => GetCmdRemoveTrigger(); set => SetCmdRemoveTrigger(value); }

        public static string BotCmdSonglikeTrigger { get => GetCmdSonglikeTrigger(); set => SetCmdSonglikeTrigger(value); }

        private static string GetCmdSonglikeTrigger()
        {
            return _currentConfig.BotConfig.BotCmdSonglikeTrigger;
        }

        private static void SetCmdSonglikeTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdSonglikeTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        public static bool BotCmdSonglike { get => GetBotCmdSonglike(); set => SetBotCmdSonglike(value); }
        public static string BotRespSongLike { get => GetBot_Resp_SongLike(); set => SetBot_Resp_SongLike(value); }
        public static bool BotCmdPlayPause { get => GetBotCmdPlayPause(); set => SetBotCmdPlayPause(value); }
        public static bool AddSrToPlaylist { get => GetAddSrToPlaylist(); set => SetAddSrToPlaylist(value); }
        public static List<int> QueueWindowColumns { get => GetQueueWindowColumns(); set => SetQueueWindowColumns(value); }

        private static void SetQueueWindowColumns(List<int> value)
        {
            _currentConfig.AppConfig.QueueWindowColumns = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static List<int> GetQueueWindowColumns()
        {
            return _currentConfig.AppConfig.QueueWindowColumns;
        }

        private static bool GetAddSrToPlaylist()
        {
            return _currentConfig.AppConfig.AddSrToPlaylist;
        }

        private static void SetAddSrToPlaylist(bool value)
        {
            _currentConfig.AppConfig.AddSrToPlaylist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static bool GetBotCmdPlayPause()
        {
            return _currentConfig.BotConfig.BotCmdPlayPause;
        }

        private static void SetBotCmdPlayPause(bool value)
        {
            _currentConfig.BotConfig.BotCmdPlayPause = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static string GetBot_Resp_SongLike()
        {
            return _currentConfig.BotConfig.BotRespSongLike;
        }

        private static void SetBot_Resp_SongLike(string value)
        {
            _currentConfig.BotConfig.BotRespSongLike = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static bool GetBotCmdSonglike()
        {
            return _currentConfig.BotConfig.BotCmdSonglike;
        }


        private static void SetBotCmdSonglike(bool value)
        {
            _currentConfig.BotConfig.BotCmdSonglike = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetCmdRemoveTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdRemoveTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static string GetCmdRemoveTrigger()
        {
            return _currentConfig.BotConfig.BotCmdRemoveTrigger;
        }

        public static Configuration Export()
        {
            SpotifyCredentials spotifyCredentials = new SpotifyCredentials
            {
                AccessToken = GetSpotifyAccessToken(),
                ClientId = GetClientId(),
                ClientSecret = GetClientSecret(),
                DeviceId = GetSpotifyDeviceId(),
                RefreshToken = GetSpotifyRefreshToken(),

            };

            TwitchCredentials twitchCredentials = new TwitchCredentials
            {
                AccessToken = GetTwitchAccessToken(),
                BotAccountName = GetTwAcc(),
                BotOAuthToken = GetTwOAuth(),
                BotUser = GetTwitchBotUser(),
                ChannelId = GetTwitchChannelId(),
                ChannelName = GetTwChannel(),
                TwitchBotToken = GetTwitchBotToken(),
                TwitchUser = GetTwitchUser(),
            };

            BotConfig botConfig = new BotConfig
            {
                BotCmdNext = GetBotCmdNext(),
                BotCmdNextTrigger = GetCmdNextTrigger(),
                BotCmdPos = GetBotCmdPos(),
                BotCmdPosTrigger = GetCmdPosTrigger(),
                BotCmdRemove = GetBotCmdRemove(),
                BotCmdRemoveTrigger = GetCmdRemoveTrigger(),
                BotCmdSkip = GetBotCmdSkip(),
                BotCmdSkipTrigger = GetCmdSkipTrigger(),
                BotCmdSkipVote = GetBotCmdSkipVote(),
                BotCmdSkipVoteCount = GetBotCmdSkipVoteCount(),
                BotCmdSong = GetBotCmdSong(),
                BotCmdSongTrigger = GetCmdSongTrigger(),
                BotCmdSsrTrigger = GetCmdSsrTrigger(),
                BotCmdVoteskipTrigger = GetCmdVoteskipTrigger(),
                BotRespBlacklist = GetBot_Resp_Blacklist(),
                BotRespError = GetBot_Resp_Error(),
                BotRespIsInQueue = GetBot_Resp_IsInQueue(),
                BotRespLength = GetBot_Resp_Length(),
                BotRespMaxReq = GetBot_Resp_MaxReq(),
                BotRespModSkip = GetBot_Resp_ModSkip(),
                BotRespNext = GetBot_Resp_Next(),
                BotRespNoSong = GetBot_Resp_NoSong(),
                BotRespPos = GetBot_Resp_Pos(),
                BotRespRefund = GetBot_Resp_Refund(),
                BotRespSong = GetBot_resp_Song(),
                BotRespSuccess = GetBot_Resp_Success(),
                BotRespVoteSkip = GetBot_Resp_VoteSkip(),
                ChatLiveStatus = GetChatLiveStatus(),
                OnlyWorkWhenLive = GetBotOnlyWorkWhenLive(),
                BotCmdSonglike = GetBotCmdSonglike(),
                BotCmdSonglikeTrigger = GetCmdSonglikeTrigger(),
                BotRespSongLike = GetBot_Resp_SongLike(),
                BotCmdPlayPause = GetBotCmdPlayPause(),
            };

            AppConfig appConfig = new AppConfig
            {
                AccessKey = GetAccessKey(),
                AnnounceInChat = GetAnnounceInChat(),
                AppendSpaces = GetAppendSpaces(),
                ArtistBlacklist = GetArtistBlacklist(),
                AutoClearQueue = GetAutoClearQueue(),
                Autostart = GetAutostart(),
                AutoStartWebServer = GetAutoStartWebServer(),
                BetaUpdates = GetBetaUpdates(),
                BotOnlyWorkWhenLive = GetBotOnlyWorkWhenLive(),
                ChromeFetchRate = GetChromeFetchRate(),
                Color = GetColor(),
                CustomPauseText = GetCustomPauseText(),
                CustomPauseTextEnabled = GetCustomPauseTextEnabled(),
                Directory = GetDirectory(),
                DownloadCover = GetDownloadCover(),
                Language = GetLanguage(),
                MaxSongLength = GetMaxSongLength(),
                MsgLoggingEnabled = GetMsgLoggingEnabled(),
                OpenQueueOnStartup = GetOpenQueueOnStartup(),
                OutputString = GetOutputString(),
                OutputString2 = GetOutputString2(),
                Player = GetSource(),
                PosX = (int)GetPosX(),
                PosY = (int)GetPosY(),
                RefundConditons = GetRefundConditons(),
                RewardGoalAmount = GetRewardGoalAmount(),
                RewardGoalEnabled = GetRewardGoalEnabled(),
                RewardGoalSong = GetRewardGoalSong(),
                SaveHistory = GetSaveHistory(),
                SongBlacklist = GetSongBlacklist(),
                SpaceCount = GetSpaceCount(),
                SplitOutput = GetSplitOutput(),
                SpotifyPlaylistId = GetSpotifyPlaylistId(),
                Systray = GetSystray(),
                Telemetry = GetTelemetry(),
                Theme = GetTheme(),
                TwAutoConnect = GetTwAutoConnect(),
                TwitchFetchPort = GetTwitchFetchPort(),
                TwitchRedirectPort = GetTwitchRedirectPort(),
                TwRewardGoalRewardId = GetTwRewardGoalRewardId(),
                TwRewardId = GetTwRewardId(),
                TwRewardSkipId = GetTwRewardSkipId(),
                TwSrCommand = GetTwSrCommand(),
                TwSrCooldown = GetTwSrCooldown(),
                TwSrMaxReq = GetTwSrMaxReq(),
                TwSrMaxReqBroadcaster = GetTwSrMaxReqBroadcaster(),
                TwSrMaxReqEveryone = GetTwSrMaxReqEveryone(),
                TwSrMaxReqModerator = GetTwSrMaxReqModerator(),
                TwSrMaxReqSubscriber = GetTwSrMaxReqSubscriber(),
                TwSrMaxReqVip = GetTwSrMaxReqVip(),
                TwSrReward = GetTwSrReward(),
                TwSrUnlimitedSr = GetTwSrUnlimitedSr(),
                TwSrUserLevel = GetTwSrUserLevel(),
                UpdateRequired = GetUpdateRequired(),
                Upload = GetUpload(),
                UploadHistory = GetUploadHistory(),
                UseOwnApp = GetUseOwnApp(),
                UserBlacklist = GetUserBlacklist(),
                Uuid = GetUuid(),
                WebServerPort = GetWebServerPort(),
                WebUserAgent = GetWebua(),
                UserLevelsCommand = GetUserLevelsCommand(),
                AddSrToPlaylist = GetAddSrToPlaylist(),
                QueueWindowColumns = GetQueueWindowColumns(),
                UserLevelsReward = GetUserLevelsReward(),
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
            _currentConfig = config;

            //#region SpotifyCredential
            //SetSpotifyAccessToken(config.SpotifyCredentials.AccessToken);
            //SetClientId(config.SpotifyCredentials.ClientId);
            //SetClientSecret(config.SpotifyCredentials.ClientSecret);
            //SetSpotifyRefreshToken(config.SpotifyCredentials.RefreshToken);
            //SetSpotifyDeviceId(config.SpotifyCredentials.DeviceId);
            //#endregion

            //#region BotConfig
            //SetBot_Resp_ModSkip(config.BotConfig.BotRespModSkip);
            //SetBot_Resp_VoteSkip(config.BotConfig.BotRespVoteSkip);
            //SetBot_Resp_Pos(config.BotConfig.BotRespPos);
            //SetBot_Resp_Next(config.BotConfig.BotRespNext);
            //SetBot_Resp_MaxReq(config.BotConfig.BotRespMaxReq);
            //SetBot_Resp_IsInQueue(config.BotConfig.BotRespIsInQueue);
            //SetBot_Resp_Length(config.BotConfig.BotRespLength);
            //SetBot_Resp_Blacklist(config.BotConfig.BotRespBlacklist);
            //SetBot_Resp_Error(config.BotConfig.BotRespError);
            //SetBot_Resp_Success(config.BotConfig.BotRespSuccess);
            //SetBot_Resp_NoSong(config.BotConfig.BotRespNoSong);
            //SetBotCmdNext(config.BotConfig.BotCmdNext);
            //SetBotCmdPos(config.BotConfig.BotCmdPos);
            //SetBotCmdSkip(config.BotConfig.BotCmdSkip);
            //SetBotCmdSkipVote(config.BotConfig.BotCmdSkipVote);
            //SetBotCmdSkipVoteCount(config.BotConfig.BotCmdSkipVoteCount);
            //SetBotCmdSong(config.BotConfig.BotCmdSong);
            //#endregion

            //#region TwitchCredentials
            //SetTwAcc(config.TwitchCredentials.BotAccountName);
            //SetTwChannel(config.TwitchCredentials.ChannelName);
            //SetTwOAuth(config.TwitchCredentials.BotOAuthToken);
            //#endregion

            //#region AppConfig
            //SetAnnounceInChat(config.AppConfig.AnnounceInChat);
            //SetAppendSpaces(config.AppConfig.AppendSpaces);
            //SetArtistBlacklist(config.AppConfig.ArtistBlacklist);
            //SetAutoClearQueue(config.AppConfig.AutoClearQueue);
            //SetAutostart(config.AppConfig.Autostart);
            //SetColor(config.AppConfig.Color);
            //SetCustomPauseText(config.AppConfig.CustomPauseText);
            //SetCustomPauseTextEnabled(config.AppConfig.CustomPauseTextEnabled);
            //SetDirectory(config.AppConfig.Directory);
            //SetDownloadCover(config.AppConfig.DownloadCover);
            //SetLangauge(config.AppConfig.Language);
            //SetMaxSongLength(config.AppConfig.MaxSongLength);
            //SetMsgLoggingEnabled(config.AppConfig.MsgLoggingEnabled);
            //SetOpenQueueOnStartup(config.AppConfig.OpenQueueOnStartup);
            //SetOutputString(config.AppConfig.OutputString);
            //SetOutputString2(config.AppConfig.OutputString2);
            //SetPosX(config.AppConfig.PosX);
            //SetPosY(config.AppConfig.PosY);
            //SetSaveHistory(config.AppConfig.SaveHistory);
            //SetSpaceCount(config.AppConfig.SpaceCount);
            //SetSplitOutput(config.AppConfig.SplitOutput);
            //SetSystray(config.AppConfig.Systray);
            //SetTelemetry(config.AppConfig.Telemetry);
            //SetTheme(config.AppConfig.Theme);
            //SetTwAutoConnect(config.AppConfig.TwAutoConnect);
            //SetTwRewardId(config.AppConfig.TwRewardId);
            //SetTwSrCommand(config.AppConfig.TwSrCommand);
            //SetTwSrCooldown(config.AppConfig.TwSrCooldown);
            //SetTwSrMaxReq(config.AppConfig.TwSrMaxReq);
            //SetTwSrMaxReqBroadcaster(config.AppConfig.TwSrMaxReqBroadcaster);
            //SetTwSrMaxReqEveryone(config.AppConfig.TwSrMaxReqEveryone);
            //SetTwSrMaxReqModerator(config.AppConfig.TwSrMaxReqModerator);
            //SetTwSrMaxReqSubscriber(config.AppConfig.TwSrMaxReqSubscriber);
            //SetTwSrMaxReqVip(config.AppConfig.TwSrMaxReqVip);
            //SetTwSrReward(config.AppConfig.TwSrReward);
            //SetTwSrUserLevel(config.AppConfig.TwSrUserLevel);
            //SetUpload(config.AppConfig.Upload);
            //SetUploadHistory(config.AppConfig.UploadHistory);
            //SetUseOwnApp(config.AppConfig.UseOwnApp);
            //SetUserBlacklist(config.AppConfig.UserBlacklist);
            //SetUuid(config.AppConfig.Uuid);
            //SetRefundConditons(config.AppConfig.RefundConditons);
            //SetWebServerPort(config.AppConfig.WebServerPort);
            //SetBetaUpdates(config.AppConfig.BetaUpdates);
            //#endregion

            ConfigHandler.WriteAllConfig(config);
        }

        public static void ResetConfig()
        {
            _currentConfig = new Configuration();
        }

        private static string GetAccessKey()
        {
            return _currentConfig.AppConfig.AccessKey;
        }

        private static bool GetAnnounceInChat()
        {
            return _currentConfig.AppConfig.AnnounceInChat;
        }

        private static bool GetAppendSpaces()
        {
            return _currentConfig.AppConfig.AppendSpaces;
        }

        private static List<string> GetArtistBlacklist()
        {
            return _currentConfig.AppConfig.ArtistBlacklist;
        }

        private static bool GetAutoClearQueue()
        {
            return _currentConfig.AppConfig.AutoClearQueue;
        }

        private static bool GetAutostart()
        {
            return _currentConfig.AppConfig.Autostart;
        }

        private static bool GetAutoStartWebServer()
        {
            return _currentConfig.AppConfig.AutoStartWebServer;
        }

        private static bool GetBetaUpdates()
        {
            return _currentConfig.AppConfig.BetaUpdates;
        }

        private static string GetBot_Resp_Blacklist()
        {
            return _currentConfig.BotConfig.BotRespBlacklist;
        }

        private static string GetBot_Resp_Error()
        {
            return _currentConfig.BotConfig.BotRespError;
        }

        private static string GetBot_Resp_IsInQueue()
        {
            return _currentConfig.BotConfig.BotRespIsInQueue;
        }

        private static string GetBot_Resp_Length()
        {
            return _currentConfig.BotConfig.BotRespLength;
        }

        private static string GetBot_Resp_MaxReq()
        {
            return _currentConfig.BotConfig.BotRespMaxReq;
        }

        private static string GetBot_Resp_ModSkip()
        {
            return _currentConfig.BotConfig.BotRespModSkip;
        }

        private static string GetBot_Resp_Next()
        {
            return _currentConfig.BotConfig.BotRespNext;
        }

        private static string GetBot_Resp_NoSong()
        {
            return _currentConfig.BotConfig.BotRespNoSong;
        }

        private static string GetBot_Resp_Pos()
        {
            return _currentConfig.BotConfig.BotRespPos;
        }

        private static string GetBot_Resp_Refund()
        {
            return _currentConfig.BotConfig.BotRespRefund;
        }

        private static string GetBot_resp_Song()
        {
            return _currentConfig.BotConfig.BotRespSong;
        }

        private static string GetBot_Resp_Success()
        {
            return _currentConfig.BotConfig.BotRespSuccess;
        }

        private static string GetBot_Resp_VoteSkip()
        {

            return _currentConfig.BotConfig.BotRespVoteSkip;
        }

        private static bool GetBotCmdNext()
        {
            return _currentConfig.BotConfig.BotCmdNext;
        }

        private static bool GetBotCmdPos()
        {
            return _currentConfig.BotConfig.BotCmdPos;
        }

        private static bool GetBotCmdRemove()
        {
            return _currentConfig.BotConfig.BotCmdRemove;
        }

        private static bool GetBotCmdSkip()
        {
            return _currentConfig.BotConfig.BotCmdSkip;
        }

        private static bool GetBotCmdSkipVote()
        {
            return _currentConfig.BotConfig.BotCmdSkipVote;
        }

        private static int GetBotCmdSkipVoteCount()
        {
            return _currentConfig.BotConfig.BotCmdSkipVoteCount;
        }

        private static bool GetBotCmdSong()
        {
            return _currentConfig.BotConfig.BotCmdSong;
        }

        private static bool GetBotOnlyWorkWhenLive()
        {
            return _currentConfig.AppConfig.BotOnlyWorkWhenLive;
        }

        private static bool GetChatLiveStatus()
        {
            return _currentConfig.BotConfig.ChatLiveStatus;
        }

        private static int GetChromeFetchRate()
        {
            return _currentConfig.AppConfig.ChromeFetchRate;
        }

        private static string GetClientId()
        {
            return _currentConfig.SpotifyCredentials.ClientId;
        }

        private static string GetClientSecret()
        {
            return _currentConfig.SpotifyCredentials.ClientSecret;
        }

        private static string GetCmdNextTrigger()
        {
            return _currentConfig.BotConfig.BotCmdNextTrigger;
        }

        private static string GetCmdPosTrigger()
        {
            return _currentConfig.BotConfig.BotCmdPosTrigger;
        }

        private static string GetCmdSkipTrigger()
        {
            return _currentConfig.BotConfig.BotCmdSkipTrigger;
        }

        private static string GetCmdSongTrigger()
        {
            return _currentConfig.BotConfig.BotCmdSongTrigger;
        }

        private static string GetCmdSsrTrigger()
        {
            return _currentConfig.BotConfig.BotCmdSsrTrigger;
        }

        private static string GetCmdVoteskipTrigger()
        {
            return _currentConfig.BotConfig.BotCmdVoteskipTrigger;
        }

        private static string GetColor()
        {
            return _currentConfig.AppConfig.Color;
        }

        private static string GetCustomPauseText()
        {
            return _currentConfig.AppConfig.CustomPauseText;
        }

        private static bool GetCustomPauseTextEnabled()
        {
            return _currentConfig.AppConfig.CustomPauseTextEnabled;
        }

        private static string GetDirectory()
        {
            return _currentConfig.AppConfig.Directory;
        }

        private static bool GetDownloadCover()
        {
            return _currentConfig.AppConfig.DownloadCover;
        }

        private static string GetLanguage()
        {
            return _currentConfig.AppConfig.Language;
        }

        private static int GetMaxSongLength()
        {
            return _currentConfig.AppConfig.MaxSongLength;
        }

        private static bool GetMsgLoggingEnabled()
        {
            return _currentConfig.AppConfig.MsgLoggingEnabled;
        }

        private static bool GetOpenQueueOnStartup()
        {
            return _currentConfig.AppConfig.OpenQueueOnStartup;
        }

        private static string GetOutputString()
        {
            return _currentConfig.AppConfig.OutputString;
        }

        private static string GetOutputString2()
        {
            return _currentConfig.AppConfig.OutputString2;
        }

        private static double GetPosX()
        {
            return _currentConfig.AppConfig.PosX;
        }

        private static double GetPosY()
        {
            return _currentConfig.AppConfig.PosY;
        }

        private static int[] GetRefundConditons()
        {
            return _currentConfig.AppConfig.RefundConditons;
        }

        private static int GetRewardGoalAmount()
        {
            return _currentConfig.AppConfig.RewardGoalAmount;
        }

        private static bool GetRewardGoalEnabled()
        {
            return _currentConfig.AppConfig.RewardGoalEnabled;
        }

        private static string GetRewardGoalSong()
        {
            return _currentConfig.AppConfig.RewardGoalSong;
        }

        private static bool GetSaveHistory()
        {
            return _currentConfig.AppConfig.SaveHistory;
        }

        private static List<TrackItem> GetSongBlacklist()
        {
            return _currentConfig.AppConfig.SongBlacklist;
        }

        private static int GetSource()
        {
            return _currentConfig.AppConfig.Player;
        }

        private static int GetSpaceCount()
        {
            return _currentConfig.AppConfig.SpaceCount;
        }

        private static bool GetSplitOutput()
        {
            return _currentConfig.AppConfig.SplitOutput;
        }

        private static string GetSpotifyAccessToken()
        {
            return _currentConfig.SpotifyCredentials.AccessToken;
        }

        private static string GetSpotifyDeviceId()
        {
            return _currentConfig.SpotifyCredentials.DeviceId;
        }

        private static string GetSpotifyPlaylistId()
        {
            return _currentConfig.AppConfig.SpotifyPlaylistId;
        }

        private static string GetSpotifyRefreshToken()
        {
            return _currentConfig.SpotifyCredentials.RefreshToken;
        }

        private static bool GetSystray()
        {
            return _currentConfig.AppConfig.Systray;
        }

        private static bool GetTelemetry()
        {
            return _currentConfig.AppConfig.Telemetry;
        }

        private static string GetTheme()
        {
            return _currentConfig.AppConfig.Theme;
        }

        private static string GetTwAcc()
        {
            return _currentConfig.TwitchCredentials.BotAccountName;
        }

        private static bool GetTwAutoConnect()
        {
            return _currentConfig.AppConfig.TwAutoConnect;
        }

        private static string GetTwChannel()
        {
            return _currentConfig.TwitchCredentials.ChannelName;
        }

        private static string GetTwitchAccessToken()
        {
            return _currentConfig.TwitchCredentials.AccessToken;

        }

        private static string GetTwitchBotToken()
        {
            return _currentConfig.TwitchCredentials.TwitchBotToken;
        }

        private static User GetTwitchBotUser()
        {
            return _currentConfig.TwitchCredentials.BotUser;
        }

        private static string GetTwitchChannelId()
        {
            return _currentConfig.TwitchCredentials.ChannelId;

        }

        private static int GetTwitchFetchPort()
        {
            return _currentConfig.AppConfig.TwitchFetchPort;
        }

        private static int GetTwitchRedirectPort()
        {
            return _currentConfig.AppConfig.TwitchRedirectPort;
        }

        private static User GetTwitchUser()
        {
            return _currentConfig.TwitchCredentials.TwitchUser;
        }

        private static string GetTwOAuth()
        {
            return _currentConfig.TwitchCredentials.BotOAuthToken;
        }

        private static string GetTwRewardGoalRewardId()
        {
            return _currentConfig.AppConfig.TwRewardGoalRewardId;
        }

        private static string GetTwRewardId()
        {
            return _currentConfig.AppConfig.TwRewardId;
        }

        private static string GetTwRewardSkipId()
        {
            return _currentConfig.AppConfig.TwRewardSkipId;

        }

        private static bool GetTwSrCommand()
        {
            return _currentConfig.AppConfig.TwSrCommand;
        }

        private static int GetTwSrCooldown()
        {
            return _currentConfig.AppConfig.TwSrCooldown;
        }

        private static int GetTwSrMaxReq()
        {
            return _currentConfig.AppConfig.TwSrMaxReq;
        }

        private static int GetTwSrMaxReqBroadcaster()
        {
            return _currentConfig.AppConfig.TwSrMaxReqBroadcaster;
        }

        private static int GetTwSrMaxReqEveryone()
        {
            return _currentConfig.AppConfig.TwSrMaxReqEveryone;
        }

        private static int GetTwSrMaxReqModerator()
        {
            return _currentConfig.AppConfig.TwSrMaxReqModerator;
        }

        private static int GetTwSrMaxReqSubscriber()
        {
            return _currentConfig.AppConfig.TwSrMaxReqSubscriber;
        }

        private static int GetTwSrMaxReqVip()
        {
            return _currentConfig.AppConfig.TwSrMaxReqVip;
        }

        private static bool GetTwSrReward()
        {
            return _currentConfig.AppConfig.TwSrReward;
        }

        private static bool GetTwSrUnlimitedSr()
        {
            return _currentConfig.AppConfig.TwSrUnlimitedSr;
        }

        private static int GetTwSrUserLevel()
        {
            return _currentConfig.AppConfig.TwSrUserLevel;
        }

        private static bool GetUpdateRequired()
        {
            return _currentConfig.AppConfig.UpdateRequired;
        }

        private static bool GetUpload()
        {
            return _currentConfig.AppConfig.Upload;
        }

        private static bool GetUploadHistory()
        {
            return _currentConfig.AppConfig.UploadHistory;
        }

        private static bool GetUseOwnApp()
        {
            return _currentConfig.AppConfig.UseOwnApp;
        }

        private static List<string> GetUserBlacklist()
        {
            return _currentConfig.AppConfig.UserBlacklist;
        }

        private static string GetUuid()
        {
            return _currentConfig.AppConfig.Uuid;
        }

        private static int GetWebServerPort()
        {
            return _currentConfig.AppConfig.WebServerPort;
        }

        private static string GetWebua()
        {
            return "Songify Data Provider";
        }

        private static void SetAccessKey(string value)
        {
            _currentConfig.AppConfig.AccessKey = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }
        private static void SetAnnounceInChat(bool value)
        {
            _currentConfig.AppConfig.AnnounceInChat = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetAppendSpaces(bool value)
        {
            _currentConfig.AppConfig.AppendSpaces = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetArtistBlacklist(List<string> value)
        {
            _currentConfig.AppConfig.ArtistBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetAutoClearQueue(bool value)
        {
            _currentConfig.AppConfig.AutoClearQueue = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetAutostart(bool autostart)
        {
            _currentConfig.AppConfig.Autostart = autostart;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetAutoStartWebServer(bool value)
        {
            _currentConfig.AppConfig.AutoStartWebServer = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetBetaUpdates(bool value)
        {
            _currentConfig.AppConfig.BetaUpdates = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetBot_Resp_Blacklist(string value)
        {
            _currentConfig.BotConfig.BotRespBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Error(string value)
        {
            _currentConfig.BotConfig.BotRespError = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_IsInQueue(string value)
        {
            _currentConfig.BotConfig.BotRespIsInQueue = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Length(string value)
        {
            _currentConfig.BotConfig.BotRespLength = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_MaxReq(string value)
        {
            _currentConfig.BotConfig.BotRespMaxReq = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_ModSkip(string value)
        {
            _currentConfig.BotConfig.BotRespModSkip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Next(string value)
        {
            _currentConfig.BotConfig.BotRespNext = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_NoSong(string value)
        {
            _currentConfig.BotConfig.BotRespNoSong = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Pos(string value)
        {
            _currentConfig.BotConfig.BotRespPos = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Refund(string value)
        {
            _currentConfig.BotConfig.BotRespRefund = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Song(string value)
        {
            _currentConfig.BotConfig.BotRespSong = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Success(string value)
        {
            _currentConfig.BotConfig.BotRespSuccess = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBot_Resp_VoteSkip(string value)
        {
            _currentConfig.BotConfig.BotRespVoteSkip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdNext(bool value)
        {
            _currentConfig.BotConfig.BotCmdNext = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdPos(bool value)
        {
            _currentConfig.BotConfig.BotCmdPos = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdRemove(bool value)
        {
            _currentConfig.BotConfig.BotCmdRemove = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdSkip(bool value)
        {
            _currentConfig.BotConfig.BotCmdSkip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdSkipVote(bool value)
        {
            _currentConfig.BotConfig.BotCmdSkipVote = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdSkipVoteCount(int value)
        {
            _currentConfig.BotConfig.BotCmdSkipVoteCount = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotCmdSong(bool value)
        {
            _currentConfig.BotConfig.BotCmdSong = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetBotOnlyWorkWhenLive(bool value)
        {
            _currentConfig.AppConfig.BotOnlyWorkWhenLive = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetChatLiveStatus(bool value)
        {
            _currentConfig.BotConfig.ChatLiveStatus = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetChromeFetchRate(int value)
        {
            _currentConfig.AppConfig.ChromeFetchRate = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetClientId(string value)
        {
            _currentConfig.SpotifyCredentials.ClientId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, _currentConfig.SpotifyCredentials);
        }

        private static void SetClientSecret(string value)
        {
            _currentConfig.SpotifyCredentials.ClientSecret = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, _currentConfig.SpotifyCredentials);
        }

        private static void SetCmdNextTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdNextTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetCmdPosTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdPosTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetCmdSkipTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdSkipTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetCmdSongTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdSongTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetCmdSsrTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdSsrTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetCmdVoteskipTrigger(string value)
        {
            _currentConfig.BotConfig.BotCmdVoteskipTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, _currentConfig.BotConfig);
        }

        private static void SetColor(string value)
        {
            _currentConfig.AppConfig.Color = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetCustomPauseText(string value)
        {
            _currentConfig.AppConfig.CustomPauseText = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetCustomPauseTextEnabled(bool value)
        {
            _currentConfig.AppConfig.CustomPauseTextEnabled = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetDirectory(string value)
        {
            _currentConfig.AppConfig.Directory = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetDownloadCover(bool value)
        {
            _currentConfig.AppConfig.DownloadCover = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetLangauge(string value)
        {
            _currentConfig.AppConfig.Language = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetMaxSongLength(int value)
        {
            _currentConfig.AppConfig.MaxSongLength = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetMsgLoggingEnabled(bool value)
        {
            _currentConfig.AppConfig.MsgLoggingEnabled = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetOpenQueueOnStartup(bool value)
        {
            _currentConfig.AppConfig.OpenQueueOnStartup = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetOutputString(string value)
        {
            _currentConfig.AppConfig.OutputString = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetOutputString2(string value)
        {
            _currentConfig.AppConfig.OutputString2 = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetPosX(double value)
        {
            _currentConfig.AppConfig.PosX = (int)value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetPosY(double value)
        {
            _currentConfig.AppConfig.PosY = (int)value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetRefundConditons(int[] value)
        {
            _currentConfig.AppConfig.RefundConditons = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetRewardGoalAmount(int value)
        {
            _currentConfig.AppConfig.RewardGoalAmount = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetRewardGoalEnabled(bool value)
        {
            _currentConfig.AppConfig.RewardGoalEnabled = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetRewardGoalSong(string value)
        {
            _currentConfig.AppConfig.RewardGoalSong = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSaveHistory(bool value)
        {
            _currentConfig.AppConfig.SaveHistory = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSongBlacklist(List<TrackItem> value)
        {
            _currentConfig.AppConfig.SongBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSource(int value)
        {
            _currentConfig.AppConfig.Player = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSpaceCount(int value)
        {
            _currentConfig.AppConfig.SpaceCount = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSplitOutput(bool value)
        {
            _currentConfig.AppConfig.SplitOutput = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSpotifyAccessToken(string value)
        {
            _currentConfig.SpotifyCredentials.AccessToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, _currentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyDeviceId(string value)
        {
            _currentConfig.SpotifyCredentials.DeviceId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, _currentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyPlaylistId(string value)
        {
            _currentConfig.AppConfig.SpotifyPlaylistId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetSpotifyRefreshToken(string value)
        {
            _currentConfig.SpotifyCredentials.RefreshToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, _currentConfig.SpotifyCredentials);
        }

        private static void SetSystray(bool value)
        {
            _currentConfig.AppConfig.Systray = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTelemetry(bool value)
        {
            _currentConfig.AppConfig.Systray = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTheme(string value)
        {
            _currentConfig.AppConfig.Theme = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwAcc(string value)
        {
            _currentConfig.TwitchCredentials.BotAccountName = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwAutoConnect(bool value)
        {
            _currentConfig.AppConfig.TwAutoConnect = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwChannel(string value)
        {
            _currentConfig.TwitchCredentials.ChannelName = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwitchAccessToken(string value)
        {
            _currentConfig.TwitchCredentials.AccessToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwitchBotToken(string value)
        {
            _currentConfig.TwitchCredentials.TwitchBotToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwitchBotUser(User value)
        {
            _currentConfig.TwitchCredentials.BotUser = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwitchChannelId(string value)
        {
            _currentConfig.TwitchCredentials.ChannelId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwitchFetchPort(int value)
        {
            _currentConfig.AppConfig.TwitchFetchPort = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwitchRedirectPort(int value)
        {
            _currentConfig.AppConfig.TwitchRedirectPort = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }
        private static void SetTwitchUser(User value)
        {
            _currentConfig.TwitchCredentials.TwitchUser = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwOAuth(string value)
        {
            _currentConfig.TwitchCredentials.BotOAuthToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, _currentConfig.TwitchCredentials);
        }

        private static void SetTwRewardGoalRewardId(string value)
        {
            _currentConfig.AppConfig.TwRewardGoalRewardId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }
        private static void SetTwRewardId(string value)
        {
            _currentConfig.AppConfig.TwRewardId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwRewardSkipId(string value)
        {
            _currentConfig.AppConfig.TwRewardSkipId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);

        }
        private static void SetTwSrCommand(bool value)
        {
            _currentConfig.AppConfig.TwSrCommand = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrCooldown(int value)
        {
            _currentConfig.AppConfig.TwSrCooldown = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReq(int value)
        {
            _currentConfig.AppConfig.TwSrMaxReq = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqBroadcaster(int value)
        {
            _currentConfig.AppConfig.TwSrMaxReqBroadcaster = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqEveryone(int value)
        {
            _currentConfig.AppConfig.TwSrMaxReqEveryone = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqModerator(int value)
        {
            _currentConfig.AppConfig.TwSrMaxReqModerator = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqSubscriber(int value)
        {
            _currentConfig.AppConfig.TwSrMaxReqSubscriber = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqVip(int value)
        {
            _currentConfig.AppConfig.TwSrMaxReqVip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrReward(bool value)
        {
            _currentConfig.AppConfig.TwSrReward = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetTwSrUnlimitedSr(bool value)
        {
            _currentConfig.AppConfig.TwSrUnlimitedSr = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }
        private static void SetTwSrUserLevel(int value)
        {
            _currentConfig.AppConfig.TwSrUserLevel = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetUpdateRequired(bool value)
        {
            _currentConfig.AppConfig.UpdateRequired = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);

        }
        private static void SetUpload(bool value)
        {
            _currentConfig.AppConfig.Upload = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetUploadHistory(bool value)
        {
            _currentConfig.AppConfig.UploadHistory = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetUseOwnApp(bool value)
        {
            _currentConfig.AppConfig.UseOwnApp = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetUserBlacklist(List<string> value)
        {
            _currentConfig.AppConfig.UserBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetUuid(string value)
        {
            _currentConfig.AppConfig.Uuid = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }

        private static void SetWebServerPort(int value)
        {
            _currentConfig.AppConfig.WebServerPort = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, _currentConfig.AppConfig);
        }
    }
}