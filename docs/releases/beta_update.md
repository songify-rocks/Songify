# Songify 2.0

Songify 2.0 is a new app on the outside, with the same job as **1.8.13**: now playing for your overlay, Twitch song requests, and the players you already use.

Coming from 1.8.x? **[Migrating from 1.8](https://github.com/songify-rocks/Songify/wiki/Migrating-from-1.8)** covers backup, the Songify API token, OBS, history, and rollback. This page is what’s new.

> **Screenshot:** `hero-home.png`  
> Full Home screen: now playing, album art, Up Next, Getting started checklist, sidebar, and status bar.  
> ![Home](./screenshots/2.0/hero-home.png)

---

## Home and onboarding

- **Getting started** checklist on Home: Spotify, Twitch, API token, OBS output file, widget. Each row has a **Go** button into Settings. Dismiss when you are done.
- **Setup wizard** (new installs, or **Help → Getting started**): language → player → Spotify → Twitch → channel points vs chat requests → create/select rewards → who can request / queue limit → token → output folder → widget. Existing 1.8 installs skip it automatically.
- **Up Next** — next three queued songs with art and requester chips. Click through to the full queue.
- **Canvas** — when a Spotify track has a canvas and download is enabled, Home can play looping `canvas.mp4` (also saved for OBS).
- Quiet **Songify Premium** entry on Home and About if you are not subscribed (tooltip lists what it includes). No status-bar nag.

> **Screenshot:** `getting-started-wizard.png`  
> Setup wizard step (e.g. song requests or rewards).  
> ![Setup wizard](./screenshots/2.0/getting-started-wizard.png)

> **Screenshot:** `home-up-next.png`  
> Home Up Next row with three upcoming requests.  
> ![Up Next](./screenshots/2.0/home-up-next.png)

---

## Appearance

- **Theme** — light or dark (live switch).
- **Accent color** — color wheel, Windows system accent, last 7 custom colors, or a hex value.
- **Interface scale** — zoom the whole UI from **100% to 200%** on top of Windows DPI. Useful on 4K / high-DPI screens.
- **Window backdrop** — Mica, Acrylic, Tabbed, Auto, or None.
- **Side menu** — left (expanded/collapsed, **remembered**), top, or bottom. Collapsed icons show a tooltip on hover.
- **Overrule minimum size** — allow the main window below 900×500.

> **Screenshot:** `appearance-accent-scale.png`  
> Settings → Appearance: accent wheel, scale slider, backdrop, side menu.  
> ![Appearance](./screenshots/2.0/appearance-accent-scale.png)

---

## Queue, History, Help

- Queue: larger now-playing art, request badges, pending list kept in sync under load.
- **Pop-out queue window** (**Tools**). Optional **open queue on startup** (in-app page or pop-out).
- History: **calendar** of days that have plays, delete-a-day, context actions.
- **Help** page: Getting started, patch notes, Discord, wiki, config/log folders, live log, API metrics.
- **Detachable console** from Tools / Help.

> **Screenshot:** `queue-page.png`  
> Queue page: now playing + pending requests.  
> ![Queue](./screenshots/2.0/queue-page.png)

> **Screenshot:** `queue-popout.png`  
> Separate pop-out queue window.  
> ![Queue pop-out](./screenshots/2.0/queue-popout.png)

> **Screenshot:** `history-calendar.png`  
> History page with calendar days that have plays.  
> ![History](./screenshots/2.0/history-calendar.png)

> **Screenshot:** `help-page.png`  
> Help with links, folders, and live log.  
> ![Help](./screenshots/2.0/help-page.png)

---

## Players

The player dropdown on Home is grouped:

- **Song requests** — Spotify, Pear Desktop (YouTube Music)
- **Now playing only** — Windows Playback API, foobar2000, VLC, Qobuz, …

**Browser Companion** is hidden unless you were already using it (it is still supported).

> **Screenshot:** `player-dropdown-grouped.png`  
> Home player ComboBox with “Song requests” and “Now playing only” groups.  
> ![Player picker](./screenshots/2.0/player-dropdown-grouped.png)

### Pear Desktop (YouTube Music)

Pear is a first-class source for now playing **and** song requests.

If Pear’s API Server uses authorization (not “No authorization”):

1. Open **Settings → YouTube**.
2. Click **Authorize**.
3. **Allow** Songify in the Pear Desktop prompt. The Allow/Deny dialog appears on the `/auth` request, not when the WebSocket connects.
4. You can paste a token manually if you already have one.

Selecting Pear as the player can also start that auth request.

> **Screenshot:** `settings-pear-authorize.png`  
> Settings → YouTube with Pear Authorize button and short hint text.  
> ![Pear authorize](./screenshots/2.0/settings-pear-authorize.png)

Optional: [your own YouTube Data API key](https://github.com/songify-rocks/Songify/wiki/YouTube-Data-API-Key) for more reliable YouTube metadata (stays on your PC).

---

## Twitch and song requests

Commands you already used (`!ssr`, `!song`, `!skip`, `!voteskip`, `!queue`, `!play`, `!pause`, `!vol`, …) are unchanged. Triggers and responses still live under **Bot commands** / **Bot responses**.

**New in 2.0:**

- **Ignored chat users** — editable list under **Settings → Twitch**. Add Nightbot-style bots, extra bots, or even yourself. The linked Songify bot is always ignored.
- **`!playlist`** — posts the current playlist name and URL (Spotify and Pear). Placeholders: `{playlist_name}`, `{playlist_url}`. **Off by default**; enable under Bot commands.
- **`!skippoll`** — starts a skip poll from chat (moderators by default). Will not start a second poll if one is already running. You can also bind a **channel point reward** to start a skip poll.
- **Explicit songs** — if “block all explicit” is on, you can allow specific **user levels** (VIP, mods, …) to request them anyway.
- **Minimum messages between song announcements** — auto-announce waits until chat has had N messages, so quiet chats are not spammed.
- Spotify short links: **`spotify.link`** and **`open.spotify.com/s/`** both work for requests.
- Refund conditions UI is simpler to toggle.

> **Screenshot:** `settings-ignored-users.png`  
> Settings → Twitch, ignored chat users list.  
> ![Ignored users](./screenshots/2.0/settings-ignored-users.png)

> **Screenshot:** `bot-commands.png`  
> Bot commands list including playlist and skip poll.  
> ![Commands](./screenshots/2.0/bot-commands.png)

> **Screenshot:** `song-requests-explicit.png`  
> Song requests: explicit user-level exceptions and min messages between announces.  
> ![Song request rules](./screenshots/2.0/song-requests-explicit.png)

---

## Blocklists and rewards

- Blocklist page redesigned (artists, songs, users).
- Reward dialogs cleaned up; creating rewards **in Songify** is still required for automatic refunds (Twitch-dashboard rewards cannot be cancelled by Songify).
- Artist CSV blocklist sync from 1.8.13 is still there.

> **Screenshot:** `blocklist-page.png`  
> Blocklist with artists / songs / users.  
> ![Blocklist](./screenshots/2.0/blocklist-page.png)

---

## Cloud settings (Premium)

- Cloud save/restore keeps **revisions**. When you restore, pick an older snapshot.
- Config has a **schema version**. If a cloud revision is **newer than this build**, restore is blocked until you update Songify.
- Config **import preview** is clearer about permission changes before you apply.

> **Screenshot:** `cloud-revisions.png`  
> Cloud restore with a list of dated revisions.  
> ![Cloud revisions](./screenshots/2.0/cloud-revisions.png)

---

## Songify Premium (optional)

The core app stays **free**. Premium does **not** lock now playing, requests, or OBS files.

Included when active:

- Stream recap, top songs, top requesters
- Cloud settings sync
- Extra widget styles on [songify.rocks/widgets](https://songify.rocks/widgets/)

When subscribed, the window title becomes **Songify Premium**. Open recap or your account from Home, About, or History. The optional startup reminder can be turned off under **Settings → Behavior**.

**Activate:** Ko-fi → [songify.rocks](https://songify.rocks) → link Ko-fi with the **same email** → generate token → paste in Songify.

> **Screenshot:** `premium-home.png`  
> Home/About Premium area when subscribed (title “Songify Premium”).  
> ![Premium](./screenshots/2.0/premium-home.png)

---

## Status bar, notifications, Spotify errors

- Twitch API, Twitch bot, Spotify, Pear, and web server indicators are **buttons**, not just lights.
- In-app **notifications / PSAs** replace the old separate popup window.
- Persistent **Spotify issue banner** when something keeps failing (for example 403). Clearer than a one-shot dialog.
- If Spotify returns **403 “app owner must have Premium”**, that refers to the **Developer Dashboard account that created your Client ID**, not Songify Premium and not necessarily the playback account you linked. Subscribe that Dashboard account to Spotify Premium and wait — access can lag a few hours.

> **Screenshot:** `status-bar-connected.png`  
> Footer with Twitch API, Twitch chat, Spotify, Pear, and web server as clickable icons.  
> ![Status bar](./screenshots/2.0/status-bar-connected.png)

> **Screenshot:** `spotify-issue-banner.png`  
> Home Spotify persistent-issue banner.  
> ![Spotify banner](./screenshots/2.0/spotify-issue-banner.png)

> **Screenshot:** `psa-notifications.png`  
> In-app PSA / notification panel.  
> ![PSAs](./screenshots/2.0/psa-notifications.png)

---

## Localization

The new UI is fully resource-based and switches language live (**Settings → System**). Wizard, Premium, new commands, appearance, and release channel strings are included for:

English, German, Dutch, French, Spanish, Italian, Polish, Portuguese (PT & BR), Russian, Belarusian.

Help finish translations on [Weblate](https://translate.songify.rocks/projects/songify/songify/). Missing strings fall back to English.

> **Screenshot:** `settings-language.png`  
> Settings → System language picker.  
> ![Language](./screenshots/2.0/settings-language.png)

---

## Logs (for bug reports)

New log files include **Windows version, CPU, GPU, and RAM** at the top so Discord / GitHub reports are easier to diagnose. Logs still live in:

`%LocalAppData%\Songify.Rocks\Logs`

**Help → Log folder** opens that directory.

---

## Overlays and local APIs

These still behave like **1.8.13** for a typical OBS / automation setup:

- Output folder: `Songify.txt`, `Artist.txt` / `Title.txt`, `url.txt`, `cover.png`
- Placeholders such as `{artist}`, `{title}`, `{extra}`, `{url}`, `{{requested by {req}}}`
- **Upload song info** for the [widget generator](https://widget.songify.rocks) (needs a [Songify API token](https://github.com/songify-rocks/Songify/wiki/Migrating-from-1.8#1-add-a-songify-api-token))
- Local **web server** HTTP JSON and WebSocket control (`ws://127.0.0.1:<port>/`) and data stream (`/ws/data`)
- Optional **WebSocket password** (added in 1.8.13)
- Players: Spotify, Windows Playback API, foobar2000, VLC, Browser Companion, Pear Desktop, Qobuz
- Twitch rewards, refunds, Bits song requests, command aliases, skip-only-non-requested
- Artist CSV blocklist sync
- Autostart, minimize to tray, single-instance / `songify://` deep links

**Download album cover** is always on. New optional OBS file: **`canvas.mp4`**, if canvas download is enabled.

Widget gallery: [songify.rocks/widgets](https://songify.rocks/widgets/)  
Widget generator: [widget.songify.rocks](https://widget.songify.rocks)

> **Screenshot:** `obs-browser-widget.png`  
> OBS Browser source with a Songify widget (optional).  
> ![OBS widget](./screenshots/2.0/obs-browser-widget.png)

---

## Fixes worth knowing

Compared with **1.8.13**:

- Queue and now playing stay aligned when many requests come in at once; playhead follows the selected/current row more reliably.
- **Region-locked Spotify tracks** are detected from `is_playable` / market restrictions (Spotify dropped `available_markets`).
- Pear queue index lookup and connection errors are more reliable; Pear state shows in the status bar.
- Light theme: section titles and icons no longer wash out.
- Secret fields (tokens, passwords) are not wiped when you change theme or language.
- Skip poll will not stack on an already running poll.
- History saves are more reliable (less chance of a truncated file if you close quickly).

---

## Screenshot checklist (for this post)

Drop files in `screenshots/2.0/` (or your CDN) and replace the `./screenshots/2.0/...` paths above.

| File | What to capture |
|------|-----------------|
| `hero-home.png` | Home: art, track, Up Next, checklist, sidebar, status bar |
| `getting-started-wizard.png` | Wizard step |
| `home-up-next.png` | Up Next chips |
| `appearance-accent-scale.png` | Accent + scale |
| `queue-page.png` | In-app queue |
| `queue-popout.png` | Pop-out queue |
| `history-calendar.png` | History calendar |
| `help-page.png` | Help + log |
| `player-dropdown-grouped.png` | Grouped player list |
| `settings-pear-authorize.png` | Pear Authorize |
| `settings-ignored-users.png` | Ignored chat users |
| `bot-commands.png` | Commands including playlist / skip poll |
| `song-requests-explicit.png` | Explicit levels / announce gap |
| `blocklist-page.png` | Blocklist |
| `cloud-revisions.png` | Cloud restore revisions |
| `premium-home.png` | Premium UI |
| `status-bar-connected.png` | Footer icons, connected |
| `spotify-issue-banner.png` | Spotify banner |
| `psa-notifications.png` | PSA panel |
| `settings-language.png` | Language |
| `obs-browser-widget.png` | Optional OBS |

---

## Support

Songify stays free. Premium and [Ko-fi](https://ko-fi.com/overcodetv) keep recap, cloud sync, and the site running.

Upgrading from 1.8.13: **[Migrating from 1.8](https://github.com/songify-rocks/Songify/wiki/Migrating-from-1.8)**

- App: [songify.rocks](https://songify.rocks)
- Wiki: [Getting started](https://github.com/songify-rocks/Songify/wiki/Getting-Started)
- Discord: [discord.gg/H8nd4T4](https://discord.gg/H8nd4T4)
- Ko-fi: [ko-fi.com/overcodetv](https://ko-fi.com/overcodetv)
