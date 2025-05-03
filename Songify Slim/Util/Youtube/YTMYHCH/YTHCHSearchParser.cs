
using Songify_Slim.Models.YTMD;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Songify_Slim.Util.Youtube.YTMYHCH
{
    class YTHCHSearchParser
    {
        public static YTMYHCHSearchResponse? ParseTopSongResult(string json)
        {
            JsonNode root = JsonNode.Parse(json);
            JsonNode contents = root?["contents"]?["tabbedSearchResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]
                             ?["sectionListRenderer"]?["contents"];

            if (contents is not JsonArray sections)
                return null;

            foreach (JsonNode section in sections)
            {
                JsonNode shelf = section?["musicShelfRenderer"];
                if (shelf == null) continue;

                JsonArray items = shelf["contents"]?.AsArray();
                if (items == null || items.Count == 0)
                    continue;

                JsonNode item = items[0]?["musicResponsiveListItemRenderer"];
                if (item == null) continue;

                YTMYHCHSearchResponse result = new YTMYHCHSearchResponse();

                // Title
                result.Title = item["flexColumns"]?[0]?["musicResponsiveListItemFlexColumnRenderer"]?["text"]?["runs"]?[0]?["text"]?.ToString();

                // VideoId
                result.VideoId = item["playlistItemData"]?["videoId"]?.ToString();

                // Thumbnail
                result.ThumbnailUrl = item["thumbnail"]?["musicThumbnailRenderer"]?["thumbnail"]?["thumbnails"]?[1]?["url"]?.ToString();

                // Duration and Views usually live in 2nd or 3rd column
                JsonArray allColumns = item["flexColumns"]?.AsArray();
                if (allColumns != null && allColumns.Count > 1)
                {
                    JsonArray runs = allColumns[1]?["musicResponsiveListItemFlexColumnRenderer"]?["text"]?["runs"]?.AsArray();
                    if (runs != null)
                    {
                        foreach (JsonNode run in runs)
                        {
                            string text = run?["text"]?.ToString()?.Trim();

                            if (string.IsNullOrWhiteSpace(text) || text == "•")
                                continue;

                            // Try to detect known patterns
                            if (text.Contains("views"))
                            {
                                result.Views = text;
                            }
                            else if (Regex.IsMatch(text, @"^\d+:\d+$"))
                            {
                                result.Duration = text;
                            }
                            else if (!text.Contains("Album"))
                            {
                                result.Artists.Add(text);
                            }
                            else
                            {
                                result.Album = text;
                            }
                        }
                    }
                }

                return result;
            }

            return null;
        }
    }
}
