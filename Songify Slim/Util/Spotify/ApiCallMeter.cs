using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Songify_Slim.Util.General;
using SpotifyAPI.Web;

namespace Songify_Slim.Util.Spotify;

public static class ApiCallMeter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static DateTimeOffset? _globalRetryUntil;
    private static readonly object RetryLock = new();

    private sealed class Counter
    {
        // lock-free enough for moderate traffic
        private readonly ConcurrentQueue<long> _ticks = new();

        public void MarkNow()
        {
            _ticks.Enqueue(DateTime.UtcNow.Ticks);
            Trim();
        }

        public int CountLastMinute()
        {
            Trim();
            return _ticks.Count;
        }

        public IReadOnlyList<DateTime> Snapshot()
        {
            Trim();
            return _ticks.Select(t => new DateTime(t, DateTimeKind.Utc)).ToArray();
        }

        private void Trim()
        {
            long cutoff = DateTime.UtcNow.Subtract(Window).Ticks;
            while (_ticks.TryPeek(out long t) && t < cutoff)
                _ticks.TryDequeue(out _);
        }
    }

    private static readonly ConcurrentDictionary<string, Counter> _perKey = new();

    /// <summary>
    /// Mark + execute + surface 429 Retry-After automatically.
    /// Optionally soft-throttle if you set softLimitPerMinute.
    /// </summary>

    public static async Task<T> RunAsync<T>(
        string key,
        Func<Task<T>> action,
        int? softLimitPerMinute = null,
        CancellationToken ct = default)
    {
        Counter c = _perKey.GetOrAdd(key, _ => new Counter());

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // Global retry-after gate
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                DateTimeOffset? retryUntil;
                lock (RetryLock)
                {
                    retryUntil = _globalRetryUntil;
                }

                if (retryUntil is null || retryUntil <= DateTimeOffset.UtcNow)
                    break;

                TimeSpan wait = retryUntil.Value - DateTimeOffset.UtcNow;
                if (wait < TimeSpan.FromMilliseconds(200))
                    wait = TimeSpan.FromMilliseconds(200);

                Logger.Log(LogLevel.Debug, LogSource.Spotify,
                    $"Global Spotify cooldown active, waiting {wait.TotalMilliseconds:0} ms");

                await Task.Delay(wait, ct);
            }

            // Soft pre-throttle
            if (softLimitPerMinute is { } limit and > 0)
            {
                while (c.CountLastMinute() >= limit)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(200, ct);
                }
            }

            c.MarkNow();

            try
            {
                return await action();
            }
            catch (APITooManyRequestsException ex)
            {
                int retrySeconds = GetRetryAfterSeconds(ex);
                DateTimeOffset retryUntil = DateTimeOffset.UtcNow.AddSeconds(retrySeconds);

                lock (RetryLock)
                {
                    if (_globalRetryUntil == null || retryUntil > _globalRetryUntil.Value)
                        _globalRetryUntil = retryUntil;
                }

                string rateLimitDetails = FormatApiExceptionDetails(ex);
                if (retrySeconds > 300)
                {
                    Logger.Log(LogLevel.Error, LogSource.Spotify,
                        $"Spotify daily quota exceeded. Retry after {TimeSpan.FromSeconds(retrySeconds):hh:mm:ss}. {rateLimitDetails}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, LogSource.Spotify,
                        $"Spotify rate limit hit for '{key}'. Global cooldown for {retrySeconds}s. {rateLimitDetails}");
                }

                // Loop continues and all requests will respect the cooldown
            }
            catch (APIUnauthorizedException ex)
            {
                Logger.Error(LogSource.Spotify,
                    $"Spotify unauthorized on '{key}'. Access token may be invalid or expired. {FormatApiExceptionDetails(ex)}");
                break;
            }
            catch (APIException ex)
            {
                if (key == "Playlists.Get" && ex.Message == "Resource not found")
                    Logger.Error(LogSource.Spotify,
                        $"Spotify API: Can't get public playlist Info. {FormatApiExceptionDetails(ex)}");
                else
                    Logger.Error(LogSource.Spotify, $"Spotify API error on '{key}': {FormatApiExceptionDetails(ex)}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Spotify, $"Unexpected error on Spotify request '{key}'.");
                Logger.LogExc(ex);
                break;
            }
        }

        return default;
    }

    public static void ReleaseRateLimit()
    {
        lock (RetryLock)
        {
            _globalRetryUntil = null;
        }

        Logger.Log(LogLevel.Info, LogSource.Spotify,
            "Spotify rate limit lock released");
    }

    public static bool IsRateLimited(out TimeSpan remaining)
    {
        lock (RetryLock)
        {
            if (_globalRetryUntil is null)
            {
                remaining = TimeSpan.Zero;
                return false;
            }

            remaining = _globalRetryUntil.Value - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                _globalRetryUntil = null;
                remaining = TimeSpan.Zero;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// HTTP status, parsed message, and response body (JSON when possible) for Spotify Web API errors.
    /// </summary>
    internal static string FormatApiExceptionDetails(APIException ex)
    {
        if (ex == null)
            return "(null exception)";

        string status = ex.Response != null
            ? $"{(int)ex.Response.StatusCode} {ex.Response.StatusCode}"
            : "no response";

        string body;
        if (ex.Response?.Body != null)
        {
            try
            {
                body = JsonSerializer.Serialize(ex.Response.Body);
            }
            catch
            {
                body = ex.Response.Body.ToString();
            }
        }
        else
            body = "(no body)";

        return $"Status={status}, Message={ex.Message}, Body={body}";
    }

    private static int GetRetryAfterSeconds(APITooManyRequestsException ex)
    {
        if (ex.Response?.Headers != null)
        {
            foreach (KeyValuePair<string, string> h in ex.Response.Headers)
            {
                if (string.Equals(h.Key, "Retry-After", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(h.Value, out int parsed))
                {
                    return Math.Max(parsed, 1);
                }
            }
        }

        return 1;
    }

    public static IDictionary<string, int> GetAllCountsPerMinute()
        => _perKey.ToDictionary(kv => kv.Key, kv => kv.Value.CountLastMinute());
}