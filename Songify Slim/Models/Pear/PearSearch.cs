using System.Collections.Generic;

namespace Songify_Slim.Models.Pear
{
    public class PearSearch
    {
        public string Title { get; set; }
        public string VideoId { get; set; }
        public List<string> Artists { get; set; } = [];
        public string Album { get; set; }
        public string Duration { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Views { get; set; }
    }
}