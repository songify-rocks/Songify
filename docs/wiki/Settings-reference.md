# Settings reference

Settings are organized in tabs. Names may differ slightly when the UI is translated.

---

## System

- **Language** — UI language. Help translate via [Weblate](https://translate.songify.rocks/projects/songify/songify/).
- **Behavior** — Autostart, minimize to tray, open queue on startup, **Chrome / Browser Companion fetch rate** (lower = more CPU use; only relevant for Browser Companion).
- **Privacy** — Show or hide account names and avatars in Songify.
- **Appearance** — Accent color, light/dark theme.
- **Songify token** — Used for [Songify Premium](Songify-Premium) and cloud sync (if enabled).

---

## Output

- **Output directory** — Where `Songify.txt`, `Artist.txt`, `Title.txt`, cover image, etc. are written.
- **Output format (text file & widget)** — Placeholders such as `{artist}`, `{title}`, `{extra}`, `{url}`, `{{requested by {req}}}` (block only shown for requests).
- **Output format (Twitch chat)** — Format when posting current song to chat via command.
- **Append spaces** — Pads text files for smoother scrolling in some setups.
- **Pause text** — Text shown when playback is paused (if enabled).
- **Upload song info** — Uploads track data for the [widget](Widget-and-OBS) and queue website features.
- **Split artist and title** — Writes separate `Artist.txt` / `Title.txt` files.
- **Download album cover** — Saves cover art (works when metadata comes from APIs that provide it, e.g. Spotify).

---

## Integration (Twitch)

- **Login with Twitch** — OAuth for API features (rewards, stream state, etc.).
- **Account name, OAuth token, channel** — Bot identity and channel to join.
- **Autoconnect** — Connect bot when Songify starts.
- **Announce song to chat** — Post track info when the song changes.
- **Limit to live stream** — Bot/features only when you are live (requires Twitch API).

---

## Rewards

- Select **channel point rewards**, refresh list, create rewards via Songify (needed for refund integration).
- **Refund conditions** — When to refund channel points automatically.

---

## Polls

Configure Twitch polls integration (when available in your build).

---

## Song requests

- Enable **channel point** requests and/or **command** requests.
- **Queue** — Clear on startup, user level, max requests per user, cooldown, max song length.
- Refund and moderation options as shown in the UI.

---

## Bot commands & Bot responses

- **Commands** — Enable/disable commands, change triggers (no spaces in triggers).
- **Responses** — Templates with placeholders like `{user}`, `{artist}`, `{title}` (varies per response).

---

## Spotify

- **Link** — Connect your Spotify account.
- **Use Songify App ID / own App ID** — **Own App ID** is recommended; create an app on the [Spotify Dashboard](https://developer.spotify.com/dashboard/applications). See [Spotify setup](Spotify-setup).
- **Client ID / Client Secret** — From your Spotify application settings.

---

## YouTube (YTM Desktop / Pear)

Settings for linking **YTM Desktop** or related integrations. See [YTM Desktop](YTM-Desktop) and [Pear (YouTube Music)](Pear-YouTube-Music).

---

## Web server

- **Port** — Local HTTP + WebSocket (use a port above 1024; avoid conflicts).
- **Start / auto-start** — Run the built-in server for JSON and WebSocket control. See [Web server and API](Web-server-and-API).

---

## Config

- Beta updates, export/import configuration.

---

## Windows in the app (outside Settings)

- **Queue** — Request queue (app queue is not always identical to Spotify’s queue; deleting here may not remove from Spotify).
- **Blocklist** — Block artists, songs, or users from requesting.
- **History** — Listening history (optional upload/share).
- **Console** — Log output for debugging.
