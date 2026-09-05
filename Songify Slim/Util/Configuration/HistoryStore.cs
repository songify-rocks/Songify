using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Songify_Slim.Util.General;
using Songify_Slim.Views;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Songify_Slim.Util.Configuration;

/// <summary>
/// Local song history stored as YAML (<c>history.yaml</c>), grouped by local calendar date.
/// Migrates legacy XML <c>history.shr</c> once, then deletes it.
/// </summary>
public static class HistoryStore
{
    private static readonly object Gate = new();
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static HistoryDocument _cache;
    private static int _migrationRunning;

    public static string FilePath => Path.Combine(AppPaths.GetAppDirectory(), "history.yaml");
    public static string LegacyFilePath => Path.Combine(AppPaths.GetAppDirectory(), "history.shr");

    public static bool NeedsLegacyMigration
    {
        get { lock (Gate) return File.Exists(LegacyFilePath); }
    }

    /// <summary>
    /// Converts <c>history.shr</c> → <c>history.yaml</c> if needed. Shows a short progress dialog when <paramref name="owner"/> is set.
    /// </summary>
    public static async Task MigrateLegacyIfNeededAsync(Window owner = null)
    {
        if (!NeedsLegacyMigration)
            return;

        if (Interlocked.CompareExchange(ref _migrationRunning, 1, 0) != 0)
        {
            // Another migration is in progress — wait briefly for it to finish.
            for (int i = 0; i < 200 && Volatile.Read(ref _migrationRunning) != 0; i++)
                await Task.Delay(50);
            return;
        }

        Window_HistoryMigration dialog = null;
        try
        {
            if (owner != null)
            {
                await owner.Dispatcher.InvokeAsync(() =>
                {
                    dialog = new Window_HistoryMigration
                    {
                        Owner = owner,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    dialog.Show();
                });
                // Let the dialog paint before heavy work.
                await Task.Delay(50);
            }

            await Task.Run(() =>
            {
                lock (Gate)
                {
                    MigrateLegacy_NoLock();
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
        finally
        {
            if (dialog != null)
            {
                try
                {
                    await dialog.Dispatcher.InvokeAsync(() =>
                    {
                        try { dialog.Close(); } catch { /* ignore */ }
                    });
                }
                catch { /* ignore */ }
            }

            Interlocked.Exchange(ref _migrationRunning, 0);
        }
    }

    /// <summary>Silent sync migration for writers that may run before UI is ready.</summary>
    public static void EnsureReady()
    {
        lock (Gate)
        {
            if (File.Exists(LegacyFilePath))
                MigrateLegacy_NoLock();
            else
                EnsureLoaded_NoLock();
        }
    }

    public static HistoryDocument LoadCopy()
    {
        lock (Gate)
        {
            EnsureReady_NoLock();
            return Clone(_cache);
        }
    }

    public static IReadOnlyList<(string DateKey, DateTime Date, int SongCount)> GetDateSummaries()
    {
        lock (Gate)
        {
            EnsureReady_NoLock();
            List<(string DateKey, DateTime Date, int SongCount)> list = [];
            foreach (KeyValuePair<string, List<HistorySongRecord>> kv in _cache.Dates)
            {
                if (!TryParseDateKey(kv.Key, out DateTime dt))
                    continue;
                int count = kv.Value?.Count(s => s != null && !string.IsNullOrWhiteSpace(s.Song)) ?? 0;
                list.Add((kv.Key, dt.Date, count));
            }

            return list.OrderByDescending(x => x.Date).ToList();
        }
    }

    public static IReadOnlyList<HistorySongRecord> GetSongsForDate(string dateKey)
    {
        lock (Gate)
        {
            EnsureReady_NoLock();
            if (string.IsNullOrEmpty(dateKey) ||
                !_cache.Dates.TryGetValue(dateKey, out List<HistorySongRecord> songs) ||
                songs == null)
                return [];

            return songs
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Song))
                .OrderByDescending(s => s.Time)
                .Select(s => new HistorySongRecord { Time = s.Time, Song = s.Song })
                .ToList();
        }
    }

    public static void AppendSong(string song, long unixTimestamp)
    {
        if (string.IsNullOrWhiteSpace(song))
            return;

        song = song.Trim();
        lock (Gate)
        {
            EnsureReady_NoLock();
            string dateKey = ToDateKey(DateTime.Now);
            if (!_cache.Dates.TryGetValue(dateKey, out List<HistorySongRecord> day) || day == null)
            {
                day = [];
                _cache.Dates[dateKey] = day;
            }

            HistorySongRecord last = day.LastOrDefault();
            if (last != null && string.Equals(last.Song, song, StringComparison.Ordinal))
                return;

            day.Add(new HistorySongRecord { Time = unixTimestamp, Song = song });
            Save_NoLock();
        }
    }

    public static void DeleteDate(string dateKey)
    {
        if (string.IsNullOrEmpty(dateKey))
            return;

        lock (Gate)
        {
            EnsureReady_NoLock();
            _cache.Dates.Remove(dateKey);
            Save_NoLock();
        }
    }

    public static void DeleteSong(string dateKey, long unixTimestamp)
    {
        if (string.IsNullOrEmpty(dateKey))
            return;

        lock (Gate)
        {
            EnsureReady_NoLock();
            if (!_cache.Dates.TryGetValue(dateKey, out List<HistorySongRecord> day) || day == null)
                return;

            day.RemoveAll(s => s != null && s.Time == unixTimestamp);
            if (day.Count == 0)
                _cache.Dates.Remove(dateKey);
            Save_NoLock();
        }
    }

    public static string ToDateKey(DateTime localDate) => localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static bool TryParseDateKey(string key, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        // Legacy key without d_ prefix: dd.MM.yyyy
        if (DateTime.TryParseExact(key, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        return false;
    }

    private static void EnsureReady_NoLock()
    {
        if (File.Exists(LegacyFilePath))
            MigrateLegacy_NoLock();
        else
            EnsureLoaded_NoLock();
    }

    private static void EnsureLoaded_NoLock()
    {
        if (_cache != null)
            return;

        if (!File.Exists(FilePath))
        {
            _cache = new HistoryDocument();
            Save_NoLock();
            return;
        }

        try
        {
            string yaml = File.ReadAllText(FilePath);
            _cache = Deserializer.Deserialize<HistoryDocument>(yaml) ?? new HistoryDocument();
            _cache.Dates ??= new Dictionary<string, List<HistorySongRecord>>(StringComparer.Ordinal);
            NormalizeDateKeys_NoLock();
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
            _cache = new HistoryDocument();
        }
    }

    private static void MigrateLegacy_NoLock()
    {
        if (!File.Exists(LegacyFilePath))
        {
            EnsureLoaded_NoLock();
            return;
        }

        HistoryDocument migrated = ReadLegacyShr(LegacyFilePath);

        // Load existing yaml (if any) and merge — prefer keeping both day lists.
        HistoryDocument existing = new();
        if (File.Exists(FilePath))
        {
            try
            {
                string yaml = File.ReadAllText(FilePath);
                existing = Deserializer.Deserialize<HistoryDocument>(yaml) ?? new HistoryDocument();
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        existing.Dates ??= new Dictionary<string, List<HistorySongRecord>>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<HistorySongRecord>> kv in migrated.Dates)
        {
            if (!existing.Dates.TryGetValue(kv.Key, out List<HistorySongRecord> day) || day == null)
            {
                existing.Dates[kv.Key] = kv.Value ?? [];
                continue;
            }

            HashSet<long> times = day.Where(s => s != null).Select(s => s.Time).ToHashSet();
            foreach (HistorySongRecord song in kv.Value ?? [])
            {
                if (song == null || times.Contains(song.Time))
                    continue;
                day.Add(song);
                times.Add(song.Time);
            }
        }

        _cache = existing;
        NormalizeDateKeys_NoLock();
        Save_NoLock();

        try
        {
            File.Delete(LegacyFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }
    }

    private static HistoryDocument ReadLegacyShr(string path)
    {
        var doc = new HistoryDocument();
        try
        {
            XDocument xdoc = XDocument.Load(path);
            if (xdoc.Root == null)
                return doc;

            foreach (XElement dayElem in xdoc.Root.Elements())
            {
                string raw = dayElem.Name.LocalName;
                if (raw.StartsWith("d_", StringComparison.Ordinal))
                    raw = raw[2..];

                if (!TryParseDateKey(raw, out DateTime dt))
                    continue;

                string dateKey = ToDateKey(dt);
                List<HistorySongRecord> songs = [];
                foreach (XElement songElem in dayElem.Elements("Song"))
                {
                    string timeVal = songElem.Attribute("Time")?.Value;
                    if (string.IsNullOrEmpty(timeVal))
                        continue;
                    if (!double.TryParse(timeVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double unix) &&
                        !double.TryParse(timeVal, out unix))
                        continue;

                    string name = songElem.Value?.Trim() ?? "";
                    if (string.IsNullOrEmpty(name))
                        continue;

                    songs.Add(new HistorySongRecord
                    {
                        Time = (long)unix,
                        Song = name
                    });
                }

                if (songs.Count == 0)
                    continue;

                if (!doc.Dates.TryGetValue(dateKey, out List<HistorySongRecord> existing))
                    doc.Dates[dateKey] = songs;
                else
                    existing.AddRange(songs);
            }
        }
        catch (Exception ex)
        {
            Logger.LogExc(ex);
        }

        return doc;
    }

    private static void NormalizeDateKeys_NoLock()
    {
        if (_cache?.Dates == null || _cache.Dates.Count == 0)
            return;

        var normalized = new Dictionary<string, List<HistorySongRecord>>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<HistorySongRecord>> kv in _cache.Dates)
        {
            string key = kv.Key;
            if (TryParseDateKey(key, out DateTime dt))
                key = ToDateKey(dt);

            if (!normalized.TryGetValue(key, out List<HistorySongRecord> list))
            {
                normalized[key] = kv.Value ?? [];
            }
            else if (kv.Value != null)
            {
                HashSet<long> times = list.Where(s => s != null).Select(s => s.Time).ToHashSet();
                foreach (HistorySongRecord song in kv.Value)
                {
                    if (song == null || times.Contains(song.Time))
                        continue;
                    list.Add(song);
                    times.Add(song.Time);
                }
            }
        }

        _cache.Dates = normalized;
    }

    private static void Save_NoLock()
    {
        _cache ??= new HistoryDocument();
        _cache.Dates ??= new Dictionary<string, List<HistorySongRecord>>(StringComparer.Ordinal);

        string dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        string yaml = Serializer.Serialize(_cache);
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, yaml);
        if (File.Exists(FilePath))
            File.Replace(temp, FilePath, null);
        else
            File.Move(temp, FilePath);
    }

    private static HistoryDocument Clone(HistoryDocument source)
    {
        var clone = new HistoryDocument();
        if (source?.Dates == null)
            return clone;

        foreach (KeyValuePair<string, List<HistorySongRecord>> kv in source.Dates)
        {
            clone.Dates[kv.Key] = (kv.Value ?? [])
                .Where(s => s != null)
                .Select(s => new HistorySongRecord { Time = s.Time, Song = s.Song })
                .ToList();
        }

        return clone;
    }
}

public sealed class HistoryDocument
{
    public Dictionary<string, List<HistorySongRecord>> Dates { get; set; } =
        new(StringComparer.Ordinal);
}

public sealed class HistorySongRecord
{
    public long Time { get; set; }
    public string Song { get; set; }
}
