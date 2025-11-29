using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Pear.WebSocket
{
    public abstract class PearMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}