# Patchnotes 1.6.8

## New Features
### 🎵 YouTube Music Desktop Support
- **Added support for [YTMDesktop](https://github.com/ytmdesktop/ytmdesktop)**  
  Follow this [guide](https://github.com/songify-rocks/Songify/blob/master/YTMDestkop.md) to link it with Songify.
  - Retrieves **Title, Artist, and Cover/Video Thumbnail**.
  - The data is included in the **webserver response**.
  - Compatible with all **widgets**.
  - **NOTE:** This **does not enable YouTube song requests**.

### 🏆 Expanded User Levels
- Songify can now check **subscriber tier levels** for song requests.
- *(Note: You will need to re-link Twitch as we use new scopes.)*

### 👥 Twitch User List
- Added a new **Viewer List** window (**View -> Viewer List**) displaying all users in the chat.
  - **SR**: Indicates if the user is blocked from making song requests.
  - **Name**: The user's display name.
  - **User Level**: The user's assigned level.
  - **Sub Tier**: Subscriber Tier (-, 1, 2, 3).
  - The user list syncs every **30 seconds**.

### 🔄 Inter-Process Communication (IPC)
- If Songify is running in the **system tray**, opening another instance will now bring the existing instance into view instead of just doing nothing.

### ⚙️ New Commands & Responses
- **New `!commands` Command**: Lists all active commands in the chat.
- **Additional Responses**:
  - Added response when the **User Level is too low** for the `!ssr` command.
  - Added response for **song request rewards**.

### 🎟️ Reward Actions & Skip Reward
- **Reward Actions**: You can now choose between **song request** and **skip** actions for rewards.
- **Skip Reward**: Users can now redeem a reward to skip the current song.

## Changes
### 🔑 Important: Spotify API Credentials Required
- **As of version 1.6.8, you must provide your own Spotify API credentials.**  
  This helps avoid rate limits and ensures faster app updates.  
  📖 Follow [this guide](https://github.com/songify-rocks/Songify/wiki/Setting-up-song-requests#spotify-setup) for setup instructions.

### 📡 Stream & Internet Checks
- **Stream Online Checks**: Now runs every **5 seconds** for improved accuracy.
- **Improved Internet Checks**:
  - More reliable connection verification.
  - Songify **will no longer close** if the connection is lost. It will continue checking until the connection is restored.

### 🔄 Reverted URL Encoding
- A change from **version 1.6.7.0** has been reverted to improve search results.

### 🖥️ UI Improvements
- **Redesigned Commands Tab**: Completely overhauled for better usability.
- **Integrated Response Pairing**: Command responses are now part of the **Commands Tab**.
  *(Song request responses remain in the Responses section to avoid clutter.)*
- **Customizable User Levels**: Set user levels for each command.
- **Chat Announcements**: Commands can now send responses as **chat announcements**.
- **Updated UI**:
  - **Responses Tab**: Refreshed interface.
  - **Rewards Tab**: Now allows selection between **song request** or **skip** rewards.
- **Settings Adjustments**:
  - **Get Beta Updates** moved to **Settings -> System**.
  - **Removed** the option to **hide profile pictures and usernames** from **Settings -> System**.
  - **Adjusted Responses Tab** layout.

### ⚡ Revamped Command Handling
- The **command system** has been rewritten.
- Commands are now managed in a **dedicated configuration file**: `TwitchCommands.yaml`.

## Fixes
### 🛠️ Twitch Authentication
- Resolved issues with the **alternative Twitch authentication code login**.

### 🐞 Miscellaneous Fixes
- Fixed a crash when a **Windows notification** failed to send.
- Fixed several **Spotify API-related issues**.

## Languages
### 🌍 Translation Improvements
- Updated and corrected translations across multiple languages.

### 🔄 Language Switching
- Switching languages **no longer requires a restart**.
