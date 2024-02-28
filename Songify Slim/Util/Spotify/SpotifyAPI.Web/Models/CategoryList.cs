using Newtonsoft.Json;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models
{
  public class CategoryList : BasicModel
  {
    [JsonProperty("categories")]
    public Paging<Category> Categories { get; set; }
  }
}