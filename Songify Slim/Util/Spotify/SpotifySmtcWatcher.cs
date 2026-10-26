using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Songify_Slim.Util.General;
using Windows.Media.Control;

namespace Songify_Slim.Util.Spotify;

/// <summary>Now-playing data as Spotify publishes it to Windows media controls (SMTC).</summary>
public sealed record SmtcTrack(string Title, string Artist, string Album)
{
    /// <summary>Normalized title|artist|album. Equal keys mean the same track.</summary>
    public string Key => $"{Normalize(Title)}|{Normalize(Artist)}|{Normalize(Album)}";

    /// <summary>Lowercase, diacritics stripped, trimmed.</summary>
    public static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        string decomposed = s.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}

/// <summary>
/// Watches the Spotify SMTC session and raises events. Never calls the Web API.
/// SMTC callbacks arrive on thread-pool threads; every handler is marshalled to the UI dispatcher
/// (same as the Windows Playback source), so state and events are single-threaded.
/// </summary>
public sealed class SpotifySmtcWatcher : IDisposable
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(400);

    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _debounceTimer;
    private GlobalSystemMediaTransportControlsSessionManager _manager;
    private GlobalSystemMediaTransportControlsSession _session;
    private string _lastKey;
    private int _mediaReadVersion;
    private DateTimeOffset _mediaChangeSeenAt;
    private bool _keepPositionOnNextTrack;
    private bool _disposed;

    // Timeline for local extrapolation. Spotify updates the SMTC timeline rarely.
    private TimeSpan _position;
    private TimeSpan _duration;
    private DateTimeOffset _positionStamp = DateTimeOffset.Now;
    private bool _isPlaying;

    /// <summary>Raised only when the normalized track key changes. Ads and idle states are ignored.</summary>
    public event Action<SmtcTrack> TrackChanged;

    /// <summary>True = playing.</summary>
    public event Action<bool> PlaybackStateChanged;

    /// <summary>False = no Spotify session on this PC (phone or another Connect device).</summary>
    public event Action<bool> LocalSessionAvailabilityChanged;

    public SpotifySmtcWatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _debounceTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher) { Interval = DebounceDelay };
        _debounceTimer.Tick += OnDebounceElapsed;
    }

    public bool HasLocalSession => _session != null;

    public bool IsPlaying => _isPlaying;

    /// <summary>Last track raised by <see cref="TrackChanged"/>.</summary>
    public SmtcTrack CurrentTrack { get; private set; }

    public TimeSpan Duration => _duration;

    /// <summary>Position extrapolated locally while playing, frozen while paused.</summary>
    public TimeSpan CurrentPosition
    {
        get
        {
            TimeSpan pos = _isPlaying ? _position + (DateTimeOffset.Now - _positionStamp) : _position;
            if (pos < TimeSpan.Zero)
                pos = TimeSpan.Zero;
            return _duration > TimeSpan.Zero && pos > _duration ? _duration : pos;
        }
    }

    /// <summary>Call on the UI thread.</summary>
    public async Task StartAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        if (_disposed)
            return;

        _manager.SessionsChanged += OnSessionsChanged;
        AttachToSpotify();
    }

    /// <summary>Re-anchors the extrapolated position, e.g. to the Web API's progress_ms after a resolve.</summary>
    public void ReanchorPosition(TimeSpan position)
    {
        _position = position < TimeSpan.Zero ? TimeSpan.Zero : position;
        _positionStamp = DateTimeOffset.Now;
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) =>
        Post(AttachToSpotify);

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) =>
        Post(ScheduleMediaPropertiesRead);

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) =>
        Post(ReadPlaybackInfo);

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) =>
        Post(ReadTimeline);

    private void Post(Action action)
    {
        if (_disposed)
            return;

        _dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Warning(LogSource.Spotify, "Spotify SMTC watcher handler failed.", ex);
            }
        });
    }

    private void AttachToSpotify()
    {
        if (_manager == null)
            return;

        // Matches both the classic installer (Spotify.exe) and the Microsoft Store package.
        GlobalSystemMediaTransportControlsSession spotify = _manager.GetSessions()
            .FirstOrDefault(s => s.SourceAppUserModelId?.IndexOf("spotify", StringComparison.OrdinalIgnoreCase) >= 0);

        if (ReferenceEquals(spotify, _session))
            return;

        bool wasAvailable = _session != null;
        Detach();
        _session = spotify;

        if (_session != null)
        {
            _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            if (!wasAvailable)
                Logger.Info(LogSource.Spotify, $"Attached to Spotify media session ({_session.SourceAppUserModelId}).");
        }
        else if (wasAvailable)
        {
            Logger.Info(LogSource.Spotify, "Spotify media session closed.");
        }

        if (wasAvailable != (_session != null))
            LocalSessionAvailabilityChanged?.Invoke(_session != null);

        if (_session == null)
            return;

        // The timeline read here belongs to the track that is already playing.
        _keepPositionOnNextTrack = true;
        ReadPlaybackInfo();
        ReadTimeline();
        ScheduleMediaPropertiesRead();
    }

    private void Detach()
    {
        _debounceTimer.Stop();
        if (_session == null)
            return;

        _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _session = null;
        // _lastKey is kept: reopening Spotify on the same track is not a track change.
    }

    private void ScheduleMediaPropertiesRead()
    {
        // Spotify fires this several times per skip (empty title first, artwork later).
        if (!_debounceTimer.IsEnabled)
            _mediaChangeSeenAt = DateTimeOffset.Now;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void OnDebounceElapsed(object sender, EventArgs e)
    {
        _debounceTimer.Stop();
        GlobalSystemMediaTransportControlsSession session = _session;
        if (session == null || _disposed)
            return;

        int version = ++_mediaReadVersion;
        try
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties props = await session.TryGetMediaPropertiesAsync();

            // A newer change or a re-attach superseded this read.
            if (_disposed || version != _mediaReadVersion || _debounceTimer.IsEnabled || !ReferenceEquals(session, _session))
                return;
            if (props == null)
                return;

            string title = props.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
                return; // transient empty state during a skip

            if (title.Equals("Advertisement", StringComparison.OrdinalIgnoreCase) ||
                title.Equals("Spotify", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Debug(LogSource.Spotify, $"SMTC: ignoring Spotify ad/idle state ('{title}').");
                return;
            }

            SmtcTrack track = new(title, props.Artist?.Trim() ?? "", props.AlbumTitle?.Trim() ?? "");
            bool keepPosition = _keepPositionOnNextTrack;
            _keepPositionOnNextTrack = false;
            if (track.Key == _lastKey)
                return; // artwork-only update, re-attach on the same track, etc.

            _lastKey = track.Key;
            CurrentTrack = track;

            // Raw fields: the SMTC Artist format for multi-artist tracks is unconfirmed.
            Logger.Debug(LogSource.Spotify,
                $"SMTC track changed. Raw Title='{props.Title}', Artist='{props.Artist}', AlbumArtist='{props.AlbumArtist}', Album='{props.AlbumTitle}'.");

            // Spotify does not always publish a new timeline on a skip. If nothing arrived since the
            // change began, the stored position is the previous track's: start from zero instead.
            if (!keepPosition && _positionStamp < _mediaChangeSeenAt)
            {
                _position = TimeSpan.Zero;
                _positionStamp = _mediaChangeSeenAt;
            }

            TrackChanged?.Invoke(track);
        }
        catch (Exception ex)
        {
            Logger.Warning(LogSource.Spotify, "Reading Spotify SMTC media properties failed.", ex);
        }
    }

    private void ReadPlaybackInfo()
    {
        GlobalSystemMediaTransportControlsSessionPlaybackInfo info = _session?.GetPlaybackInfo();
        if (info == null)
            return;

        bool playing = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        if (playing == _isPlaying)
            return;

        // Freeze or unfreeze the extrapolation at the current point.
        _position = CurrentPosition;
        _positionStamp = DateTimeOffset.Now;
        _isPlaying = playing;
        PlaybackStateChanged?.Invoke(playing);
    }

    private void ReadTimeline()
    {
        GlobalSystemMediaTransportControlsSessionTimelineProperties t = _session?.GetTimelineProperties();
        if (t == null)
            return;

        TimeSpan duration = t.EndTime - t.StartTime;
        DateTimeOffset now = DateTimeOffset.Now;
        // The position is valid as of LastUpdatedTime, not "now". Guard against unset or future stamps.
        DateTimeOffset stamp = t.LastUpdatedTime;
        if (stamp.Year < 2000 || stamp > now)
            stamp = now;

        _position = t.Position;
        _duration = duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        _positionStamp = stamp;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _debounceTimer.Stop();
        _debounceTimer.Tick -= OnDebounceElapsed;
        if (_manager != null)
            _manager.SessionsChanged -= OnSessionsChanged;
        Detach();
    }
}
