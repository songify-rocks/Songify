using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Pear.WebSocket
{
    public class VideoChangedMessage : PearMessage
    {
        [JsonPropertyName("song")]
        public PearSong Song { get; set; } = new();

        [JsonPropertyName("position")]
        public int Position { get; set; }
    }
}