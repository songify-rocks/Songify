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
/// Operational API errors (401/5xx/etc.) share a global quiet period so alternating failures do not spam toasts;
/// the persistent banner still updates with counts.
/// </summary>
public static class SpotifyUserNotifier
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, DateTimeOffset> LastShownByKey = new(StringComparer.Ordinal);

    /// <summary>
    /// After any operational error toast (unauthorized / API / unexpected), suppress further
    /// operational toasts for this long — even if the status code changes (e.g. 401 then 500).
    /// </summary>
    private static readonly TimeSpan OperationalToastQuietPeriod = TimeSpan.FromMinutes(3);

    private static DateTimeOffset? _lastOperationalToastUtc;

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
        if (TryClaimToastSlot(throttleKey, minInterval ?? TimeSpan.FromSeconds(90), operational: false))
            ShowToastOnUiThread(title, body);
    }

    /// <summary>
    /// Toast for ongoing Spotify API/auth failures. Respects per-category throttle and a global
    /// quiet period across categories so 401/500 flip-flops do not spam. Always updates the banner.
    /// </summary>
    private static void NotifyOperational(
        string title,
        string body,
        string throttleKey,
        string issueKind,
        string dedupKind,
        TimeSpan? perCategoryInterval = null,
        DateTime? expiresAtUtc = null)
    {
        DateTime nowUtc = DateTime.UtcNow;
        UpsertOperationalIssue(title, body, issueKind, dedupKind, nowUtc, expiresAtUtc);

        if (TryClaimToastSlot(throttleKey, perCategoryInterval ?? TimeSpan.FromMinutes(3), operational: true))
            ShowToastOnUiThread(title, body);
    }

    /// <summary>Rate-limit toasts: one per distinct cooldown end time.</summary>
    public static void NotifyRateLimited(string title, string body, DateTimeOffset retryUntilUtc)
    {
        string key = "ratelimit:" + retryUntilUtc.ToUnixTimeSeconds();

        if (TryClaimToastSlot(key, TimeSpan.FromDays(365), operational: false))
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
                LastSeenAtUtc = DateTime.UtcNow,
                OccurrenceCount = 1,
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

        if (issue.LastSeenAtUtc == default)
            issue.LastSeenAtUtc = issue.CreatedAtUtc != default ? issue.CreatedAtUtc : DateTime.UtcNow;

        if (issue.OccurrenceCount < 1)
            issue.OccurrenceCount = 1;

        List<SpotifyPersistentIssue> list = new(Settings.SpotifyPersistentIssues ?? new List<SpotifyPersistentIssue>());

        DateTime nowUtc = DateTime.UtcNow;
        list = list.Where(x => x != null && !x.IsStale(nowUtc)).ToList();

        int existingIndex = list.FindIndex(x => string.Equals(x.DedupKey, dedup, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            SpotifyPersistentIssue existing = list[existingIndex];
            existing.Title = issue.Title ?? existing.Title;
            existing.Body = ComposeBodyWithCount(issue.Body ?? existing.Body, existing.OccurrenceCount + 1);
            existing.Kind = issue.Kind ?? existing.Kind;
            existing.OccurrenceCount = existing.OccurrenceCount + 1;
            existing.LastSeenAtUtc = issue.LastSeenAtUtc != default ? issue.LastSeenAtUtc : nowUtc;
            existing.RetryUntilUtc = issue.RetryUntilUtc ?? existing.RetryUntilUtc;
            existing.ExpiresAtUtc = issue.ExpiresAtUtc ?? existing.ExpiresAtUtc;
            existing.Dismissed = false;
            list[existingIndex] = existing;
        }
        else
        {
            issue.Body = ComposeBodyWithCount(issue.Body, issue.OccurrenceCount);
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

    private static void UpsertOperationalIssue(
        string title,
        string body,
        string issueKind,
        string dedupKind,
        DateTime nowUtc,
        DateTime? expiresAtUtc)
    {
        try
        {
            SpotifyPersistentIssue issue = new()
            {
                Kind = issueKind,
                DedupKey = dedupKind,
                Title = title,
                Body = body,
                CreatedAtUtc = nowUtc,
                LastSeenAtUtc = nowUtc,
                OccurrenceCount = 1,
                ExpiresAtUtc = expiresAtUtc ?? nowUtc.AddHours(2),
                Dismissed = false
            };
            AddOrUpdatePersistentIssue(issue);
        }
        catch
        {
            // ignored
        }
    }

    private static bool TryClaimToastSlot(string throttleKey, TimeSpan minInterval, bool operational)
    {
        if (string.IsNullOrWhiteSpace(throttleKey))
            throttleKey = "default";

        DateTimeOffset now = DateTimeOffset.UtcNow;

        lock (Gate)
        {
            if (operational &&
                _lastOperationalToastUtc is { } lastOp &&
                now - lastOp < OperationalToastQuietPeriod)
            {
                return false;
            }

            if (LastShownByKey.TryGetValue(throttleKey, out DateTimeOffset last) &&
                now - last < minInterval)
            {
                return false;
            }

            LastShownByKey[throttleKey] = now;
            if (operational)
                _lastOperationalToastUtc = now;

            return true;
        }
    }

    private static string ComposeBodyWithCount(string body, int count)
    {
        string baseBody = StripOccurrenceSuffix(body ?? "");
        if (count <= 1)
            return baseBody;

        return string.IsNullOrWhiteSpace(baseBody)
            ? $"Seen {count} times."
            : $"{baseBody}\nSeen {count} times.";
    }

    private static string StripOccurrenceSuffix(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        // Avoid stacking "Seen N times." when we rewrite the body on each update.
        int idx = body.LastIndexOf("\nSeen ", StringComparison.Ordinal);
        if (idx >= 0 && body.EndsWith(" times.", StringComparison.Ordinal))
            return body.Substring(0, idx).TrimEnd();

        if (body.StartsWith("Seen ", StringComparison.Ordinal) && body.EndsWith(" times.", StringComparison.Ordinal))
            return "";

        return body;
    }

    private static string ComputeDedupKey(SpotifyPersistentIssue issue)
    {
        // Prefer an explicit DedupKey set by callers (status family / kind), so request-specific
        // bodies do not create a new banner row for every failing endpoint.
        if (!string.IsNullOrWhiteSpace(issue.DedupKey))
            return issue.DedupKey.Trim();

        string kind = (issue.Kind ?? "").Trim();
        string retry = issue.RetryUntilUtc.HasValue ? issue.RetryUntilUtc.Value.ToUniversalTime().ToString("O") : "";

        if (kind is "rate_limit" or "quota")
            return $"{kind}|{retry}";

        if (kind is "unauthorized" or "api_error" or "unexpected")
            return kind;

        string title = (issue.Title ?? "").Trim();
        string body = (issue.Body ?? "").Trim();
        return $"{kind}|{title}|{body}";
    }

    private static void ShowToastOnUiThread(string title, string body)
    {
        try
        {
            if (!Settings.ShowSpotifyToasts)
                return;
        }
        catch
        {
            // If settings are unavailable, fall through and attempt the toast.
        }

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

    /// <summary>Groups HTTP status codes so related failures share one toast/banner slot.</summary>
    private static string StatusFamily(int code) => code switch
    {
        >= 500 and <= 599 => "5xx",
        (int)HttpStatusCode.Unauthorized => "401",
        (int)HttpStatusCode.Forbidden => "403",
        (int)HttpStatusCode.NotFound => "404",
        >= 400 and <= 499 => "4xx",
        _ => "other"
    };

    internal static void NotifyApiException(string requestKey, APIException ex)
    {
        if (ShouldThrottleApiException(requestKey, ex, out int? status))
            return;

        int code = status ?? 0;
        string family = StatusFamily(code);
        string detail = FormatApiErrorBody(ex);

        string title = code switch
        {
            (int)HttpStatusCode.Forbidden => "Spotify access denied",
            (int)HttpStatusCode.NotFound => "Spotify not found",
            (int)HttpStatusCode.BadGateway => "Spotify temporarily unavailable",
            (int)HttpStatusCode.ServiceUnavailable => "Spotify temporarily unavailable",
            >= 500 and <= 599 => "Spotify temporarily unavailable",
            _ => "Spotify API error"
        };

        // Keep request name for diagnostics, but throttle / dedupe by status family only.
        string body = code > 0
            ? $"{detail}\n(Last request: {requestKey})"
            : $"{detail}\n(Last request: {requestKey})";

        NotifyOperational(
            title,
            body,
            throttleKey: "api:" + family,
            issueKind: "api_error",
            dedupKind: "api_error:" + family,
            perCategoryInterval: TimeSpan.FromMinutes(3),
            expiresAtUtc: DateTime.UtcNow.AddHours(2));
    }

    internal static void NotifyUnauthorized(string requestKey)
    {
        NotifyOperational(
            "Spotify session expired",
            $"Your Spotify login is no longer valid. Use Link in Songify to sign in again.\n(Last request: {requestKey})",
            throttleKey: "spotify:unauthorized",
            issueKind: "unauthorized",
            dedupKind: "unauthorized",
            perCategoryInterval: TimeSpan.FromMinutes(5),
            expiresAtUtc: DateTime.UtcNow.AddHours(12));
    }

    internal static void NotifyUnexpected(string requestKey, Exception ex)
    {
        string msg = ex?.Message ?? "Unknown error";
        if (msg.Length > 160)
            msg = msg.Substring(0, 157) + "...";

        NotifyOperational(
            "Spotify request failed",
            $"{msg}\n(Last request: {requestKey})",
            throttleKey: "spotify:unexpected",
            issueKind: "unexpected",
            dedupKind: "unexpected",
            perCategoryInterval: TimeSpan.FromMinutes(3),
            expiresAtUtc: DateTime.UtcNow.AddHours(1));
    }
}
