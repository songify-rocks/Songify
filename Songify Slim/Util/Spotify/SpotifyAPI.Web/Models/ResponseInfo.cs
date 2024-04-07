using System.Net;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class ResponseInfo
  {
    public WebHeaderCollection Headers { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    public static readonly ResponseInfo Empty = new();
  }
}