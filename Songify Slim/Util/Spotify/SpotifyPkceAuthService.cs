using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Songify_Slim.Util.Spotify;

public sealed class SpotifyPkceLoginResult
{
    public PKCETokenResponse TokenResponse { get; set; }
    public SpotifyClient SpotifyClient { get; set; }
}

public sealed class SpotifyPkceAuthService
{
    private readonly string _clientId;
    private readonly string _redirectUri;
    private readonly string _callbackPrefix;

    private string _verifier;
    private string _state;

    public SpotifyPkceAuthService(string clientId, string redirectUri = "http://127.0.0.1:4002/auth")
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));

        // HttpListener prefix must end with a slash
        _callbackPrefix = _redirectUri.EndsWith("/")
            ? _redirectUri
            : _redirectUri + "/";
    }

    public async Task<SpotifyPkceLoginResult> LoginAsync(string[] scopes, CancellationToken cancellationToken = default)
    {
        (string verifier, string challenge) = PKCEUtil.GenerateCodes();
        _verifier = verifier;
        _state = Guid.NewGuid().ToString("N");

        LoginRequest loginRequest = new(
            new Uri(_redirectUri),
            _clientId,
            LoginRequest.ResponseType.Code)
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = challenge,
            Scope = scopes,
            State = _state
        };

        using HttpListener http = new();
        http.Prefixes.Add(_callbackPrefix);
        http.Start();

        Process.Start(new ProcessStartInfo
        {
            FileName = loginRequest.ToUri().ToString(),
            UseShellExecute = true
        });

        HttpListenerContext context = await http.GetContextAsync().ConfigureAwait(false);

        NameValueCollection query = context.Request.QueryString;
        string code = query["code"];
        string state = query["state"];
        string error = query["error"];

        await RespondToBrowserAsync(context.Response, error).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(error))
            throw new Exception("Spotify authorization failed: " + error);

        if (string.IsNullOrWhiteSpace(code))
            throw new Exception("Spotify authorization code missing from callback.");

        if (!string.Equals(state, _state, StringComparison.Ordinal))
            throw new Exception("Invalid OAuth state.");

        PKCETokenResponse tokenResponse = await new OAuthClient().RequestToken(
            new PKCETokenRequest(_clientId, code, new Uri(_redirectUri), _verifier), cancellationToken).ConfigureAwait(false);

        PKCEAuthenticator authenticator = new(_clientId, tokenResponse);

        SpotifyClientConfig config = SpotifyClientConfig.CreateDefault()
            .WithAuthenticator(authenticator);

        SpotifyClient spotify = new(config);

        return new SpotifyPkceLoginResult
        {
            TokenResponse = tokenResponse,
            SpotifyClient = spotify
        };
    }

    public async Task<PKCETokenResponse> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentNullException(nameof(refreshToken));

        return await new OAuthClient().RequestToken(
            new PKCETokenRefreshRequest(_clientId, refreshToken)
        ).ConfigureAwait(false);
    }

    private static async Task RespondToBrowserAsync(HttpListenerResponse response, string error)
    {
        string html = string.IsNullOrWhiteSpace(error)
            ? "<html><body><h2>Spotify login completed.</h2><p>You can close this window now.</p></body></html>"
            : "<html><body><h2>Spotify login failed.</h2><p>You can close this window now.</p></body></html>";

        byte[] buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        response.StatusCode = 200;

        using Stream output = response.OutputStream;
        await output.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
    }
}