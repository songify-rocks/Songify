using Newtonsoft.Json;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify.APIs;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify;

internal enum SongifyPremiumState
{
    Unknown,
    NoToken,
    Inactive,
    Active,
    InvalidToken
}

/// <summary>
/// Resolves Songify Premium from the website token, with cloud-sync status as a fallback.
/// Polls every 5 minutes after <see cref="Start"/> so Ko-fi activation shows up without a restart.
/// </summary>
internal static class SongifyPremiumService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private static Timer _timer;

    public static SongifyPremiumState Current { get; private set; } = SongifyPremiumState.Unknown;

    public static bool IsRefreshing { get; private set; }

    public static event Action StatusChanged;

    public static bool IsActive => Current == SongifyPremiumState.Active;

    public static void Start()
    {
        if (_timer != null)
            return;

        Timer created = new(_ => _ = RefreshAsync(), null, PollInterval, PollInterval);
        Timer existing = Interlocked.CompareExchange(ref _timer, created, null);
        if (existing != null)
            created.Dispose();
    }

    public static void Stop()
    {
        Timer timer = Interlocked.Exchange(ref _timer, null);
        timer?.Dispose();
    }

    public static void ApplyFromCloudStatus(HttpStatusCode statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCode.OK:
                Set(SongifyPremiumState.Active);
                return;
            case HttpStatusCode.Forbidden:
                Set(SongifyPremiumState.Inactive);
                return;
            case HttpStatusCode.Unauthorized:
                Set(SongifyPremiumState.InvalidToken);
                return;
        }
    }

    public static async Task RefreshAsync()
    {
        if (!await Gate.WaitAsync(0).ConfigureAwait(false))
        {
            await Gate.WaitAsync().ConfigureAwait(false);
            Gate.Release();
            return;
        }

        SetRefreshing(true);
        try
        {
            if (!AccountLinking.HasSongifyApiToken())
            {
                Set(SongifyPremiumState.NoToken);
                return;
            }

            SongifyPremiumState fromSite = await TryWebsitePremiumStatusAsync().ConfigureAwait(false);
            if (fromSite != SongifyPremiumState.Unknown)
            {
                Set(fromSite);
                return;
            }

            SongifyPremiumState fromCloud = await TryCloudPremiumStatusAsync().ConfigureAwait(false);
            if (fromCloud != SongifyPremiumState.Unknown)
                Set(fromCloud);
        }
        catch (Exception ex)
        {
            Logger.Error(LogSource.Api, "Error refreshing Songify Premium status.", ex);
        }
        finally
        {
            SetRefreshing(false);
            Gate.Release();
        }
    }

    private static async Task<SongifyPremiumState> TryWebsitePremiumStatusAsync()
    {
        try
        {
            string url = $"{GlobalObjects.BaseUrl.TrimEnd('/')}/api/premium-status";
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", Settings.SongifyApiKey);

            using HttpResponseMessage response = await SongifyAuthService.HttpClient
                .SendAsync(request)
                .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return SongifyPremiumState.InvalidToken;

            if (response.StatusCode != HttpStatusCode.OK)
                return SongifyPremiumState.Unknown;

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            PremiumStatusResponse parsed = JsonConvert.DeserializeObject<PremiumStatusResponse>(json);
            return parsed?.IsPremium == true
                ? SongifyPremiumState.Active
                : SongifyPremiumState.Inactive;
        }
        catch (Exception ex)
        {
            Logger.Warning(LogSource.Api, $"Premium status (website) failed: {ex.Message}");
            return SongifyPremiumState.Unknown;
        }
    }

    private static async Task<SongifyPremiumState> TryCloudPremiumStatusAsync()
    {
        try
        {
            string userId = Settings.TwitchUser?.Id;
            if (string.IsNullOrWhiteSpace(userId))
                return SongifyPremiumState.Unknown;

            using HttpResponseMessage response = await SongifyApi.GetUserSettingsAsync(userId)
                .ConfigureAwait(false);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => SongifyPremiumState.Active,
                HttpStatusCode.Forbidden => SongifyPremiumState.Inactive,
                HttpStatusCode.Unauthorized => SongifyPremiumState.InvalidToken,
                _ => SongifyPremiumState.Unknown
            };
        }
        catch (Exception ex)
        {
            Logger.Warning(LogSource.Api, $"Premium status (cloud) failed: {ex.Message}");
            return SongifyPremiumState.Unknown;
        }
    }

    private static void SetRefreshing(bool value)
    {
        if (IsRefreshing == value)
            return;
        IsRefreshing = value;
        StatusChanged?.Invoke();
    }

    private static void Set(SongifyPremiumState state)
    {
        if (Current == state)
            return;
        Current = state;
        StatusChanged?.Invoke();
    }

    private sealed class PremiumStatusResponse
    {
        [JsonProperty("isPremium")]
        public bool IsPremium { get; set; }
    }
}
