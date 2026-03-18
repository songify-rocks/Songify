using System.Windows.Media;
using Newtonsoft.Json;
using Songify_Slim.Models.Twitch;
using Songify_Slim.Util;
using Songify_Slim.Util.General;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Songify_Slim.Models;

public class RequestObject
{
    [JsonProperty("queueid")]
    public int Queueid { get; set; }

    [JsonProperty("uuid")]
    public string Uuid { get; set; }

    [JsonProperty("trackid")]
    public string Trackid { get; set; }

    [JsonProperty("artist")]
    public string Artist { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("length")]
    public string Length { get; set; }

    [JsonProperty("requester")]
    public string Requester { get; set; }

    [JsonIgnore] // ignore this field during JSON deserialization
    public int Played { get; set; }

    private string _albumcover;

    [JsonProperty("albumcover")]
    public string Albumcover
    {
        get => _albumcover;
        set
        {
            _albumcover = value;
            AlbumcoverImageSource = UrlToImageSourceConverter.FromUrl(value);
        }
    }

    [JsonIgnore]
    public ImageSource AlbumcoverImageSource { get; private set; }

    [JsonProperty("playerType")]
    public string PlayerType { get; set; }

    public bool IsLiked { get; set; } = false;
    public SimpleTwitchUser FullRequester { get; set; } = null;
}

public enum RequestPlayerType
{
    Spotify,
    Youtube
}