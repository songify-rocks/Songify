# Music sources

Use the **player dropdown** on the main window to choose where Songify reads “now playing” information.

| Source | Description |
|--------|-------------|
| **Spotify** | Full API integration; best for song requests, rich metadata, and cover art. |
| **Windows Playback API** | Anything exposing metadata through Windows’ media session (many desktop players). |
| **foobar2000** | Title via foobar2000 integration. |
| **VLC** | VLC media player window/title integration. |
| **Browser Companion** | Reads from supported browser tabs (can use more CPU at high refresh rates—adjust **Chrome fetch rate** under **Settings → System**). |
| **Pear Desktop** | YouTube Music via [Pear Desktop](https://github.com/pear-devs/pear-desktop); supports Pear-backed requests. See [Pear (YouTube Music)](Pear-YouTube-Music). |

---

## Covers and metadata

**Download album cover** (Settings → Output) works when the active source supplies enough data—typically **Spotify**, **Browser Companion**, **Pear**, and **Windows Playback** in supported configurations.

---

## Song requests

**Spotify** and **Pear** modes integrate with Twitch song requests according to your settings. Other sources are for **now playing** / display unless your build adds more.

---

## Related guides

- [Spotify setup](Spotify-setup)
- [Pear (YouTube Music)](Pear-YouTube-Music)
- [YTM Desktop](YTM-Desktop) (alternative YouTube Music desktop client)
