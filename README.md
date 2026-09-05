![Songify](https://github.com/user-attachments/assets/06d27662-18c8-465a-a2e9-a30be43830cb)

Now playing overlays, Twitch chat integration, and song requests for streamers.

---

[![Song requests made](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.songify.rocks%2Fv2%2Fstats&query=%24.sr_total&style=for-the-badge&label=song%20requests%20made&color=%2316a349)](https://songify.rocks)
[![Active users](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.songify.rocks%2Fv2%2Fstats&query=%24.monthly_users&style=for-the-badge&label=active%20users&color=%2316a349)](https://songify.rocks)

[![Windows](https://img.shields.io/badge/platform-windows-blue?style=for-the-badge&color=%2316a349)](https://github.com/songify-rocks/Songify/releases/latest)
[![C#](https://img.shields.io/badge/written_in-C%23-blue?style=for-the-badge&color=%2316a349)](https://github.com/songify-rocks/Songify)
[![GitHub Downloads](https://img.shields.io/github/downloads/songify-rocks/Songify/total?style=for-the-badge&color=%2316a349)](https://github.com/songify-rocks/Songify/releases)

[![GitHub Repo stars](https://img.shields.io/github/stars/songify-rocks/Songify?style=for-the-badge&color=%2316a349)](https://github.com/songify-rocks/Songify/stargazers)
[![GitHub contributors](https://img.shields.io/github/contributors/songify-rocks/Songify?style=for-the-badge&color=%2316a349)](https://github.com/songify-rocks/Songify/graphs/contributors)
[![License](https://img.shields.io/badge/LICENSE-GPLv3-blue?style=for-the-badge&color=%2316a349)](https://github.com/songify-rocks/Songify/blob/master/LICENSE)

[![Discord](https://img.shields.io/discord/117032577977679873?style=for-the-badge&logo=discord&logoColor=%23ffffff&color=%2316a349)](https://discord.gg/H8nd4T4)
[![Support on Ko-Fi](https://img.shields.io/badge/support_on-Ko--Fi-blue?style=for-the-badge&logo=kofi&logoColor=%23ffffff&color=%2316a349)](https://ko-fi.com/S6S167PLK)

---

### Download

**[Get the latest `Songify.zip`](https://github.com/songify-rocks/Songify/releases/latest)** - extract it and run `Songify.exe`. Windows only.

![Songify Interface](https://github.com/user-attachments/assets/6fab125f-e0f6-4b00-b11d-aefa34639553)

---

### How it works

Songify reads now-playing data from Spotify (and other players) and talks to Twitch so viewers can request songs via **chat commands** or **channel points**. Playback commands (play, pause, vote skip) and current-song lookups are built in.

Supported sources:

- **Spotify** - full API integration; best for song requests, metadata, and cover art
- **[Pear Desktop](https://github.com/pear-devs/pear-desktop)** (formerly th-ch YouTube Music) - YouTube Music, including song requests
- **Windows Playback API** - anything that exposes a Windows media session
- **foobar2000**
- **VLC**
- **Qobuz**

---

### Features

- **Now playing** - text files, a hosted widget, or your own overlay via Songify’s local web server
- **Song requests** - Twitch channel points or chat, with queue and blocklist controls
- **Chat commands** - playback, queue, and current-song info
- **Playlists** - add requests to a playlist, or limit requests to one
- **Album covers** - download art for OBS when the source provides it
- **Widgets** - build an overlay with the [widget generator](https://widget.songify.rocks) or grab pre-built widgets [here](https://songify.rocks/widgets/)
- **Local API** - JSON and WebSocket on a port you choose, for custom visuals and automation

- **🎵 Real-Time Song Info**: Display the current song with support for text files, a hosted widget, or your own custom visuals using Songify's web server.
- **🔊 Spotify Song Requests**: Let viewers request songs via channel points or chat commands.
- **💬 Chat Integration**: Built-in commands to manage playback, queues, and retrieve song information.
- **🎧 Playlist Control**: Add all song requests to a dedicated playlist or restrict requests to specific playlists.
- **🖼️ Album Covers**: Automatically download album covers to enhance your stream's visuals.
- **💿 Custom Widgets**: Use the [widget gallery](https://songify.rocks/widgets/) (including Premium styles) or the [widget generator](https://widget.songify.rocks) for a simpler now-playing bar.
- **🎉 And More!** Discover additional features to elevate your streaming experience.
The core app is free. [Songify Premium](https://github.com/songify-rocks/Songify/wiki/Songify-Premium) is optional (cloud sync and extra widget styles) and does not remove free features.

---

### Translations

Songify is available in 11 languages:

- 🇬🇧 English
- 🇳🇱 Dutch
- 🇩🇪 German
- 🇫🇷 French
- 🇮🇹 Italian
- 🇪🇸 Spanish
- 🇵🇱 Polish
- 🇵🇹 Portuguese
- 🇧🇷 Brazilian Portuguese
- 🇷🇺 Russian
- 🇧🇾 Belarusian

Translations live on [Weblate](https://translate.songify.rocks/projects/songify/songify/). Contributions are welcome.

---

### Guides

- [Wiki](https://github.com/songify-rocks/Songify/wiki) — source in [`docs/wiki`](docs/wiki)
  - [Getting started](https://github.com/songify-rocks/Songify/wiki/Getting-Started)
  - [Song requests](https://github.com/songify-rocks/Songify/wiki/Song-requests)
  - [Widget and OBS](https://github.com/songify-rocks/Songify/wiki/Widget-and-OBS)
  - [Troubleshooting](https://github.com/songify-rocks/Songify/wiki/Troubleshooting)

Questions or ideas: [Discord](https://discord.gg/H8nd4T4). If Songify is useful, a GitHub star helps others find it.

---

### Contributors

<a href="https://github.com/songify-rocks/songify/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=songify-rocks/songify" alt="Songify contributors" />
</a>

---

### Powered by

[![JetBrains logo.](https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.svg)](https://jb.gg/OpenSourceSupport)
