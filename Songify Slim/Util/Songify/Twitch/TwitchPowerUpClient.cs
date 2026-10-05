using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Songify_Slim.Util.Configuration;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Songify.Twitch
{
    /// <summary>
    /// A custom Power-up the broadcaster created. Viewers redeem these with Bits.
    /// Twitch currently only lets apps list them and receive redemptions.
    /// </summary>
    internal sealed class TwitchPowerUp
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public int Bits { get; init; }
        public string BackgroundColor { get; init; }
        public string ImageUrl { get; init; }
    }

    /// <summary>
    /// Reads custom Power-ups from Helix (<c>GET /helix/bits/custom_power_ups</c>).
    /// </summary>
    internal static class TwitchPowerUpClient
    {
        private static readonly object Gate = new();
        private static readonly HashSet<string> KnownIds = new(StringComparer.Ordinal);

        public static bool IsKnown(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            lock (Gate)
                return KnownIds.Contains(id);
        }

        public static void Remember(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            lock (Gate)
                KnownIds.Add(id);
        }

        /// <summary>
        /// Power-ups are paid with Bits. A pre-recorded song in exchange for Bits is not allowed,
        /// so these ids cannot stay assigned as song requests.
        /// </summary>
        public static void DropSongRequestAssignments(IEnumerable<string> powerUpIds)
        {
            if (powerUpIds == null)
                return;

            List<string> current = Settings.TwRewardId;
            if (current == null || current.Count == 0)
                return;

            HashSet<string> ids = new(powerUpIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            if (ids.Count == 0 || !current.Any(ids.Contains))
                return;

            Settings.TwRewardId = current.Where(id => !ids.Contains(id)).ToList();
            Logger.Info(LogSource.Twitch,
                "Removed Power-up song-request assignments. Twitch's Bits policy does not allow a pre-recorded song in exchange for Bits.");
        }

        /// <summary>
        /// Lists the broadcaster's custom Power-ups.
        /// <c>null</c> means the request failed and callers should keep existing assignments.
        /// An empty list means the channel has none (or Twitch refused because the channel is not monetized).
        /// </summary>
        public static async Task<List<TwitchPowerUp>> GetCustomPowerUpsAsync()
        {
            string broadcasterId = Settings.TwitchChannelId;
            if (string.IsNullOrWhiteSpace(broadcasterId))
                broadcasterId = Settings.TwitchUser?.Id;

            if (string.IsNullOrWhiteSpace(broadcasterId) || string.IsNullOrWhiteSpace(Settings.TwitchAccessToken))
                return null;

            try
            {
                using HttpClient http = new();
                using HttpRequestMessage request = new(
                    HttpMethod.Get,
                    "https://api.twitch.tv/helix/bits/custom_power_ups?broadcaster_id=" + Uri.EscapeDataString(broadcasterId));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.TwitchAccessToken);
                request.Headers.Add("Client-Id", TwitchHandler.ClientId);

                using HttpResponseMessage response = await http.SendAsync(request).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if ((int)response.StatusCode == 403)
                {
                    Logger.Info(LogSource.Twitch,
                        "Custom Power-ups are unavailable for this channel (Twitch requires a monetized channel).");
                    ReplaceKnown([]);
                    return [];
                }

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning(LogSource.Twitch,
                        $"Get Custom Power-ups failed: {(int)response.StatusCode} {response.StatusCode}. {Trim(body)}");
                    return null;
                }

                List<TwitchPowerUp> powerUps = Parse(body);
                ReplaceKnown(powerUps.ConvertAll(p => p.Id));
                return powerUps;
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Twitch, "Get Custom Power-ups failed.", ex);
                return null;
            }
        }

        private static void ReplaceKnown(IEnumerable<string> ids)
        {
            lock (Gate)
            {
                KnownIds.Clear();
                if (ids == null)
                    return;

                foreach (string id in ids)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        KnownIds.Add(id);
                }
            }
        }

        private static List<TwitchPowerUp> Parse(string body)
        {
            List<TwitchPowerUp> powerUps = [];
            if (string.IsNullOrWhiteSpace(body))
                return powerUps;

            using JsonDocument doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Array)
                return powerUps;

            foreach (JsonElement item in data.EnumerateArray())
            {
                string id = ReadString(item, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                int bits = 0;
                if (item.TryGetProperty("bits", out JsonElement bitsEl) && bitsEl.TryGetInt32(out int parsed))
                    bits = parsed;

                powerUps.Add(new TwitchPowerUp
                {
                    Id = id,
                    Title = ReadString(item, "title"),
                    Bits = bits,
                    BackgroundColor = ReadString(item, "background_color"),
                    ImageUrl = ReadImageUrl(item)
                });
            }

            return powerUps;
        }

        private static string ReadImageUrl(JsonElement item)
        {
            string custom = ReadNestedString(item, "image", "url_1x");
            if (!string.IsNullOrWhiteSpace(custom))
                return custom;

            return ReadNestedString(item, "default_image", "url_1x");
        }

        private static string ReadNestedString(JsonElement item, string objectName, string propertyName)
        {
            if (!item.TryGetProperty(objectName, out JsonElement child) || child.ValueKind != JsonValueKind.Object)
                return "";

            return ReadString(child, propertyName);
        }

        private static string ReadString(JsonElement item, string name)
        {
            if (!item.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
                return "";

            return value.GetString() ?? "";
        }

        private static string Trim(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "";

            body = body.Replace("\r", " ").Replace("\n", " ").Trim();
            return body.Length <= 240 ? body : body.Substring(0, 237) + "...";
        }
    }
}
