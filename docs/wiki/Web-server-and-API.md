# Web server and API

**Settings → Web server** starts a local server on a port you choose (use **1025–65535**; pick a port that is not used by other apps).

---

## HTTP — JSON

When the web server is running, **GET** requests to the root URL return **JSON** with the current track (same payload Songify uses for integrations).

Examples:

- `http://127.0.0.1:<port>/`
- If Songify runs as administrator, it may also bind to your LAN IP—see the in-app / log output for the exact base URL.

**CORS** headers are set for browser use on simple setups.

---

## WebSocket — control API

Connect to:

`ws://127.0.0.1:<port>/`

Send **JSON** messages with an `"action"` field (and optional `"data"`). Supported actions include queue, volume, skip, play/pause, blocklist actions, and more.

Full reference: [WebSocket commands](WebSocket-commands).

---

## Behavior by player mode

Volume and queue actions apply to **Spotify** or **Pear** according to the active player and implementation. If something does not respond, confirm the correct player is selected and APIs are linked.

---

## Troubleshooting

- **Port in use** — Choose another port in settings.
- **Firewall** — Allow **Songify** on private networks if another PC must reach the server (most setups only need localhost).
