using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Pear.WebSocket
{
    public class PearSong
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("alternativeTitle")]
        public string? AlternativeTitle { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = string.Empty;

        [JsonPropertyName("artistUrl")]
        public string? ArtistUrl { get; set; }

        [JsonPropertyName("views")]
        public int Views { get; set; }

        [JsonPropertyName("uploadDate")]
        public DateTime UploadDate { get; set; }

        [JsonPropertyName("imageSrc")]
        public string ImageSrc { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public PearImage Image { get; set; } = new();

        [JsonPropertyName("isPaused")]
        public bool IsPaused { get; set; }

        [JsonPropertyName("songDuration")]
        public int SongDuration { get; set; }

        [JsonPropertyName("elapsedSeconds")]
        public int ElapsedSeconds { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("videoId")]
        public string VideoId { get; set; } = string.Empty;

        [JsonPropertyName("playlistId")]
        public string? PlaylistId { get; set; }

        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }
}