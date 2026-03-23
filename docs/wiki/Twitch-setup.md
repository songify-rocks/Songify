# Twitch setup

Songify’s Twitch integration powers chat commands, channel point song requests, stream-aware behavior, and reward management.

---

## Login (API)

1. Open **Settings → Integration** (Twitch).
2. Click **Login with Twitch** and authorize Songify.
3. Complete the browser flow until you see success.

This grants API access for features that need Helix (rewards, stream state, etc.).

---

## Bot account

Configure:

- **Account name** — The Twitch account the bot uses in chat (often a dedicated bot account).
- **OAuth token** — Token for that account with the scopes Songify requests. If you use a separate bot account, generate the token for **that** account.
- **Channel** — Your channel name only (no full URL), e.g. `yourname`.

Enable **Autoconnect** if you want the bot to join when Songify starts.

---

## Connect the bot

From the main window **Twitch** menu:

- **Connect** — Join chat and enable commands (when other settings allow).
- **Disconnect** — Leave chat.

If you use **“Limit activity to when stream is live”**, the bot may only react while you are live; if you start Songify while already live, use **Check online status** (Twitch menu) so PubSub/stream state can catch up.

---

## Song requests and rewards

After API login:

1. Configure **Rewards** — Select or create channel point rewards (creating through Songify enables refund behavior where supported).
2. Configure **Song requests** — Enable channel points and/or commands, cooldowns, limits. See [Song requests](Song-requests).

---

## Next steps

- [Song requests](Song-requests)
- [Twitch commands](Twitch-commands)
