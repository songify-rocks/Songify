using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Youtube.YTMYHCH;

public class Song
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public TimeSpan Length { get; set; }
    public int Pos { get; set; }          // position in the JSON array (current order)
    public string CoverUrl { get; set; }
    public bool IsCurrent { get; set; }
}

public static class QueueParser
{
    public static List<Song> ExtractSongs(string json)
    {
        List<Song> list = [];
        JToken root = JToken.Parse(json);
        JArray items = root["items"] as JArray ?? [];

        for (int i = 0; i < items.Count; i++)
        {
            JToken item = items[i];

            // 1) direct shape
            JToken r = item["playlistPanelVideoRenderer"];

            // 2) wrapped: items[i].playlistPanelVideoWrapperRenderer.primaryRenderer.playlistPanelVideoRenderer
            r ??= item["playlistPanelVideoWrapperRenderer"]?["primaryRenderer"]?["playlistPanelVideoRenderer"];

            // 3) wrapped counterpart: items[i].playlistPanelVideoWrapperRenderer.counterpart[0].counterpartRenderer.playlistPanelVideoRenderer
            r ??= item["playlistPanelVideoWrapperRenderer"]?["counterpart"]?
                    .FirstOrDefault()?["counterpartRenderer"]?["playlistPanelVideoRenderer"];

            if (r == null)
            {
                // Unknown shape – just skip this entry
                Logger.LogStr($"DEBUG[Ytmthch]: Skipping item[{i}] – no playlistPanelVideoRenderer found.");
                continue;
            }

            string id = (string)r["videoId"]
                        ?? (string)r.SelectToken("navigationEndpoint.watchEndpoint.videoId")
                        ?? "";

            string title = (string)r.SelectToken("title.runs[0].text") ?? "";
            string artist = (string)r.SelectToken("shortBylineText.runs[0].text") ?? "";
            string lenStr = (string)r.SelectToken("lengthText.runs[0].text") ?? "";

            TimeSpan length = ParseDuration(lenStr);
            bool isCurrent = r.Value<bool?>("selected") ?? false;

            // pick largest thumbnail
            JArray thumbs = r.SelectToken("thumbnail.thumbnails") as JArray;
            string cover = thumbs?
                .OrderByDescending(t => (int?)t["width"] ?? 0)
                .FirstOrDefault()?["url"]?.ToString() ?? "";

            list.Add(new Song
            {
                Id = id,
                Title = title,
                Artist = artist,
                Length = length,
                Pos = i,          // used by your ordering/patching logic
                CoverUrl = cover,
                IsCurrent = isCurrent
            });
        }

        return list;
    }



    private static TimeSpan ParseDuration(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return TimeSpan.Zero;
        if (text.Count(c => c == ':') == 1) text = "0:" + text; // mm:ss -> hh:mm:ss
        TimeSpan ts;
        return TimeSpan.TryParse(text, out ts) ? ts : TimeSpan.Zero;
    }
}