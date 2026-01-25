using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Models.Pear;
using static System.Net.WebRequestMethods;

namespace Songify_Slim.Util.Youtube.Youtube
{
    public static class YouTubeDataApiClient
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        public static async Task<PearSearch> GetMetaAsync(string apiKey, string videoId)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("apiKey missing");
            if (string.IsNullOrWhiteSpace(videoId)) throw new ArgumentException("videoId missing");

            string url =
                "https://www.googleapis.com/youtube/v3/videos" +
                "?part=snippet,contentDetails,statistics" +
                "&id=" + Uri.EscapeDataString(videoId) +
                "&key=" + Uri.EscapeDataString(apiKey);

            using HttpResponseMessage resp = await _http.GetAsync(url).ConfigureAwait(false);
            string json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                // keep the full JSON for caller to decide if fallback is appropriate
                throw new YouTubeApiException((int)resp.StatusCode, json);
            }

            JObject root = JObject.Parse(json);
            if (root["items"]?.FirstOrDefault() is not JObject item) return null;

            JObject snippet = item["snippet"] as JObject;
            JObject contentDetails = item["contentDetails"] as JObject;
            JObject statistics = item["statistics"] as JObject;

            string title = snippet?["title"]?.ToString();
            string channelTitle = snippet?["channelTitle"]?.ToString();

            string durationIso = contentDetails?["duration"]?.ToString();
            string duration = Iso8601ToHms(durationIso);

            string thumb = PickBestThumbnailUrl(snippet?["thumbnails"] as JObject)
                           ?? $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

            string views = statistics?["viewCount"]?.ToString();

            // artists best-effort (same heuristic as before)
            List<string> artists = GuessArtists(title, channelTitle);

            return new PearSearch()
            {
                VideoId = videoId,
                Title = title,
                Duration = duration,
                ThumbnailUrl = thumb,
                Views = views,
                Artists = artists,
                Album = null
            };
        }

        private static string PickBestThumbnailUrl(JObject thumbnails)
        {
            if (thumbnails == null) return null;
            string[] keys = ["maxres", "standard", "high", "medium", "default"];
            return keys.Select(k => thumbnails[k]?["url"]?.ToString()).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        }

        // YouTube API duration is ISO 8601 like "PT3M12S"
        private static string Iso8601ToHms(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return null;

            // minimal parser
            int h = 0, m = 0, s = 0;
            string t = iso.Trim().ToUpperInvariant();
            if (!t.StartsWith("PT")) return null;
            t = t.Substring(2);

            int ReadUntil(char suffix)
            {
                int idx = t.IndexOf(suffix);
                if (idx < 0) return -1;
                string num = t.Substring(0, idx);
                t = t.Substring(idx + 1);
                return int.TryParse(num, out int v) ? v : -1;
            }

            if (t.Contains("H")) { int v = ReadUntil('H'); if (v >= 0) h = v; }
            if (t.Contains("M")) { int v = ReadUntil('M'); if (v >= 0) m = v; }
            if (t.Contains("S")) { int v = ReadUntil('S'); if (v >= 0) s = v; }

            if (h > 0) return $"{h}:{m:D2}:{s:D2}";
            return $"{m}:{s:D2}";
        }

        private static List<string> GuessArtists(string title, string channelTitle)
        {
            List<string> artists = [];

            if (!string.IsNullOrWhiteSpace(title))
            {
                string[] seps = [" - ", " – ", " — ", " | "];
                foreach (string sep in seps)
                {
                    int idx = title.IndexOf(sep, StringComparison.Ordinal);
                    if (idx is <= 0 or >= 80) continue;
                    artists.Add(title.Substring(0, idx).Trim());
                    break;
                }
            }

            if (artists.Count == 0 && !string.IsNullOrWhiteSpace(channelTitle))
                artists.Add(channelTitle.Trim());

            return artists.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public class YouTubeApiException : Exception
    {
        public int StatusCode { get; }
        public string Body { get; }

        public YouTubeApiException(int statusCode, string body)
            : base($"YouTube API error {statusCode}")
        {
            StatusCode = statusCode;
            Body = body;
        }
    }
}