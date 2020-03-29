using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    public class RequestObject
    {
        public string TrackID { get; set; }
        public string Artists { get; set; }
        public string Title { get; set; }
        public string Length { get; set; }
        public string Requester { get; set; }

    }
}
