# Troubleshooting

### Songify won’t start or behaves oddly after an update

- Install the [latest release](https://github.com/songify-rocks/Songify/releases/latest).
- As a last resort, reset local data (this removes settings): delete the folder  
  `%LocalAppData%\Songify.Rocks`  
  **Warning:** Backup/export config first if you use **Settings → Config → Export**.

---

### No song info

- Confirm the correct **player** is selected (main window dropdown).
- For **Spotify**, ensure the account is **linked** (Settings → Spotify; status icons in the footer).
- For **Browser Companion**, try increasing the fetch interval under **Settings → System** to reduce missed updates / CPU load.

---

### No album cover

- Enable **Download album cover** under **Settings → Output**.
- Covers need a source that provides art (e.g. Spotify API). Some sources only provide title text.

---

### Song requests not working

- **Spotify Premium** is required for Spotify-based requests.
- Complete [Spotify setup](Spotify-setup) and [Twitch setup](Twitch-setup).
- Bot must be **connected**; reward/command toggles must be enabled under **Song requests**.
- See [Song requests](Song-requests).

---

### High CPU with Browser Companion / Chrome

Songify reads browser tab information on an interval. **Lower the polling rate** (increase seconds between fetches) under **Settings → System → Chrome fetch rate** (or similarly named option).

---

### `INVALID CLIENT: Failed to get client` (Spotify)

Usually wrong **Client ID** or **Client Secret**. Recreate them on the [Spotify Dashboard](https://developer.spotify.com/dashboard/applications) and paste again under **Settings → Spotify**.

---

### `INVALID_CLIENT: Invalid redirect URI` (Spotify)

Add the redirect URI to your Spotify app (**Edit settings → Redirect URIs → Save**):

`http://127.0.0.1:4002/auth`

If it still fails, also add:

`http://localhost:4002/auth`

See [Spotify setup](Spotify-setup).

---

### Web server won’t start

- Pick a different **port** (another app may be using it).
- On Windows, check that **Songify** is allowed through the firewall if you access it from another device.

---

### Logs

Logs: `%LocalAppData%\Songify.Rocks\Logs` — attach relevant logs when reporting issues on GitHub or Discord.
