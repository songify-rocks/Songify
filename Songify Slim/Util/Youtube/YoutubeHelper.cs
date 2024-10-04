using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Songify_Slim.Util.General;

namespace Songify_Slim.Util.Youtube
{
    public static class YoutubeHelper
    {
        public static async Task<YouTubeOEmbedResponse> GetVideoInfoAsync(string videoId)
        {
            string requestUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        YouTubeOEmbedResponse videoInfo = JsonConvert.DeserializeObject<YouTubeOEmbedResponse>(json);
                        return videoInfo;
                    }
                    else
                    {
                        // Handle non-success status codes
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., network errors)
                    Console.WriteLine($"Exception: {ex.Message}");
                    return null;
                }
            }
        }
    }

    public class YouTubeOEmbedResponse
    {
        public string Title { get; set; }
        public string Author_Name { get; set; }
        public string Author_Url { get; set; }
        public string Type { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Version { get; set; }
        public string Provider_Name { get; set; }
        public string Provider_Url { get; set; }
        public string Thumbnail_Height { get; set; }
        public string Thumbnail_Width { get; set; }
        public string Thumbnail_Url { get; set; }
        public string Html { get; set; }
    }

    public class BaseMessage
    {
        public string Type { get; set; }
    }

    public class VideoPlayingMessage : BaseMessage
    {
        public string VideoId { get; set; }
        public Enums.YoutubePlayerState State { get; set; }
        public string Title { get; set; }
        public bool IsPlaylist { get; set; }
    }

    public class VideoEndedMessage : BaseMessage
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public bool IsPlaylist { get; set; }
    }

    public class PlaylistEndedMessage : BaseMessage
    {
        // No additional properties
    }
}
