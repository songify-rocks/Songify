using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models.YTMD
{
    class YTMYHCHResponse
    {
            public string Title { get; set; }
            public string AlternativeTitle { get; set; }
            public string Artist { get; set; }
            public long Views { get; set; }
            public DateTime UploadDate { get; set; }
            public string ImageSrc { get; set; }
            public bool IsPaused { get; set; }
            public int SongDuration { get; set; } // in seconds
            public int ElapsedSeconds { get; set; }
            public string Url { get; set; }
            public string VideoId { get; set; }
            public string PlaylistId { get; set; }
            public string MediaType { get; set; }
        
    }
}
