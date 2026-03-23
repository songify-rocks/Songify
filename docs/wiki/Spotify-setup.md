# Spotify setup

Songify uses the [Spotify Web API](https://developer.spotify.com/documentation/web-api). You should **create your own Spotify application** for stable authentication.

---

## 1. Create a Spotify application

1. Open the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications) and log in.
2. Create an app and note the **Client ID** and **Client Secret**.

---

## 2. Redirect URI

In the app’s **Settings**, under **Redirect URIs**, add **exactly**:

```
http://127.0.0.1:4002/auth
```

Click **Add**, then **Save**.

If linking still fails, also add:

```
http://localhost:4002/auth
```

Spotify treats `localhost` and `127.0.0.1` as different redirect URIs. Songify listens on port **4002** for the OAuth callback.

---

## 3. Enter credentials in Songify

1. Open **Settings → Spotify**.
2. Paste **Client ID** and **Client Secret**.
3. Enable **Use own App ID** (recommended).
4. Restart Songify if prompted.
5. Click **Link** and complete the browser login.

You should see your linked Spotify account in settings when successful.

---

## Song requests

**Spotify Premium** is required on the account you use for playback when using Spotify-backed **song requests**.

---

## Related errors

See [Troubleshooting](Troubleshooting) for `INVALID CLIENT` and invalid redirect URI.
