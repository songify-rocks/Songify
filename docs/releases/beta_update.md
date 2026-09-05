# Songify 2.0.0 Beta

Songify 2.0 is a new app on the outside, with the same job as 1.8.x: now playing for your overlay, Twitch song requests, and the players you already use.

This is a **beta**. Settings and history migrate automatically. Before you switch, copy the folder that contains `Songify.exe` (that is where `AppConfig.yaml` and history live).

Compared with **1.8.13**, a few things you relied on have changed. Those are listed first.

---

## Do this after updating

1. **Add a Songify API token** (Home checklist, or Settings → System → Songify token). Widget upload, the online queue, recap, and cloud sync need it. Get one at [songify.rocks](https://songify.rocks/token-import).
2. **Link Spotify and Twitch again** if the status bar dots stay red. Tokens and your Client ID / Secret from 1.8 usually carry over; if they do not, use Home → Getting started.
3. Confirm **OBS** still points at the same output folder (`Songify.txt`, `cover.png`). Covers are always written now.
4. If you used **in-app history on the website**, that upload is gone. Local history is still in Songify; recap is on [songify.rocks/recap](https://songify.rocks/recap).

---

## Breaking changes (read this)

### Windows and install

- Songify now runs on **.NET 10**. You need **Windows 10 version 1809** (October 2018) or later, or **Windows 11**. Windows 7 / 8 are not supported.
- Keep using the **zip + `Songify.exe`** install. The leftover ClickOnce/publish machinery from old builds is gone.
- The window is larger: **minimum 900×500**. On a small screen, Home now **scrolls**.
- In-app patch notes use **WebView2**. If it is missing, Songify opens them in your browser instead.

### Songify API token (replaces AccessKey)

Songify no longer stores or sends an **AccessKey**. Uploads to songify.rocks now authenticate with:

- your **UUID** (unchanged, still in config), and
- a **Songify API token** from your website account (Bearer auth).

**If you skip the token:** now-playing files and the local web server still work. The **hosted widget**, **online queue page**, **song upload**, **YouTube metadata helper**, **cloud settings**, and **recap** will not.

Generate the token once on [songify.rocks](https://songify.rocks/token-import). It is shown once; if you lose it, rotate it on the site.

### History

- Local history is now **`history.yaml`** next to Songify. On first start, **`history.shr` is converted and then deleted**.
- You may see a short **migration progress** window if the old file is large.
- **Uploading history to the website from the app is removed.** Stream recap lives on [songify.rocks](https://songify.rocks/recap) (Premium). The in-app History page is local only.

### Album covers

**Download album cover** is always on. There is no toggle anymore. `cover.png` is written whenever the player provides art (Spotify, Pear, and other sources that have it).

### First-run wizard

Existing 1.8 installs skip the wizard. New installs get a **setup wizard** (language, Spotify, Twitch, token) and a **Getting started** list on Home until the basics are done. Spotify still uses **your** Client ID and Secret, same as 1.8.

---

## Where things moved (1.x → 2.0)

The old MahApps windows (main window, separate Settings, History, Queue, Blocklist, Console popups) are replaced by one **sidebar shell**.

| 1.8 location | 2.0 location |
|--------------|----------------|
| Center of the main window (now playing) | **Home** |
| Song requests → Queue | **Queue** in the sidebar |
| History window | **History** in the sidebar (calendar) |
| Song requests → Blocklist | **Blocklist** in the sidebar |
| User list | **Users** in the sidebar |
| View → Console | **Console** in the sidebar, or **Tools → Console window** (detach) |
| File → Settings | **Settings** in the sidebar |
| File → Widget | **Tools → Widget** |
| File → Patch notes / Help | **Help** menu |
| Twitch → Connect / login | **Twitch** menu, or click the **Twitch** dots in the status bar |
| Player dropdown | Still on **Home** (top right) |
| Footer status icons | Same idea — **clickable** (connect, start web server, open Pear, …) |

Settings tabs are the same ideas (System, Output, Twitch, Rewards, Song requests, Bot commands, Spotify, …) in a cleaner layout. Bot responses are one catalog with edit, reset, and preview.

**“Get beta updates”** is gone. Use **Settings → System → Release channel**:

- **Stable** — `update.xml`
- **Beta** — `update-beta.xml` (this 2.0 beta)
- **Dev (Unstable)** — `update-dev.xml`

If you had “Get beta updates” turned **on**, you are on **Beta** automatically.

---

## What is new

### Home

- **Getting started** checklist (Spotify, Twitch, API token, OBS output file) with Go buttons into Settings. Dismiss when you are done.
- **Up Next** — next three queued songs with art and requester chips. Click through to the full queue.
- **Canvas** — when a Spotify track has a canvas and download is enabled, Home can play looping `canvas.mp4` (also saved in your output folder for OBS).
- Quiet **Songify Premium** button on Home and About if you are not subscribed (tooltip lists what it includes). No status-bar nag.

### Queue and History

- Queue: larger now-playing art, request badges, pending list kept in sync under load.
- History: **calendar** with days that have plays, delete-a-day, context actions. Data is YAML, grouped by local date.

### Twitch and song requests

- **`!playlist`** — posts the current playlist name and URL (Spotify and Pear). Placeholders: `{playlist_name}`, `{playlist_url}`. Off by default; enable under Bot commands.
- **`!skippoll`** — starts a skip poll from chat. Moderators by default. Will not start a second poll if one is already running. You can also bind a **channel point reward** to start a skip poll.
- **Explicit songs** — if “block all explicit” is on, you can allow specific **user levels** (VIP, mods, …) to request them anyway.
- **Minimum messages between song announcements** — auto-announce waits until chat has had N messages, so quiet chats are not spammed.
- Spotify short links: **`spotify.link`** and **`open.spotify.com/s/`** both work for requests.

Commands you already used (`!ssr`, `!song`, `!skip`, `!voteskip`, `!queue`, …) are unchanged. Triggers and responses still live under Bot commands / Bot responses.

### Songify Premium (optional)

The core app stays **free**. Premium does not lock now playing, requests, or OBS files.

Included when active:

- Stream recap, top songs, top requesters
- Cloud settings sync
- Extra widget styles on songify.rocks

When subscribed, the window title becomes **Songify Premium**. Open recap or your account from Home, About, or History. The optional startup reminder can be turned off under Settings → Behavior.

Activate: Ko-fi → [songify.rocks](https://songify.rocks) → link Ko-fi with the same email → generate token → paste in Songify.

### Status bar and notifications

- Twitch API, Twitch bot, Spotify, Pear, and WebServer indicators are **buttons**, not just lights.
- In-app **notifications / PSAs** replace the old separate popup window.
- Clearer Spotify errors. If Spotify returns 403 “app owner must have Premium”, that refers to the **Developer Dashboard account that created your Client ID**, not Songify Premium and not necessarily the playback account you linked. Subscribe that Dashboard account to Spotify Premium and wait — access can lag a few hours.

### Localization

The new UI is fully resource-based and switches language live (Settings → System). Strings for the wizard, Premium, new commands, and release channel are included for:

English, German, Dutch, French, Spanish, Italian, Polish, Portuguese (PT & BR), Russian, Belarusian.

Help finish translations on [Weblate](https://translate.songify.rocks/projects/songify/songify/).

---

## Unchanged (so overlays keep working)

These still behave like 1.8 for a typical OBS setup:

- Output folder: `Songify.txt`, `Artist.txt` / `Title.txt`, `url.txt`, `cover.png`
- **Upload song info** for the [widget generator](https://widget.songify.rocks) (needs the new API token)
- Local **web server** HTTP JSON and WebSocket control (`ws://127.0.0.1:<port>/`) and data stream (`/ws/data`)
- Optional **WebSocket password** (added in 1.8.13)
- Players: Spotify, Windows Playback API, foobar2000, VLC, Browser Companion, Pear Desktop
- Twitch rewards, refunds, Bits SR, aliases, skip-only-non-requested, artist CSV blocklist sync from 1.8.13

New file you may want in OBS: **`canvas.mp4`** in the same output folder, if canvas download is enabled.

---

## Fixes worth knowing

- Queue and now playing stay aligned when many requests come in at once.
- Light theme: section titles and icons no longer wash out.
- Secret fields (tokens, passwords) are not wiped when you change theme or language.
- Pear connection state shows in the status bar.
- Skip poll will not stack on an already running poll.

---

## Beta caveats

- First launch after 1.8.x migrates history and may rewrite `AppConfig.yaml`.
- The UI is new; a few labels or empty states may still be rough. Missing translations will fall back to English.
- Report anything that worked in **1.8.13** and does not in 2.0: [GitHub issues](https://github.com/songify-rocks/Songify/issues) or [Discord](https://discord.gg/H8nd4T4). Attach logs from `%LocalAppData%\Songify.Rocks\Logs`.

---

## Support

Songify stays free. Premium and [Ko-fi](https://ko-fi.com/overcodetv) keep recap, cloud sync, and the site running.

- https://songify.rocks
- https://ko-fi.com/overcodetv
