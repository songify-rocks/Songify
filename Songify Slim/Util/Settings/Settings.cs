using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using YamlDotNet.Core.Tokens;

namespace Songify_Slim.Util.Settings
{
    /// <summary>
    ///     This class is a getter and setter for Settings
    /// </summary>
    internal class Settings
    {
        private static Configuration currentConfig = new Configuration();

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

        public static bool SaveHistory
        {
            get => GetSaveHistory();
            set => SetSaveHistory(value);
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
        public static string BotCmdPosTrigger { get => GetCmdPosTrigger(); set => SetCmdPosTrigger(value); }
        public static string BotCmdSongTrigger { get => GetCmdSongTrigger(); set => SetCmdSongTrigger(value); }
        public static string BotCmdNextTrigger { get => GetCmdNextTrigger(); set => SetCmdNextTrigger(value); }

        private static void SetCmdNextTrigger(string value)
        {
            currentConfig.BotConfig.BotCmdNextTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static string GetCmdNextTrigger()
        {
            return currentConfig.BotConfig.BotCmdNextTrigger;
        }


        public static string BotCmdSsrTrigger { get => GetCmdSsrTrigger(); set => SetCmdSsrTrigger(value); }

        private static void SetCmdSsrTrigger(string value)
        {
            currentConfig.BotConfig.BotCmdSsrTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static string GetCmdSsrTrigger()
        {
            return currentConfig.BotConfig.BotCmdSsrTrigger;
        }

        private static void SetCmdSkipTrigger(string value)
        {
            currentConfig.BotConfig.BotCmdSkipTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static string GetCmdSkipTrigger()
        {
            return currentConfig.BotConfig.BotCmdSkipTrigger;
        }

        private static void SetCmdVoteskipTrigger(string value)
        {
            currentConfig.BotConfig.BotCmdVoteskipTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static string GetCmdVoteskipTrigger()
        {
            return currentConfig.BotConfig.BotCmdVoteskipTrigger;
        }



        public static string BotCmdSkipTrigger { get => GetCmdSkipTrigger(); set => SetCmdSkipTrigger(value); }
        public static string BotCmdVoteskipTrigger { get => GetCmdVoteskipTrigger(); set => SetCmdVoteskipTrigger(value); }
        public static bool TwSrUnlimitedSr { get => GetTwSrUnlimitedSr(); set => SetTwSrUnlimitedSr(value); }

        private static bool GetTwSrUnlimitedSr()
        {
            return currentConfig.AppConfig.TwSrUnlimitedSr;
        }

        private static void SetTwSrUnlimitedSr(bool value)
        {
            currentConfig.AppConfig.TwSrUnlimitedSr = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }


        private static void SetCmdSongTrigger(string value)
        {
            currentConfig.BotConfig.BotCmdSongTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static string GetCmdSongTrigger()
        {
            return currentConfig.BotConfig.BotCmdSongTrigger;
        }

        private static void SetCmdPosTrigger(string value)
        {
            currentConfig.BotConfig.BotCmdPosTrigger = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static string GetCmdPosTrigger()
        {
            return currentConfig.BotConfig.BotCmdPosTrigger;
        }

        public static Configuration Export()
        {
            SpotifyCredentials spotifyCredentials = new SpotifyCredentials
            {
                AccessToken = GetSpotifyAccessToken(),
                RefreshToken = GetSpotifyRefreshToken(),
                DeviceId = GetSpotifyDeviceId(),
                ClientId = GetClientId(),
                ClientSecret = GetClientSecret(),

            };

            TwitchCredentials twitchCredentials = new TwitchCredentials
            {
                AccessToken = GetTwitchAccessToken(),
                ChannelName = GetTwChannel(),
                ChannelId = GetTwitchChannelId(),
                BotAccountName = GetTwAcc(),
                BotOAuthToken = GetTwOAuth(),
                TwitchUser = GetTwitchUser(),

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
                BotCmdPosTrigger = GetCmdPosTrigger(),
                BotCmdSongTrigger = GetCmdSongTrigger(),
                BotCmdNextTrigger = GetCmdNextTrigger(),
                BotCmdSkipTrigger = GetCmdSkipTrigger(),
                BotCmdVoteskipTrigger = GetCmdVoteskipTrigger(),
                BotCmdSsrTrigger = GetCmdSsrTrigger(),
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
                BetaUpdates = GetBetaUpdates(),
                ChromeFetchRate = GetChromeFetchRate(),
                Player = GetSource(),
                WebUserAgent = GetWebua(),
                UpdateRequired = GetUpdateRequired(),
                BotOnlyWorkWhenLive = GetBotOnlyWorkWhenLive(),

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
            currentConfig = config;

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

        private static bool GetAnnounceInChat()
        {
            return currentConfig.AppConfig.AnnounceInChat;
        }

        private static bool GetAppendSpaces()
        {
            return currentConfig.AppConfig.AppendSpaces;
        }

        private static List<string> GetArtistBlacklist()
        {
            return currentConfig.AppConfig.ArtistBlacklist;
        }

        private static bool GetAutoClearQueue()
        {
            return currentConfig.AppConfig.AutoClearQueue;
        }

        private static bool GetAutostart()
        {
            return currentConfig.AppConfig.Autostart;
        }

        private static bool GetAutoStartWebServer()
        {
            return currentConfig.AppConfig.AutoStartWebServer;
        }

        private static bool GetBetaUpdates()
        {
            return currentConfig.AppConfig.BetaUpdates;
        }

        private static string GetBot_Resp_Blacklist()
        {
            return currentConfig.BotConfig.BotRespBlacklist;
        }

        private static string GetBot_Resp_Error()
        {
            return currentConfig.BotConfig.BotRespError;
        }

        private static string GetBot_Resp_IsInQueue()
        {
            return currentConfig.BotConfig.BotRespIsInQueue;
        }

        private static string GetBot_Resp_Length()
        {
            return currentConfig.BotConfig.BotRespLength;
        }

        private static string GetBot_Resp_MaxReq()
        {
            return currentConfig.BotConfig.BotRespMaxReq;
        }

        private static string GetBot_Resp_ModSkip()
        {
            return currentConfig.BotConfig.BotRespModSkip;
        }

        private static string GetBot_Resp_Next()
        {
            return currentConfig.BotConfig.BotRespNext;
        }

        private static string GetBot_Resp_NoSong()
        {
            return currentConfig.BotConfig.BotRespNoSong;
        }

        private static string GetBot_Resp_Pos()
        {
            return currentConfig.BotConfig.BotRespPos;
        }

        private static string GetBot_Resp_Success()
        {
            return currentConfig.BotConfig.BotRespSuccess;
        }

        private static string GetBot_Resp_VoteSkip()
        {

            return currentConfig.BotConfig.BotRespVoteSkip;
        }

        private static bool GetBotCmdNext()
        {
            return currentConfig.BotConfig.BotCmdNext;
        }

        private static bool GetBotCmdPos()
        {
            return currentConfig.BotConfig.BotCmdPos;
        }

        private static bool GetBotCmdSkip()
        {
            return currentConfig.BotConfig.BotCmdSkip;
        }

        private static bool GetBotCmdSkipVote()
        {
            return currentConfig.BotConfig.BotCmdSkipVote;
        }

        private static int GetBotCmdSkipVoteCount()
        {
            return currentConfig.BotConfig.BotCmdSkipVoteCount;
        }

        private static bool GetBotCmdSong()
        {
            return currentConfig.BotConfig.BotCmdSong;
        }

        private static bool GetBotOnlyWorkWhenLive()
        {
            return currentConfig.AppConfig.BotOnlyWorkWhenLive;
        }

        private static int GetChromeFetchRate()
        {
            return currentConfig.AppConfig.ChromeFetchRate;
        }

        private static string GetClientId()
        {
            return currentConfig.SpotifyCredentials.ClientId;
        }

        private static string GetClientSecret()
        {
            return currentConfig.SpotifyCredentials.ClientSecret;
        }

        private static string GetColor()
        {
            return currentConfig.AppConfig.Color;
        }

        private static string GetCustomPauseText()
        {
            return currentConfig.AppConfig.CustomPauseText;
        }

        private static bool GetCustomPauseTextEnabled()
        {
            return currentConfig.AppConfig.CustomPauseTextEnabled;
        }

        private static string GetDirectory()
        {
            return currentConfig.AppConfig.Directory;
        }

        private static bool GetDownloadCover()
        {
            return currentConfig.AppConfig.DownloadCover;
        }

        private static string GetLanguage()
        {
            return currentConfig.AppConfig.Language;
        }

        private static int GetMaxSongLength()
        {
            return currentConfig.AppConfig.MaxSongLength;
        }

        private static bool GetMsgLoggingEnabled()
        {
            return currentConfig.AppConfig.MsgLoggingEnabled;
        }

        private static bool GetOpenQueueOnStartup()
        {
            return currentConfig.AppConfig.OpenQueueOnStartup;
        }

        private static string GetOutputString()
        {
            return currentConfig.AppConfig.OutputString;
        }

        private static string GetOutputString2()
        {
            return currentConfig.AppConfig.OutputString2;
        }

        private static double GetPosX()
        {
            return currentConfig.AppConfig.PosX;
        }

        private static double GetPosY()
        {
            return currentConfig.AppConfig.PosY;
        }

        private static int[] GetRefundConditons()
        {
            return currentConfig.AppConfig.RefundConditons;
        }

        private static bool GetSaveHistory()
        {
            return currentConfig.AppConfig.SaveHistory;
        }

        private static int GetSource()
        {
            return currentConfig.AppConfig.Player;
        }

        private static int GetSpaceCount()
        {
            return currentConfig.AppConfig.SpaceCount;
        }

        private static bool GetSplitOutput()
        {
            return currentConfig.AppConfig.SplitOutput;
        }

        private static string GetSpotifyAccessToken()
        {
            return currentConfig.SpotifyCredentials.AccessToken;
        }

        private static string GetSpotifyDeviceId()
        {
            return currentConfig.SpotifyCredentials.DeviceId;
        }

        private static string GetSpotifyRefreshToken()
        {
            return currentConfig.SpotifyCredentials.RefreshToken;
        }

        private static bool GetSystray()
        {
            return currentConfig.AppConfig.Systray;
        }

        private static bool GetTelemetry()
        {
            return currentConfig.AppConfig.Telemetry;
        }

        private static string GetTheme()
        {
            return currentConfig.AppConfig.Theme;
        }

        private static string GetTwAcc()
        {
            return currentConfig.TwitchCredentials.BotAccountName;
        }

        private static bool GetTwAutoConnect()
        {
            return currentConfig.AppConfig.TwAutoConnect;
        }

        private static string GetTwChannel()
        {
            return currentConfig.TwitchCredentials.ChannelName;
        }

        private static string GetTwitchAccessToken()
        {
            return currentConfig.TwitchCredentials.AccessToken;

        }

        private static string GetTwitchChannelId()
        {
            return currentConfig.TwitchCredentials.ChannelId;

        }

        private static User GetTwitchUser()
        {
            return currentConfig.TwitchCredentials.TwitchUser;
        }

        private static string GetTwOAuth()
        {
            return currentConfig.TwitchCredentials.BotOAuthToken;
        }

        private static string GetTwRewardId()
        {
            return currentConfig.AppConfig.TwRewardId;
        }

        private static bool GetTwSrCommand()
        {
            return currentConfig.AppConfig.TwSrCommand;
        }

        private static int GetTwSrCooldown()
        {
            return currentConfig.AppConfig.TwSrCooldown;
        }

        private static int GetTwSrMaxReq()
        {
            return currentConfig.AppConfig.TwSrMaxReq;
        }

        private static int GetTwSrMaxReqBroadcaster()
        {
            return currentConfig.AppConfig.TwSrMaxReqBroadcaster;
        }

        private static int GetTwSrMaxReqEveryone()
        {
            return currentConfig.AppConfig.TwSrMaxReqEveryone;
        }

        private static int GetTwSrMaxReqModerator()
        {
            return currentConfig.AppConfig.TwSrMaxReqModerator;
        }

        private static int GetTwSrMaxReqSubscriber()
        {
            return currentConfig.AppConfig.TwSrMaxReqSubscriber;
        }

        private static int GetTwSrMaxReqVip()
        {
            return currentConfig.AppConfig.TwSrMaxReqVip;
        }

        private static bool GetTwSrReward()
        {
            return currentConfig.AppConfig.TwSrReward;
        }

        private static int GetTwSrUserLevel()
        {
            return currentConfig.AppConfig.TwSrUserLevel;
        }

        private static bool GetUpdateRequired()
        {
            return currentConfig.AppConfig.UpdateRequired;
        }

        private static bool GetUpload()
        {
            return currentConfig.AppConfig.Upload;
        }

        private static bool GetUploadHistory()
        {
            return currentConfig.AppConfig.UploadHistory;
        }

        private static bool GetUseOwnApp()
        {
            return currentConfig.AppConfig.UseOwnApp;
        }

        private static List<string> GetUserBlacklist()
        {
            return currentConfig.AppConfig.UserBlacklist;
        }

        private static string GetUuid()
        {
            return currentConfig.AppConfig.Uuid;
        }

        private static int GetWebServerPort()
        {
            return currentConfig.AppConfig.WebServerPort;
        }

        private static string GetWebua()
        {
            return "Songify Data Provider";
        }

        private static void SetAnnounceInChat(bool value)
        {
            currentConfig.AppConfig.AnnounceInChat = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetAppendSpaces(bool value)
        {
            currentConfig.AppConfig.AppendSpaces = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetArtistBlacklist(List<string> value)
        {
            currentConfig.AppConfig.ArtistBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetAutoClearQueue(bool value)
        {
            currentConfig.AppConfig.AutoClearQueue = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetAutostart(bool autostart)
        {
            currentConfig.AppConfig.Autostart = autostart;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetAutoStartWebServer(bool value)
        {
            currentConfig.AppConfig.AutoStartWebServer = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetBetaUpdates(bool value)
        {
            currentConfig.AppConfig.BetaUpdates = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetBot_Resp_Blacklist(string value)
        {
            currentConfig.BotConfig.BotRespBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Error(string value)
        {
            currentConfig.BotConfig.BotRespError = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_IsInQueue(string value)
        {
            currentConfig.BotConfig.BotRespIsInQueue = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Length(string value)
        {
            currentConfig.BotConfig.BotRespLength = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_MaxReq(string value)
        {
            currentConfig.BotConfig.BotRespMaxReq = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_ModSkip(string value)
        {
            currentConfig.BotConfig.BotRespModSkip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Next(string value)
        {
            currentConfig.BotConfig.BotRespNext = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_NoSong(string value)
        {
            currentConfig.BotConfig.BotRespNoSong = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Pos(string value)
        {
            currentConfig.BotConfig.BotRespPos = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_Success(string value)
        {
            currentConfig.BotConfig.BotRespSuccess = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBot_Resp_VoteSkip(string value)
        {
            currentConfig.BotConfig.BotRespVoteSkip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotCmdNext(bool value)
        {
            currentConfig.BotConfig.BotCmdNext = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotCmdPos(bool value)
        {
            currentConfig.BotConfig.BotCmdPos = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotCmdSkip(bool value)
        {
            currentConfig.BotConfig.BotCmdSkip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotCmdSkipVote(bool value)
        {
            currentConfig.BotConfig.BotCmdSkipVote = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotCmdSkipVoteCount(int value)
        {
            currentConfig.BotConfig.BotCmdSkipVoteCount = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotCmdSong(bool value)
        {
            currentConfig.BotConfig.BotCmdSong = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.BotConfig, currentConfig.BotConfig);
        }

        private static void SetBotOnlyWorkWhenLive(bool value)
        {
            currentConfig.AppConfig.BotOnlyWorkWhenLive = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }
        private static void SetChromeFetchRate(int value)
        {
            currentConfig.AppConfig.ChromeFetchRate = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetClientId(string value)
        {
            currentConfig.SpotifyCredentials.ClientId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, currentConfig.SpotifyCredentials);
        }

        private static void SetClientSecret(string value)
        {
            currentConfig.SpotifyCredentials.ClientSecret = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, currentConfig.SpotifyCredentials);
        }

        private static void SetColor(string value)
        {
            currentConfig.AppConfig.Color = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetCustomPauseText(string value)
        {
            currentConfig.AppConfig.CustomPauseText = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetCustomPauseTextEnabled(bool value)
        {
            currentConfig.AppConfig.CustomPauseTextEnabled = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetDirectory(string value)
        {
            currentConfig.AppConfig.Directory = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetDownloadCover(bool value)
        {
            currentConfig.AppConfig.DownloadCover = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetLangauge(string value)
        {
            currentConfig.AppConfig.Language = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetMaxSongLength(int value)
        {
            currentConfig.AppConfig.MaxSongLength = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetMsgLoggingEnabled(bool value)
        {
            currentConfig.AppConfig.MsgLoggingEnabled = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetOpenQueueOnStartup(bool value)
        {
            currentConfig.AppConfig.OpenQueueOnStartup = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetOutputString(string value)
        {
            currentConfig.AppConfig.OutputString = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetOutputString2(string value)
        {
            currentConfig.AppConfig.OutputString2 = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetPosX(double value)
        {
            currentConfig.AppConfig.PosX = (int)value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetPosY(double value)
        {
            currentConfig.AppConfig.PosY = (int)value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetRefundConditons(int[] value)
        {
            currentConfig.AppConfig.RefundConditons = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetSaveHistory(bool value)
        {
            currentConfig.AppConfig.SaveHistory = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetSource(int value)
        {
            currentConfig.AppConfig.Player = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetSpaceCount(int value)
        {
            currentConfig.AppConfig.SpaceCount = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetSplitOutput(bool value)
        {
            currentConfig.AppConfig.SplitOutput = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetSpotifyAccessToken(string value)
        {
            currentConfig.SpotifyCredentials.AccessToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, currentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyDeviceId(string value)
        {
            currentConfig.SpotifyCredentials.DeviceId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, currentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyRefreshToken(string value)
        {
            currentConfig.SpotifyCredentials.RefreshToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.SpotifyCredentials, currentConfig.SpotifyCredentials);
        }

        private static void SetSystray(bool value)
        {
            currentConfig.AppConfig.Systray = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTelemetry(bool value)
        {
            currentConfig.AppConfig.Systray = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTheme(string value)
        {
            currentConfig.AppConfig.Theme = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwAcc(string value)
        {
            currentConfig.TwitchCredentials.BotAccountName = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, currentConfig.TwitchCredentials);
        }

        private static void SetTwAutoConnect(bool value)
        {
            currentConfig.AppConfig.TwAutoConnect = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwChannel(string value)
        {
            currentConfig.TwitchCredentials.ChannelName = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, currentConfig.TwitchCredentials);
        }

        private static void SetTwitchAccessToken(string value)
        {
            currentConfig.TwitchCredentials.AccessToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, currentConfig.TwitchCredentials);
        }

        private static void SetTwitchChannelId(string value)
        {
            currentConfig.TwitchCredentials.ChannelId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, currentConfig.TwitchCredentials);
        }

        private static void SetTwitchUser(User value)
        {
            currentConfig.TwitchCredentials.TwitchUser = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, currentConfig.TwitchCredentials);
        }

        private static void SetTwOAuth(string value)
        {
            currentConfig.TwitchCredentials.BotOAuthToken = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.TwitchCredentials, currentConfig.TwitchCredentials);
        }

        private static void SetTwRewardId(string value)
        {
            currentConfig.AppConfig.TwRewardId = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrCommand(bool value)
        {
            currentConfig.AppConfig.TwSrCommand = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrCooldown(int value)
        {
            currentConfig.AppConfig.TwSrCooldown = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReq(int value)
        {
            currentConfig.AppConfig.TwSrMaxReq = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqBroadcaster(int value)
        {
            currentConfig.AppConfig.TwSrMaxReqBroadcaster = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqEveryone(int value)
        {
            currentConfig.AppConfig.TwSrMaxReqEveryone = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqModerator(int value)
        {
            currentConfig.AppConfig.TwSrMaxReqModerator = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqSubscriber(int value)
        {
            currentConfig.AppConfig.TwSrMaxReqModerator = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqVip(int value)
        {
            currentConfig.AppConfig.TwSrMaxReqVip = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrReward(bool value)
        {
            currentConfig.AppConfig.TwSrReward = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetTwSrUserLevel(int value)
        {
            currentConfig.AppConfig.TwSrUserLevel = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetUpdateRequired(bool value)
        {
            currentConfig.AppConfig.UpdateRequired = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);

        }
        private static void SetUpload(bool value)
        {
            currentConfig.AppConfig.Upload = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetUploadHistory(bool value)
        {
            currentConfig.AppConfig.UploadHistory = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetUseOwnApp(bool value)
        {
            currentConfig.AppConfig.UseOwnApp = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetUserBlacklist(List<string> value)
        {
            currentConfig.AppConfig.UserBlacklist = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetUuid(string value)
        {
            currentConfig.AppConfig.Uuid = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }

        private static void SetWebServerPort(int value)
        {
            currentConfig.AppConfig.WebServerPort = value;
            ConfigHandler.WriteConfig(ConfigHandler.ConfigTypes.AppConfig, currentConfig.AppConfig);
        }
    }
}