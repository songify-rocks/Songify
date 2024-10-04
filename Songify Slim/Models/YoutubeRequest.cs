﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    public class YoutubeRequest
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Length { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Requester { get; set; }
    }
}
