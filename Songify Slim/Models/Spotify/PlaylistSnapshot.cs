using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Songify_Slim.Models.Spotify;

public class PlaylistSnapshot
{
    public string PlaylistId { get; set; }
    public string Snapshot { get; set; }
}