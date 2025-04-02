# WebSocket Command Reference

This document provides examples of all supported WebSocket commands used to control Songify via a WebSocket connection.

Each message must be sent as **raw JSON** with an `"action"` key and an optional `"data"` object depending on the action.

---

### 📝 Notes

- The `track` field in `queue_add` can be a full Spotify link or a text search query.
- The `requester` field is optional and will default to `""` if not specified.
- All messages must be sent as **raw JSON strings** through your WebSocket client.
---
### 🎵 Add to Queue

Adds a song to the request queue by Spotify link or search term.
```JSON
{
  "action": "queue_add",
  "data": {
    "track": "https://open.spotify.com/track/4PTG3Z6ehGkBFwjybzWkR8",
    "requester": "Viewer42"
  }
}
```

---

### 🔊 Set Volume

Sets the Spotify volume to a specific value between 0 and 100.
```JSON
{
  "action": "vol_set",
  "data": {
    "value": 80
  }
}
```
---

### ⏭️ Skip / Next Song

Skips the currently playing song.
```JSON
{
  "action": "skip"
}
```
Alternative:
```JSON
{
  "action": "next"
}
```
---

### ⏯️ Play / Pause

Toggles playback. Also supports explicit pause or play.
```JSON
{
  "action": "play_pause"
}
```
```JSON
{
  "action": "pause"
}
```
```JSON
{
  "action": "play"
}
```
---

### 💬 Send Current Song to Chat

Sends the currently playing song info to Twitch chat.
```JSON
{
  "action": "send_to_chat"
}
```
---

### 🚫 Block Current Artist

Blocks the currently playing song’s artist from future requests.
```JSON
{
  "action": "block_artist"
}
```
---

### 🚫 Block All Artists in Current Song

Blocks all artists listed on the currently playing song.
```JSON
{
  "action": "block_all_artists"
}
```
---

### 🚫 Block Current Song

Blocks the currently playing song from being requested again.
```JSON
{
  "action": "block_song"
}
```
---

### 🚫 Block Requesting User

Blocks the last user who requested a song.
```JSON
{
  "action": "block_user"
}
```
---

### 🛑 Stop Song Request Reward

Pauses all Twitch channel point song request rewards. (Only works if the Reward was created using Songify)
```JSON
{
  "action": "stop_sr_reward"
}
```
---

### 🔉 Increase Volume

Increases volume by 5%.
```JSON
{
  "action": "vol_up"
}
```
---

### 🔉 Decrease Volume

Decreases volume by 5%.
```JSON
{
  "action": "vol_down"
}
```
