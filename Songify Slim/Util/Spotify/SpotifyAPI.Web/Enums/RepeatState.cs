using System;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums
{
  [Flags]
  public enum RepeatState
  {
    [String("track")]
    Track = 1,

    [String("context")]
    Context = 2,

    [String("off")]
    Off = 4
  }
}