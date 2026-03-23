# WebSocket commands

Connect to the local WebSocket URL (see [Web server and API](Web-server-and-API)):

`ws://127.0.0.1:<port>/`

Send **raw JSON** with an `"action"` key and optional `"data"` object.

---

## Notes

- `queue_add` — `track` may be a full Spotify URI or URL, or a search string.
- `requester` is optional; defaults to empty if omitted.
- `play_playlist` — Starts playback from a playlist (Spotify); see below.

---

## `queue_add`

```json
{
  "action": "queue_add",
  "data": {
    "track": "https://open.spotify.com/track/4PTG3Z6ehGkBFwjybzWkR8",
    "requester": "Viewer42"
  }
}
```

---

## `vol_set`

```json
{
  "action": "vol_set",
  "data": {
    "value": 80
  }
}
```

---

## `skip` / `next`

```json
{ "action": "skip" }
```

```json
{ "action": "next" }
```

---

## `play_pause` / `pause` / `play`

```json
{ "action": "play_pause" }
```

```json
{ "action": "pause" }
```

```json
{ "action": "play" }
```

---

## `send_to_chat`

Sends current song info to Twitch chat.

```json
{ "action": "send_to_chat" }
```

---

## Blocklist actions

**Block current artist**

```json
{ "action": "block_artist" }
```

**Block all artists on current track**

```json
{ "action": "block_all_artists" }
```

**Block current song**

```json
{ "action": "block_song" }
```

**Block last requester**

```json
{ "action": "block_user" }
```

---

## `stop_sr_reward`

Pauses Twitch channel point song request rewards (only reliable for rewards created through Songify).

```json
{ "action": "stop_sr_reward" }
```

---

## `vol_up` / `vol_down`

Changes volume by 5%.

```json
{ "action": "vol_up" }
```

```json
{ "action": "vol_down" }
```

---

## `play_playlist`

Starts Spotify playback from a playlist URI or ID.

```json
{
  "action": "play_playlist",
  "data": {
    "playlist": "spotify:playlist:37i9dQZF1DXcBWIdG12345",
    "Shuffle": false
  }
}
```

---

## `youtube`

Updates YouTube-related overlay data when used by supported integrations. Format matches internal models—see application source if you use this action.
