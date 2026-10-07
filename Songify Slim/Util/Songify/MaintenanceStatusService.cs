using Newtonsoft.Json;
using Songify_Slim.Models.Responses;
using Songify_Slim.Util.General;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Songify;

/// <summary>
/// Reads https://maintenance.songify.rocks/status.json.
/// Only HTTP 200 with <c>maintenance: true</c> pauses API calls.
/// Timeout, DNS failure, a non-200, or invalid JSON always means normal operation,
/// including when a previous poll had maintenance on.
/// </summary>
internal static class MaintenanceStatusService
{
    public const string StatusUrl = "https://maintenance.songify.rocks/status.json";

    private const int NoticeIdPrefix = 0x4D410000;
    private const string DefaultMessage =
        "Songify is in maintenance. Hosted widgets are paused. Song requests in the app still work.";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly object StateLock = new();
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    private static Timer _timer;
    private static bool _active;
    private static string _message;
    private static DateTimeOffset? _until;
    private static bool _loggedFetchFailure;

    public static event Action Changed;

    public static bool IsInMaintenance
    {
        get
        {
            lock (StateLock)
                return _active && !WindowElapsed(_until);
        }
    }

    public static string Message
    {
        get
        {
            lock (StateLock)
                return IsActiveUnsafe() ? _message : null;
        }
    }

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

    public static async Task RefreshAsync()
    {
        if (!await Gate.WaitAsync(0).ConfigureAwait(false))
            return;

        try
        {
            using HttpResponseMessage response = await Http.GetAsync(StatusUrl).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogFetchFailure($"HTTP {(int)response.StatusCode}");
                ContinueNormally();
                return;
            }

            if (!TryParse(body, out MaintenanceFile file))
            {
                LogFetchFailure("invalid JSON");
                ContinueNormally();
                return;
            }

            _loggedFetchFailure = false;
            Publish(file.Maintenance, file.Message, file.Until);
        }
        catch (Exception ex)
        {
            LogFetchFailure(ex.Message);
            ContinueNormally();
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    /// A real API 503 whose body is the status document opens the circuit immediately.
    /// HTML or an empty 503 does not.
    /// </summary>
    public static void NoteTransportMaintenance(string body)
    {
        if (!TryParse(body, out MaintenanceFile file) || !file.Maintenance)
            return;

        Publish(true, file.Message, file.Until);
    }

    public static HttpResponseMessage CreateBlockedResponse()
    {
        string message;
        DateTimeOffset? until;
        lock (StateLock)
        {
            message = _message;
            until = _until;
        }

        string json = JsonConvert.SerializeObject(new
        {
            maintenance = true,
            message,
            until = until?.ToString("o")
        });

        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static bool IsNotice(Psa psa)
    {
        return psa != null && IsNoticeId(psa.Id);
    }

    public static bool IsNoticeId(int id)
    {
        return (id & unchecked((int)0xFFFF0000)) == NoticeIdPrefix;
    }

    public static Psa CreateNotice()
    {
        if (!IsInMaintenance)
            return null;

        string message = Message;
        if (string.IsNullOrWhiteSpace(message))
            message = DefaultMessage;

        int hash = StringComparer.Ordinal.GetHashCode(message);
        return new Psa
        {
            Id = NoticeIdPrefix | (hash & 0xFFFF),
            Author = "Songify",
            Severity = "High",
            MessageText = message,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsActive = true,
            Channel = "All"
        };
    }

    private static void Publish(bool maintenance, string message, DateTimeOffset? until)
    {
        bool active = maintenance && !WindowElapsed(until);
        string text = active
            ? (string.IsNullOrWhiteSpace(message) ? DefaultMessage : message.Trim())
            : null;

        bool changed;
        lock (StateLock)
        {
            changed = active != _active || !string.Equals(text, _message, StringComparison.Ordinal);
            _active = active;
            _message = text;
            _until = until;
        }

        if (!changed)
            return;

        if (active)
            Logger.Info(LogSource.Api, $"Songify maintenance mode is on. {text}");
        else
            Logger.Info(LogSource.Api, "Songify maintenance mode is off. API calls resume.");

        Changed?.Invoke();
    }

    /// <summary>
    /// Status could not be read. Leave API calls enabled. A dead status host must not
    /// freeze clients in maintenance.
    /// </summary>
    private static void ContinueNormally()
    {
        Publish(false, null, null);
    }

    private static bool WindowElapsed(DateTimeOffset? until)
    {
        return until.HasValue && until.Value <= DateTimeOffset.UtcNow;
    }

    private static bool IsActiveUnsafe()
    {
        return _active && !WindowElapsed(_until);
    }

    private static bool TryParse(string json, out MaintenanceFile file)
    {
        file = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        json = json.Trim();
        if (!json.StartsWith("{", StringComparison.Ordinal))
            return false;

        try
        {
            file = JsonConvert.DeserializeObject<MaintenanceFile>(json);
            return file != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void LogFetchFailure(string reason)
    {
        if (_loggedFetchFailure)
            return;

        _loggedFetchFailure = true;
        Logger.Warning(LogSource.Api, $"Maintenance status check failed: {reason}");
    }

    private sealed class MaintenanceFile
    {
        [JsonProperty("maintenance")]
        public bool Maintenance { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("until")]
        public DateTimeOffset? Until { get; set; }
    }
}
