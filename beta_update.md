# 🎉 Patchnotes 1.6.8 BETA
### ❗ Please log out of Twitch and log back in if you haven't already ❗

---

## 🔍 What’s New in This Update
- Added **WebSocket command support** for external control [📄 WebSocket Docs](https://github.com/songify-rocks/Songify/blob/master/WebSockets.md)
- New **“Ignore and Continue”** option for startup without internet
- Added **Belarusian translation**
- Added **Time-to-play estimation** (`{ttp}`) for song request replies
- Requester’s **profile picture now saved**
- Fixed multiple crashes and visibility bugs

# 🔧 Full Changelog

## ⭐ New Features

### WebSocket Command Support
- Added support for `queue_add`, `vol_set`, `skip`, `next`, `play`, `pause`, `play_pause`, `send_to_chat`, `block_song`, `block_artist`, `block_all_artists`, `block_user`, `stop_sr_reward`, `vol_up`, `vol_down`
- Default requester is set to `""` if not provided
- More info: [📄 WebSocket Docs](https://github.com/songify-rocks/Songify/blob/master/WebSockets.md)

### “Ignore and Continue” Button
- Added a third option to the no-internet dialog to continue using Songify offline

### Time-to-Play Estimation
- `{ttp}` placeholder now supported in song request responses to estimate when a song will play (mm:ss)

### Requester Profile Picture
- New `requester.png` file is created for the current requester

## 🔁 Changes

### Song Request Validation
- Improved checks for blocked/explicit content, playlist enforcement, and duplicates
- Fallback logic improved when queue window is closed

### Output File Behavior
- Split output files are now cleared when the "Clear Pause" option is used

### Async Error Handling
- Refactored async methods for better stability and debugging

## 🐛 Fixes

### Core Fixes
- Prevented crash when `currSong` is null
- Fixed rare crash on song request
- Fixed issue where Songify starts minimized or off-screen
- Fixed Twitch reward sync UI glitch

### Translation Fixes
- Improved accuracy and formatting for multi-language support

<br>
<br>

# 🔄 Previous Beta Updates

### 🎵 YouTube Music Desktop Support
- Integration with [YTMDesktop](https://github.com/ytmdesktop/ytmdesktop)
- Retrieves Title, Artist, and Thumbnail
- Fully widget-compatible (no YouTube SR)

### 🏆 Expanded User Levels
- Supports checking for Twitch subscriber tiers (1, 2, 3)
- Requires re-linking Twitch due to new scopes

### 👥 Viewer List Window
- View all Twitch chat users (SR status, User level, Sub tier)
- Refreshes every 30 seconds

### 🔄 Inter-Process Communication
- Running a second Songify instance brings the first one to front if minimized

### ⚙️ New Commands & Responses
- Added `!commands` to list all commands in chat
- Added response when user level is too low or rewards are redeemed

### 🎟️ Reward Enhancements
- Choose between SR or skip action for channel point rewards
- Skip reward now supported

---

## 🛠 Other Improvements

### Spotify API Requirement
- You must now use your own Spotify API credentials
- Setup instructions: [Spotify Setup Guide](https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup)

### Internet & Stream Checks
- Internet checks are now non-blocking — Songify stays open and retries
- Stream status check now runs every 5 seconds

### UI Improvements
- Redesigned **Commands**, **Responses**, and **Rewards** tabs
- Commands can now send responses as Twitch chat announcements
- “Hide user info” option removed
- “Get Beta Updates” moved to Settings → System

### Revamped Command System
- Commands now stored in `TwitchCommands.yaml` for easier customization

---

## 🧹 Fixes (from earlier beta updates)

- Fixed command crash due to outdated Twitch scopes
- Fixed Windows notifications crash
- Fixed token expiry display issues
- Fixed logout/re-login without restart
- Added refresh button to Twitch settings tab
- Added token expiration info (main + bot)

---

## 🌍 Language Support

- Improved and corrected translations across supported languages
- Language switching now works without needing to restart the app
