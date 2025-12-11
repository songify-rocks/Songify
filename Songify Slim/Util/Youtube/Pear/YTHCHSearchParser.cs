using Songify_Slim.Models.Pear;
using Swan;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Songify_Slim.Util.Youtube.YTMYHCH
{
    internal class YTHCHSearchParser
    {
        public static PearSearch? ParseTopSongResult(string json)
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(json);
            }
            catch
            {
                return null;
            }

            JsonNode? contents = root?["contents"]?["tabbedSearchResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]
                                 ?["sectionListRenderer"]?["contents"];

            if (contents is not JsonArray sections)
                return null;

            //
            // 1) Try the big top card (musicCardShelfRenderer)
            //
            foreach (JsonNode? section in sections)
            {
                JsonNode? card = section?["musicCardShelfRenderer"];
                if (card is null) continue;

                string? videoId = card["title"]?["runs"]?[0]?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString();
                if (string.IsNullOrWhiteSpace(videoId))
                    continue;

                PearSearch result = new()
                {
                    Title = card["title"]?["runs"]?[0]?["text"]?.ToString(),
                    VideoId = videoId,
                    ThumbnailUrl = card["thumbnail"]?["musicThumbnailRenderer"]?["thumbnail"]?["thumbnails"]
                                       ?.AsArray()?.LastOrDefault()?["url"]?.ToString()
                };

                // Optional: parse subtitle for artist + duration
                JsonArray? subtitleRuns = card["subtitle"]?["runs"]?.AsArray();
                if (subtitleRuns != null)
                {
                    foreach (JsonNode? run in subtitleRuns)
                    {
                        string? text = run?["text"]?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(text) || text == "•")
                            continue;

                        if (Regex.IsMatch(text, @"^\d+:\d+$"))
                        {
                            result.Duration ??= text;
                        }
                        else if (!text.Equals("Song", StringComparison.OrdinalIgnoreCase) &&
                                 !text.Equals("Video", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Artists.Add(text);
                        }
                    }
                }

                return result;
            }

            //
            // 2) Fallback: scan shelves for the first playable item
            //
            foreach (JsonNode? section in sections)
            {
                JsonNode? shelf = section?["musicShelfRenderer"];
                if (shelf == null) continue;

                JsonArray? items = shelf["contents"]?.AsArray();
                if (items == null || items.Count == 0)
                    continue;

                foreach (JsonNode? itemNode in items)
                {
                    JsonNode? item = itemNode?["musicResponsiveListItemRenderer"];
                    if (item == null) continue;

                    // Prefer playlistItemData.videoId
                    string? videoId = item["playlistItemData"]?["videoId"]?.ToString();

                    // Fallback: navigationEndpoint on the title
                    if (string.IsNullOrWhiteSpace(videoId))
                    {
                        videoId = item["flexColumns"]?[0]?["musicResponsiveListItemFlexColumnRenderer"]?["text"]?
                                      ["runs"]?[0]?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString();
                    }

                    if (string.IsNullOrWhiteSpace(videoId))
                        continue; // artist row / something non-playable

                    PearSearch result = new()
                    {
                        Title = item["flexColumns"]?[0]?["musicResponsiveListItemFlexColumnRenderer"]?["text"]?
                                    ["runs"]?[0]?["text"]?.ToString(),
                        VideoId = videoId,
                        ThumbnailUrl = item["thumbnail"]?["musicThumbnailRenderer"]?["thumbnail"]?["thumbnails"]
                                           ?.AsArray()?.LastOrDefault()?["url"]?.ToString()
                    };

                    // Parse additional metadata (duration, views/plays, artists, album)
                    JsonArray? allColumns = item["flexColumns"]?.AsArray();
                    if (allColumns != null)
                    {
                        for (int col = 1; col < allColumns.Count; col++)
                        {
                            JsonArray? runs = allColumns[col]?["musicResponsiveListItemFlexColumnRenderer"]?
                                                  ["text"]?["runs"]?.AsArray();
                            if (runs == null) continue;

                            foreach (JsonNode? run in runs)
                            {
                                string? text = run?["text"]?.ToString()?.Trim();
                                if (string.IsNullOrWhiteSpace(text) || text == "•")
                                    continue;

                                if (Regex.IsMatch(text, @"^\d+:\d+$"))
                                {
                                    result.Duration ??= text;
                                }
                                else if (text.IndexOf("views", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         text.IndexOf("plays", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    result.Views ??= text;
                                }
                                else if (text.IndexOf("Album", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    result.Album ??= text;
                                }
                                else if (!text.Equals("Song", StringComparison.OrdinalIgnoreCase) &&
                                         !text.Equals("Video", StringComparison.OrdinalIgnoreCase))
                                {
                                    result.Artists.Add(text);
                                }
                            }
                        }
                    }

                    return result;
                }
            }

            return null;
        }
    }
}