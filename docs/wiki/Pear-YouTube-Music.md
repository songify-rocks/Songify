# Pear (YouTube Music)

Songify supports **YouTube Music** through **[Pear Desktop](https://github.com/pear-devs/pear-desktop)** (formerly the th-ch YouTube Music client).

---

## 1. Install Pear Desktop

Download and install from the [Pear Desktop releases](https://github.com/pear-devs/pear-desktop/releases).

---

## 2. Enable the API Server plugin

1. Open Pear Desktop.
2. Go to **Plugins → API Server**.
3. Turn the API Server **ON**.

---

## 3. API Server settings

Recommended defaults:

- **Hostname:** `0.0.0.0` (or as needed for your network)
- **Port:** `26538`
- **Authorization strategy:** **No authorization** (unless you know you need otherwise)

Restart Pear Desktop after changing settings.

---

## 4. Songify

1. In Songify, set the player to **Pear Desktop** (wording may vary slightly).
2. Start playback in Pear; Songify should show the current track. If not, try skipping to the next track once.

Song requests through Songify in Pear mode use the Pear/YouTube pipeline as implemented in your Songify version.

---

## See also

- [Music sources](Music-sources)
- [YTM Desktop](YTM-Desktop) if you use the alternative **YTM Desktop** app instead
