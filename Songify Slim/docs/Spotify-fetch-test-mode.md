# Spotify fetching, Test Mode, and live gating

Short reference for how Songify polls the Spotify Web API, what **Test Mode** is for, and when **live gating** applies.

---

## Default behavior (gating off)

**By default, live/Test Mode gating is turned off:** **Bypass live/TestMode gating** is **enabled** in **Settings → Spotify**.

That means Songify **does** call the Spotify Web API for “now playing” whenever **Spotify** is your player - including while your Twitch channel is **offline**. You do **not** need to go live or use Test Mode for fetching to run.

- The main-window **Test Mode** toggle is **hidden** while bypass stays on (there is nothing to “test” against a gate).
- If you want to **reduce** background API traffic and rate-limit risk, you can **turn gating on** (see below).

<!-- Screenshot: Settings → Spotify - bypass on by default -->
![Spotify settings - fetch rate and bypass (default)](images/placeholder-spotify-settings.png)

---

## What Test Mode does

**Test Mode** matters only when you have **enabled** live gating by turning **Bypass live/TestMode gating** **off**.

In that situation, Songify **does not** poll Spotify while you are **offline** - unless **Test Mode** is on. Test Mode is a **temporary** way to fetch anyway (overlays, output files, live view) **without** starting a stream.

- Test Mode **turns off automatically after 5 minutes**, or you can turn it off manually.
- The **Test Mode** toggle appears only when **Spotify** is selected **and** bypass is **off**.

<!-- Screenshot: main window - Test Mode toggle (Spotify, gating on) -->
![Test Mode on the main window](images/placeholder-test-mode.png)

---

## Requirements for Spotify fetching (Web API)

**1. Spotify is set up as the player**

- Choose **Spotify** as the playback source in Songify.
- Complete Spotify login (PKCE): a **Client ID** in settings and a **connected account** with valid **access/refresh tokens** so the API client can run. If the client is not ready, Songify will try to refresh auth when tokens exist.

**2. When gating is off (default)**  

No extra condition: fetching runs on the normal timer (subject to your **Spotify fetch rate** in Settings).

**3. When gating is on (bypass off)**  

Polling runs only if **any** of these is true:

- You are **live on Twitch**, **or**
- **Test Mode** is **on**, **or**
- You turn **Bypass live/TestMode gating** **on** again.

Otherwise Songify skips the fetch and shows a message that fetching is disabled until one of those applies.

**4. Other settings**

- **Spotify fetch rate** (Settings → Spotify): interval in seconds (clamped in the app, typically **1–30**; default **2**). Lower values mean more API calls.
- **Spotify Premium**: Songify may warn if your account is not Premium; some features (e.g. song requests) expect Premium.

---

## How to turn gating on or off

**Gating off (default)** - **Bypass live/TestMode gating** is **on**: fetch while offline, no Test Mode needed.

**Gating on** - In **Settings → Spotify**, turn **Bypass live/TestMode gating** **off**. Songify then limits polling to live / Test Mode / turning bypass back on (tooltip in the app explains the tradeoff: less API usage vs. no fetch when offline without Test Mode).

**Gating off again** - Turn **Bypass live/TestMode gating** **on**. The **Test Mode** strip hides and Test Mode is cleared.

<!-- Screenshot: bypass off (gating on) vs on (gating off) -->
![Bypass toggle - gating on vs off](images/placeholder-bypass-on.png)

---

## Quick comparison

| Situation | Fetches Spotify Web API? (Spotify player, auth OK) |
|-----------|-----------------------------------------------------|
| **Default:** bypass on (gating off) | **Yes**, including while offline |
| Gating on, offline, Test Mode off | No |
| Gating on, offline, Test Mode on | Yes (until 5 minutes or you turn it off) |
| Gating on, live on Twitch | Yes |
| Gating on, bypass turned back on | Yes |

---

## Image placeholders

Replace the paths above with your own files, for example:

- `images/spotify-settings-bypass.png`
- `images/test-mode-main-window.png`

Or delete the `![...](...)` lines and paste images inline in your wiki or CMS.
