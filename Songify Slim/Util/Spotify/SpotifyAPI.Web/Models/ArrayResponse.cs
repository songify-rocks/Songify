using System.Collections.Generic;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class ListResponse<T> : BasicModel
  {
    public List<T> List { get; set; }
  }
}