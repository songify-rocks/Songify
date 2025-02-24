using System;
using System.Collections;
using Songify_Slim.Views;
using System.Collections.Generic;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using Songify_Slim.Models;
using Songify_Slim.Util.Songify;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Songify_Slim.UserControls;

namespace Songify_Slim.Util.Settings
{
    /// <summary>
    ///     This class is a getter and setter for Settings
    /// </summary>
    internal class Settings
    {
        public static Configuration CurrentConfig = new();

        public static string AccessKey
        {
            get => GetAccessKey();
            set => SetAccessKey(value);
        }

        public static bool AddSrToPlaylist { get => GetAddSrToPlaylist(); set => SetAddSrToPlaylist(value); }

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

        public static bool AppendSpacesSplitFiles { get => GetAppendSpacesSplitFiles(); set => SetAppendSpacesSplitFiles(value); }

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

        public static string BaseUrl { get => GetBaseUrl(); set => SetBaseUrl(value); }

        public static bool BetaUpdates
        {
            get => GetBetaUpdates();
            set => SetBetaUpdates(value);
        }

        public static bool BlockAllExplicitSongs { get => GetBlockAllExplicitSongs(); set => SetBlockAllExplicitSongs(value); }

        public static bool BotCmdCommands { get => GetBotCmdCommands(); set => SetBotCmdCommands(value); }

        public static bool BotCmdNext
        {
            get => GetBotCmdNext();
            set => SetBotCmdNext(value);
        }

        public static string BotCmdNextTrigger { get => GetCmdNextTrigger(); set => SetCmdNextTrigger(value); }

        public static bool BotCmdPlayPause { get => GetBotCmdPlayPause(); set => SetBotCmdPlayPause(value); }

        public static bool BotCmdPos
        {
            get => GetBotCmdPos();
            set => SetBotCmdPos(value);
        }

        public static string BotCmdPosTrigger { get => GetCmdPosTrigger(); set => SetCmdPosTrigger(value); }

        public static bool BotCmdQueue { get => GetBotCmdQueue(); set => SetBotCmdQueue(value); }

        public static string BotCmdQueueTrigger { get => GetBotCmdQueueTrigger(); set => SetBotCmdQueueTrigger(value); }

        public static bool BotCmdRemove { get => GetBotCmdRemove(); set => SetBotCmdRemove(value); }

        public static string BotCmdRemoveTrigger { get => GetCmdRemoveTrigger(); set => SetCmdRemoveTrigger(value); }

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

        public static bool BotCmdSonglike { get => GetBotCmdSonglike(); set => SetBotCmdSonglike(value); }

        public static string BotCmdSonglikeTrigger { get => GetCmdSonglikeTrigger(); set => SetCmdSonglikeTrigger(value); }

        public static string BotCmdSongTrigger { get => GetCmdSongTrigger(); set => SetCmdSongTrigger(value); }

        public static string BotCmdSsrTrigger { get => GetCmdSsrTrigger(); set => SetCmdSsrTrigger(value); }

        public static bool BotCmdVol { get => GetBotCmdVol(); set => SetBotCmdVol(value); }

        public static bool BotCmdVolIgnoreMod
        {
            get => GetBotCmdVolIgnoreMod(); set => SetBotCmdVolIgnoreMod(value);
        }

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

        public static string BotRespCooldown { get => GetBot_Resp_Cooldown(); set => SetBot_Resp_Cooldown(value); }

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

        public static string BotRespNoTrackFound { get => GetBot_Resp_NoTrackFound(); set => SetBot_Resp_NoTrackFound(value); }

        public static string BotRespPlaylist
        {
            get => GetBot_Resp_Playlist();
            set => SetBot_Resp_Playlist(value);
        }

        public static string BotRespPos
        {
            get => GetBot_Resp_Pos();
            set => SetBot_Resp_Pos(value);
        }

        public static string BotRespRefund { get => GetBot_Resp_Refund(); set => SetBot_Resp_Refund(value); }

        public static string BotRespRemove { get => GetBot_Resp_Remove(); set => SetBot_Resp_Remove(value); }

        public static string BotRespSong { get => GetBot_resp_Song(); set => SetBot_Resp_Song(value); }

        public static string BotRespSongLike { get => GetBot_Resp_SongLike(); set => SetBot_Resp_SongLike(value); }

        public static string BotRespSuccess
        {
            get => GetBot_Resp_Success();
            set => SetBot_Resp_Success(value);
        }

        public static string BotRespTrackExplicit { get => GetBot_Resp_ExplicitSong(); set => SetBot_Resp_ExplicitSong(value); }

        public static string BotRespUnavailable { get => GetBot_Resp_SongUnavailable(); set => SetBot_Resp_SongUnavailable(value); }

        public static string BotRespUserCooldown { get => GetBotRespUserCooldown(); set => SetBotRespUserCooldown(value); }

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

        public static bool DonationReminder { get => GetDonationReminder(); set => SetDonationReminder(value); }

        public static bool DownloadCanvas { get => GetDownloadCanvas(); set => SetDownloadCanvas(value); }

        public static bool DownloadCover
        {
            get => GetDownloadCover();
            set => SetDownloadCover(value);
        }

        public static int Fontsize { get => GetFontSize(); set => SetFontSize(value); }

        public static int FontsizeQueue { get => GetFontSizeQueue(); set => SetFontSizeQueue(value); }

        public static bool IsLive { get; set; }

        public static bool KeepAlbumCover { get => GetKeepAlubmCover(); set => SetKeepAlbumCover(value); }

        public static string Language
        {
            get => GetLanguage();

            set => SetLanguage(value);
        }

        public static int LastShownMotdId { get => GetLastShownMotdId(); set => SetLastShownMotdId(value); }

        public static bool LimitSrToPlaylist
        {
            get => GetLimitSrToPlaylist();
            set => SetLimitSrToPlaylist(value);
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

        public static Enums.PauseOptions PauseOption
        {
            get => GetPauseOption();
            set => SetPauseOption(value);
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

        public static List<int> QueueWindowColumns { get => GetQueueWindowColumns(); set => SetQueueWindowColumns(value); }
        public static List<int> ReadNotificationIds { get => GetReadNotificationIds(); set => SetReadNotificationIds(value); }

        public static int[] RefundConditons
        {
            get => GetRefundConditons();
            set => SetRefundConditons(value);
        }

        public static string RequesterPrefix { get => GetRequesterPrefix(); set => SetRequesterPrefix(value); }

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

        public static bool SpotifyControlVisible
        {
            get => GetSpotifyControlVisible(); set => SetSpotifyControlVisible(value);
        }

        public static string SpotifyDeviceId
        {
            get => GetSpotifyDeviceId();
            set => SetSpotifyDeviceId(value);
        }

        public static List<SimplePlaylist> SpotifyPlaylistCache { get => GetSpotifyPlaylistCache(); set => SetSpotifyPlaylistCache(value); }

        public static string SpotifyPlaylistId { get => GetSpotifyPlaylistId(); set => SetSpotifyPlaylistId(value); }

        public static PrivateProfile SpotifyProfile { get => GetSpotifyProfile(); set => SetSpotifyProfile(value); }

        public static string SpotifyRefreshToken
        {
            get => GetSpotifyRefreshToken();
            set => SetSpotifyRefreshToken(value);
        }

        public static string SpotifySongLimitPlaylist { get => GetSpotifySongLimitPlaylist(); set => SetSpotifySongLimitPlaylist(value); }

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

        public static List<string> TwRewardId
        {
            get => GetTwRewardId();
            set => SetTwRewardId(value);
        }

        public static List<string> TwRewardSkipId { get => GetTwRewardSkipId(); set => SetTwRewardSkipId(value); }

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

        public static int TwSrMaxReqFollower
        {
            get => GetTwSrMaxReqFollower();
            set => SetTwSrMaxReqFollower(value);
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

        public static int TwSrMaxReqSubscriberT2 { get => GetTwSrMaxReqSubscriberT2(); set => SetTwSrMaxReqSubscriberT2(value); }

        public static int TwSrMaxReqSubscriberT3 { get => GetTwSrMaxReqSubscriberT3(); set => SetTwSrMaxReqSubscriberT3(value); }

        public static int TwSrMaxReqVip
        {
            get => GetTwSrMaxReqVip();
            set => SetTwSrMaxReqVip(value);
        }

        public static int TwSrPerUserCooldown
        {
            get => GetTwSrPerUserCooldown(); set => SetTwSrPerUserCooldown(value);
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

        public static bool UseDefaultBrowser { get => GetUseDefaultBrowser(); set => SetUseDefaultBrowser(value); }

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

        public static List<int> UserLevelsCommand
        {
            get => GetUserLevelsCommand();
            set => SetUserLevelsCommand(value);
        }

        public static List<int> UserLevelsReward
        {
            get => GetUserLevelsReward();
            set => SetUserLevelsReward(value);
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
        public static string YtmdToken { get => GetYtmdToken(); set => SetYtmdToken(value); }
        public static string BotCmdCommandsTrigger { get => GetBotCmdCommandsTrigger(); set => SetBotCmdCommandsTrigger(value); }
        public static string BotRespUserlevelTooLowCommand { get => GetBotRespUserlevelTooLowCommand(); set => SetBotRespUserlevelTooLowCommand(value); }
        public static bool ShowUserLevelBadges { get => GetShowUserLevelBadges(); set => SetShowUserLevelBadges(value); }

        public static List<TwitchCommand> Commands
        {
            get => CurrentConfig.TwitchCommands.Commands;
            set => CurrentConfig.TwitchCommands.Commands = value;
        }

        public static string TwitchUserColor { get => GetTwitchUserColor(); set => SetTwitchUserColor(value); }
        public static List<int> UnlimitedSrUserlevelsReward { get => GetUnlimitedSrUserlevelsReward(); set => SetUnlimitedSrUserlevelsReward(value); }

        private static void SetUnlimitedSrUserlevelsReward(List<int> value)
        {
            CurrentConfig.AppConfig.UnlimitedSrUserlevelsReward = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static List<int> GetUnlimitedSrUserlevelsReward()
        {
            return CurrentConfig.AppConfig.UnlimitedSrUserlevelsReward;
        }

        public static List<int> UnlimitedSrUserlevelsCommand { get => GetUnlimitedSrUserlevelsCommand(); set => SetUnlimitedSrUserlevelsCommand(value); }

        private static void SetUnlimitedSrUserlevelsCommand(List<int> value)
        {
            CurrentConfig.AppConfig.UnlimitedSrUserlevelsCommand = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static List<int> GetUnlimitedSrUserlevelsCommand()
        {
            return CurrentConfig.AppConfig.UnlimitedSrUserlevelsCommand;
        }

        private static string GetTwitchUserColor()
        {
            return CurrentConfig.TwitchCredentials.TwitchUserColor;
        }

        private static void SetTwitchUserColor(string value)
        {
            CurrentConfig.TwitchCredentials.TwitchUserColor = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static void SetShowUserLevelBadges(bool value)
        {
            CurrentConfig.AppConfig.ShowUserLevelBadges = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static bool GetShowUserLevelBadges()
        {
            return CurrentConfig.AppConfig.ShowUserLevelBadges;
        }

        private static void SetBotRespUserlevelTooLowCommand(string value)
        {
            CurrentConfig.BotConfig.BotRespUserLevelTooLowCommand = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static string GetBotRespUserlevelTooLowCommand()
        {
            return CurrentConfig.BotConfig.BotRespUserLevelTooLowCommand;
        }

        public static string BotRespUserlevelTooLowReward { get => GetBotRespUserlevelTooLowReward(); set => SetBotRespUserlevelTooLowReward(value); }

        private static void SetBotRespUserlevelTooLowReward(string value)
        {
            CurrentConfig.BotConfig.BotRespUserLevelTooLowReward = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static string GetBotRespUserlevelTooLowReward()
        {
            return CurrentConfig.BotConfig.BotRespUserLevelTooLowReward;
        }

        private static void SetBotCmdCommandsTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdCommandsTrigger = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static string GetBotCmdCommandsTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdCommandsTrigger;
        }

        public static Configuration Export()
        {
            SpotifyCredentials spotifyCredentials = new()
            {
                AccessToken = GetSpotifyAccessToken(),
                ClientId = GetClientId(),
                ClientSecret = GetClientSecret(),
                DeviceId = GetSpotifyDeviceId(),
                RefreshToken = GetSpotifyRefreshToken(),
                Profile = GetSpotifyProfile(),
                PlaylistCache = GetSpotifyPlaylistCache()
            };

            TwitchCredentials twitchCredentials = new()
            {
                AccessToken = GetTwitchAccessToken(),
                BotAccountName = GetTwAcc(),
                BotOAuthToken = GetTwOAuth(),
                BotUser = GetTwitchBotUser(),
                ChannelId = GetTwitchChannelId(),
                ChannelName = GetTwChannel(),
                TwitchBotToken = GetTwitchBotToken(),
                TwitchUser = GetTwitchUser(),
                TwitchUserColor = GetTwitchUserColor(),
            };

            BotConfig botConfig = new()
            {
                BotCmdCommands = GetBotCmdCommands(),
                BotCmdCommandsTrigger = GetBotCmdCommandsTrigger(),
                BotCmdNext = GetBotCmdNext(),
                BotCmdNextTrigger = GetCmdNextTrigger(),
                BotCmdPlayPause = GetBotCmdPlayPause(),
                BotCmdPlayPauseTrigger = "!play, !pause",
                BotCmdPos = GetBotCmdPos(),
                BotCmdPosTrigger = GetCmdPosTrigger(),
                BotCmdQueue = GetBotCmdQueue(),
                BotCmdQueueTrigger = GetBotCmdQueueTrigger(),
                BotCmdRemove = GetBotCmdRemove(),
                BotCmdRemoveTrigger = GetCmdRemoveTrigger(),
                BotCmdSkip = GetBotCmdSkip(),
                BotCmdSkipTrigger = GetCmdSkipTrigger(),
                BotCmdSkipVote = GetBotCmdSkipVote(),
                BotCmdSkipVoteCount = GetBotCmdSkipVoteCount(),
                BotCmdSkipVoteTrigger = "!voteskip",
                BotCmdSong = GetBotCmdSong(),
                BotCmdSonglike = GetBotCmdSonglike(),
                BotCmdSonglikeTrigger = GetCmdSonglikeTrigger(),
                BotCmdSongTrigger = GetCmdSongTrigger(),
                BotCmdSsrTrigger = GetCmdSsrTrigger(),
                BotCmdVol = GetBotCmdVol(),
                BotCmdVolIgnoreMod = GetBotCmdVolIgnoreMod(),
                BotCmdVolTrigger = "!vol",
                BotCmdVoteskipTrigger = GetCmdVoteskipTrigger(),
                BotRespBlacklist = GetBot_Resp_Blacklist(),
                BotRespCooldown = GetBot_Resp_Cooldown(),
                BotRespError = GetBot_Resp_Error(),
                BotRespExplicitSong = GetBot_Resp_ExplicitSong(),
                BotRespIsInQueue = GetBot_Resp_IsInQueue(),
                BotRespLength = GetBot_Resp_Length(),
                BotRespMaxReq = GetBot_Resp_MaxReq(),
                BotRespModSkip = GetBot_Resp_ModSkip(),
                BotRespNext = GetBot_Resp_Next(),
                BotRespNoSong = GetBot_Resp_NoSong(),
                BotRespNoTrackFound = GetBot_Resp_NoTrackFound(),
                BotRespPlaylist = GetBot_Resp_Playlist(),
                BotRespPos = GetBot_Resp_Pos(),
                BotRespRefund = GetBot_Resp_Refund(),
                BotRespRemove = GetBot_Resp_Remove(),
                BotRespSong = GetBot_resp_Song(),
                BotRespSongLike = GetBot_Resp_SongLike(),
                BotRespSuccess = GetBot_Resp_Success(),
                BotRespUnavailable = GetBot_Resp_SongUnavailable(),
                BotRespUserCooldown = GetBotRespUserCooldown(),
                BotRespUserLevelTooLowCommand = GetBotRespUserlevelTooLowCommand(),
                BotRespUserLevelTooLowReward = GetBotRespUserlevelTooLowReward(),
                BotRespVoteSkip = GetBot_Resp_VoteSkip(),
                ChatLiveStatus = GetChatLiveStatus(),
                OnlyWorkWhenLive = GetBotOnlyWorkWhenLive(),
            };

            AppConfig appConfig = new()
            {
                AccessKey = GetAccessKey(),
                AddSrToPlaylist = GetAddSrToPlaylist(),
                AnnounceInChat = GetAnnounceInChat(),
                AppendSpaces = GetAppendSpaces(),
                AppendSpacesSplitFiles = GetAppendSpacesSplitFiles(),
                ArtistBlacklist = GetArtistBlacklist(),
                AutoClearQueue = GetAutoClearQueue(),
                Autostart = GetAutostart(),
                AutoStartWebServer = GetAutoStartWebServer(),
                BaseUrl = GetBaseUrl(),
                BetaUpdates = GetBetaUpdates(),
                BlockAllExplicitSongs = GetBlockAllExplicitSongs(),
                BotOnlyWorkWhenLive = GetBotOnlyWorkWhenLive(),
                ChromeFetchRate = GetChromeFetchRate(),
                Color = GetColor(),
                CustomPauseText = GetCustomPauseText(),
                CustomPauseTextEnabled = GetCustomPauseTextEnabled(),
                Directory = GetDirectory(),
                DonationReminder = GetDonationReminder(),
                DownloadCanvas = GetDownloadCanvas(),
                DownloadCover = GetDownloadCover(),
                FontSize = GetFontSize(),
                FontsizeQueue = GetFontSizeQueue(),
                KeepAlbumCover = GetKeepAlubmCover(),
                Language = GetLanguage(),
                LastShownMotdId = GetLastShownMotdId(),
                LimitSrToPlaylist = GetLimitSrToPlaylist(),
                MaxSongLength = GetMaxSongLength(),
                MsgLoggingEnabled = GetMsgLoggingEnabled(),
                OpenQueueOnStartup = GetOpenQueueOnStartup(),
                OutputString = GetOutputString(),
                OutputString2 = GetOutputString2(),
                PauseOption = GetPauseOption(),
                Player = GetSource(),
                PosX = (int)GetPosX(),
                PosY = (int)GetPosY(),
                QueueWindowColumns = GetQueueWindowColumns(),
                ReadNotificationIds = GetReadNotificationIds(),
                RefundConditons = GetRefundConditons(),
                RequesterPrefix = GetRequesterPrefix(),
                RewardGoalAmount = GetRewardGoalAmount(),
                RewardGoalEnabled = GetRewardGoalEnabled(),
                RewardGoalSong = GetRewardGoalSong(),
                SaveHistory = GetSaveHistory(),
                ShowUserLevelBadges = GetShowUserLevelBadges(),
                UnlimitedSrUserlevelsReward = GetUnlimitedSrUserlevelsReward(),
                UnlimitedSrUserlevelsCommand = GetUnlimitedSrUserlevelsCommand(),
                SongBlacklist = GetSongBlacklist(),
                SpaceCount = GetSpaceCount(),
                SplitOutput = GetSplitOutput(),
                SpotifyControlVisible = GetSpotifyControlVisible(),
                SpotifyPlaylistId = GetSpotifyPlaylistId(),
                SpotifySongLimitPlaylist = GetSpotifySongLimitPlaylist(),
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
                TwSrMaxReqFollower = GetTwSrMaxReqFollower(),
                TwSrMaxReqModerator = GetTwSrMaxReqModerator(),
                TwSrMaxReqSubscriber = GetTwSrMaxReqSubscriber(),
                TwSrMaxReqSubscriberT2 = GetTwSrMaxReqSubscriberT2(),
                TwSrMaxReqSubscriberT3 = GetTwSrMaxReqSubscriberT3(),
                TwSrMaxReqVip = GetTwSrMaxReqVip(),
                TwSrPerUserCooldown = GetTwSrPerUserCooldown(),
                TwSrReward = GetTwSrReward(),
                TwSrUnlimitedSr = GetTwSrUnlimitedSr(),
                TwSrUserLevel = GetTwSrUserLevel(),
                UpdateRequired = GetUpdateRequired(),
                Upload = GetUpload(),
                UploadHistory = GetUploadHistory(),
                UseDefaultBrowser = GetUseDefaultBrowser(),
                UseOwnApp = GetUseOwnApp(),
                UserBlacklist = GetUserBlacklist(),
                UserLevelsCommand = GetUserLevelsCommand(),
                UserLevelsReward = GetUserLevelsReward(),
                Uuid = GetUuid(),
                WebServerPort = GetWebServerPort(),
                WebUserAgent = GetWebua(),
                YtmdToken = GetYtmdToken(),
            };

            TwitchCommands twitchCommands = new()
            {
                Commands = CurrentConfig.TwitchCommands.Commands
            };

            return new Configuration
            {
                AppConfig = appConfig,
                SpotifyCredentials = spotifyCredentials,
                TwitchCredentials = twitchCredentials,
                BotConfig = botConfig,
                TwitchCommands = twitchCommands
            };
        }

        public static void Import(Configuration config)
        {
            CurrentConfig = config;

            ConfigHandler.WriteAllConfig(config);
        }

        public static void ResetConfig()
        {
            CurrentConfig = new Configuration();
        }

        private static string GetAccessKey()
        {
            return CurrentConfig.AppConfig.AccessKey;
        }

        private static bool GetAddSrToPlaylist()
        {
            return CurrentConfig.AppConfig.AddSrToPlaylist;
        }

        private static bool GetAnnounceInChat()
        {
            return CurrentConfig.AppConfig.AnnounceInChat;
        }

        private static bool GetAppendSpaces()
        {
            return CurrentConfig.AppConfig.AppendSpaces;
        }

        private static bool GetAppendSpacesSplitFiles()
        {
            return CurrentConfig.AppConfig.AppendSpacesSplitFiles;
        }

        private static List<string> GetArtistBlacklist()
        {
            return CurrentConfig.AppConfig.ArtistBlacklist;
        }

        private static bool GetAutoClearQueue()
        {
            return CurrentConfig.AppConfig.AutoClearQueue;
        }

        private static bool GetAutostart()
        {
            return CurrentConfig.AppConfig.Autostart;
        }

        private static bool GetAutoStartWebServer()
        {
            return CurrentConfig.AppConfig.AutoStartWebServer;
        }

        private static string GetBaseUrl()
        {
            return CurrentConfig.AppConfig.BaseUrl;
        }

        private static bool GetBetaUpdates()
        {
            return CurrentConfig.AppConfig.BetaUpdates;
        }

        private static bool GetBlockAllExplicitSongs()
        {
            return CurrentConfig.AppConfig.BlockAllExplicitSongs;
        }

        private static string GetBot_Resp_Blacklist()
        {
            return CurrentConfig.BotConfig.BotRespBlacklist;
        }

        private static string GetBot_Resp_Cooldown()
        {
            return CurrentConfig.BotConfig.BotRespCooldown;
        }

        private static string GetBot_Resp_Error()
        {
            return CurrentConfig.BotConfig.BotRespError;
        }

        private static string GetBot_Resp_ExplicitSong()
        {
            return CurrentConfig.BotConfig.BotRespExplicitSong;
        }

        private static string GetBot_Resp_IsInQueue()
        {
            return CurrentConfig.BotConfig.BotRespIsInQueue;
        }

        private static string GetBot_Resp_Length()
        {
            return CurrentConfig.BotConfig.BotRespLength;
        }

        private static string GetBot_Resp_MaxReq()
        {
            return CurrentConfig.BotConfig.BotRespMaxReq;
        }

        private static string GetBot_Resp_ModSkip()
        {
            return CurrentConfig.BotConfig.BotRespModSkip;
        }

        private static string GetBot_Resp_Next()
        {
            return CurrentConfig.BotConfig.BotRespNext;
        }

        private static string GetBot_Resp_NoSong()
        {
            return CurrentConfig.BotConfig.BotRespNoSong;
        }

        private static string GetBot_Resp_NoTrackFound()
        {
            return CurrentConfig.BotConfig.BotRespNoTrackFound;
        }

        private static string GetBot_Resp_Playlist()
        {
            return CurrentConfig.BotConfig.BotRespPlaylist;
        }

        private static string GetBot_Resp_Pos()
        {
            return CurrentConfig.BotConfig.BotRespPos;
        }

        private static string GetBot_Resp_Refund()
        {
            return CurrentConfig.BotConfig.BotRespRefund;
        }

        private static string GetBot_Resp_Remove()
        {
            return CurrentConfig.BotConfig.BotRespRemove;
        }

        private static string GetBot_resp_Song()
        {
            return CurrentConfig.BotConfig.BotRespSong;
        }

        private static string GetBot_Resp_SongLike()
        {
            return CurrentConfig.BotConfig.BotRespSongLike;
        }

        private static string GetBot_Resp_SongUnavailable()
        {
            return CurrentConfig.BotConfig.BotRespUnavailable;
        }

        private static string GetBot_Resp_Success()
        {
            return CurrentConfig.BotConfig.BotRespSuccess;
        }

        private static string GetBot_Resp_VoteSkip()
        {
            return CurrentConfig.BotConfig.BotRespVoteSkip;
        }

        private static bool GetBotCmdCommands()
        {
            return CurrentConfig.BotConfig.BotCmdCommands;
        }

        private static bool GetBotCmdNext()
        {
            return CurrentConfig.BotConfig.BotCmdNext;
        }

        private static bool GetBotCmdPlayPause()
        {
            return CurrentConfig.BotConfig.BotCmdPlayPause;
        }

        private static bool GetBotCmdPos()
        {
            return CurrentConfig.BotConfig.BotCmdPos;
        }

        private static bool GetBotCmdQueue()
        {
            return CurrentConfig.BotConfig.BotCmdQueue;
        }

        private static string GetBotCmdQueueTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdQueueTrigger;
        }

        private static bool GetBotCmdRemove()
        {
            return CurrentConfig.BotConfig.BotCmdRemove;
        }

        private static bool GetBotCmdSkip()
        {
            return CurrentConfig.BotConfig.BotCmdSkip;
        }

        private static bool GetBotCmdSkipVote()
        {
            return CurrentConfig.BotConfig.BotCmdSkipVote;
        }

        private static int GetBotCmdSkipVoteCount()
        {
            return CurrentConfig.BotConfig.BotCmdSkipVoteCount;
        }

        private static bool GetBotCmdSong()
        {
            return CurrentConfig.BotConfig.BotCmdSong;
        }

        private static bool GetBotCmdSonglike()
        {
            return CurrentConfig.BotConfig.BotCmdSonglike;
        }

        private static bool GetBotCmdVol()
        {
            return CurrentConfig.BotConfig.BotCmdVol;
        }

        private static bool GetBotCmdVolIgnoreMod()
        {
            return CurrentConfig.BotConfig.BotCmdVolIgnoreMod;
        }

        private static bool GetBotOnlyWorkWhenLive()
        {
            return CurrentConfig.AppConfig.BotOnlyWorkWhenLive;
        }

        private static string GetBotRespUserCooldown()
        {
            return CurrentConfig.BotConfig.BotRespUserCooldown;
        }

        private static bool GetChatLiveStatus()
        {
            return CurrentConfig.BotConfig.ChatLiveStatus;
        }

        private static int GetChromeFetchRate()
        {
            return CurrentConfig.AppConfig.ChromeFetchRate;
        }

        private static string GetClientId()
        {
            return CurrentConfig.SpotifyCredentials.ClientId;
        }

        private static string GetClientSecret()
        {
            return CurrentConfig.SpotifyCredentials.ClientSecret;
        }

        private static string GetCmdNextTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdNextTrigger;
        }

        private static string GetCmdPosTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdPosTrigger;
        }

        private static string GetCmdRemoveTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdRemoveTrigger;
        }

        private static string GetCmdSkipTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdSkipTrigger;
        }

        private static string GetCmdSonglikeTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdSonglikeTrigger;
        }

        private static string GetCmdSongTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdSongTrigger;
        }

        private static string GetCmdSsrTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdSsrTrigger;
        }

        private static string GetCmdVoteskipTrigger()
        {
            return CurrentConfig.BotConfig.BotCmdVoteskipTrigger;
        }

        private static string GetColor()
        {
            return CurrentConfig.AppConfig.Color;
        }

        private static string GetCustomPauseText()
        {
            return CurrentConfig.AppConfig.CustomPauseText;
        }

        private static bool GetCustomPauseTextEnabled()
        {
            return CurrentConfig.AppConfig.CustomPauseTextEnabled;
        }

        private static string GetDirectory()
        {
            return CurrentConfig.AppConfig.Directory;
        }

        private static bool GetDonationReminder()
        {
            return CurrentConfig.AppConfig.DonationReminder;
        }

        private static bool GetDownloadCanvas()
        {
            return CurrentConfig.AppConfig.DownloadCanvas;
        }

        private static bool GetDownloadCover()
        {
            return CurrentConfig.AppConfig.DownloadCover;
        }

        private static int GetFontSize()
        {
            return CurrentConfig.AppConfig.FontSize;
        }

        private static int GetFontSizeQueue()
        {
            return CurrentConfig.AppConfig.FontsizeQueue;
        }

        private static bool GetKeepAlubmCover()
        {
            return CurrentConfig.AppConfig.KeepAlbumCover;
        }

        private static string GetLanguage()
        {
            return CurrentConfig.AppConfig.Language;
        }

        private static int GetLastShownMotdId()
        {
            return CurrentConfig.AppConfig.LastShownMotdId;
        }

        private static bool GetLimitSrToPlaylist()
        {
            return CurrentConfig.AppConfig.LimitSrToPlaylist;
        }

        private static int GetMaxSongLength()
        {
            return CurrentConfig.AppConfig.MaxSongLength;
        }

        private static bool GetMsgLoggingEnabled()
        {
            return CurrentConfig.AppConfig.MsgLoggingEnabled;
        }

        private static bool GetOpenQueueOnStartup()
        {
            return CurrentConfig.AppConfig.OpenQueueOnStartup;
        }

        private static string GetOutputString()
        {
            return CurrentConfig.AppConfig.OutputString;
        }

        private static string GetOutputString2()
        {
            return CurrentConfig.AppConfig.OutputString2;
        }

        private static Enums.PauseOptions GetPauseOption()
        {
            return CurrentConfig.AppConfig.PauseOption;
        }

        private static double GetPosX()
        {
            return CurrentConfig.AppConfig.PosX;
        }

        private static double GetPosY()
        {
            return CurrentConfig.AppConfig.PosY;
        }

        private static List<int> GetQueueWindowColumns()
        {
            return CurrentConfig.AppConfig.QueueWindowColumns;
        }

        private static List<int> GetReadNotificationIds()
        {
            return CurrentConfig.AppConfig.ReadNotificationIds;
        }

        private static int[] GetRefundConditons()
        {
            return CurrentConfig.AppConfig.RefundConditons;
        }

        private static string GetRequesterPrefix()
        {
            return CurrentConfig.AppConfig.RequesterPrefix;
        }

        private static int GetRewardGoalAmount()
        {
            return CurrentConfig.AppConfig.RewardGoalAmount;
        }

        private static bool GetRewardGoalEnabled()
        {
            return CurrentConfig.AppConfig.RewardGoalEnabled;
        }

        private static string GetRewardGoalSong()
        {
            return CurrentConfig.AppConfig.RewardGoalSong;
        }

        private static bool GetSaveHistory()
        {
            return CurrentConfig.AppConfig.SaveHistory;
        }

        private static List<TrackItem> GetSongBlacklist()
        {
            return CurrentConfig.AppConfig.SongBlacklist;
        }

        private static int GetSource()
        {
            return CurrentConfig.AppConfig.Player;
        }

        private static int GetSpaceCount()
        {
            return CurrentConfig.AppConfig.SpaceCount;
        }

        private static bool GetSplitOutput()
        {
            return CurrentConfig.AppConfig.SplitOutput;
        }

        private static string GetSpotifyAccessToken()
        {
            return CurrentConfig.SpotifyCredentials.AccessToken;
        }

        private static bool GetSpotifyControlVisible()
        {
            return CurrentConfig.AppConfig.SpotifyControlVisible;
        }

        private static string GetSpotifyDeviceId()
        {
            return CurrentConfig.SpotifyCredentials.DeviceId;
        }

        private static List<SimplePlaylist> GetSpotifyPlaylistCache()
        {
            return CurrentConfig.SpotifyCredentials.PlaylistCache;
        }

        private static string GetSpotifyPlaylistId()
        {
            return CurrentConfig.AppConfig.SpotifyPlaylistId;
        }

        private static PrivateProfile GetSpotifyProfile()
        {
            return CurrentConfig.SpotifyCredentials.Profile;
        }

        private static string GetSpotifyRefreshToken()
        {
            return CurrentConfig.SpotifyCredentials.RefreshToken;
        }

        private static string GetSpotifySongLimitPlaylist()
        {
            return CurrentConfig.AppConfig.SpotifySongLimitPlaylist;
        }

        private static bool GetSystray()
        {
            return CurrentConfig.AppConfig.Systray;
        }

        private static bool GetTelemetry()
        {
            return CurrentConfig.AppConfig.Telemetry;
        }

        private static string GetTheme()
        {
            return CurrentConfig.AppConfig.Theme;
        }

        private static string GetTwAcc()
        {
            return CurrentConfig.TwitchCredentials.BotAccountName;
        }

        private static bool GetTwAutoConnect()
        {
            return CurrentConfig.AppConfig.TwAutoConnect;
        }

        private static string GetTwChannel()
        {
            return CurrentConfig.TwitchCredentials.ChannelName;
        }

        private static string GetTwitchAccessToken()
        {
            return CurrentConfig.TwitchCredentials.AccessToken;
        }

        private static string GetTwitchBotToken()
        {
            return CurrentConfig.TwitchCredentials.TwitchBotToken;
        }

        private static User GetTwitchBotUser()
        {
            return CurrentConfig.TwitchCredentials.BotUser;
        }

        private static string GetTwitchChannelId()
        {
            return CurrentConfig.TwitchCredentials.ChannelId;
        }

        private static int GetTwitchFetchPort()
        {
            return CurrentConfig.AppConfig.TwitchFetchPort;
        }

        private static int GetTwitchRedirectPort()
        {
            return CurrentConfig.AppConfig.TwitchRedirectPort;
        }

        private static User GetTwitchUser()
        {
            return CurrentConfig.TwitchCredentials.TwitchUser;
        }

        private static string GetTwOAuth()
        {
            return CurrentConfig.TwitchCredentials.BotOAuthToken;
        }

        private static string GetTwRewardGoalRewardId()
        {
            return CurrentConfig.AppConfig.TwRewardGoalRewardId;
        }

        private static List<string> GetTwRewardId()
        {
            return CurrentConfig.AppConfig.TwRewardId;
        }

        private static List<string> GetTwRewardSkipId()
        {
            return CurrentConfig.AppConfig.TwRewardSkipId;
        }

        private static bool GetTwSrCommand()
        {
            return CurrentConfig.AppConfig.TwSrCommand;
        }

        private static int GetTwSrCooldown()
        {
            return CurrentConfig.AppConfig.TwSrCooldown;
        }

        private static int GetTwSrMaxReq()
        {
            return CurrentConfig.AppConfig.TwSrMaxReq;
        }

        private static int GetTwSrMaxReqBroadcaster()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqBroadcaster;
        }

        private static int GetTwSrMaxReqEveryone()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqEveryone;
        }

        private static int GetTwSrMaxReqFollower()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqFollower;
        }

        private static int GetTwSrMaxReqModerator()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqModerator;
        }

        private static int GetTwSrMaxReqSubscriber()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqSubscriber;
        }

        private static int GetTwSrMaxReqSubscriberT2()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqSubscriberT2;
        }

        private static int GetTwSrMaxReqSubscriberT3()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqSubscriberT3;
        }

        private static int GetTwSrMaxReqVip()
        {
            return CurrentConfig.AppConfig.TwSrMaxReqVip;
        }

        private static int GetTwSrPerUserCooldown()
        {
            return CurrentConfig.AppConfig.TwSrPerUserCooldown;
        }

        private static bool GetTwSrReward()
        {
            return CurrentConfig.AppConfig.TwSrReward;
        }

        private static bool GetTwSrUnlimitedSr()
        {
            return CurrentConfig.AppConfig.TwSrUnlimitedSr;
        }

        private static int GetTwSrUserLevel()
        {
            return CurrentConfig.AppConfig.TwSrUserLevel;
        }

        private static bool GetUpdateRequired()
        {
            return CurrentConfig.AppConfig.UpdateRequired;
        }

        private static bool GetUpload()
        {
            return CurrentConfig.AppConfig.Upload;
        }

        private static bool GetUploadHistory()
        {
            return CurrentConfig.AppConfig.UploadHistory;
        }

        private static bool GetUseDefaultBrowser()
        {
            return CurrentConfig.AppConfig.UseDefaultBrowser;
        }

        private static bool GetUseOwnApp()
        {
            return CurrentConfig.AppConfig.UseOwnApp;
        }

        private static List<string> GetUserBlacklist()
        {
            return CurrentConfig.AppConfig.UserBlacklist;
        }

        private static List<int> GetUserLevelsCommand()
        {
            return CurrentConfig.AppConfig.UserLevelsCommand;
        }

        private static List<int> GetUserLevelsReward()
        {
            return CurrentConfig.AppConfig.UserLevelsReward;
        }

        private static string GetUuid()
        {
            return CurrentConfig.AppConfig.Uuid;
        }

        private static int GetWebServerPort()
        {
            return CurrentConfig.AppConfig.WebServerPort;
        }

        private static string GetWebua()
        {
            return "Songify Data Provider";
        }

        private static string GetYtmdToken()
        {
            return CurrentConfig.AppConfig.YtmdToken;
        }

        private static void SetAccessKey(string value)
        {
            CurrentConfig.AppConfig.AccessKey = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAddSrToPlaylist(bool value)
        {
            CurrentConfig.AppConfig.AddSrToPlaylist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAnnounceInChat(bool value)
        {
            CurrentConfig.AppConfig.AnnounceInChat = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAppendSpaces(bool value)
        {
            CurrentConfig.AppConfig.AppendSpaces = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAppendSpacesSplitFiles(bool value)
        {
            CurrentConfig.AppConfig.AppendSpacesSplitFiles = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetArtistBlacklist(List<string> value)
        {
            CurrentConfig.AppConfig.ArtistBlacklist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAutoClearQueue(bool value)
        {
            CurrentConfig.AppConfig.AutoClearQueue = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAutostart(bool autostart)
        {
            CurrentConfig.AppConfig.Autostart = autostart;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetAutoStartWebServer(bool value)
        {
            CurrentConfig.AppConfig.AutoStartWebServer = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetBaseUrl(string value)
        {
            CurrentConfig.AppConfig.BaseUrl = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetBetaUpdates(bool value)
        {
            CurrentConfig.AppConfig.BetaUpdates = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetBlockAllExplicitSongs(bool value)
        {
            CurrentConfig.AppConfig.BlockAllExplicitSongs = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetBot_Resp_Blacklist(string value)
        {
            CurrentConfig.BotConfig.BotRespBlacklist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Cooldown(string value)
        {
            CurrentConfig.BotConfig.BotRespCooldown = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Error(string value)
        {
            CurrentConfig.BotConfig.BotRespError = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_ExplicitSong(string value)
        {
            CurrentConfig.BotConfig.BotRespExplicitSong = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_IsInQueue(string value)
        {
            CurrentConfig.BotConfig.BotRespIsInQueue = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Length(string value)
        {
            CurrentConfig.BotConfig.BotRespLength = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_MaxReq(string value)
        {
            CurrentConfig.BotConfig.BotRespMaxReq = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_ModSkip(string value)
        {
            CurrentConfig.BotConfig.BotRespModSkip = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Next(string value)
        {
            CurrentConfig.BotConfig.BotRespNext = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_NoSong(string value)
        {
            CurrentConfig.BotConfig.BotRespNoSong = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_NoTrackFound(string value)
        {
            CurrentConfig.BotConfig.BotRespNoTrackFound = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Playlist(string value)
        {
            CurrentConfig.BotConfig.BotRespPlaylist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Pos(string value)
        {
            CurrentConfig.BotConfig.BotRespPos = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Refund(string value)
        {
            CurrentConfig.BotConfig.BotRespRefund = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Remove(string value)
        {
            CurrentConfig.BotConfig.BotRespRemove = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Song(string value)
        {
            CurrentConfig.BotConfig.BotRespSong = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_SongLike(string value)
        {
            CurrentConfig.BotConfig.BotRespSongLike = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_SongUnavailable(string value)
        {
            CurrentConfig.BotConfig.BotRespUnavailable = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_Success(string value)
        {
            CurrentConfig.BotConfig.BotRespSuccess = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBot_Resp_VoteSkip(string value)
        {
            CurrentConfig.BotConfig.BotRespVoteSkip = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdCommands(bool value)
        {
            CurrentConfig.BotConfig.BotCmdCommands = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static void SetBotCmdNext(bool value)
        {
            CurrentConfig.BotConfig.BotCmdNext = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdPlayPause(bool value)
        {
            CurrentConfig.BotConfig.BotCmdPlayPause = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdPos(bool value)
        {
            CurrentConfig.BotConfig.BotCmdPos = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdQueue(bool value)
        {
            CurrentConfig.BotConfig.BotCmdQueue = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdQueueTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdQueueTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdRemove(bool value)
        {
            CurrentConfig.BotConfig.BotCmdRemove = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdSkip(bool value)
        {
            CurrentConfig.BotConfig.BotCmdSkip = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdSkipVote(bool value)
        {
            CurrentConfig.BotConfig.BotCmdSkipVote = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdSkipVoteCount(int value)
        {
            CurrentConfig.BotConfig.BotCmdSkipVoteCount = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdSong(bool value)
        {
            CurrentConfig.BotConfig.BotCmdSong = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdSonglike(bool value)
        {
            CurrentConfig.BotConfig.BotCmdSonglike = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdVol(bool value)
        {
            CurrentConfig.BotConfig.BotCmdVol = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotCmdVolIgnoreMod(bool value)
        {
            CurrentConfig.BotConfig.BotCmdVolIgnoreMod = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetBotOnlyWorkWhenLive(bool value)
        {
            CurrentConfig.AppConfig.BotOnlyWorkWhenLive = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetBotRespUserCooldown(string value)
        {
            CurrentConfig.BotConfig.BotRespUserCooldown = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetChatLiveStatus(bool value)
        {
            CurrentConfig.BotConfig.ChatLiveStatus = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetChromeFetchRate(int value)
        {
            CurrentConfig.AppConfig.ChromeFetchRate = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetClientId(string value)
        {
            CurrentConfig.SpotifyCredentials.ClientId = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetClientSecret(string value)
        {
            CurrentConfig.SpotifyCredentials.ClientSecret = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetCmdNextTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdNextTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdPosTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdPosTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdRemoveTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdRemoveTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdSkipTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdSkipTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdSonglikeTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdSonglikeTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdSongTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdSongTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdSsrTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdSsrTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetCmdVoteskipTrigger(string value)
        {
            CurrentConfig.BotConfig.BotCmdVoteskipTrigger = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.BotConfig, CurrentConfig.BotConfig);
        }

        private static void SetColor(string value)
        {
            CurrentConfig.AppConfig.Color = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetCustomPauseText(string value)
        {
            CurrentConfig.AppConfig.CustomPauseText = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetCustomPauseTextEnabled(bool value)
        {
            CurrentConfig.AppConfig.CustomPauseTextEnabled = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetDirectory(string value)
        {
            CurrentConfig.AppConfig.Directory = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetDonationReminder(bool value)
        {
            CurrentConfig.AppConfig.DonationReminder = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetDownloadCanvas(bool value)
        {
            CurrentConfig.AppConfig.DownloadCanvas = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static void SetDownloadCover(bool value)
        {
            CurrentConfig.AppConfig.DownloadCover = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetFontSize(int value)
        {
            CurrentConfig.AppConfig.FontSize = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetFontSizeQueue(int value)
        {
            CurrentConfig.AppConfig.FontsizeQueue = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetKeepAlbumCover(bool value)
        {
            CurrentConfig.AppConfig.KeepAlbumCover = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetLanguage(string value)
        {
            CurrentConfig.AppConfig.Language = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetLastShownMotdId(int value)
        {
            CurrentConfig.AppConfig.LastShownMotdId = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetLimitSrToPlaylist(bool value)
        {
            CurrentConfig.AppConfig.LimitSrToPlaylist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetMaxSongLength(int value)
        {
            CurrentConfig.AppConfig.MaxSongLength = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetMsgLoggingEnabled(bool value)
        {
            CurrentConfig.AppConfig.MsgLoggingEnabled = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetOpenQueueOnStartup(bool value)
        {
            CurrentConfig.AppConfig.OpenQueueOnStartup = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetOutputString(string value)
        {
            CurrentConfig.AppConfig.OutputString = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetOutputString2(string value)
        {
            CurrentConfig.AppConfig.OutputString2 = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetPauseOption(Enums.PauseOptions value)
        {
            CurrentConfig.AppConfig.PauseOption = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetPosX(double value)
        {
            CurrentConfig.AppConfig.PosX = (int)value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetPosY(double value)
        {
            CurrentConfig.AppConfig.PosY = (int)value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetQueueWindowColumns(List<int> value)
        {
            CurrentConfig.AppConfig.QueueWindowColumns = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetReadNotificationIds(List<int> value)
        {
            CurrentConfig.AppConfig.ReadNotificationIds = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetRefundConditons(int[] value)
        {
            CurrentConfig.AppConfig.RefundConditons = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetRequesterPrefix(string value)
        {
            CurrentConfig.AppConfig.RequesterPrefix = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetRewardGoalAmount(int value)
        {
            CurrentConfig.AppConfig.RewardGoalAmount = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetRewardGoalEnabled(bool value)
        {
            CurrentConfig.AppConfig.RewardGoalEnabled = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetRewardGoalSong(string value)
        {
            CurrentConfig.AppConfig.RewardGoalSong = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSaveHistory(bool value)
        {
            CurrentConfig.AppConfig.SaveHistory = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSongBlacklist(List<TrackItem> value)
        {
            CurrentConfig.AppConfig.SongBlacklist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSource(int value)
        {
            CurrentConfig.AppConfig.Player = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSpaceCount(int value)
        {
            CurrentConfig.AppConfig.SpaceCount = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSplitOutput(bool value)
        {
            CurrentConfig.AppConfig.SplitOutput = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSpotifyAccessToken(string value)
        {
            CurrentConfig.SpotifyCredentials.AccessToken = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyControlVisible(bool value)
        {
            CurrentConfig.AppConfig.SpotifyControlVisible = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSpotifyDeviceId(string value)
        {
            CurrentConfig.SpotifyCredentials.DeviceId = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyPlaylistCache(List<SimplePlaylist> value)
        {
            CurrentConfig.SpotifyCredentials.PlaylistCache = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyPlaylistId(string value)
        {
            CurrentConfig.AppConfig.SpotifyPlaylistId = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSpotifyProfile(PrivateProfile value)
        {
            CurrentConfig.SpotifyCredentials.Profile = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetSpotifyRefreshToken(string value)
        {
            CurrentConfig.SpotifyCredentials.RefreshToken = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.SpotifyCredentials, CurrentConfig.SpotifyCredentials);
        }

        private static void SetSpotifySongLimitPlaylist(string value)
        {
            CurrentConfig.AppConfig.SpotifySongLimitPlaylist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetSystray(bool value)
        {
            CurrentConfig.AppConfig.Systray = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTelemetry(bool value)
        {
            CurrentConfig.AppConfig.Systray = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTheme(string value)
        {
            CurrentConfig.AppConfig.Theme = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwAcc(string value)
        {
            CurrentConfig.TwitchCredentials.BotAccountName = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwAutoConnect(bool value)
        {
            CurrentConfig.AppConfig.TwAutoConnect = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwChannel(string value)
        {
            CurrentConfig.TwitchCredentials.ChannelName = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwitchAccessToken(string value)
        {
            CurrentConfig.TwitchCredentials.AccessToken = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwitchBotToken(string value)
        {
            CurrentConfig.TwitchCredentials.TwitchBotToken = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwitchBotUser(User value)
        {
            CurrentConfig.TwitchCredentials.BotUser = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwitchChannelId(string value)
        {
            CurrentConfig.TwitchCredentials.ChannelId = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwitchFetchPort(int value)
        {
            CurrentConfig.AppConfig.TwitchFetchPort = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwitchRedirectPort(int value)
        {
            CurrentConfig.AppConfig.TwitchRedirectPort = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwitchUser(User value)
        {
            CurrentConfig.TwitchCredentials.TwitchUser = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwOAuth(string value)
        {
            CurrentConfig.TwitchCredentials.BotOAuthToken = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.TwitchCredentials, CurrentConfig.TwitchCredentials);
        }

        private static void SetTwRewardGoalRewardId(string value)
        {
            CurrentConfig.AppConfig.TwRewardGoalRewardId = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwRewardId(List<string> value)
        {
            CurrentConfig.AppConfig.TwRewardId = value;
            CurrentConfig.AppConfig.TwRewardId.RemoveAll(string.IsNullOrEmpty);
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwRewardSkipId(List<string> value)
        {
            CurrentConfig.AppConfig.TwRewardSkipId = value;
            CurrentConfig.AppConfig.TwRewardSkipId.RemoveAll(string.IsNullOrEmpty);
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrCommand(bool value)
        {
            CurrentConfig.AppConfig.TwSrCommand = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrCooldown(int value)
        {
            CurrentConfig.AppConfig.TwSrCooldown = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReq(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReq = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqBroadcaster(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqBroadcaster = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqEveryone(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqEveryone = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqFollower(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqFollower = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqModerator(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqModerator = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqSubscriber(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqSubscriber = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrMaxReqSubscriberT2(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqSubscriberT2 = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static void SetTwSrMaxReqSubscriberT3(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqSubscriberT3 = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        private static void SetTwSrMaxReqVip(int value)
        {
            CurrentConfig.AppConfig.TwSrMaxReqVip = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrPerUserCooldown(int value)
        {
            CurrentConfig.AppConfig.TwSrPerUserCooldown = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrReward(bool value)
        {
            CurrentConfig.AppConfig.TwSrReward = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrUnlimitedSr(bool value)
        {
            CurrentConfig.AppConfig.TwSrUnlimitedSr = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetTwSrUserLevel(int value)
        {
            CurrentConfig.AppConfig.TwSrUserLevel = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUpdateRequired(bool value)
        {
            CurrentConfig.AppConfig.UpdateRequired = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUpload(bool value)
        {
            CurrentConfig.AppConfig.Upload = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUploadHistory(bool value)
        {
            CurrentConfig.AppConfig.UploadHistory = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUseDefaultBrowser(bool value)
        {
            CurrentConfig.AppConfig.UseDefaultBrowser = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUseOwnApp(bool value)
        {
            CurrentConfig.AppConfig.UseOwnApp = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUserBlacklist(List<string> value)
        {
            CurrentConfig.AppConfig.UserBlacklist = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUserLevelsCommand(List<int> value)
        {
            CurrentConfig.AppConfig.UserLevelsCommand = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUserLevelsReward(List<int> value)
        {
            CurrentConfig.AppConfig.UserLevelsReward = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetUuid(string value)
        {
            CurrentConfig.AppConfig.Uuid = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetWebServerPort(int value)
        {
            CurrentConfig.AppConfig.WebServerPort = value;
            ConfigHandler.WriteConfig(Enums.ConfigTypes.AppConfig, CurrentConfig.AppConfig);
        }

        private static void SetYtmdToken(string value)
        {
            CurrentConfig.AppConfig.YtmdToken = value;
            ConfigHandler.WriteAllConfig(CurrentConfig);
        }

        public static void UpdateCommand(TwitchCommand command)
        {
            // Update the command in the config
            int index = CurrentConfig.TwitchCommands.Commands.FindIndex(x => x.Name == command.Name);
            if (index == -1) return;
            CurrentConfig.TwitchCommands.Commands[index] = command;
            ConfigHandler.WriteAllConfig(CurrentConfig);
            TwitchHandler.InitializeCommands(Commands);
        }
    }
}