# 📡 WebSocket: Subscribe to real-time data

Songify provides a WebSocket endpoint that streams live data such as the current track, requester, queue, and user information.

## Endpoint

```
/ws/data
```

## How to connect

#### JavaScript (Browser / Node.js)

```javascript
const ws = new WebSocket("ws://localhost:PORT/ws/data");

ws.onmessage = (event) => {
  const data = JSON.parse(event.data);
  console.log(data);
};
```

#### C# example

```csharp
using System.Net.WebSockets;
using System.Text;

using var client = new ClientWebSocket();
await client.ConnectAsync(new Uri("ws://localhost:PORT/ws/data"), CancellationToken.None);

var buffer = new byte[8192];

while (client.State == WebSocketState.Open)
{
    var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

    Console.WriteLine(message);
}
```

## Response structure

Each message contains the full current state:

```json
{
   "UserInfo":{
      "TwitchUser":{
         "Id":"string",
         "Login":"string",
         "BroadcasterType":"string"
      },
      "SpotifyUser":{
         "Id":"string",
         "DisplayName":"string",
         "Product":"string"
      }
   },
   "SongifyInfo":{
      "Version":"string",
      "Beta":"boolean"
   },
   "Track":{
      "Data":{
         "Artists":"string",
         "Title":"string",
         "Albums":[
            {
               "Height":"integer",
               "Width":"integer",
               "Url":"string"
            }
         ],
         "SongId":"string",
         "DurationMs":"integer",
         "IsPlaying":"boolean",
         "Url":"string",
         "DurationPercentage":"number",
         "DurationTotal":"integer",
         "Progress":"integer",
         "Playlist":"string | null",
         "FullArtists":[
            {
               "ExternalUrls":{
                  "spotify":"string"
               },
               "Href":"string",
               "Id":"string",
               "Name":"string",
               "Type":"string",
               "Uri":"string"
            }
         ]
      },
      "CanvasUrl":"string",
      "IsInLikedPlaylist":"boolean",
      "Requester":{
         "Name":"string",
         "ProfilePicture":"string"
      }
   },
   "Queue":{
      "Count":"integer",
      "Requests":[
         {
            "queueid":"integer",
            "uuid":"string",
            "trackid":"string",
            "artist":"string",
            "title":"string",
            "length":"string",
            "requester":"string",
            "albumcover":"string",
            "playerType":"string | null",
            "IsLiked":"boolean",
            "FullRequester":{
               "Id":"string",
               "DisplayName":"string",
               "ProfileImageUrl":"string"
            }
         }
      ],
      "Tracks":[
         {
            "queueid":"integer",
            "uuid":"string",
            "trackid":"string",
            "artist":"string",
            "title":"string",
            "length":"string",
            "requester":"string",
            "albumcover":"string",
            "playerType":"string | null",
            "IsLiked":"boolean",
            "FullRequester":"object | null"
         }
      ],
      "songRequests":{
         "chat":"boolean",
         "reward":"boolean"
      }
   }
```

## Behavior

- The WebSocket pushes updates automatically whenever:
  - the current track changes
  - playback state updates
  - the queue changes
  - requester info updates

- Each message contains the full state, not partial updates.

## Notes

- Replace `PORT` with your configured Songify WebServer port  
- Use `ws://` for local connections   
- No authentication is required unless configured otherwise  

---

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
---

### Enable / Disable Song Requests

#### Enable - `sr_enable` (same payloads work for alias `sr_open`)

```json
{"action": "sr_enable"}
```

```json
{"action": "sr_enable", "data": {}}
```

```json
{"action": "sr_enable", "data": {"scope": "both"}}
```

```json
{"action": "sr_enable", "data": {"scope": "reward"}}
```

```json
{"action": "sr_enable", "data": {"scope": "command"}}
```

Alias examples (identical behavior to `sr_enable`):

```json
{"action": "sr_open"}
```

```json
{"action": "sr_open", "data": {}}
```

```json
{"action": "sr_open", "data": {"scope": "both"}}
```

```json
{"action": "sr_open", "data": {"scope": "reward"}}
```

```json
{"action": "sr_open", "data": {"scope": "command"}}
```

#### Disable - `sr_disable` (same payloads work for alias `sr_close`)

```json
{"action": "sr_disable"}
```

```json
{"action": "sr_disable", "data": {}}
```

```json
{"action": "sr_disable", "data": {"scope": "both"}}
```

```json
{"action": "sr_disable", "data": {"scope": "reward"}}
```

```json
{"action": "sr_disable", "data": {"scope": "command"}}
```

Alias examples (identical behavior to `sr_disable`):

```json
{"action": "sr_close"}
```

```json
{"action": "sr_close", "data": {}}
```

```json
{"action": "sr_close", "data": {"scope": "both"}}
```

```json
{"action": "sr_close", "data": {"scope": "reward"}}
```

```json
{"action": "sr_close", "data": {"scope": "command"}}
```

| Scope        | Meaning                                      |
|---------------|----------------------------------------------|
| `chat`   | Turns the Song Request command on/off              |
| `reward`  | Turns the Song Request reward on/off              |
| `both`     | Turns both on/off      |

Optional body defaults to **`both`** if omitted or if `data` is missing:
