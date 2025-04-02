using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    public class QueueAddData
    {
        public string Track { get; set; }
        public string Requester { get; set; } = "WebSocket";
    }
}
