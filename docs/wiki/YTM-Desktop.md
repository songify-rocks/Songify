# YTM Desktop

Some users run **[YTM Desktop](https://github.com/ytmdesktop/ytmdesktop)** (YouTube Music Desktop) instead of Pear. Songify can integrate via the **Companion Server**.

---

## 1. Install YTM Desktop

Download from [YTM Desktop releases](https://github.com/ytmdesktop/ytmdesktop/releases/latest).

---

## 2. Enable Companion Server

1. Open YTM Desktop → **Settings** (cog).
2. Open the **Integrations** tab.
3. Enable **Companion Server**.
4. Enable **Allow browser communication**.
5. Enable **Companion authorization** (note: this option may time out after a few minutes in some versions—authorize promptly).

---

## 3. Link in Songify

1. In Songify: **File → Settings** and open the **YouTube** / **YTM Desktop** section (exact label depends on version).
2. Click **Link** and confirm the **4-digit code** matches in YTM Desktop, then **Allow**.

---

## Pear vs YTM Desktop

- **[Pear Desktop](Pear-YouTube-Music)** uses the Pear API Server plugin (port **26538** by default).
- **YTM Desktop** uses the companion protocol above.

Use the integration that matches the app you actually run; they are not the same binary.
