# Widget and OBS

Songify can drive overlays via a **hosted widget**, **text/image files**, or **custom HTML** using the local JSON API.

---

## Widget generator

1. In Songify: **File → Widget** (opens the widget site), or open [Widget Generator](https://widget.songify.rocks) directly.
2. Enable **Upload song info** under **Settings → Output** if the generator prompts for it—widgets and some web features need uploaded metadata.

Customize appearance (corners, icon, scroll, transparency, album art, etc.) and copy the generated URL.

---

## OBS — Browser source

1. Add a **Browser** source.
2. Paste the **widget URL** from the generator.
3. Set size to match the generator (commonly **312×64** pixels unless you changed layout—adjust as needed).

---

## OBS — Text and cover files

Output files are written to your **Output directory** (Settings → Output).

- **Text:** Add a **Text (GDI+)** or similar source → enable **Read from file** → point to `Songify.txt`, or `Artist.txt` / `Title.txt` if you use split output.
- **Cover:** Add an **Image** source → `cover.png` (when cover download is enabled and the source provides art).

---

## Custom visuals / JSON

The **local web server** exposes JSON with current track data for your own HTML/CSS/JS. See [Web server and API](Web-server-and-API).

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

Extra widget styles may be available with [Songify Premium](Songify-Premium). Free widgets remain available for everyone.
