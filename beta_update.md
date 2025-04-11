# 🎉 Songify 1.7.0 – Official Release Notes  
**🚨 Important: Please log out and back into Twitch to refresh your permissions.**

---

## 🔍 What’s New

- 🌐 **WebSocket Command Support**  
  Control Songify externally with a powerful WebSocket API.  
  [📄 View WebSocket Documentation](https://github.com/songify-rocks/Songify/blob/master/WebSockets.md)

- ⚠️ **"Ignore and Continue" Offline Mode**  
  Start Songify without an internet connection using a new option in the startup dialog.

- ⏱ **Time-to-Play Estimation (`{ttp}`)**  
  The `{ttp}` placeholder shows an estimated time until a requested song will play (in `mm:ss` format).

- 🛠 **Redesigned Command System (Breaking Change)**  
  The **Commands**, **Responses**, and **Rewards** sections have been completely redesigned for easier management.  
  - All command settings are now stored in `TwitchCommands.yaml`  
  - You will need to **reconfigure your commands manually** after updating

- 🎵 **YouTube Music Desktop & Browser Extension Support**  
  - Full integration with [YouTube Music Desktop App](https://github.com/ytmdesktop/ytmdesktop)  
  - Initial support for the upcoming **Songify Browser Extension** (awaiting approval from Chrome Web Store)

- 🖼️ **Requester Profile Picture Storage**  
  Saves the current requester’s profile image as `requester.png`

- 👤 **Viewer List Window**  
  Displays all active Twitch chat users along with their SR status, user level, and sub tier. Refreshes every 30 seconds.

- 🏆 **Expanded User Level Handling**  
  Supports Twitch subscriber tiers (1, 2, 3).  
  🔁 Requires re-linking your Twitch account due to updated scopes.

- 🔄 **Single Instance Handling**  
  Launching a second instance will now bring the existing window to the foreground if minimized or hidden.

---

## 🔧 Full Changelog

### ⭐ New Features

#### ✅ WebSocket Command Support
Supports external commands:
```queue_add, vol_set, skip, next, play, pause, play_pause, send_to_chat, block_song, block_artist, block_all_artists, block_user, stop_sr_reward, vol_up, vol_down```

- Default requester is set to `""` if not provided.

#### 🚫 “Ignore and Continue” Button
Use Songify offline by skipping the internet check on startup.

#### ⏳ Time-to-Play (`{ttp}`)
- Estimate displayed in song request replies.
- Reflects the current queue length and playback status.

#### 🛠 Redesigned Command System
- New UI for **Commands**, **Responses**, and **Rewards**
- Commands now stored in `TwitchCommands.yaml`
- ⚠️ You must **recreate your command setup manually**

#### 🖼 Requester Profile Picture
- Automatically saves a `requester.png` file for the current song requester.

#### 👥 Viewer List
- View all Twitch chat users, including their roles and request statuses.

#### 🧠 Smart Instance Behavior
- Prevents multiple Songify instances from running simultaneously.
- Automatically brings the original window into focus.

---

### 🔁 Improvements

#### 🎵 Song Request Logic
- Improved handling of blocked songs, explicit content, and duplicates.
- More reliable fallback logic when the queue window is closed.

#### 📁 Output File Behavior
- Output files are now cleared when using the “Clear Pause” option.

#### 🔧 Async & Error Handling
- Improved async methods for better performance and stability.
- Refactored error handling to reduce app crashes and provide better debug output.

#### 🔗 Spotify Auth Redirects
- Updated internal redirect URI to `http://127.0.0.1` per Spotify’s latest requirements.  
  [Read more](https://developer.spotify.com/blog/2025-02-12-increasing-the-security-requirements-for-integrating-with-spotify)

---

### 🐞 Bug Fixes

- Prevented crash when `currSong` was null  
- Fixed a rare crash during song requests  
- Fixed issue where Songify starts minimized or off-screen  
- Resolved Twitch reward sync UI display issues  
- Fixed translation formatting and accuracy across languages  
- Fixed Twitch command crashes caused by outdated scopes  
- Resolved crashes related to Windows notifications  
- Fixed display issues with token expiration time  
- Logout and re-login now work without needing to restart the app  
- Added refresh button to Twitch settings  
- Token expiration is now shown for both main and bot accounts  

---

## 🛠 Additional Enhancements

- 🎧 **Spotify Credentials Now Required**  
  You must use your own Spotify API credentials.  
  [🎓 Setup Guide](https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup)

- 🌐 **Improved Internet & Stream Checks**
  - Internet check is now non-blocking — Songify stays open and retries automatically
  - Stream status check now refreshes every 5 seconds

- 🖥 **UI Upgrades**
  - “Get Beta Updates” option moved to **Settings → System**
  - Removed outdated “Hide user info” setting

---

## 🌍 Language Support

- Polished translations across all supported languages  
- Language switching now works **without requiring a restart**
