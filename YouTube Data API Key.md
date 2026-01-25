# How to Get Your Own YouTube Data API Key (for Songify)

Songify can optionally use **your own YouTube API key** to fetch YouTube metadata directly.
This improves reliability and avoids shared rate limits.

Your key stays **only on your PC** and is never sent to the Songify server.

---

## Step 1: Open Google Cloud Console

1. Go to:  
   https://console.cloud.google.com/

2. Sign in with your Google account.

---

## Step 2: Create a New Project

1. Click the project dropdown at the top (next to the Google Cloud logo).
2. Click **New Project**.
3. Enter a name like:  
   `Songify YouTube API`
4. Click **Create**.

Wait a few seconds until the project is ready.

---

## Step 3: Enable the YouTube Data API v3

1. In the left menu, go to:  
   **APIs & Services → Library**
2. Search for:  
   `YouTube Data API v3`
3. Click it.
4. Click **Enable**.

---

## Step 4: Create an API Key

1. In the left menu, go to:  
   **APIs & Services → Credentials**
2. Click **Create Credentials**.
3. Choose **API key**.
4. Google will generate a key.  
   Copy it somewhere safe.

Example (not a real key):
`AIzaSyBxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`


---

## Step 5 (Optional but Recommended): Restrict the API Key

Restricting the key prevents misuse if it ever leaks.

1. On the Credentials page, click your new API key.
2. Under **API restrictions**:
   - Choose: **Restrict key**
   - Select: **YouTube Data API v3**
3. Under **Application restrictions** (optional):
   - You can leave this as **None**  
     (Songify is a desktop app, so IP restrictions are usually not practical)
4. Click **Save**.

---

## Step 6: Enter the Key in Songify

1. Open Songify.
2. Go to **Settings → YouTube**.
3. Paste your API key into the field:  
   `YouTube API Key`
4. Click **Test Key** (if available).
5. Save the settings.

---

## Troubleshooting

### “API key not valid”
- Make sure:
  - You copied the full key.
  - The YouTube Data API v3 is enabled in your project.

---

### “Quota exceeded”
- Each key has a free quota of **10,000 units per day**.
- Songify only uses cheap metadata calls.
- Quota resets daily.

---

### “API not enabled for this project”
- You forgot Step 3 (enabling the YouTube Data API).

---

## Privacy & Security Notes

- Your API key is:
  - stored locally on your PC
  - never sent to Songify servers
- Do not share your key publicly.
- If your key ever leaks:
  - Go to Google Cloud Console → Credentials
  - Delete the key
  - Create a new one

---

## Why Songify Uses Your Own Key (Optional)

- Avoids shared rate limits
- Improves reliability
- No central API dependency
- No cost for normal usage

---

If you get stuck, join the Songify Discord or check the documentation.
