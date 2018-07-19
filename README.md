# SONGIFY NO LONGER WORKS DUE TO CHANGES THAT SPOTIFY MADE. THIS PROJECT WILL LIKELY NEVER BE WORKING AGAIN.
# I'M SORRY BUT THERE IS NOTHING I CAN DO AT THE MOMENT
# THANKS EVERYONE FOR USING IT.

#
#
#
#


```
# Songify
A simple tool that gets the current track from Spotify and saves it as a text file.

# Installation
Download and unzip the application to any destination.

If you are updating from an older version just replace the Songify.exe file.
### [DOWNLOAD](https://github.com/Inzaniity/Songify/releases)

# Settings
In the top right corner you'll find a settings tab.
Inside are various different setting such as output directory and custom output string!

# Custom Output
You can customize the output of the song! 

{artist}, {title} and {album} are the paramteres you can use

For example

**{artist} - {title}**

will produce

**Welshly Arms - Legendary**

If you plan on using this to display the song in OBS / XSplit I recommend adding a few (5-15) spaces at the end to ensure a clean look.

The application allows you to set a custom pause text. (For Example: Just blank, or "Pause")

You can also download the album cover to display it in OBS / XSplit.


# How Songrequests work
In order to get Songrequest to work you first have to provide Twitch credentials. 

 - Go to Settings -> Twitch Connection
 - Enter Username, OAuth Token (Click the "OAuth Token" text to open a Link where you can get the token) and the Channel you want to connect to.
 - Click "SAVE"
 - Click on the Checkbox in the top right corner which says "Twitch / Spotify"
 - On the first time you'll be prompted to allow Songify through the firewall
 - A browser window will open asking for Spotify Authenticatoin. (This is only used to get the SR Name and Artist)
 - After you did all this, in Songify there should be a text saying "Connected to #CHANNEL" on the bottom. 
 - You are good to go. 

## Syntax for Songrequests
The syntax is very basic, all you have to do is type `!sr SPOTIFY_URI` for example 

    !sr spotify:track:4EBisBBehGON4ESJsNZBsP
    or 
    !sr 4EBisBBehGON4ESJsNZBsP

## Managing Songrequests
Open the Songrequests window by clicking on "Songrequets" in the top bar. 
Max Songrequests per user is how many SRs a single user can request.
Playlist to play after all SR are done is as it says the playlist that'll be player if there are no more SR. 
Syntax for this is the same as for the SR `spotify:user:warner.music.central.europe:playlist:4PUhLy92EEIKVx3XcuMcfz`
The skip button does what it says, it skips the current SR. 


# Screenshots 

![](https://i.imgur.com/b4Mc5hF.png)

![](https://i.imgur.com/19nEKYn.png)

Light Mode

![](https://i.imgur.com/mGZslVP.png)

Dark Mode

![](https://i.imgur.com/k1Fc2lh.png)

Colors!

![](https://i.imgur.com/yNBJMN1.gif)
```
