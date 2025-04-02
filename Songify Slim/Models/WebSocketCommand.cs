using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    public class WebSocketCommand
    {
        public string Action { get; set; }
        public JsonElement? Data { get; set; } // Use System.Text.Json
    }
}
