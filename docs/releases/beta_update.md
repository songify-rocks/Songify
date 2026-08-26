# ✨ Songify **1.8.0** – Pre Release

Songify 1.8.0 is our biggest update yet. Faster, cleaner, and packed with improvements.

---

## 🍐 Pear (YouTube Music) Support

- Full support for [**Pear** 🍐](https://github.com/pear-devs/pear-desktop) (formerly known as YouTube Music Dekstop (th-ch))
- Improved playback detection and stability  
- Working commands: play, pause, skip, voteskip, vol, vol [0-100]
- UI & settings updated to reflect the new player

---

## 🎵 Spotify Improvements

- More reliable and accurate track detection  
- Faster communication between Songify and Spotify  
- Better handling of errors, playback state, and metadata
- songlike can now be used by the requester (if enabled on the command settings)
- Automatically add Song Requests to the liked songs playlist now reliably works (songlike too). For that the playlist has to be cached on app startup (also happens when selecting a new liked song playlist). This can take a few seconds (~150 tracks per second). Tested with a 5500+ track playlist and it took about 32s.

---

## 💬 Smarter Twitch Song Requests

- Added **Bits support** for requesting songs  
- Added **alias support** (multiple names for the same command)  
- New option: **Skip only non-requested songs** 
  - This disables Skip-Reward for requested songs
- Better handling of channel point requests  
- Much more accurate and reliable permission detection  
  (mods, VIPs, followers, and T1–T3 subscribers)

---

## ⚡ Major Twitch Stability Improvements

A huge rewrite of the Twitch system makes Songify far more stable:

- Stronger chat connection  
- More accurate user role & permission detection  
- Better error recovery  
- Cleaner internal logic = fewer surprises and more predictable behavior

(EventSub WebSocket support has been added behind the scenes for future improvements.)

---

## ☁️ Cloud Settings Enhancements

- Improved sync between local and cloud settings  
- Added **cloud vs. local comparison** tools  
- Better UI feedback for cloud-enabled users  
- Smoother experience when switching between machines

---

## 🪄 New Quality-of-Life Features

- **Windows Playback API support** → detect “what’s playing” across local music players  
- Updated tooltips and UI hints throughout Songify  
- Improved deep-link handling  
- Cleaner error/warning messages

---

## 🌍 Localization Updates

New or updated translations for:

- Dutch  
- French  
- Spanish  
- Italian  
- Portuguese  
- Belarusian  

Plus improvements across many existing languages.

---

## 🧹 General UI Improvements

- Cleaner and more responsive Settings window  
- Improved refund tab layout  
- Updated command list and permission UI  
- More consistent theming  
- Removed outdated integrations and unused options  
- Overall cleaner, more modern feel

---

## 🐞 Bug Fixes

- Fixed incorrect ordering of YouTube song request results (thanks to @NaGeL182 for the help)  
- Fixed rare crashes and several null-reference issues  
- Fixed label typos and inconsistent UI states  
- Fixed Allow only songs from specific playlist not working (https://github.com/songify-rocks/Songify/issues/170)
- Fixed !bansong command be usable by any user (https://github.com/songify-rocks/Songify/issues/164, https://github.com/songify-rocks/Songify/pull/153) (@folle)
- Fixed stream online status check (https://github.com/songify-rocks/Songify/issues/166)
- Improved handling of Twitch login and token importing  
- More reliable Spotify and Twitch recovery (fewer “stuck state” issues)

---

## 📦 Other Improvements

- Streamlined internal logic for better performance  
- Removed outdated services and old APIs  
- Numerous refactors to keep Songify stable long-term

---

## 🌐 New: Redesigned Songify Website

We’re excited to launch the new **Songify website**!

It includes:
- Queue Display  
- Status Page  
- FAQ Section
- Widgets (Premium and Free)
- Login through Twitch
- ... and more

👉 Visit: https://v2.songify.rocks

---

## ❤️ Thank You!

Thank you to everyone who helped test the beta versions and provided feedback.  
Songify 1.8.0 is a big milestone, enjoy the new features, and keep the suggestions coming!
