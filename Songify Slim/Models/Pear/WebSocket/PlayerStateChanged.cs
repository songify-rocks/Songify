using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Pear.WebSocket
{
    public class PlayerStateChangedMessage : PearMessage
    {
        [JsonPropertyName("isPlaying")]
        public bool IsPlaying { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }
    }
}