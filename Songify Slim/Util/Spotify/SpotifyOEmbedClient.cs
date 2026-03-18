using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Spotify;

public sealed class SpotifyOEmbedClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _disposeHttp;

    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(6);

    public SpotifyOEmbedClient(HttpClient httpClient = null)
    {
        if (httpClient != null)
        {
            _http = httpClient;
            _disposeHttp = false;
        }
        else
        {
            _http = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            {
                BaseAddress = new Uri("https://open.spotify.com/")
            };

            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Songify/1.0");
            _disposeHttp = true;
        }
    }

    public async Task<SpotifyOEmbedResponse> GetAsync(
        string input,
        SpotifyItemType? typeHint = null,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.", nameof(input));

        string normalizedUrl = NormalizeToOpenSpotifyUrl(input, typeHint);

        string cacheKey = normalizedUrl;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (_cache.TryGetValue(cacheKey, out CacheItem cached) && cached.ExpiresAt > now)
            return cached.Value;

        string encoded = Uri.EscapeDataString(normalizedUrl);
        string endpoint = $"oembed?url={encoded}";

        using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using HttpResponseMessage res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!res.IsSuccessStatusCode)
            return null;

        string json = await res.Content.ReadAsStringAsync();

        SpotifyOEmbedResponse value = JsonConvert.DeserializeObject<SpotifyOEmbedResponse>(json);

        TimeSpan effectiveTtl = ttl ?? DefaultTtl;

        if (value != null && effectiveTtl > TimeSpan.Zero)
        {
            _cache[cacheKey] = new CacheItem(value, now.Add(effectiveTtl));
        }

        return value;
    }

    private static string NormalizeToOpenSpotifyUrl(string input, SpotifyItemType? typeHint)
    {
        input = input.Trim();

        if (Uri.TryCreate(input, UriKind.Absolute, out Uri uri) &&
            (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            return input;
        }

        if (input.StartsWith("spotify:", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = input.Split([':'], StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 3)
            {
                string type = parts[1];
                string id = parts[2];
                return $"https://open.spotify.com/{type}/{id}";
            }

            throw new ArgumentException("Invalid Spotify URI format.", nameof(input));
        }

        if (typeHint.HasValue)
        {
            string typePath = typeHint.Value switch
            {
                SpotifyItemType.Track => "track",
                SpotifyItemType.Album => "album",
                SpotifyItemType.Artist => "artist",
                SpotifyItemType.Playlist => "playlist",
                SpotifyItemType.Episode => "episode",
                SpotifyItemType.Show => "show",
                _ => throw new ArgumentOutOfRangeException(nameof(typeHint))
            };

            return $"https://open.spotify.com/{typePath}/{input}";
        }

        throw new ArgumentException(
            "Input must be an open.spotify.com URL, a spotify: URI, or an ID with a typeHint.",
            nameof(input));
    }

    public void Dispose()
    {
        if (_disposeHttp)
            _http.Dispose();
    }

    private sealed class CacheItem
    {
        public SpotifyOEmbedResponse Value { get; }
        public DateTimeOffset ExpiresAt { get; }

        public CacheItem(SpotifyOEmbedResponse value, DateTimeOffset expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }
    }
}

public enum SpotifyItemType
{
    Track,
    Album,
    Artist,
    Playlist,
    Episode,
    Show
}

public class SpotifyOEmbedResponse
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("html")]
    public string Html { get; set; }

    [JsonProperty("thumbnail_url")]
    public string ThumbnailUrl { get; set; }

    [JsonProperty("thumbnail_width")]
    public int? ThumbnailWidth { get; set; }

    [JsonProperty("thumbnail_height")]
    public int? ThumbnailHeight { get; set; }

    [JsonProperty("width")]
    public int? Width { get; set; }

    [JsonProperty("height")]
    public int? Height { get; set; }

    [JsonProperty("provider_name")]
    public string ProviderName { get; set; }

    [JsonProperty("provider_url")]
    public string ProviderUrl { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }
}