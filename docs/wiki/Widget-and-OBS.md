# Widget and OBS

Songify can drive overlays via a **hosted widget**, **text/image files**, or **custom HTML** using the local JSON API.

---

## Hosted widgets (recommended)

The full gallery — including **Premium** styles — is at [songify.rocks/widgets](https://songify.rocks/widgets/).

1. In Songify: **Tools → Widget → Widget gallery**, or use **Browse widgets** on Home.
2. Enable **Upload song info** under **Settings → Output** if the site prompts for it (Tools → Widget turns this on for you).
3. Copy a widget URL into an OBS **Browser** source.

A Songify API token (Settings → Config) is required so the hosted widgets can follow the current song.

---

## Widget generator

The generator at [widget.songify.rocks](https://widget.songify.rocks) is a **simpler** customizer (corners, icon, scroll, transparency, album art). It has fewer layouts than the gallery and does **not** list Premium widgets.

**Tools → Widget → Widget generator** opens it with your stream ID already in the URL.

---

## OBS — Browser source

1. Add a **Browser** source.
2. Paste the **widget URL** from the gallery or generator.
3. Set size to match the widget (commonly **312×64** pixels for the basic generator bar—adjust as needed).

---

## OBS — Text and cover files

Output files are written to your **Output directory** (Settings → Output). Getting started can pick this folder for you.

- **Text:** Add a **Text (GDI+)** or similar source → enable **Read from file** → point to `Songify.txt`, or `Artist.txt` / `Title.txt` if you use split output.
- **Cover:** Add an **Image** source → `cover.png` (when cover download is enabled and the source provides art).

---

## Custom visuals / JSON

The **local web server** (Settings → Web server) exposes JSON with current track data for your own HTML/CSS/JS. See [Web server and API](Web-server-and-API).

This is separate from the hosted gallery: use it when you want a custom overlay on `localhost`, not to customize the Songify-hosted widgets.

Example shape (fields may vary by version):

```json
{
  "Artists": "",
  "Title": "",
  "albums": [
    { "Url": "", "Width": 640, "Height": 640 },
    { "Url": "", "Width": 300, "Height": 300 },
    { "Url": "", "Width": 64, "Height": 64 }
  ],
  "SongID": "",
  "DurationMS": 0,
  "isPlaying": true,
  "url": "",
  "DurationPercentage": 0,
  "DurationTotal": 0,
  "Progress": 0
}
```

---

## Premium widgets

Extra widget styles are listed on [songify.rocks/widgets](https://songify.rocks/widgets/). Free widgets remain available for everyone. See [Songify Premium](Songify-Premium).
