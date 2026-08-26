# Songify **2.0.0** – Beta

Songify 2.0 is a full visual and platform rewrite on top of everything from **1.8.13**.
New shell, new onboarding, Songify Premium, and a pile of quality-of-life features.

This is a beta. Settings and history are migrated automatically, but keep a backup of your config folder if you like to be extra safe.

---

## Brand-new app (WPF UI)

The old MahApps windows are gone. Songify now runs in a modern **WPF-UI** shell:

- Sidebar navigation: Home, Queue, History, Blocklist, Users, Console, Settings, About
- Mica / Acrylic window backdrop and light/dark theme
- Clickable status bar for Twitch, Spotify, Pear, and the web server (with live tooltips)
- Fluent dialogs throughout (cloud import, Twitch login, rewards, artist import, …)
- Detachable **Console window** (Tools → Console window)
- In-app **PSAs / notifications** instead of a separate popup window

Same Songify under the hood, it just looks and feels like a 2026 app.

---

## Easier first run

- **Setup wizard** on first launch (language, Spotify, Twitch, Songify token)
- Home **onboarding checklist** until the basics are done (dismiss anytime)
- Optional interactive **tour** of the new layout
- Clear **Songify API token** setup in Settings, with status and a Home warning if it’s missing

---

## Songify Premium

Premium is optional. Songify stays free.

**Included with Premium**
- Stream recap
- Top songs and top requesters
- Cloud settings sync
- Extra widgets

**How it shows up**
- Not subscribed: a quiet **Songify Premium** button on Home and About (hover for what’s included)
- Subscribed: the window title becomes **Songify Premium**
- Optional startup reminder (can be turned off in Settings → Behavior)

Open recap or your account any time from Home, About, or History.

---

## History & queue

- History is stored as **`history.yaml`** (legacy `.shr` files are migrated with a progress dialog)
- History page is a **calendar** with day markers, delete-day, and context actions
- Queue shows **pending tracks only**, with larger now-playing art and request badges
- Home **Up Next** list with requester chips and album art
- **Canvas** (looping `canvas.mp4`) on Home when a track has one; album covers always download
- Safer queue updates (thread-safe snapshots so the UI doesn’t glitch mid-request)

The old “upload history to the website” path is retired, recap lives on songify.rocks instead.

---

## Twitch & song requests

- New **`!playlist`** command — posts the current playlist name and URL (Spotify and Pear). Placeholders: `{playlist_name}`, `{playlist_url}`
- New **Skip Poll** command — start a skip poll from chat (won’t stack a second poll if one is already running)
- **User levels for explicit songs** — when “block all explicit” is on, choose which roles can still request them
- **Minimum messages between song announcements** — optional chat-activity gate so auto-announce doesn’t spam quiet chats
- Spotify short links: both `spotify.link` and `open.spotify.com/s/` expand correctly
- Skip-only-non-requested-songs, Bits SR, aliases, and permission detection from 1.8 still apply

---

## Settings & tools

- Settings live **in the sidebar**, not a separate window, same options, cleaner layout
- Bot responses are a single catalog (edit, reset, live preview)
- Blocklist is a two-column page with a Spotify artist picker when a search is ambiguous
- Create Custom Reward dialog is a modern single-column Fluent window
- **Release channel** dropdown: **Stable**, **Beta**, or **Dev (Unstable)**  
  (replaces the old “Get beta updates” toggle)
  - Stable → `update.xml`
  - Beta → `update-beta.xml`
  - Dev → `update-dev.xml`
- Existing “beta updates on” configs migrate to the **Beta** channel automatically

---

## Localization

The whole new UI is resource-based and switches language live.

Updated or completed strings for:

- German
- Dutch
- French
- Spanish
- Italian
- Polish
- Portuguese (PT & BR)
- Russian
- Belarusian

Including wizard, Premium, playlist command, skip poll, and release channel.

---

## Under the hood

- Migrated from .NET Framework to **.NET 10** (Windows)

---

## Fixes & polish

- Queue / now-playing stay in sync under load
- Spotify `/s/` short URLs work for song requests
- Theme-safe icons and headers (light theme no longer greys out section titles)
- Secret fields (API keys, passwords) no longer get wiped when the theme or language changes
- Status bar services are clickable (connect / start / open) instead of decoration-only
- Pear connection status shows in the status bar

---

## Beta notes

- First launch after 1.8.x will migrate history to YAML and may rewrite `AppConfig.yaml`
- Please report UI glitches, missing translations, and anything that used to work in 1.8.13

---

## Thank you

Huge thanks to everyone who used 1.8.x, translated on Weblate, and tested early 2.0 builds.

Songify is still free. Premium helps keep recap, cloud sync, and the site going.

👉 https://songify.rocks  
👉 https://ko-fi.com/overcodetv
