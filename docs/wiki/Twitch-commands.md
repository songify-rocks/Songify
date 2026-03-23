# Twitch commands

Each command uses a **`!trigger`** in Twitch chat. Commands support **responses** and **user level** restrictions (configured in **Bot commands** / **Bot responses**).

---

## User levels

| Level | Role |
|-------|------|
| 0 | Viewer |
| 1 | Follower |
| 2 | Subscriber |
| 3 | Subscriber T2 |
| 4 | Subscriber T3 |
| 5 | VIP |
| 6 | Moderator |

---

## Commands

### `!ssr` — Song request

- **Default response:** `{artist} - {title} requested by @{user} has been added to the queue.`
- **User levels:** Viewer through Moderator (default; adjust in app)

---

### `!next` — Next

- **Response:** `@{user} {song}`

---

### `!play` — Play

- **Response:** `Playback resumed.`
- **User levels:** Moderator (default)

---

### `!pause` — Pause

- **Response:** `Playback stopped.`
- **User levels:** Moderator (default)

---

### `!pos` — Position in queue

- **Response:** `@{user} {songs}{pos} {song}{/songs}`

---

### `!queue` — Queue

- **Response:** `{queue}`

---

### `!remove` — Remove own request

- **Response:** `{user} your previous request ({song}) will be skipped.`

---

### `!skip` — Skip

- **Response:** `@{user} skipped the current song.`
- **User levels:** Moderator (default)

---

### `!voteskip` — Vote skip

- **Response:** `@{user} voted to skip the current song. ({votes})`
- **Note:** `SkipCount` (e.g. votes needed) is configurable in the app.

---

### `!song` — Current song

- **Response:** `@{user} {single_artist} - {title} {{requested by @{req}}}`

---

### `!songlike` — Add song to playlist

- **Response:** `The Song {song} has been added to the playlist {playlist}.`
- **User levels:** Moderator (default)

---

### `!vol` — Volume

- **Response:** `Spotify volume at {vol}%`
- **Set response:** `Spotify volume set to {vol}%`
- **User levels:** Moderator (default)

---

### `!cmds` — List commands

- **Response:** `Active Songify commands: {commands}`

---

Triggers and responses can be customized in Songify. This page reflects common defaults.
