using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    internal class QueueItem
    {
        public int queueid { get; set; }
        public string uuid { get; set; }
        public string trackid { get; set; }
        public string artist { get; set; }
        public string titel { get; set; }
        public string length { get; set; }
        public string requester { get; set; }
        public int played { get; set; }
    }
}
