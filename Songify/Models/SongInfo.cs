using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify.Models
{
    public class SongInfo
    {
        public string[] Artists { get; set; }
        public string Title { get; set; }
        public string AlbumCover { get; set; }
        public int DurationMS { get; set; }
    }
}
