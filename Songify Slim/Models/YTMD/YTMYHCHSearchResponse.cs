using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models.YTMD
{
    class YTMYHCHSearchResponse
    {
        public string Title { get; set; }
        public string VideoId { get; set; }
        public List<string> Artists { get; set; } = new();
        public string Album { get; set; }
        public string Duration { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Views { get; set; }
    }
}
