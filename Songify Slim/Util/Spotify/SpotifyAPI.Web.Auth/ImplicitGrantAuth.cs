using System.Threading.Tasks;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Enums;
using Songify_Slim.Util.Spotify.SpotifyAPI.Web.Models;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace Songify_Slim.Util.Spotify.SpotifyAPI.Web.Auth
{
  public class ImplicitGrantAuth : SpotifyAuthServer<Token>
  {
    public ImplicitGrantAuth(string clientId, string redirectUri, string serverUri, Scope scope = Scope.None, string state = "") : base("token", "ImplicitGrantAuth", redirectUri, serverUri, scope, state)
    {
      ClientId = clientId;
    }

    protected override void AdaptWebServer(WebServer webServer)
    {
      webServer.Module<WebApiModule>().RegisterController<ImplicitGrantAuthController>();
    }
  }

  public class ImplicitGrantAuthController : WebApiController
  {
    [WebApiHandler(HttpVerbs.Get, "/auth")]
    public Task<bool> GetAuth()
    {
      string state = Request.QueryString["state"];
      SpotifyAuthServer<Token> auth = ImplicitGrantAuth.GetByState(state);
      if (auth == null)
        return HttpContext.StringResponseAsync(
          $"Failed - Unable to find auth request with state \"{state}\" - Please retry");

      Token token;
      string error = Request.QueryString["error"];
      if (error == null)
      {
        string accessToken = Request.QueryString["access_token"];
        string tokenType = Request.QueryString["token_type"];
        string expiresIn = Request.QueryString["expires_in"];
        token = new Token
        {
          AccessToken = accessToken,
          ExpiresIn = double.Parse(expiresIn),
          TokenType = tokenType
        };
      }
      else
      {
        token = new Token
        {
          Error = error
        };
      }

      Task.Factory.StartNew(() => auth.TriggerAuth(token));
      return HttpContext.HtmlResponseAsync("<html><script type=\"text/javascript\">window.close();</script>OK - This window can be closed now</html>");
    }

    public ImplicitGrantAuthController(IHttpContext context) : base(context)
    { }
  }
}