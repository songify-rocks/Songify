using System;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums
{
  [Flags]
  public enum FollowType
  {
    [String("artist")]
    Artist = 1,

    [String("user")]
    User = 2
  }
}