using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Songify_Slim.Util.Songify;
using SpotifyAPI.Web;
using Image = SpotifyAPI.Web.Image;

namespace Songify_Slim.Util.Spotify;

/// <summary>
/// SMTC-driven Spotify now-playing. <see cref="SpotifySmtcWatcher"/> is the event source; the Web API
/// is called only when the track actually changes (to enrich with id, artwork, playlist), cached by
/// track key. Without a local Spotify session it falls back to a slow API poll.
/// Runs on the UI dispatcher, like the rest of the fetch pipeline.
/// </summary>
public sealed class NowPlayingCoordinator : IDisposable
{
    /// <summary>The API often lags SMTC by about a second after a skip.</summary>
    private static readonly int[] ResolveDelaysMs = [0, 1000, 2500];

    private static readonly TimeSpan FallbackPollInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan AuthRetryInterval = TimeSpan.FromSeconds(20);
    private const int CacheCapacity = 500;

    private readonly SongFetcher _fetcher;
    private readonly SpotifySmtcWatcher _watcher;

    // LRU cache: track key -> enriched TrackInfo.
    private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, TrackInfo>>> _cache = new();
    private readonly LinkedList<KeyValuePair<string, TrackInfo>> _cacheOrder = new();

    private CancellationTokenSource _resolveCts;
    private string _publishedKey;
    private bool _gateWasClosed;
    private DateTime _lastFallbackPollUtc = DateTime.MinValue;
    private DateTime _lastAuthAttemptUtc = DateTime.MinValue;
    private bool _disposed;

    internal NowPlayingCoordinator(SongFetcher fetcher, SpotifySmtcWatcher watcher)
    {
        _fetcher = fetcher;
        _watcher = watcher;
    }

    /// <summary>Call on the UI thread.</summary>
    public async Task StartAsync()
    {
        _watcher.TrackChanged += OnTrackChanged;
        _watcher.PlaybackStateChanged += OnPlaybackStateChanged;
        _watcher.LocalSessionAvailabilityChanged += OnLocalSessionAvailabilityChanged;
        await _watcher.StartAsync();

        Logger.Info(LogSource.Spotify, _watcher.HasLocalSession
            ? "Spotify now-playing: SMTC mode active."
            : "Spotify now-playing: SMTC mode active, no local Spotify session yet. Using the 20 s API fallback poll.");
    }

    /// <summary>
    /// Called by <see cref="AppFetchService"/> about once per second. Never calls the API while a local
    /// session exists, except for the auth retry. Without one, polls the API every 20 s.
    /// </summary>
    internal async Task TickAsync()
    {
        if (_disposed)
            return;

        ApiCallMeter.LogHourlySummaryIfDue();

        if (!SpotifyLiveGate.AllowsSpotifyFetch)
        {
            _gateWasClosed = true;
            return;
        }

        if (_gateWasClosed)
        {
            // Gate reopened: republish the current track (usually a cache hit, so no API call).
            _gateWasClosed = false;
            _publishedKey = null;
        }

        if (SpotifyApiHandler.Client == null &&
            !string.IsNullOrEmpty(Settings.SpotifyAccessToken) &&
            !string.IsNullOrEmpty(Settings.SpotifyRefreshToken) &&
            DateTime.UtcNow - _lastAuthAttemptUtc >= AuthRetryInterval)
        {
            _lastAuthAttemptUtc = DateTime.UtcNow;
            await SpotifyApiHandler.Auth();
        }

        if (!_watcher.HasLocalSession)
        {
            if (DateTime.UtcNow - _lastFallbackPollUtc < FallbackPollInterval)
                return;

            _lastFallbackPollUtc = DateTime.UtcNow;
            Logger.Debug(LogSource.Spotify, "No local Spotify session: fallback API poll.");
            await _fetcher.FetchSpotifyWeb();
            return;
        }

        SmtcTrack track = _watcher.CurrentTrack;
        if (track != null && track.Key != _publishedKey)
        {
            _ = ResolveAsync(track);
            return;
        }

        // CurrentSong is still the previous track until the resolve publishes.
        if (_resolveCts != null)
            return;

        // Pause, resume and seek land here: local clock only, no API call.
        await _fetcher.ApplySpotifyPlaybackStateAsync(_watcher.IsPlaying, ToMs(_watcher.CurrentPosition), ToMs(_watcher.Duration));
    }

    private void OnTrackChanged(SmtcTrack track)
    {
        if (_disposed)
            return;
        _ = ResolveAsync(track);
    }

    private async void OnPlaybackStateChanged(bool isPlaying)
    {
        // During a resolve the final publish carries the live play state.
        if (_disposed || _resolveCts != null || !SpotifyLiveGate.AllowsSpotifyFetch)
            return;

        try
        {
            await _fetcher.ApplySpotifyPlaybackStateAsync(isPlaying, ToMs(_watcher.CurrentPosition), ToMs(_watcher.Duration));
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private void OnLocalSessionAvailabilityChanged(bool available)
    {
        if (available)
        {
            Logger.Info(LogSource.Spotify, "Local Spotify session found: stopping the API fallback poll.");
            return;
        }

        // Poll right away on the next tick, then every 20 s.
        Logger.Info(LogSource.Spotify, "No local Spotify session: starting the 20 s API fallback poll.");
        _lastFallbackPollUtc = DateTime.MinValue;
        _resolveCts?.Cancel();
    }

    private async Task ResolveAsync(SmtcTrack track)
    {
        if (track == null || _disposed || !SpotifyLiveGate.AllowsSpotifyFetch)
            return;

        // A newer track change cancels any resolve still in flight.
        _resolveCts?.Cancel();
        CancellationTokenSource cts = new();
        _resolveCts = cts;
        CancellationToken ct = cts.Token;
        _publishedKey = track.Key;

        try
        {
            if (TryGetCached(track.Key, out TrackInfo cached))
            {
                Logger.Debug(LogSource.Spotify, $"SMTC track '{track.Artist} - {track.Title}': cache hit, no API call.");
                await _fetcher.PublishSpotifyTrackAsync(WithLiveState(cached), SpotifyPlaylistResolve.KeepCurrent);
                return;
            }

            bool previewShown = false;
            foreach (int delayMs in ResolveDelaysMs)
            {
                if (delayMs > 0)
                    await Task.Delay(delayMs, ct);

                if (ApiCallMeter.IsRateLimited(out TimeSpan remaining))
                {
                    Logger.Debug(LogSource.Spotify,
                        $"Spotify rate limited for {remaining.TotalSeconds:0} s: keeping SMTC-only data for '{track.Title}'.");
                    break;
                }

                TrackInfo apiTrack = await SpotifyApiHandler.GetSongInfo();
                if (ct.IsCancellationRequested)
                    return;

                if (apiTrack != null && Matches(track, apiTrack))
                {
                    Put(track.Key, apiTrack);
                    _watcher.ReanchorPosition(TimeSpan.FromMilliseconds(apiTrack.Progress));
                    await _fetcher.PublishSpotifyTrackAsync(WithLiveState(apiTrack), SpotifyPlaylistResolve.FromTrack);
                    return;
                }

                Logger.Debug(LogSource.Spotify, apiTrack == null
                    ? $"API returned no track yet for SMTC '{track.Artist} - {track.Title}'."
                    : $"API track '{apiTrack.Artists} - {apiTrack.Title}' does not match SMTC '{track.Artist} - {track.Title}' yet.");

                // Show the SMTC data while the API catches up.
                if (!previewShown)
                {
                    previewShown = true;
                    await _fetcher.PreviewSpotifyTrackAsync(BuildSmtcTrackInfo(track));
                    if (ct.IsCancellationRequested)
                        return;
                }
            }

            // No match (local file, podcast, API lag, rate limit): keep SMTC-only data.
            Logger.Debug(LogSource.Spotify, $"No API match for SMTC '{track.Artist} - {track.Title}': publishing SMTC-only data.");
            await _fetcher.PublishSpotifyTrackAsync(BuildSmtcTrackInfo(track), SpotifyPlaylistResolve.KeepCurrent);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer track change.
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
        finally
        {
            if (ReferenceEquals(_resolveCts, cts))
                _resolveCts = null;
            cts.Dispose();
        }
    }

    /// <summary>
    /// Titles must match after normalization, and at least one API artist must be contained in the
    /// SMTC artist string (SMTC may carry only the primary artist or a joined list).
    /// </summary>
    private static bool Matches(SmtcTrack smtc, TrackInfo api)
    {
        if (SmtcTrack.Normalize(smtc.Title) != SmtcTrack.Normalize(api.Title))
            return false;

        string smtcArtist = SmtcTrack.Normalize(smtc.Artist);
        List<string> apiArtists = (api.FullArtists ?? [])
            .Select(a => SmtcTrack.Normalize(a?.Name))
            .Where(n => n.Length > 0)
            .ToList();

        // Local files often have no artist on one side.
        if (smtcArtist.Length == 0 || apiArtists.Count == 0)
            return true;

        return apiArtists.Any(a => smtcArtist.Contains(a, StringComparison.Ordinal));
    }

    /// <summary>Copy with play state and progress from SMTC; duration from the API when known.</summary>
    private TrackInfo WithLiveState(TrackInfo source)
    {
        TrackInfo t = Clone(source);
        int durationTotal = t.DurationTotal > 0 ? t.DurationTotal : ToMs(_watcher.Duration);
        int progress = ToMs(_watcher.CurrentPosition);
        if (durationTotal > 0)
            progress = Math.Min(progress, durationTotal);

        t.IsPlaying = _watcher.IsPlaying;
        t.Progress = progress;
        t.DurationTotal = durationTotal;
        t.DurationMs = Math.Max(0, durationTotal - progress);
        t.DurationPercentage = durationTotal > 0 ? (int)(100.0 * progress / durationTotal) : 0;
        return t;
    }

    private TrackInfo BuildSmtcTrackInfo(SmtcTrack track) =>
        WithLiveState(new TrackInfo
        {
            Artists = track.Artist,
            Title = track.Title,
            Albums = new List<Image>(),
            SongId = null,
            Url = null,
            FullArtists = new List<SimpleArtist>()
        });

    private static TrackInfo Clone(TrackInfo s) => new()
    {
        Artists = s.Artists,
        Title = s.Title,
        Albums = s.Albums?.ToList() ?? new List<Image>(),
        SongId = s.SongId,
        DurationMs = s.DurationMs,
        IsPlaying = s.IsPlaying,
        Url = s.Url,
        DurationPercentage = s.DurationPercentage,
        DurationTotal = s.DurationTotal,
        Progress = s.Progress,
        Playlist = s.Playlist,
        FullArtists = s.FullArtists?.ToList() ?? new List<SimpleArtist>()
    };

    private bool TryGetCached(string key, out TrackInfo track)
    {
        if (_cache.TryGetValue(key, out LinkedListNode<KeyValuePair<string, TrackInfo>> node))
        {
            _cacheOrder.Remove(node);
            _cacheOrder.AddFirst(node);
            track = node.Value.Value;
            return true;
        }

        track = null;
        return false;
    }

    private void Put(string key, TrackInfo track)
    {
        TrackInfo copy = Clone(track);
        if (_cache.TryGetValue(key, out LinkedListNode<KeyValuePair<string, TrackInfo>> existing))
            _cacheOrder.Remove(existing);

        LinkedListNode<KeyValuePair<string, TrackInfo>> node = _cacheOrder.AddFirst(new KeyValuePair<string, TrackInfo>(key, copy));
        _cache[key] = node;

        while (_cache.Count > CacheCapacity && _cacheOrder.Last != null)
        {
            _cache.Remove(_cacheOrder.Last.Value.Key);
            _cacheOrder.RemoveLast();
        }
    }

    private static int ToMs(TimeSpan t) =>
        (int)Math.Clamp(t.TotalMilliseconds, 0, int.MaxValue);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _resolveCts?.Cancel();
        _watcher.TrackChanged -= OnTrackChanged;
        _watcher.PlaybackStateChanged -= OnPlaybackStateChanged;
        _watcher.LocalSessionAvailabilityChanged -= OnLocalSessionAvailabilityChanged;
        _watcher.Dispose();
    }
}
