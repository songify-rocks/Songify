using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Songify_Slim.Util.General;
using SpotifyAPI.Web;

namespace Songify_Slim.Util.Spotify;

public static class ApiCallMeter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

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

        // Soft pre-throttle (optional)
        if (softLimitPerMinute is { } limit and > 0)
        {
            // Busy-wait in short sleeps until we're below the soft limit
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
        catch (APITooManyRequestsException tooManyRequestsEx)
        {
            TimeSpan retryAfter = tooManyRequestsEx.RetryAfter > TimeSpan.Zero
                ? tooManyRequestsEx.RetryAfter
                : TimeSpan.FromSeconds(1);

            Logger.Log(
                LogLevel.Warning,
                LogSource.Spotify,
                $"API call for key '{key}' was rate-limited. Handling 429 Retry-After {retryAfter.TotalSeconds}s");

            await Task.Delay(retryAfter, ct);
            c.MarkNow();
            return await action();
        }
    }

    public static IDictionary<string, int> GetAllCountsPerMinute()
        => _perKey.ToDictionary(kv => kv.Key, kv => kv.Value.CountLastMinute());
}