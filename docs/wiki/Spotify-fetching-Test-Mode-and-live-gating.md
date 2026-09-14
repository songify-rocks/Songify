# Spotify fetching, Test Mode, and live gating

How Songify polls the Spotify Web API, what **Test Mode** is for, and when **live gating** applies.

---

## Default (gating off)

**Enable live gate / test mode** in **Settings → Spotify** is **off** by default.

Songify calls the Spotify Web API for now-playing whenever **Spotify** is your player — including while your Twitch channel is **offline**. You do not need to go live or use Test Mode.

Turn gating **on** if you want to reduce background API traffic and rate-limit risk.

---

## Test Mode

**Test Mode** appears on **Home** (next to the player dropdown) and in the **status bar** only when:

- **Spotify** is selected, and
- **Enable live gate / test mode** is **on**.

With gating on, Songify does **not** poll Spotify while you are offline unless Test Mode is on. Use it to drive overlays, output files, and the live view **without** starting a stream.

Test Mode **turns off automatically after 2 minutes**, or you can turn it off yourself.

---

## When Spotify fetching runs

Assuming Spotify is the player and login succeeded:

| Situation | Fetches now-playing? |
|-----------|----------------------|
| Live gate **off** (default) | Yes, including while offline |
| Gate **on**, offline, Test Mode off | No (Home shows a paused banner) |
| Gate **on**, offline, Test Mode on | Yes, until 2 minutes or you turn it off |
| Gate **on**, live on Twitch | Yes |
| Gate turned **off** again | Yes |

**Spotify fetch rate** (Settings → Spotify) is the poll interval in seconds (typically 1–30; default 2). Lower values mean more API calls.

---

## Related

- [Spotify setup](Spotify-setup)
- [Settings reference](Settings-reference)
- [Music sources](Music-sources)
