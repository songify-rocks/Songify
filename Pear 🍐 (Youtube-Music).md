## 🎵 How to Enable Pear 🍐 (YouTube Music) Support in Songify

Songify now supports song requests from **YouTube Music** using [Pear 🍐 Desktop](https://github.com/pear-devs/pear-desktop) (Formerly known as Youtube Music (th-ch)).

Follow these steps to set it up:

1. **Download & Install**  
   Get the [YouTube Music Desktop Client](https://github.com/pear-devs/pear-desktop/releases) and install it on your system.

2. **Open the App**  
   Launch the YouTube Music Desktop Client.

3. **Enable the API Server Plugin**  
   - In the top menu, go to **Plugins** → **API Server**.  
   - Make sure the API Server is **enabled** (toggle switch ON).

4. **Set API Server Settings**  
   - **Hostname:** `0.0.0.0` *(default)*  
   - **Port:** `26538` *(default)*  
   - **Authorization Strategy:** `No Authorization`

5. **Restart the Client**  
   Close and reopen the YouTube Music Desktop Client to ensure the settings are applied.

6. **Start Songify**  
   With the API Server running, Songify will automatically connect and allow YouTube Music requests!

💡 *Tip: You can verify it’s working by observing Songify and see if the current song shows in the app. Sometimes you have to skip to the next song for it work.*
