# 🎵 Songify Twitch Commands

Each command uses a `!trigger` in Twitch chat. Commands have associated responses and user level restrictions.

### 👥 User Levels
| Level | Role             |
|-------|------------------|
| 0     | Viewer           |
| 1     | Follower         |
| 2     | Subscriber       |
| 3     | Subscriber T2    |
| 4     | Subscriber T3    |
| 5     | VIP              |
| 6     | Moderator        |

---

## 📜 Commands List

### `!ssr` - **Song Request**
- **Response:** `{artist} - {title} requested by @{user} has been added to the queue.`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator

---

### `!next` - **Next**
- **Response:** `@{user} {song}`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator

---

### `!play` - **Play**
- **Response:** `Playback resumed.`
- **User Levels:** Moderator

---

### `!pause` - **Pause**
- **Response:** `Playback stopped.`
- **User Levels:** Moderator

---

### `!pos` - **Position**
- **Response:** `@{user} {songs}{pos} {song}{/songs}`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator

---

### `!queue` - **Queue**
- **Response:** `{queue}`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator

---

### `!remove` - **Remove**
- **Response:** `{user} your previous request ({song}) will be skipped.`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator

---

### `!skip` - **Skip**
- **Response:** `@{user} skipped the current song.`
- **User Levels:** Moderator

---

### `!voteskip` - **Voteskip**
- **Response:** `@{user} voted to skip the current song. ({votes})`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator  
- **Additional Property:** `SkipCount: 5`

---

### `!song` - **Song**
- **Response:** `@{user} {single_artist} - {title} {{requested by @{req}}}`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator

---

### `!songlike` - **Songlike**
- **Response:** `The Song {song} has been added to the playlist {playlist}.`
- **User Levels:** Moderator

---

### `!vol` - **Volume**
- **Response:** `Spotify volume at {vol}%`  
- **Additional Property:** `VolumeSetResponse: Spotify volume set to {vol}%`
- **User Levels:** Moderator

---

### `!cmds` - **Commands**
- **Response:** `Active Songify commands: {commands}`
- **User Levels:** Viewer, Follower, Subscriber, Subscriber T2, Subscriber T3, VIP, Moderator
