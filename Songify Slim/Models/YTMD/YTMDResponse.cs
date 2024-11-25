using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models.YTMD
{
    using System.Collections.Generic;

    public class YTMDResponse
    {
        public Player Player { get; set; }
        public Video Video { get; set; }
        public string PlaylistId { get; set; }
    }

    public class Player
    {
        public TrackState TrackState { get; set; } // Enum for clarity
        public double VideoProgress { get; set; } // Assuming Number maps to double
        public double Volume { get; set; } // Assuming Number maps to double
        public bool AdPlaying { get; set; }
        public Queue Queue { get; set; }
    }

    public enum TrackState
    {
        Unknown = -1,
        Paused = 0,
        Playing = 1,
        Buffering = 2
    }

    public class Queue
    {
        public bool Autoplay { get; set; }
        public List<QueueItem> Items { get; set; }
        public List<QueueItem> AutomixItems { get; set; } // Refers to the same structure as `Items`
        public bool IsGenerating { get; set; }
        public bool IsInfinite { get; set; }
        public RepeatMode RepeatMode { get; set; } // Enum for repeat modes
        public int SelectedItemIndex { get; set; }
    }

    public enum RepeatMode
    {
        Unknown = -1,
        None = 0,
        All = 1,
        One = 2
    }

    public class QueueItem
    {
        public List<Thumbnail> Thumbnails { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Duration { get; set; }
        public bool Selected { get; set; }
        public string VideoId { get; set; }
        public List<QueueItem> Counterparts { get; set; } // Nullable array of QueueItem
    }

    public class Thumbnail
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Video
    {
        public string Author { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        public string AlbumId { get; set; }
        public LikeStatus? LikeStatus { get; set; } // Nullable enum for like status
        public List<Thumbnail> Thumbnails { get; set; }
        public int DurationSeconds { get; set; }
        public string Id { get; set; }
        public bool? IsLive { get; set; } // Nullable bool for optional property
        public VideoType? VideoType { get; set; } // Nullable enum for video types
        public bool? MetadataFilled { get; set; } // Nullable bool for optional property
    }

    public enum LikeStatus
    {
        Unknown = -1,
        Dislike = 0,
        Indifferent = 1,
        Like = 2
    }

    public enum VideoType
    {
        Unknown = -1,
        Audio = 0,
        Video = 1,
        Uploaded = 2,
        Podcast = 3
    }

}
