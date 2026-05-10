using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Toolkit.Uwp.Notifications;
using SpotifyAPI.Web;
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
    }

    public static void ClearRateLimitHint()
    {
        RateLimitHintChanged?.Invoke(null);
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
    }

    internal static void NotifyUnauthorized(string requestKey)
    {
        Notify(
            "Spotify session expired",
            "Your Spotify login is no longer valid. Use Link in Songify to sign in again.",
            "spotify:401:" + requestKey,
            TimeSpan.FromMinutes(5));
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
    }
}
