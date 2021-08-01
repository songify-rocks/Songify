using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify.Model
{
    public class Song
    {
        public string[] Artists { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        /// <summary>
        /// Duration in ms
        /// </summary>
        public int Duration { get; set; }
        public string SongURL { get; set; }
        public string CoverURL { get; set; }

        public Song(string[] artists, string title, string album = null, int duration = 0, string songUrl = null, string coverUrl = null)
        {
            Artists = artists;
            Title = title;
            Album = album;
            Duration = duration;
            SongURL = songUrl;
            CoverURL = coverUrl;
        }
    }
}
