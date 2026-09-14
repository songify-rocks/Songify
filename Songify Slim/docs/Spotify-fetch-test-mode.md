# Spotify fetching, Test Mode, and live gating

Short reference for how Songify polls the Spotify Web API, what **Test Mode** is for, and when **live gating** applies.

---

## Default behavior (gating off)

**By default, live gating is off.** **Enable live gate / test mode** in **Settings → Spotify** is **unchecked**.

Songify **does** call the Spotify Web API for “now playing” whenever **Spotify** is your player — including while your Twitch channel is **offline**. You do **not** need to go live or use Test Mode for fetching to run.

- The Home **Test Mode** toggle is **hidden** while gating stays off.
- Turn gating **on** if you want to reduce background API traffic and rate-limit risk.

---

## What Test Mode does

**Test Mode** matters only when **Enable live gate / test mode** is **on**.

In that situation, Songify **does not** poll Spotify while you are **offline** — unless **Test Mode** is on. Test Mode is a **temporary** way to fetch anyway (overlays, output files, live view) **without** starting a stream.

- Test Mode **turns off automatically after 2 minutes**, or you can turn it off manually.
- The **Test Mode** toggle appears on **Home** (next to the player dropdown) and in the **status bar** when **Spotify** is selected **and** live gating is **on**.

---

## Requirements for Spotify fetching (Web API)

**1. Spotify is set up as the player**

- Choose **Spotify** as the playback source in Songify.
- Complete Spotify login (PKCE): a **Client ID** in settings and a **connected account** with valid **access/refresh tokens**.

**2. When gating is off (default)**

No extra condition: fetching runs on the normal timer (subject to your **Spotify fetch rate** in Settings).

**3. When gating is on**

Polling runs only if **any** of these is true:

- You are **live on Twitch**, **or**
- **Test Mode** is **on**, **or**
- You turn **Enable live gate / test mode** **off** again.

Otherwise Songify skips the fetch and Home shows that fetching is paused.

**4. Other settings**

- **Spotify fetch rate** (Settings → Spotify): interval in seconds (typically **1–30**; default **2**). Lower values mean more API calls.
- **Spotify Premium**: Songify may warn if your account is not Premium; some features (e.g. song requests) expect Premium.

---

## How to turn gating on or off

**Gating off (default)** — **Enable live gate / test mode** is **off**: fetch while offline, no Test Mode needed.

**Gating on** — In **Settings → Spotify**, turn **Enable live gate / test mode** **on**. Songify then limits polling to live / Test Mode. **Test Mode** appears on Home and in the status bar.

**Gating off again** — Turn **Enable live gate / test mode** **off**. The **Test Mode** toggle hides and Test Mode is cleared.

---

## Quick comparison

| Situation | Fetches Spotify Web API? (Spotify player, auth OK) |
|-----------|-----------------------------------------------------|
| **Default:** live gate off | **Yes**, including while offline |
| Gate on, offline, Test Mode off | No |
| Gate on, offline, Test Mode on | Yes (until 2 minutes or you turn it off) |
| Gate on, live on Twitch | Yes |
| Gate on, then live gate turned off | Yes |
