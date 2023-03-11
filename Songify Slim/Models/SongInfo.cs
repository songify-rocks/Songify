using System.Collections.Generic;
using SpotifyAPI.Web.Models;

namespace Songify_Slim.Models
{
    public class SongInfo
    {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Extra { get; set; }
        public List<Image> Albums { get; set; }
        public string Url { get; set; }
    }
}