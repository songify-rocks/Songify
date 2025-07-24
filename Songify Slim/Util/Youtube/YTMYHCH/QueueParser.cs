using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

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
        var list = new List<Song>();
        var root = JToken.Parse(json);
        var items = root["items"] as JArray ?? new JArray();

        for (int i = 0; i < items.Count; i++)
        {
            var r = items[i]["playlistPanelVideoRenderer"];
            if (r == null) continue;

            string id = (string)r["videoId"]
                         ?? (string)r.SelectToken("navigationEndpoint.watchEndpoint.videoId")
                         ?? "";

            string title = (string)r.SelectToken("title.runs[0].text") ?? "";
            string artist = (string)r.SelectToken("shortBylineText.runs[0].text") ?? "";
            string lenStr = (string)r.SelectToken("lengthText.runs[0].text") ?? "";

            TimeSpan length = ParseDuration(lenStr);

            bool isCurrent = r.Value<bool?>("selected") ?? false;

            // pick largest thumbnail
            var thumbs = r.SelectToken("thumbnail.thumbnails") as JArray;
            string cover = thumbs?
                .OrderByDescending(t => (int?)t["width"] ?? 0)
                .FirstOrDefault()?["url"]?.ToString() ?? "";

            list.Add(new Song
            {
                Id = id,
                Title = title,
                Artist = artist,
                Length = length,
                Pos = i,          // <-- this is what you should use for ordering/patching
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