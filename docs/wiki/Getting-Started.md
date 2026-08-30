# Getting started

## Install

1. Download the latest **`Songify.zip`** from [Releases](https://github.com/songify-rocks/Songify/releases/latest).
2. Extract it and run **`Songify.exe`**.

On first launch, **Getting started** walks through player, accounts, song requests, the OBS text file, and widgets. You can skip any step and finish later from **Home** or **Help → Getting started**.

For a reliable Spotify setup, use your own Spotify API application—see [Spotify setup](Spotify-setup).

---

## Quick start (in the app)

The wizard asks, in order:

1. **Language** and **player** (Spotify is the usual choice).
2. **Spotify** — Client ID and account link (if you picked Spotify).
3. **Twitch** — Broadcaster login if you want song requests. Skip for overlay-only.
4. **Song requests** — Channel points, chat commands (`!ssr`), or both.
5. **Rewards** (if you chose channel points) — Create a reward or check existing ones that should add a song. **Refunds only work for rewards created in Songify**, not ones made in the Twitch dashboard (those show the Songify icon when they are refundable).
6. **Request rules** — Who can redeem channel points, and a per-user queue limit. The wizard also lists where to change this later:
   - **Settings → Song requests** — user levels, queue limits per badge, cooldowns
   - **Settings → Rewards** — which rewards add a song, skip rewards, refund conditions
   - **Settings → Commands** — `!ssr` and other chat commands, triggers, who may use them
   - **Settings → Responses** — what the bot says in chat
7. **Songify API token** — Needed to upload now playing and the queue to web widgets.
8. **Song output file** — `Songify.txt` for an OBS Text source.
9. **Stream widget** — Optional browser overlay. **Browse widgets** opens the full gallery (including Premium). **Widget generator** is a simpler customizer. The local web server port is for custom HTML overlays.

Home keeps a **Getting started** checklist until the important items are done (or you dismiss it), plus a **Stream widget** card so overlays are easy to find.

---

## Main window

- **Left navigation** — Home, Queue, Blocklists, History, User list, then Settings, Help, About. Help includes getting started, support links, folders, and the live log. When the pane is collapsed, hover an icon to see its name.
- **Tools** — Widget gallery, Widget generator, queue/console pop-out windows, local web server, queue website.
- **Player dropdown (Home)** — Where Songify reads “now playing” from: Spotify, Windows Playback API, foobar2000, VLC, Browser Companion, or Pear Desktop. See [Music sources](Music-sources).
- **Center** — Current track and album art (when supported).
- **Footer** — Status icons (Twitch chat, Twitch API, PubSub, Spotify, web server), info text, version, website link.

---

## Common tasks

| Goal | Where to start |
|------|----------------|
| Link Spotify | [Spotify setup](Spotify-setup) |
| Twitch bot & song requests | [Twitch setup](Twitch-setup), [Song requests](Song-requests) |
| Overlay in OBS | [Widget and OBS](Widget-and-OBS) |
| YouTube Music (Pear) | [Pear (YouTube Music)](Pear-YouTube-Music) |
| Local API / automation | [Web server and API](Web-server-and-API) |

---

## Logs

**Help → Log folder** opens the log directory. Logs are stored under:

`%LocalAppData%\Songify.Rocks\Logs`

Useful when reporting bugs or diagnosing connection issues.

---

## Next steps

- Full UI and settings breakdown: [Settings reference](Settings-reference)
- Chat commands: [Twitch commands](Twitch-commands)
