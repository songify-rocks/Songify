<div align="center">

![Songify](https://github.com/user-attachments/assets/06d27662-18c8-465a-a2e9-a30be43830cb)

### Your music. Your stream. Your rules.

**The all-in-one music companion for streamers.**

Bring your music to your stream with beautiful now-playing overlays,
interactive song requests, Twitch integration, and powerful customization.

<br>

[![Download Songify](https://img.shields.io/badge/Download-Songify-16a349?style=for-the-badge&logo=github&logoColor=white)](https://github.com/songify-rocks/Songify/releases/latest)
[![Website](https://img.shields.io/badge/Visit-Website-16a349?style=for-the-badge&logo=googlechrome&logoColor=white)](https://songify.rocks)
[![Discord](https://img.shields.io/badge/Join-Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/H8nd4T4)

<br>

[![Active Users](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.songify.rocks%2Fv2%2Fstats&query=%24.monthly_users&label=Monthly%20Active%20Users&color=16a349&style=for-the-badge)](https://songify.rocks)
[![Song Requests](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fapi.songify.rocks%2Fv2%2Fstats&query=%24.sr_total&label=Song%20Requests%20Made&color=16a349&style=for-the-badge)](https://songify.rocks)

<br>

[![Build](https://img.shields.io/github/actions/workflow/status/songify-rocks/Songify/build.yml?branch=master&label=build)](https://github.com/songify-rocks/Songify/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/songify-rocks/Songify?label=release&color=16a349)](https://github.com/songify-rocks/Songify/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/songify-rocks/Songify/total?label=downloads&color=16a349)](https://github.com/songify-rocks/Songify/releases)
[![License](https://img.shields.io/badge/license-GPLv3-16a349)](https://github.com/songify-rocks/Songify/blob/master/LICENSE)

[![Discord](https://img.shields.io/discord/117032577977679873?logo=discord&logoColor=white&label=Discord&color=5865F2)](https://discord.gg/H8nd4T4)
[![Ko-fi](https://img.shields.io/badge/Support_on-Ko--fi-FF5E5B?logo=kofi&logoColor=white)](https://ko-fi.com/S6S167PLK)

</div>

---

## 🎵 What is Songify?

**Songify connects your music with your livestream.**

Whether you're listening to Spotify, YouTube Music, or another supported music player, Songify makes it easy to display what's playing and let your viewers interact with your music.

Show your current track in OBS, accept song requests through Twitch, manage your queue, and customize your overlays — all from one application.

**Built for streamers. Made to be simple.**

- **Display your music** — Show the currently playing song directly on your stream.
- **Engage your viewers** — Let your community request songs using Twitch chat or channel points.
- **Customize your overlays** — Create widgets that match your stream's aesthetic.
- **Stay in control** — Manage requests, playlists, queues, and blocked songs.
- **Connect your tools** — Integrate Songify with your streaming setup using its local API.
- **Free to use** — Enjoy Songify's core functionality without a subscription.

---

## 📸 See Songify in Action

<div align="center">

### Meet your new music companion

![Songify Interface](https://github.com/user-attachments/assets/958bec2a-897c-4f94-9bdf-123b0eb5a462)

*Manage your music, song requests, and streaming integrations in one place.*

</div>

---

## ✨ Features

### 🎵 Music & Playback

| Feature | Description |
|:---|:---|
| Now Playing | Display the currently playing song, artist, and album information. |
| Album Artwork | Automatically retrieve album covers for your OBS overlays. |
| Playback Controls | Control playback using Songify or supported Twitch commands. |
| Playlist Management | Add requested songs to playlists or restrict requests to specific playlists. |
| Multiple Music Sources | Connect Songify to your preferred supported music player. |

### 🎮 Twitch Integration

| Feature | Description |
|:---|:---|
| Chat Commands | Let viewers interact with your music directly through Twitch chat. |
| Channel Points | Allow viewers to redeem channel points for song requests. |
| Song Requests | Accept and manage song requests from your community. |
| Vote Skip | Give your viewers the ability to vote on skipping songs. |
| Blocklist | Prevent unwanted songs from being requested. |
| Queue Management | Keep track of upcoming requests and manage your queue. |

### 🎨 Overlays & Customization

| Feature | Description |
|:---|:---|
| OBS Integration | Display your music directly in OBS using browser sources or text files. |
| Widget Generator | Create customizable now-playing widgets. |
| Hosted Widgets | Use hosted widgets without maintaining your own web server. |
| Widget Styles | Choose from different styles to match your stream. |
| Album Covers | Include album artwork in your overlays. |

### 🔌 APIs & Integrations

Songify provides a local API for developers and advanced users who want to create their own integrations.

- **WebSocket API** — Receive real-time playback information and interact with Songify.
- **JSON API** — Access playback information from external applications.
- **Local Web Server** — Build custom overlays and integrations.
- **Streamer.bot** — Integrate Songify into your streaming automations.

Want to build something yourself? Check out the [documentation](https://github.com/songify-rocks/Songify/wiki).

---

## 🎧 Supported Music Sources

Songify works with multiple music players and playback sources.

| Source | Support |
|:---|:---|
| **Spotify** | Full API integration, song requests, metadata, and album artwork |
| **[Pear Desktop](https://github.com/pear-devs/pear-desktop)** | YouTube Music integration, including song requests |
| **Windows Playback API** | Music applications exposing a Windows media session |
| **foobar2000** | Now-playing integration |
| **VLC** | Now-playing integration |
| **Qobuz** | Now-playing integration |

> [!NOTE]
> Available functionality depends on the selected music source.
> Spotify and Pear Desktop offer additional integration capabilities.

---

## 🚀 Getting Started

Getting started with Songify takes just a few minutes.

### 1. Download

Download the latest version of Songify from GitHub.

[![Download Latest Release](https://img.shields.io/github/v/release/songify-rocks/Songify?label=Download%20Latest%20Release&style=for-the-badge&color=16a349&logo=github)](https://github.com/songify-rocks/Songify/releases/latest)

### 2. Install

1. Download the latest `Songify.zip`.
2. Extract the archive to a folder of your choice.
3. Launch `Songify.exe`.

**System requirements:** Windows

### 3. Connect your music

Select your preferred music source and follow the setup instructions.

### 4. Connect Twitch

Connect your Twitch account to enable chat commands, channel point redemptions, and song requests.

### 5. Customize your stream

Configure your now-playing overlay and add it to OBS.

You're ready to go! 🎉

> [!TIP]
> Need help setting everything up?
> Follow our [Getting Started Guide](https://github.com/songify-rocks/Songify/wiki/Getting-Started).

---

## 🎨 Widgets & Overlays

Make your stream stand out with customizable now-playing overlays.

Songify supports multiple ways to display your music:

- **Hosted widgets** — Easy-to-use overlays that work directly in OBS.
- **Widget Generator** — Customize the appearance of your now-playing display.
- **Text files** — Display song information using OBS text sources.
- **Custom overlays** — Build your own widgets using Songify's local API.

[![Widget Generator](https://img.shields.io/badge/Open-Widget_Generator-16a349?style=for-the-badge)](https://widget.songify.rocks)
[![Browse Widgets](https://img.shields.io/badge/Browse-Widgets-16a349?style=for-the-badge)](https://songify.rocks/widgets/)

---

## 💎 Songify Premium

**Love Songify? Take your streaming experience even further.**

Songify Premium is an optional upgrade that provides additional customization and convenience features while supporting continued development.

Premium features include:

- Cloud synchronization
- Additional widget styles
- Additional premium customization options

**The core Songify application remains free.**

Premium does not remove existing free functionality.

[![Explore Premium](https://img.shields.io/badge/Explore-Songify_Premium-16a349?style=for-the-badge)](https://github.com/songify-rocks/Songify/wiki/Songify-Premium)


---

## 🌍 Translations

**Music is universal. Songify should be too.**

Songify is currently available in **11 languages**, thanks to the contributions of our community.

| Language | Language | Language |
|:---|:---|:---|
| 🇬🇧 English | 🇳🇱 Dutch | 🇩🇪 German |
| 🇫🇷 French | 🇮🇹 Italian | 🇪🇸 Spanish |
| 🇵🇱 Polish | 🇵🇹 Portuguese | 🇧🇷 Brazilian Portuguese |
| 🇷🇺 Russian | 🇧🇾 Belarusian | |

### Help translate Songify

Want to make Songify available in your language or improve an existing translation?

Translations are managed through our Weblate instance.

No programming experience required!

[![Translation Status](https://translate.songify.rocks/widgets/songify/-/svg-badge.svg)](https://translate.songify.rocks/projects/songify/)

<br>

[![Help Translate](https://img.shields.io/badge/Help-Translate_Songify-16a349?style=for-the-badge&logo=weblate&logoColor=white)](https://translate.songify.rocks/projects/songify/songify/)

---

## 📚 Documentation & Guides

Everything you need to get the most out of Songify.

| Guide | Description |
|:---|:---|
| [Wiki](https://github.com/songify-rocks/Songify/wiki) | Complete Songify documentation |
| [Getting Started](https://github.com/songify-rocks/Songify/wiki/Getting-Started) | Install and configure Songify |
| [Song Requests](https://github.com/songify-rocks/Songify/wiki/Song-requests) | Set up Twitch song requests |
| [Widgets & OBS](https://github.com/songify-rocks/Songify/wiki/Widget-and-OBS) | Configure your streaming overlays |
| [Troubleshooting](https://github.com/songify-rocks/Songify/wiki/Troubleshooting) | Solutions to common problems |

Documentation sources are also available in [`docs/wiki`](docs/wiki).

---

## 💬 Community & Support

Have a question, an idea, or need help setting up Songify?

Join our Discord community!

Connect with other Songify users, get support, share feedback, and stay up to date with development.

[![Discord](https://img.shields.io/discord/117032577977679873?label=Join%20our%20Discord&logo=discord&logoColor=white&color=5865F2&style=for-the-badge)](https://discord.gg/H8nd4T4)

### Found a bug or have an idea?

We welcome feedback and contributions!

- [Report a Bug](https://github.com/songify-rocks/Songify/issues/new)
- [Request a Feature](https://github.com/songify-rocks/Songify/issues/new)
- [View Open Issues](https://github.com/songify-rocks/Songify/issues)

---

## 📈 Project Growth

Songify is continuously evolving thanks to its users and contributors.

### ⭐ Star History

<div align="center">

[![Star History Chart](https://api.star-history.com/chart?repos=songify-rocks/songify&type=timeline&legend=bottom-right)](https://www.star-history.com/?repos=songify-rocks%2Fsongify&type=timeline&releases=&legend=bottom-right)

</div>

### 📊 Repository Activity

![Alt](https://repobeats.axiom.co/api/embed/75a510fe9b774fe3b4c03bf72177cac01c7faae8.svg "Repobeats analytics image")

Want to see what we're working on?

Check out our [commit history](https://github.com/songify-rocks/Songify/commits/master/) and [latest releases](https://github.com/songify-rocks/Songify/releases).

---

## ❤️ Contributors

Songify wouldn't be possible without the people who help improve it.

Whether you're contributing code, reporting bugs, translating the application, or sharing feedback — thank you!

<div align="center">

<a href="https://github.com/songify-rocks/Songify/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=songify-rocks/Songify" alt="Songify contributors" />
</a>

<br><br>

[![Contributors](https://img.shields.io/github/contributors/songify-rocks/Songify?color=16a349)](https://github.com/songify-rocks/Songify/graphs/contributors)
[![Pull Requests](https://img.shields.io/github/issues-pr/songify-rocks/Songify?color=16a349)](https://github.com/songify-rocks/Songify/pulls)
[![Issues](https://img.shields.io/github/issues/songify-rocks/Songify?color=16a349)](https://github.com/songify-rocks/Songify/issues)

</div>

Contributions of all kinds are welcome.

---

## ☕ Support Songify

Songify is developed and maintained with a passion for music, streaming, and open-source software.

If you enjoy using Songify and want to support its continued development, consider becoming a supporter.

Your support helps cover infrastructure costs and makes it possible to keep improving Songify.

<div align="center">

[![Support on Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6S167PLK)

<br>

**Every contribution is appreciated. 💚**

</div>

Not able to contribute financially?

No problem!

Giving the repository a ⭐, sharing Songify with other streamers, or contributing to translations also helps tremendously.

---

## 🛠️ Built With

Songify is built using modern technologies and integrates with services that make streaming more interactive.

<div align="center">

![C#](https://img.shields.io/badge/C%23-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Twitch](https://img.shields.io/badge/Twitch-9146FF?style=flat-square&logo=twitch&logoColor=white)
![Spotify](https://img.shields.io/badge/Spotify-1DB954?style=flat-square&logo=spotify&logoColor=white)
![OBS](https://img.shields.io/badge/OBS-302E31?style=flat-square&logo=obsstudio&logoColor=white)

</div>

---

## 📜 License

Songify is licensed under the **GNU General Public License v3.0**.

See the [LICENSE](LICENSE) file for more information.

---

## 💚 Powered By

Development of Songify is supported by JetBrains through their Open Source Support Program.

<div align="center">

<a href="https://jb.gg/OpenSourceSupport">
  <img src="https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.svg" alt="JetBrains" width="220" />
</a>

<br>

*Thanks to JetBrains for supporting open-source development.*

</div>

---

<div align="center">

### Made with 💚 for streamers, by streamers.

**If Songify makes your streams better, consider giving us a ⭐!**

[![Star Songify](https://img.shields.io/github/stars/songify-rocks/Songify?style=for-the-badge&logo=github&label=Star%20Songify&color=16a349)](https://github.com/songify-rocks/Songify)

<br>

[Website](https://songify.rocks) ·
[Download](https://github.com/songify-rocks/Songify/releases/latest) ·
[Documentation](https://github.com/songify-rocks/Songify/wiki) ·
[Discord](https://discord.gg/H8nd4T4) ·
[Support](https://ko-fi.com/S6S167PLK)

</div>
