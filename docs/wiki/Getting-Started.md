# Getting started

## Install

1. Download the latest **`Songify.zip`** from [Releases](https://github.com/songify-rocks/Songify/releases/latest).
2. Extract it and run **`Songify.exe`**.

On first launch you may be prompted to link **Spotify**. For a reliable setup, use your own Spotify API application—see [Spotify setup](Spotify-setup).

---

## Main window

- **Menus** — **File** (Settings, Widget, Patch notes, Help, Exit), **Twitch** (login, connect/disconnect bot, bot config), **Song requests** (queue, blocklist, clear queue), **History**, **View** (console).
- **Player dropdown (top right)** — Choose where Songify reads “now playing” from: Spotify, Windows Playback API, foobar2000, VLC, Browser Companion, or Pear Desktop. See [Music sources](Music-sources).
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
