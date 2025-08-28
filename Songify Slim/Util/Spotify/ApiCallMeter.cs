using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        if (softLimitPerMinute is int limit && limit > 0)
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
        catch (SpotifyAPI.Web.APIException apiEx) when ((int)apiEx.Response?.StatusCode == 429)
        {
            // Respect Retry-After if present
            int retryAfterSeconds = TryParseRetryAfterSeconds(apiEx) ?? 1; // default small backoff
            await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds), ct);
            // Try exactly once more (you can expand to Polly if you like)
            c.MarkNow();
            return await action();
        }
    }

    public static async Task RunAsync(
        string key,
        Func<Task> action,
        int? softLimitPerMinute = null,
        CancellationToken ct = default)
        => await RunAsync<object>(key, async () => { await action(); return null; }, softLimitPerMinute, ct);

    public static int GetCountPerMinute(string key)
        => _perKey.TryGetValue(key, out Counter c) ? c.CountLastMinute() : 0;

    public static IDictionary<string, int> GetAllCountsPerMinute()
        => _perKey.ToDictionary(kv => kv.Key, kv => kv.Value.CountLastMinute());

    public static IReadOnlyList<string> Keys() => _perKey.Keys.ToArray();

    private static int? TryParseRetryAfterSeconds(SpotifyAPI.Web.APIException ex)
    {
        try
        {
            // SpotifyAPI.Web exposes headers through ex.Response?.Headers (string→IEnumerable<string>) in recent versions.
            IReadOnlyDictionary<string, string> headers = ex.Response?.Headers;
            if (headers != null && headers.TryGetValue("Retry-After", out string values))
            {
                char v = values.FirstOrDefault();
                if (int.TryParse(v.ToString(), out int s)) return s;
            }
        }
        catch { /* ignore */ }
        return null;
    }
}