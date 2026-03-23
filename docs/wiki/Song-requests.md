# Song requests

Viewers can request songs via **Twitch channel points** and/or **chat commands** (depending on your settings). Playback and queue rules use **Spotify** when Songify is in Spotify mode.

---

## Requirements

1. **[Spotify setup](Spotify-setup)** completed and account linked.  
2. **Spotify Premium** on the account used for playback.  
3. **[Twitch setup](Twitch-setup)** — API login, bot connected, correct channel.  
4. In **Settings → Song requests**, enable the sources you want (channel points, commands).

---

## Channel point rewards

1. In **Settings → Rewards**, use **Create new reward** (or select an existing compatible reward).
2. Adjust the reward on the Twitch dashboard if needed.
3. **Refresh rewards** in Songify and select the reward in the dropdown.
4. Enable **song requests via channel points** under Song requests settings.

Rewards created through Songify can work with **automatic refunds** under conditions you configure (refund section in settings).

---

## Chat command requests

The default song request command is **`!ssr`** (configurable under **Bot commands**). Enable the command and set user levels, cooldowns, and per-user limits in Songify settings.

Full command list: [Twitch commands](Twitch-commands).

---

## Queue window

**Song requests → Queue** shows pending requests. Removing an entry in Songify does not always remove the same item from Spotify’s own queue—behavior depends on version and player; treat the in-app queue as Songify’s request list.

---

## Blocklist

Use **Song requests → Blocklist** to block **artists**, **songs**, or **users** from requesting.

---

## WebSocket / API

External tools can add to the queue via the local WebSocket API. See [WebSocket commands](WebSocket-commands).
