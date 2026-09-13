# Migrating from 1.8

This guide is for people updating **Songify 1.8.13** (or earlier 1.8.x) to **2.0**.

What’s new in the release: [Songify 2.0 patch notes](https://github.com/songify-rocks/Songify/blob/master/docs/releases/beta_update.md).

Settings and listening history migrate automatically. Overlays that read `Songify.txt` / `cover.png` keep working. A few 1.8 habits change — those are listed here.

> **Screenshot:** `hero-home.png`  
> 2.0 Home after a successful upgrade: now playing, sidebar, status bar.  
> ![Home](./screenshots/2.0/hero-home.png)

---

## Before you update

1. **Close Songify 1.8.13.**
2. **Copy the folder that contains `Songify.exe`.** That is where `AppConfig.yaml` and history live. Put the copy somewhere safe (Desktop / Documents). 2.0 rewrites config on first launch and converts history.
3. Extract the new **`Songify.zip`** over your install (or into a new folder, then copy `AppConfig.yaml` across if you want a parallel install).
4. Run **`Songify.exe`**. First launch may show a short **history migration** window if your old `history.shr` is large.

---

## After you update

Work through this once. Most 1.8 setups only need steps 1–3.

### 1. Add a Songify API token

Widget upload, the online queue, recap, YouTube metadata helper, and cloud sync now need a **Songify API token**. The old **AccessKey** is gone and is no longer sent.

1. Open [songify.rocks/token-import](https://songify.rocks/token-import) and generate a token (shown **once**).
2. Paste it in Songify: **Home checklist**, **Help**, or **Settings → System → Songify token**.
3. If you lose it, rotate it on the website and paste the new one.

**Without a token:** local files (`Songify.txt`, `cover.png`) and the **local web server** still work. Hosted widgets, the online queue page, song upload, cloud settings, and recap will not.

> **Screenshot:** `migration-api-token.png`  
> Settings → System with Songify token field, or Home checklist “Add Songify token” step.  
> ![Songify API token](./screenshots/2.0/migration-api-token.png)

### 2. Confirm Spotify and Twitch

Tokens, Client ID, and Client Secret from 1.8 usually carry over.

- Status bar dots should go green after a few seconds.
- If they stay red, use **Help → Getting started** or click the **Spotify / Twitch** status buttons.
- Spotify still uses **your** Client ID and Secret (same as 1.8).

> **Screenshot:** `status-bar-connected.png`  
> Footer with Twitch API, Twitch chat, Spotify, Pear, and web server as clickable icons.  
> ![Status bar](./screenshots/2.0/status-bar-connected.png)

### 3. Confirm OBS

Point OBS at the **same output folder** as before:

| File | Use in OBS |
|------|------------|
| `Songify.txt` | Text (GDI+) → Read from file |
| `Artist.txt` / `Title.txt` | Optional split output |
| `url.txt` | Track URL |
| `cover.png` | Image source (always written when the player provides art) |
| `canvas.mp4` | **New, optional** — Spotify canvas, if canvas download is on |

**Download album cover** is always on in 2.0. There is no toggle anymore.

> **Screenshot:** `settings-output.png`  
> Settings → Output: folder, format placeholders, canvas option.  
> ![Output settings](./screenshots/2.0/settings-output.png)

### 4. Hosted widget

If you use a Browser source on songify.rocks / widget.songify.rocks:

1. Add the API token (step 1).
2. Turn on **Upload song info** (**Settings → Output**). **Tools → Widget** can turn this on for you.
3. Keep the same widget URL in OBS. Your **UUID** is unchanged, so the stream ID stays the same once upload works again.

### 5. History / recap

- Local history is now **`history.yaml`** next to Songify.
- On first start, **`history.shr` is converted and then deleted**.
- **Uploading history to the website from the app is removed.**
- Stream recap lives on [songify.rocks/recap](https://songify.rocks/recap) (Premium).
- The in-app **History** page is local only (calendar).

> **Screenshot:** `history-calendar.png`  
> History page with calendar days that have plays.  
> ![History](./screenshots/2.0/history-calendar.png)

### 6. Updates channel

**“Get beta updates”** is gone. Use **Settings → System → Release channel**:

| Channel | What you get |
|---------|----------------|
| **Stable** | Production releases |
| **Beta** | 2.0 betas |
| **Dev (Unstable)** | Cutting-edge builds |

If you had “Get beta updates” **on** in 1.8, you are on **Beta** automatically.

> **Screenshot:** `settings-release-channel.png`  
> Settings → System → Release channel dropdown.  
> ![Release channel](./screenshots/2.0/settings-release-channel.png)

---

## Breaking changes

### Windows

- Songify 2.0 runs on **.NET 10**.
- You need **Windows 10 version 1809** (October 2018) or later, or **Windows 11**.
- **Windows 7 / 8 are not supported.**
- The main window minimum size is **900×500**. Home **scrolls** on small screens. Allow smaller windows under **Settings → Appearance → Overrule minimum size**.
- In-app patch notes use **WebView2**. If it is missing, Songify opens them in your browser instead.

### Songify API token (replaces AccessKey)

Songify no longer stores or sends an **AccessKey**. Uploads to songify.rocks authenticate with:

- your **UUID** (unchanged, still in config), and
- a **Songify API token** from your website account (Bearer auth).

Configs exported from 1.8 that only had an AccessKey will **not** upload until you add a token.

### History file format

| 1.8.13 | 2.0 |
|--------|-----|
| `history.shr` (binary) | `history.yaml` |
| Optional upload from the app | Upload removed |

Migration is one-way: after conversion, `history.shr` is deleted. Keep your folder backup if you need to roll back to 1.8.

### Album covers always on

There is no **Download album cover** checkbox. `cover.png` is written whenever the player provides art (Spotify, Pear, Windows Playback, Browser Companion, and other sources that have it).

Spotify **canvas** download is still optional (**Settings → Output**). When enabled, `canvas.mp4` is written to the same folder and can play on Home.

### First-run wizard

- **Existing 1.8 installs skip the wizard** (detected from Spotify/Twitch/Client ID already being set).
- **New installs** get a setup wizard and a **Getting started** list on Home until the basics are done.
- Re-run the wizard anytime from **Help**.

### UI chrome

The old separate windows (main window, Settings, History, Queue, Blocklist, Console) are one **sidebar shell**. See [Where things moved](#where-things-moved-1813--20).

### Config import

Cloud restore can be **blocked** if the snapshot’s schema is newer than this build — update Songify first.

---

## Where things moved (1.8.13 → 2.0)

> **Screenshot:** `nav-sidebar.png`  
> Left navigation: Home, Queue, Blocklists, History, Users, Settings, Help, About. Collapsed icons with tooltip.  
> ![Navigation](./screenshots/2.0/nav-sidebar.png)

| 1.8.13 | 2.0 |
|--------|-----|
| Center of the main window (now playing) | **Home** |
| Song requests → Queue | **Queue** in the sidebar, or **Tools → Queue window** (pop-out) |
| History window | **History** in the sidebar (calendar) |
| Song requests → Blocklist | **Blocklist** in the sidebar |
| User list | **Users** in the sidebar |
| View → Console | **Help** (live log), or **Tools → Console window** (detach) |
| File → Settings | **Settings** in the sidebar |
| File → Widget | **Tools → Widget** (gallery + generator) |
| File → Patch notes / Help | **Help** |
| Twitch → Connect / login | **Twitch** menu, or click the **Twitch** status icons |
| Player dropdown | Still on **Home** (grouped: song requests vs now playing only) |
| Footer status icons | Same idea — **clickable** (connect, start web server, open Pear, …) |
| “Get beta updates” | **Settings → System → Release channel** |

Settings tabs are the same ideas (System, Appearance, Output, Twitch, Rewards, Song requests, Bot commands, Spotify, YouTube, Web server, Config) in a cleaner layout.

Bot responses are one catalog: edit, reset, and preview in one place.

> **Screenshot:** `settings-overview.png`  
> Settings with tabs and a typical Twitch or Appearance page.  
> ![Settings](./screenshots/2.0/settings-overview.png)

---

## Rolling back to 1.8.13

2.0 converts history and rewrites `AppConfig.yaml`. To go back:

1. Close Songify 2.0.
2. Restore the **folder copy** you made before updating (exe + `AppConfig.yaml` + `history.shr`).
3. Do **not** copy `history.yaml` back into 1.8 — 1.8 does not read it.
4. If you already let 2.0 delete `history.shr` and you have no backup, in-app history from before the update is gone. Recap on the website is separate.

---

## Known caveats

- First launch after 1.8.x migrates history and may rewrite `AppConfig.yaml`.
- A few labels or empty states may still be rough. Missing translations fall back to English.
- Hosted widgets / recap / cloud do nothing until a **Songify API token** is set.
- Small laptops: enable **Overrule minimum size** or use **Interface scale** at 100% and scroll Home.
- Pear with API authorization: you must **Allow** Songify in Pear after clicking Authorize (or selecting Pear). A WebSocket connect alone will not show the prompt.

Report anything that worked in **1.8.13** and does not in 2.0:

- [GitHub issues](https://github.com/songify-rocks/Songify/issues)
- [Discord](https://discord.gg/H8nd4T4)

Attach logs from `%LocalAppData%\Songify.Rocks\Logs` (**Help → Log folder**).

---

## Screenshot checklist

| File | What to capture |
|------|-----------------|
| `hero-home.png` | Home after upgrade |
| `migration-api-token.png` | Token field (Settings or checklist) |
| `status-bar-connected.png` | Footer icons, connected |
| `settings-output.png` | Output folder + canvas |
| `history-calendar.png` | History calendar |
| `settings-release-channel.png` | Release channel |
| `nav-sidebar.png` | Sidebar expanded and/or collapsed + tooltip |
| `settings-overview.png` | Settings chrome |

---

## See also

- [Songify 2.0 patch notes](https://github.com/songify-rocks/Songify/blob/master/docs/releases/beta_update.md)
- [Getting started](Getting-Started)
- [Widget and OBS](Widget-and-OBS)
- [Troubleshooting](Troubleshooting)
