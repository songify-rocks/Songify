using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models
{
    public class VolumeData
    {
        public int Value { get; set; }

    }

    public class PlayPlaylistData
    {
        public string playlist { get; set; }
        public bool Shuffle { get; set; }
    }
}
