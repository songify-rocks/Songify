using System;
using System.Diagnostics;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Auth
{
  internal static class AuthUtil
  {
    public static bool OpenBrowser(string url)
    {
      try
      {
#if NETSTANDARD2_0
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          url = url.Replace("&", "^&");
          Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          Process.Start("open", url);
        }
#else
      url = url.Replace("&", "^&");
      Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
#endif
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
