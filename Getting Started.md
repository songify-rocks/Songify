Table of Contents for Songify v.1.3.9
- [Getting Started](#getting-started)
  - [Exploring the App](#exploring-the-app)
    - [Main Window](#main-window)
      - [Menus](#menus)
      - [Center area](#center-area)
      - [Footer](#footer)
    - [Settings Window](#settings-window)
      - [System](#system)
      - [Output](#output)
      - [Twitch](#twitch)
      - [Spotify](#spotify)
      - [Web Server](#web-server)
      - [Config](#config)
    - [Patch Notes Window](#patch-notes-window)
    - [About Window](#about-window)
    - [Bot Responses \& Commands Window](#bot-responses--commands-window)
      - [Responses](#responses)
      - [Commands](#commands)
    - [Queue Window](#queue-window)
    - [Blocklist Window](#blocklist-window)
    - [History Window](#history-window)
    - [Console Window](#console-window)
- [Setting up song requests](#setting-up-song-requests)
  - [Spotify](#spotify-1)
  - [Twitch](#twitch-1)
  - [Song Requests](#song-requests)
- [Setting up the widget](#setting-up-the-widget)
- [Setting up OBS](#setting-up-obs)
- [Troubleshooting](#troubleshooting)
    - [Songify is not working](#songify-is-not-working)
    - [Songify is not showing the song info](#songify-is-not-showing-the-song-info)
    - [Songify is not showing the song cover](#songify-is-not-showing-the-song-cover)
    - [Song requests are not working](#song-requests-are-not-working)
    - [Songify uses a lot of CPU when grabbing info from Chrome](#songify-uses-a-lot-of-cpu-when-grabbing-info-from-chrome)
    - [`INVALID CLIENT: Failed to get client` when trying to link my Spotify account](#invalid-client-failed-to-get-client-when-trying-to-link-my-spotify-account)
    - [`INVALID_CLIENT: Invalid redirect URI` when trying to link my Spotify account](#invalid_client-invalid-redirect-uri-when-trying-to-link-my-spotify-account)


# Getting Started
Download Songify from our Github [here](https://github.com/songify-rocks/Songify/releases/latest). Make sure to download the file called `Songify.zip` as the other files only contain the source code. Once downloaded extract the zip file and run `Songify.exe`. 
On the first start of the app you will be prompted to link your Spotify account. In order to do so it is advised that you create you own Spotify API Access as explained [here](#linking-spotify). Before you do though, please read the entirety of this document as it contains important information about the app and how to use it.

---

## Exploring the App
The app's main window shows a menu at the top with multiple entries (we will go over all of them), the current playing song (if connected) and displays some status indicators in the bottom left corner. 
### Main Window
#### Menus
* File => `Brings up this menu`
  * [Settings](#settings-window) => `Opens the settings window`
  * [Widget](#widget) => `Opens our widget website where you can create your own widget`
  * [Patch Notes](#patch-notes) => `Opens a window to display patch notes`
  * Help => `Opens the help sub-menu`
    * FAQ => `Links to our FAQ page`
    * Github => `Links to our Github`
    * Discord => `Links to our Discord`
    * Log Folder => `Opens the folder where log files are saved (appdata/songify.rocks)`
    * [About](#about) => `Opens a window with some information`
  * Exit => `Shuts down the app`
* Twitch => `Brings up this menu`
  * Twitch Login => `Opens the website to login with Twitch`
  * Connect => `Connects the chat bot`
  * Disconnect => `Disconnects the chat bot`
  * [Bot Config](#bot-responses--commands) => `Opens the bot config window`
* Songrequests => `Brings up this menu`
  * Queue => `Opens the queue sub-menu`
    * [Queue Window](#queue-window) => `Opens a window that shows the current song queue`
    * Queue Browser => `Opens a website with the current song queue`
    * Clear Queue => `Clears the current queue (this does not affect Spotify!)`
  * [Blocklist](#blocklist) => `Opens a window where you can block artists and users from doing song requests`
* History => `Brings up this menu`
  * [History Window](#history-window) => `Opens a window that shows your listening history (if enabled)`
  * History Browser => `Opens a website that shows your listening history`
* View => `Brings up this menu`
  * [Console](#console-window) => `Opens a consoel window that is attached to the main window and shows some logging information`
* [☕ Buy Us A Coffee](https://ko-fi.com/overcodetv) => `Links to our Ko-Fi` 
* Dropdown Menu to the top right => `This used to select which player you want to grab information from`

#### Center area
In the middle you'll see the current playing song as well as the album cover (if enabled)

#### Footer
- The footer will display a number of icons on the lower left which represent the status of services like Twitch API / Pubsub / Chatbot and web server. 
- In the middle of the bar are some informations shown
- On the right side is the current version and a link to our website.

### Settings Window
#### System
  - Language => `Language selection (English, French, Spanish, German, Russian)`
    - If you want to help translating the app visit our [Discord <img src="https://assets-global.website-files.com/6257adef93867e50d84d30e2/636e0a6ca814282eca7172c6_icon_clyde_white_RGB.svg" width="15"/>](https://discord.com/invite/H8nd4T4)!
  - Behavior
    - Autostart with windows => `Enable or disable autostart with windows`
    - Minimize to system tray => `Enable or disable minimizing the app to the tray`
    - Open the [queue window ](#queue-window)on startup => `Opens the queue window when the app launches`
    - Chrome fetch rate => `The rate in seconds at which the app pulls data from Chrome (this can slow down your browser if its set to 1 second)`
  - Privacy
    - Display account names and profile pictures => `Show or hide account name and profile picture in Songify` 
  - Appearance
    - Color => `Select one of many color accents`
    - Theme switch => `Select between light and dark theme`
#### Output
  - Output Direcotry => `Here you can select the directory where Songify saves files such Songify.txt, Artist.txt, Title.txt, cover image etc.`
  - Output Format (Text file & Widget) => `This dictates what fromat the output has (for the text files and the widget). Possible parameters are: {artist}, {title}, {extra}, {{requested by {req}}}, {url}`
    - `{{requested by {req}}}` is special because everything inside `{{` and `}}` is only shown if its a song request. `{req}` will insert the name of the person who requested the song. 
  - Output Format (Twitch Chat) => `Same as the above but only for Twich chat when the command to post the songinfo is used`
  - Append spaces => `Enabling this will appen spaces at the end of the text file in order to make it visually better when scrolling is enabled. The number of spaces is defined by the input next to it`
  - Pause Text => `If enabled, Songify will put the text from the textbox next to it as the output if playback is paused`
  - Upload Song Info => `Uploads the song data to our servers. This needed for the widget and if enabled it will also show the current playing song on the queue website`
  - Split Artist and Title => `If enabled Songify will not only produce the Songify.txt file but also separate files for the artist and title`
  - Download album cover => `Downloads the album cover of the current song. This only works of the data is retreived through the Spotify API`
#### Twitch
  - Accounts
    - Login with Twitch => `Opens the website to authenticate with twitch (only visible if not logged in)`
    - Account Name => `The name of the account you want to use for the chat bot`
    - oAuth Token => `The oAuth token of the account you want to use for the chat bot`
    - Channel => `Your Twitch channel name (just the name, not the full URL)`
    - Autoconnect => `If enabled the chat bot will automatically connect to the channel on startup`
    - Automatically announce song to chat => `If enabled the chat bot will automatically post the song info to the chat when a song changes`
    - Limit Twitch activity to only work when stream is live (requires Twitch API) => `If enabled the chat bot will only work when the stream is live`
    - Configure Bot responses and commands => `Opens the bot config window`
  - Rewards
    - Dropdown Menu => `Select the reward you want to use for song requests (requires Twitch API)`
    - Refresh Button => `Refreshes the list of rewards (requires Twitch API)`
    - Reward ID => `Displays the ID of the selected reward`
    - Create new reward => `Opens a window to create a new reward (requires Twitch API). This has to be done through the app in order to allow for refunds.`
    - Refund when => `These checkboxes are all conditions on which you could refund a reward.`
  - Song Requests
    - Enable SR (Channel Rwads) => `Enable or disable song requests through channel points`
    - ~~Enable SR (Command `!ssr`) => `Enable or disable song requests through the command !ssr`~~ *moved to the bot config*
    - Clear Queue on Startup => `Clears the queue on startup`
    - Minimum user level required for SR => `Select the minimum user level required to do song requests`
    - Max SR / User (based on user level) => `Select the maximum amount of song requests a user can do based on their user level`
    - Command Cooldown (seconds) => `Select the cooldown for song requests (Command and Channel Points)`
    - Max song length (minutes) => `Select the maximum length of a song in minutes`
#### Spotify
  - Link => `Opens the website to link your Spotify account`
  - Use Songify AppID / Own AppID => `Select if you want to use the Songify AppID or your own. It is strongly recommended to use your own AppID. If you want to use your own AppID you have to create one on the` [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications)
  - Client ID => `The Client ID of your Spotify App`
  - Client Secret => `The Client Secret of your Spotify App`
#### Web Server
  - Web Server Port => `The port on which the web server will run. It's recommended to use a port above 1024. I use 65530 for example.`
  - Start Web Server => `Starts the web server`
  - Open Website => `Opens the website in your default browser`
  - Automatically start web server => `If enabled the web server will automatically start on startup`
#### Config
  - Get Beta Updates => `If enabled you will get beta updates`
  - Export Config => `Exports the current config`
  - Import Config => `Import config files`

### Patch Notes Window
The patch notes window shows the latest patch notes. On the left side you can select the version you want to see the patch notes for. Clicking on "See all changes on Github" will open the Github page for the selected version.

### About Window
The about window shows some informations about the app and the developers.

### Bot Responses & Commands Window
#### Responses
  - Here you can configure the responses that the bot will send out when certain conditions are met. The responses have parameters such as `{user}` which will be replaced with the username of the user who triggered the response. Not all parameters are available for all responses. The standard configuration uses all the parameters that are available for that specific response.
#### Commands
  - Here you can enable or disable built in chat commands. You can also change the command trigger by clicking on the command name and type in a new one. Spaces in the command name are not allowed.

### Queue Window
The queue window shows the current queue. It displays the artist, title, length and the person who requested the song. You can remove songs from the queue by right clicking on them and selecting "Delete". The Queue inside the app is not coupled with the queue in Spotify. If you delete a song in the queue window it will not be deleted in Spotify.

### Blocklist Window
The blocklist shows two lists. One is for blocked artists the other for blocked user (chatters). You can add artists or users to the blocklist by selecting Artist or User on the dropdown menu and type in the name of the artist or user. If multiple Artist are found you'll see a list with all the results and you can check which one you want to add to the blocklist. You can remove artists or users from the blocklist by right clicking on them and selecting "Delete". 

### History Window
Songify will keep track of your listening history, if enabled. You can enable the history in the history window's title bar. THere is a button named "Save" and it has either a X or checkmark on it depending on if the history is enabled or not. If you click on the button it will enable or disable the history. The history is saved in a file called "history.shr" in the Songify folder. The history is split in days. You can select a day on the left side and see the songs you listened to with timestamp on the right side. You can also remove songs from the history by right clicking on them and selecting "Delete". If you enable "Upload" the history will be uploaded to our servers and can be shared with others. If you click the chain-link icon it will copy the url to your clipboard.

### Console Window
The console window shows the console output of the app. You can use the console to debug issues and see what is going on. The console is attached to the main window by default but can be detached using the button in the top left corner.

---

# Setting up song requests
## Spotify
In order to have song requests working you need to authenticate to the Spotify API. 
1. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications), log in and create a new app.
2. Once the app is created click on "Edit Settings" and add `http://localhost:4002/auth` to the Redirect URIs (Click ADD and SAVE, thats important!).
3. Locate the Client ID and Client Secret and copy them.
4. Go to the Songify settings and paste the Client ID and Client Secret into the corresponding fields (Settings -> Spotify).
5. Hit the switch so it says "Use Own AppID".
6. Close the settings window, this will prompt for a restart of the app (click yes).
7. Now go to Settings -> Spotify -> Link to link your Spotify account.
8. If everything worked it should say "Linked account: Your Spotify username" and show your user profile picture.
[Video](https://songify.overcode.tv/medai/video/spotify_api.webm)


## Twitch
In order to have song requests working you need to authenticate to the Twitch API.
1. Go to settings -> twitch and click on "Login with Twitch".
2. Authorize the songify app.
3. You'll get redirected to a success page.
4. Go back to the settings window and fill in account name and oAuth Token with your credentials. If you want to use a different account, for example your own bot account, make sure to get the oAuth token for that account. You want to enable autoconnect.

## Song Requests
Song requests only work with Spotify Premium.

Make sure you followed the steps above to set up [Spotify](#spotify) and [Twitch](#twitch).

1. Go to settings -> twitch -> rewards and click on "Create New Reward".
2. Fill in the title, prompt and costs of the reward you want to create. Make sure you don't use a name that is already in use by another reward.
3. Click on "Create Reward".
4. You'll be redirected to the rewards page of Twitch where you can edit the reward to your liking.
5. Go back to Songify settings -> twitch -> rewards and click on "Refresh Rewards". Select your newly created reward in the dropdown menu.
6.  Go to Song Requests and make sure that "Enable SR (Channel Points)" is enabled. If you want to use the command to do song requests make sure that "Enable SR (Command)" is enabled as well.
7.  If the Twitch chat bot is not connected yet, connect it by clicking on "Connect" in menu of the main window.
8.  Now Trigger the reward or use the command to do a song request. If everything worked you should see the song in the queue window as well as a response from the bot in the Twitch chat.

---

# Setting up the widget
Click on File -> Widget. This will open the widget generator in your browser where you can customize the widget to your liking. If the option to upload the song info is not activated, it will ask you to do so. On the widget generator you can set up the corner radius, icon position, scroll direction, transparency, scroll speed and whetehr to use the album cover or not.

---

# Setting up OBS
There are multiple ways you can set up the song display in OBS. You can use the widget or you can use the text source. 
- Widget
  - Add a browser source to your scene.
  - Set the URL to the one from the [widget generator](#setting-up-the-widget).
  - Set the width and height to 312x64.
- Source Files (text and image). These files are usually located in the Songify folder.
  - Add a text source and eneble `Read from file`.
    - Browse for either the `Songify.txt` or `Artist.txt` and `Title.txt` file.
  - Add a new image source
    - Browse for the `cover.png` file.
- Custom HTML / CSS
  - If you are familiar with HTML and CSS you can create your own widget. You can use the web server endpoint to retrieve live data from Songify. The data is presented in JSON and updates on every request made to the API. 
  ```JSON
  {
    "Artists":"",
    "Title":"",
    "albums":[
        {
          "Url":"",
          "Width":640,
          "Height":640
        },
        {
          "Url":"",
          "Width":300,
          "Height":300
        },
        {
          "Url":"",
          "Width":64,
          "Height":64
        }
    ],
    "SongID":"",
    "DurationMS":0,
    "isPlaying":true,
    "url":"",
    "DurationPercentage":0,
    "DurationTotal":0,
    "Progress":0
  }
  ```
---
# Troubleshooting
### Songify is not working
- Make sure you have the latest version of Songify installed.
- If Songify won't boot at all try deleting the entire folder `%localappdata%/Songify.Rocks`.
### Songify is not showing the song info
- Make sure your Spotify account is linked to Songify. You can check this in the settings window. Or on the bottom left corner of the main window.
### Songify is not showing the song cover
- Make sure you have enabled the option to download the album cover.
### Song requests are not working
- You need Spotify Premium to use song requests. Make sure you have followed the steps to [set up Spotify](#spotify-1), [Twitch](#twitch-1) and steps to [set up song requests](#song-requests).
### Songify uses a lot of CPU when grabbing info from Chrome
- This is due to the fact, that we have to intercept with the Chrome application to grab tab titles. This is the only way to get the song info. If you want to reduce the CPU usage you can try to increase the chrome fetch rate. You can do this in the settings window.
### `INVALID CLIENT: Failed to get client` when trying to link my Spotify account
- `INVALID CLIENT: Failed to get client` means that you have not entered the Client ID and Client Secret correctly. Make sure you have copied them correctly and pasted them into the corresponding fields. In doubt you can try to create a new app on the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications).
###  `INVALID_CLIENT: Invalid redirect URI` when trying to link my Spotify account
- `INVALID_CLIENT: Invalid redirect URI` means that you have not added the redirect URI to the app on the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/applications). Make sure you have added `http://localhost:4002/auth` to the Redirect URIs (Click ADD and SAVE, thats important!).
