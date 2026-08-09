using Songify_Slim.Models.Blocklist;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logger = Songify_Slim.Util.General.Logger;

namespace Songify_Slim.Util.Songify
{
    public static class ArtistCsvImport
    {
        public static readonly string[] NameColumnHints =
        [
            "artist", "name", "artist_name", "artistname", "artist name", "title"
        ];

        public static readonly string[] IdColumnHints =
        [
            "id", "artist_id", "artistid", "artist id", "spotify_id", "spotifyid", "spotify id", "uri"
        ];

        public const string NoneColumn = "(None)";

        public static async Task<string> DownloadCsvAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Enter a valid http(s) URL to a raw CSV.");
            }

            using HttpClient http = new();
            http.Timeout = TimeSpan.FromSeconds(60);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Songify");
            using HttpResponseMessage response =
                await http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public static bool TryParse(string csvText, out List<string> headers, out List<string[]> rows, out string error)
        {
            headers = [];
            rows = [];
            error = null;

            if (string.IsNullOrWhiteSpace(csvText))
            {
                error = "CSV is empty.";
                return false;
            }

            if (csvText.Length > 0 && csvText[0] == '\uFEFF')
                csvText = csvText.Substring(1);

            List<string[]> parsed = ParseCsv(csvText);
            if (parsed.Count == 0)
            {
                error = "No rows found in CSV.";
                return false;
            }

            string[] headerRow = parsed[0];
            bool looksLikeHeader = headerRow.Any(h =>
            {
                string n = NormalizeHeader(h);
                return NameColumnHints.Any(x => NormalizeHeader(x) == n) ||
                       IdColumnHints.Any(x => NormalizeHeader(x) == n) ||
                       n is "artist" or "name" or "id";
            });

            if (looksLikeHeader)
            {
                headers = headerRow
                    .Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Column {i + 1}" : h.Trim())
                    .ToList();
                rows = parsed.Skip(1).Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))).ToList();
            }
            else
            {
                int width = parsed.Max(r => r.Length);
                headers = Enumerable.Range(1, width).Select(i => $"Column {i}").ToList();
                rows = parsed.Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))).ToList();
            }

            if (rows.Count == 0)
            {
                error = "CSV has headers but no data rows.";
                return false;
            }

            return true;
        }

        public static int GuessColumnIndex(IReadOnlyList<string> headers, IEnumerable<string> hints)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                string normalized = NormalizeHeader(headers[i]);
                foreach (string hint in hints)
                {
                    if (normalized == NormalizeHeader(hint))
                        return i;
                }
            }

            for (int i = 0; i < headers.Count; i++)
            {
                string normalized = NormalizeHeader(headers[i]);
                foreach (string hint in hints)
                {
                    string h = NormalizeHeader(hint);
                    if (normalized.Contains(h) || h.Contains(normalized))
                        return i;
                }
            }

            return -1;
        }

        public static int ResolveColumnIndex(IReadOnlyList<string> headers, string configuredHeader, IEnumerable<string> autoHints)
        {
            if (string.IsNullOrWhiteSpace(configuredHeader) ||
                string.Equals(configuredHeader.Trim(), NoneColumn, StringComparison.OrdinalIgnoreCase))
                return -1;

            string wanted = NormalizeHeader(configuredHeader);
            for (int i = 0; i < headers.Count; i++)
            {
                if (NormalizeHeader(headers[i]) == wanted)
                    return i;
            }

            // Fall back to auto-guess when the saved header vanished from the CSV.
            return GuessColumnIndex(headers, autoHints);
        }

        public static string GetCell(string[] row, int index)
        {
            if (index < 0 || row == null || index >= row.Length)
                return "";
            return row[index] ?? "";
        }

        public static string NormalizeSpotifyArtistId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string id = raw.Trim();
            const string uriPrefix = "spotify:artist:";
            if (id.StartsWith(uriPrefix, StringComparison.OrdinalIgnoreCase))
                return id.Substring(uriPrefix.Length).Trim();

            const string urlMarker = "open.spotify.com/artist/";
            int marker = id.IndexOf(urlMarker, StringComparison.OrdinalIgnoreCase);
            if (marker >= 0)
            {
                string rest = id.Substring(marker + urlMarker.Length);
                int end = rest.IndexOfAny(new[] { '?', '/', '#' });
                return (end >= 0 ? rest.Substring(0, end) : rest).Trim();
            }

            return id;
        }

        public static string NormalizeHeader(string value) =>
            (value ?? "").Trim().ToLowerInvariant().Replace("_", " ").Replace("-", " ");

        public static ArtistCsvMergeResult MergeRows(
            IList<BlockedArtist> target,
            IEnumerable<string[]> rows,
            int nameColumnIndex,
            int idColumnIndex)
        {
            HashSet<string> existingKeys = new(
                target.Select(a => a.Key).Where(k => !string.IsNullOrWhiteSpace(k)),
                StringComparer.OrdinalIgnoreCase);

            int added = 0;
            int skippedEmpty = 0;
            int skippedDuplicate = 0;

            foreach (string[] row in rows)
            {
                string name = GetCell(row, nameColumnIndex)?.Trim() ?? "";
                string id = NormalizeSpotifyArtistId(GetCell(row, idColumnIndex));

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id))
                {
                    skippedEmpty++;
                    continue;
                }

                BlockedArtist artist = new()
                {
                    Name = name,
                    Id = string.IsNullOrWhiteSpace(id) ? null : id
                };

                if (string.IsNullOrWhiteSpace(artist.Key) || !existingKeys.Add(artist.Key))
                {
                    skippedDuplicate++;
                    continue;
                }

                target.Add(artist);
                added++;
            }

            return new ArtistCsvMergeResult(added, skippedDuplicate, skippedEmpty);
        }

        /// <summary>
        /// Downloads the configured CSV URL, maps columns, and merges new artists into the blocklist.
        /// </summary>
        public static async Task<ArtistCsvSyncResult> SyncFromSettingsAsync(CancellationToken cancellationToken = default)
        {
            string url = Settings.ArtistBlocklistSyncUrl?.Trim();
            if (string.IsNullOrWhiteSpace(url))
                return ArtistCsvSyncResult.Failed("No artist blocklist sync URL configured.");

            try
            {
                string csvText = await DownloadCsvAsync(url, cancellationToken).ConfigureAwait(false);
                if (!TryParse(csvText, out List<string> headers, out List<string[]> rows, out string error))
                    return ArtistCsvSyncResult.Failed(error);

                int nameIdx = ResolveColumnIndex(headers, Settings.ArtistBlocklistSyncNameColumn, NameColumnHints);
                int idIdx = ResolveColumnIndex(headers, Settings.ArtistBlocklistSyncIdColumn, IdColumnHints);

                // If name mapping is missing entirely, prefer auto-guess rather than failing hard.
                if (nameIdx < 0 && idIdx < 0)
                {
                    nameIdx = GuessColumnIndex(headers, NameColumnHints);
                    idIdx = GuessColumnIndex(headers, IdColumnHints);
                }

                if (nameIdx < 0 && idIdx < 0)
                    return ArtistCsvSyncResult.Failed("Could not resolve Name or Id columns for the CSV.");

                List<BlockedArtist> list = Settings.ArtistBlacklist;
                ArtistCsvMergeResult merge = MergeRows(list, rows, nameIdx, idIdx);
                Settings.ArtistBlacklist = list;
                Settings.ArtistBlocklistSyncLastUtc = DateTime.UtcNow.ToString("o");

                Logger.Info(LogSource.Spotify,
                    $"Artist blocklist sync: added {merge.Added}, skipped {merge.SkippedDuplicate} duplicates, {merge.SkippedEmpty} empty (URL).");

                return ArtistCsvSyncResult.Ok(merge, rows.Count);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogSource.Core, "Artist blocklist CSV sync failed", ex);
                return ArtistCsvSyncResult.Failed(ex.Message);
            }
        }

        public static List<string[]> ParseCsv(string text)
        {
            List<string[]> result = [];
            List<string> current = [];
            StringBuilder field = new();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        current.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        current.Add(field.ToString());
                        field.Clear();
                        result.Add(current.ToArray());
                        current = [];
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || current.Count > 0)
            {
                current.Add(field.ToString());
                result.Add(current.ToArray());
            }

            return result;
        }
    }

    public sealed class ArtistCsvColumnOption
    {
        public int Index { get; set; }
        public string Header { get; set; } = "";
        public string Display { get; set; } = "";
    }

    public readonly struct ArtistCsvMergeResult
    {
        public ArtistCsvMergeResult(int added, int skippedDuplicate, int skippedEmpty)
        {
            Added = added;
            SkippedDuplicate = skippedDuplicate;
            SkippedEmpty = skippedEmpty;
        }

        public int Added { get; }
        public int SkippedDuplicate { get; }
        public int SkippedEmpty { get; }
    }

    public sealed class ArtistCsvSyncResult
    {
        private ArtistCsvSyncResult(bool success, string message, ArtistCsvMergeResult merge, int rowCount)
        {
            Success = success;
            Message = message;
            Merge = merge;
            RowCount = rowCount;
        }

        public bool Success { get; }
        public string Message { get; }
        public ArtistCsvMergeResult Merge { get; }
        public int RowCount { get; }

        public static ArtistCsvSyncResult Ok(ArtistCsvMergeResult merge, int rowCount) =>
            new(true,
                $"Added {merge.Added} artist(s). Skipped {merge.SkippedDuplicate} duplicate(s), {merge.SkippedEmpty} empty row(s).",
                merge,
                rowCount);

        public static ArtistCsvSyncResult Failed(string message) =>
            new(false, message ?? "Sync failed.", default, 0);
    }
}
