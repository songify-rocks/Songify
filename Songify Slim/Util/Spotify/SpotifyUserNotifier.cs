using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Toolkit.Uwp.Notifications;
using SpotifyAPI.Web;
using Songify_Slim.Models.Spotify;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using Application = System.Windows.Application;

namespace Songify_Slim.Util.Spotify;

/// <summary>
/// Throttled Windows toast notifications for Spotify UX (ApiCallMeter, OAuth, token refresh).
/// </summary>
public static class SpotifyUserNotifier
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, DateTimeOffset> LastShownByKey = new(StringComparer.Ordinal);

    /// <summary>Fired when rate limit hint text changes; null or empty means cleared.</summary>
    public static event Action<string> RateLimitHintChanged;

    /// <summary>
    /// Fired when persistent Spotify issues list changes.
    /// </summary>
    public static event Action<IReadOnlyList<SpotifyPersistentIssue>> PersistentIssuesChanged;

    /// <summary>
    /// Shows a toast unless the same throttle key was shown within <paramref name="minInterval"/>.
    /// </summary>
    public static void Notify(
        string title,
        string body,
        string throttleKey,
        TimeSpan? minInterval = null)
    {
        if (string.IsNullOrWhiteSpace(throttleKey))
            throttleKey = "default";

        TimeSpan window = minInterval ?? TimeSpan.FromSeconds(90);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        lock (Gate)
        {
            if (LastShownByKey.TryGetValue(throttleKey, out DateTimeOffset last) &&
                now - last < window)
                return;

            LastShownByKey[throttleKey] = now;
        }

        ShowToastOnUiThread(title, body);
    }

    /// <summary>Rate-limit toasts: one per distinct cooldown end time.</summary>
    public static void NotifyRateLimited(string title, string body, DateTimeOffset retryUntilUtc)
    {
        string key = "ratelimit:" + retryUntilUtc.ToUnixTimeSeconds();

        lock (Gate)
        {
            if (LastShownByKey.TryGetValue(key, out _))
                return;

            LastShownByKey[key] = DateTimeOffset.UtcNow;
        }

        ShowToastOnUiThread(title, body);

        string local = retryUntilUtc.LocalDateTime.ToString("g");
        RateLimitHintChanged?.Invoke(
            $"Spotify is limiting requests. Try again after about {local}.");

        try
        {
            // Persist across restarts so the main UI can show a stable banner.
            SpotifyPersistentIssue issue = new()
            {
                Kind = (retryUntilUtc - DateTimeOffset.UtcNow) > TimeSpan.FromMinutes(5) ? "quota" : "rate_limit",
                Title = title,
                Body = body,
                CreatedAtUtc = DateTime.UtcNow,
                RetryUntilUtc = retryUntilUtc.UtcDateTime,
                // Keep it around a bit after cooldown so users see it even if they reopen slightly later.
                ExpiresAtUtc = retryUntilUtc.UtcDateTime.AddHours(6),
                Dismissed = false
            };
            AddOrUpdatePersistentIssue(issue);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, LogSource.Spotify, "Persisting rate limit issue failed: " + ex.Message);
        }
    }

    public static void ClearRateLimitHint()
    {
        RateLimitHintChanged?.Invoke(null);
        try
        {
            ClearPersistentIssuesByKind("rate_limit", "quota");
        }
        catch
        {
            // ignored
        }
    }

    public static void AddOrUpdatePersistentIssue(SpotifyPersistentIssue issue)
    {
        if (issue == null)
            return;

        string dedup = ComputeDedupKey(issue);
        issue.DedupKey = dedup;
        if (string.IsNullOrWhiteSpace(issue.Id))
            issue.Id = Guid.NewGuid().ToString("N");

        List<SpotifyPersistentIssue> list = new(Settings.SpotifyPersistentIssues ?? new List<SpotifyPersistentIssue>());

        DateTime nowUtc = DateTime.UtcNow;
        list = list.Where(x => x != null && !x.IsStale(nowUtc)).ToList();

        int existingIndex = list.FindIndex(x => string.Equals(x.DedupKey, dedup, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            SpotifyPersistentIssue existing = list[existingIndex];
            existing.Title = issue.Title ?? existing.Title;
            existing.Body = issue.Body ?? existing.Body;
            existing.Kind = issue.Kind ?? existing.Kind;
            existing.CreatedAtUtc = issue.CreatedAtUtc != default ? issue.CreatedAtUtc : existing.CreatedAtUtc;
            existing.RetryUntilUtc = issue.RetryUntilUtc ?? existing.RetryUntilUtc;
            existing.ExpiresAtUtc = issue.ExpiresAtUtc ?? existing.ExpiresAtUtc;
            existing.Dismissed = false;
            list[existingIndex] = existing;
        }
        else
        {
            list.Insert(0, issue);
        }

        const int max = 6;
        if (list.Count > max)
            list = list.Take(max).ToList();

        Settings.SpotifyPersistentIssues = list;
        PersistentIssuesChanged?.Invoke(list);
    }

    public static void DismissPersistentIssue(string issueId)
    {
        if (string.IsNullOrWhiteSpace(issueId))
            return;

        List<SpotifyPersistentIssue> list = new(Settings.SpotifyPersistentIssues ?? new List<SpotifyPersistentIssue>());
        SpotifyPersistentIssue item = list.FirstOrDefault(x => x != null && x.Id == issueId);
        if (item == null)
            return;

        item.Dismissed = true;
        Settings.SpotifyPersistentIssues = list;
        PersistentIssuesChanged?.Invoke(list);
    }

    public static void ClearPersistentIssuesByKind(params string[] kinds)
    {
        if (kinds == null || kinds.Length == 0)
            return;

        HashSet<string> set = new(kinds.Where(k => !string.IsNullOrWhiteSpace(k)), StringComparer.Ordinal);
        if (set.Count == 0)
            return;

        List<SpotifyPersistentIssue> list = new(Settings.SpotifyPersistentIssues ?? new List<SpotifyPersistentIssue>());
        DateTime nowUtc = DateTime.UtcNow;
        list = list.Where(x => x != null && !x.IsStale(nowUtc) && !set.Contains(x.Kind ?? "")).ToList();
        Settings.SpotifyPersistentIssues = list;
        PersistentIssuesChanged?.Invoke(list);
    }

    private static string ComputeDedupKey(SpotifyPersistentIssue issue)
    {
        string kind = (issue.Kind ?? "").Trim();
        string title = (issue.Title ?? "").Trim();
        string body = (issue.Body ?? "").Trim();
        string retry = issue.RetryUntilUtc.HasValue ? issue.RetryUntilUtc.Value.ToUniversalTime().ToString("O") : "";

        if (kind is "rate_limit" or "quota")
            return $"{kind}|{retry}";

        return $"{kind}|{title}|{body}";
    }

    private static void ShowToastOnUiThread(string title, string body)
    {
        void Show()
        {
            try
            {
                new ToastContentBuilder()
                    .AddText(title ?? "Songify")
                    .AddText(body ?? "")
                    .Show();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, LogSource.Spotify, "Toast notification failed.", ex);
            }
        }

        try
        {
            Application app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
                app.Dispatcher.BeginInvoke((Action)Show);
            else
                Show();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, LogSource.Spotify, "Could not marshal Spotify toast to UI thread.", ex);
            try
            {
                Show();
            }
            catch (Exception ex2)
            {
                Logger.Log(LogLevel.Warning, LogSource.Spotify, "Spotify toast fallback failed.", ex2);
            }
        }
    }

    internal static string FormatApiErrorBody(APIException ex)
    {
        if (ex?.Response == null)
            return ex?.Message ?? "";

        try
        {
            if (ex.Response.StatusCode != 0)
                return $"{(int)ex.Response.StatusCode} {ex.Response.StatusCode}: {ex.Message}";
        }
        catch
        {
            // ignored
        }

        return ex.Message;
    }

    internal static bool ShouldThrottleApiException(string requestKey, APIException ex, out int? statusCode)
    {
        statusCode = null;
        try
        {
            if (ex?.Response != null)
                statusCode = (int)ex.Response.StatusCode;
        }
        catch
        {
            // ignored
        }

        if (requestKey == "Playlists.Get" && ex?.Message == "Resource not found")
            return true;

        return false;
    }

    internal static void NotifyApiException(string requestKey, APIException ex)
    {
        if (ShouldThrottleApiException(requestKey, ex, out int? status))
            return;

        int code = status ?? 0;
        string throttleKey = $"api:{code}:{requestKey}";
        string detail = FormatApiErrorBody(ex);

        string title = code switch
        {
            (int)HttpStatusCode.Forbidden => "Spotify access denied",
            (int)HttpStatusCode.NotFound => "Spotify not found",
            (int)HttpStatusCode.BadGateway => "Spotify temporarily unavailable",
            (int)HttpStatusCode.ServiceUnavailable => "Spotify temporarily unavailable",
            _ => "Spotify API error"
        };

        string body = code > 0
            ? $"{detail}\n(Request: {requestKey})"
            : $"{detail}\n(Request: {requestKey})";

        Notify(title, body, throttleKey, TimeSpan.FromMinutes(2));

        try
        {
            SpotifyPersistentIssue issue = new()
            {
                Kind = "api_error",
                Title = title,
                Body = body,
                CreatedAtUtc = DateTime.UtcNow,
                // Don't keep generic errors forever; they get noisy.
                ExpiresAtUtc = DateTime.UtcNow.AddHours(2),
                Dismissed = false
            };
            AddOrUpdatePersistentIssue(issue);
        }
        catch
        {
            // ignored
        }
    }

    internal static void NotifyUnauthorized(string requestKey)
    {
        Notify(
            "Spotify session expired",
            "Your Spotify login is no longer valid. Use Link in Songify to sign in again.",
            "spotify:401:" + requestKey,
            TimeSpan.FromMinutes(5));

        try
        {
            SpotifyPersistentIssue issue = new()
            {
                Kind = "unauthorized",
                Title = "Spotify session expired",
                Body = "Your Spotify login is no longer valid. Use Link in Songify to sign in again.",
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(12),
                Dismissed = false
            };
            AddOrUpdatePersistentIssue(issue);
        }
        catch
        {
            // ignored
        }
    }

    internal static void NotifyUnexpected(string requestKey, Exception ex)
    {
        string msg = ex?.Message ?? "Unknown error";
        if (msg.Length > 160)
            msg = msg.Substring(0, 157) + "...";

        Notify(
            "Spotify request failed",
            $"{msg}\n(Request: {requestKey})",
            "unexpected:" + requestKey,
            TimeSpan.FromMinutes(3));

        try
        {
            SpotifyPersistentIssue issue = new()
            {
                Kind = "unexpected",
                Title = "Spotify request failed",
                Body = $"{msg}\n(Request: {requestKey})",
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                Dismissed = false
            };
            AddOrUpdatePersistentIssue(issue);
        }
        catch
        {
            // ignored
        }
    }
}
