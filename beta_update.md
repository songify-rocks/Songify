# 🎶 Songify — 1.8 Beta (Latest Update)

Please help us by reporting any bugs you encounter in the beta version.  
You can create a new issue on GitHub or join our [Discord Server](https://discord.com/invite/H8nd4T4) to share feedback in the beta-discussion channels.

---

## 🆕 Latest Beta Changes
- **Deep Link Support**
One-click token importing: You can now import Twitch and Songify tokens directly through special links. From the new Songify website you can import the Songify API token with one click. Same for the alternative Twitch Login method (auth code).

- **Changed phrasing on Refund Conditions**
Updated terminology: What was previously called "Refund Always" is now clearly labeled as "Free Song Requests".

- **Interface Improvements**
Settings Window Redesign: Reorganized the Twitch rewards section for better navigation.

- **Chat & User Management**
More accurate user data: Improved how the app handles Twitch chat messages and user information. Enhanced user role checking for commands and requests. 

- **Removed Twitch IRC**
Removed Twitch IRC client and now fully work with Twitch Eventsub websockets and API to receive and send chat messages.

- **Under the Hood Improvements**
Crash prevention: Fixed potential crashes when importing tokens. Improved validation when processing song requests. 

- **Bug Fixes**
Fixed issues with token importing that could cause the app to crash. Resolved problems with window management during setup. Improved input validation to prevent invalid song requests. Enhanced connection stability for Twitch chat

---

## ✨ New Features
- **Enhanced Refund System**  
  Improved refund handling with detailed reason tracking and better user feedback. Refunds now include specific reasons (e.g., "song blocked", "queue full") and support localized messages.  
- **YouTube Music Desktop Integration**  
  Added full API support for YouTube Music Desktop Client with playback control, queue management, and volume adjustment.  
- **Song Requests for Bits**  
  The toggle button for this option now works properly.  
- **New Tooltips for Checkmarks**  
  Checkmarks have been replaced with green/red dots and have richer tooltips.  
- **Real-Time Twitch Events**  
  Direct WebSocket connection to Twitch for instant and reliable detection of redemptions and events.  
- **Song Requests with Bits**  
  Viewers can request songs using Twitch Bits with a configurable minimum amount.  
- **Cloud Backup & Restore (Premium)**  
  Save and restore Songify settings to the cloud with preview before importing.  
  > Requires a **Ko-fi membership**.  
  > [Guide: How to link Ko-fi with Songify](https://v2.songify.rocks/faq/how-to-link-ko-fi-with-songify).  
- **Full YouTube Music Support**  
  Request songs from YouTube using the [YouTube Music Desktop Client](https://github.com/th-ch/youtube-music).  
  - [Setup Guide](https://github.com/songify-rocks/Songify/blob/master/th-ch%20Youtube-Music.md)  
- **Spotify Rate Limit Monitor**  
  Added a rate limit monitor (View -> Console -> API Metrics)  
  - <img width="633" height="285" alt="image" src="https://github.com/user-attachments/assets/85862208-c41e-44bf-94cd-f3ac38828b3c" />
  - <img width="633" height="285" alt="image" src="https://github.com/user-attachments/assets/d8a1cc9a-cf80-4678-8287-0ee3288323af" />  

- **Command Aliases**  
  Added command aliases (Right-click on command -> Add Aliases)  


---

## 🌍 Localization
- Added Dutch (NL) translation  
- Updated localization strings across multiple languages: Belarusian, German, Spanish, French, Italian, Polish, Portuguese, and Russian  
- Added `{reason}` parameter for refund responses in all supported languages  
- Added Belarusian language support  
---

## ⚡ Improvements
- **Refunds**  
  Automatic refunds now provide clear, localized reasons (explicit content, queue full, blocked, etc.).  
- **YouTube Music**  
  Improved queue management and track detection.  
- **UI/UX**  
  Cleaner settings and reward screens; improved elements for displaying refund information.  
- **Technical Enhancements**  
  - Refactored web server command handling for better structure and readability  
  - Improved Songify WebSocket server to support commands on YouTube Music Desktop  

---

## 🐞 Fixes
- More stable Twitch event handling and song requests, even after temporary disconnects  
- Better error handling so Songify recovers faster when issues occur  

---

## 🛠 Behind the Scenes
- Simplified and streamlined the codebase by removing outdated components  
- Completely rewrote Spotify API handling to make it more future-proof and less error-prone  

---

This was a major update that took significant time and effort to develop.  
If you enjoy using Songify and want to support its continued development, consider donating:  

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6S167PLK)

---

### 🔑 Checksums

Songify.zip:
MD5:    80559C38090A94A8F8D93056E0493F02
SHA1:   E695B4E0492D2A32DF56881090AF7CCC60299E93
SHA256: 935D5E8DAB3D625122881E9BA4C8196D374042DAFE492DC89314259519865F47

Songify.exe:
MD5:    484D2EC11C8A2A96939D4422AD07464E
SHA1:   336A653CDCBB91528D520AA269C67249C110D982
SHA256: C21894DBE7A7CDAF068E02678222D21E95FD6DC3C7EE21EB6B65C3617575915B
