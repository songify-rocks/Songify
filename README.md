# What is Songify Slim?

Songify Slim is a slimmed down version of my original software Songify. Songify Slim was brought to life because Spotify decided to shut down the local API which Songify was originally built on. 

# What does it do?

It fetches the currently playing song from Spotify, YouTube (Chrome) and Nightbot and saves it to a text file. Just like magic. 

# Features?

* Gets the currently playing song and saves it as following: `Artist - Title               ` (Whitespaces are for a better marquee in your streaming software of choice!)
* Automatically start with windows (trust me, it is QoL)
* Minimize to the system tray, feels like it isn't running at all.
* Custom output string! If you want to be extra fancy.
* Upload Song info to use with most common chat bots
  * Examples (replace **URL** with the URL provided by the software):
    * Nightbot: 
      * $(urlfetch **URL**)
    * Streamlabs: 
      * {readapi.**URL**}  
    * Streamelements:  
      * ${customapi.**URL**}
    * Moobot:  
      * Response -> URL fetch - Full (plain) response, URL to Fetch -> **URL**
* Switch between Dark and Light theme, not that it matters since it's most of the time minimized...
* Oh and colors, yeah a lot of colors actually. 23 if I counted that right.



# Troubleshooting
If you don't see anything happen in the Live Output try switching songs and make sure this option is turned **off**!
<br/>
<br/>
![](https://i.imgur.com/VUoPNbZ.png)

# Screenshots
![](http://songify.bloemacher.com/img/Songify_Slim_1.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_2.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_3.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_4.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_5.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_6.png)  

# How to set up
This small guide will help you to set up Songify Slim with some of the popular streaming applications.

### Initial Setup
This initial process is the same for all streaming applications that support text files.
First, start Songify Slim and choose your desired music service.

![](https://i.imgur.com/uEHboqi.png)

Next, go into the Songify Slim Settings and find the output tab.
Click on the clipboard icon to copy the path of the text file to your clipboard.

![](https://i.imgur.com/3tKtHwD.png)

### OBS Studio / StreamLabs OBS (SLOBS)
This part of the guide applies to OBS Studio and StreamLabs OBS.

First, add a new "Text (GDI+)" source to your scene and give it a name. After that, the property window for your text source will open. Once it opens, tick the "Read from file" checkbox.

![](https://i.imgur.com/JVjKvDt.png)

Next, click on "Browse" and find the "Songify.txt"-File that is located in the same directory as your Songify Slim application. The path to the file will still be copied to your clipboard, so you can use that to speed up the process. After finding the file, the preview windows should update and show the same information that Songify Slim is showing.

### XSplit Broadcaster
This part of the guide applies to XSplit Broadcaster.

First, add a new Text source to your scene. This should open up a popup property window.  
Under "Content", click on "Use Custom Script", then click "Edit Script".

![](https://i.imgur.com/vM7ZLA3.png)

Then, from the Template drop down menu, select "Load Text from Local File" and paste the path of the "Songify.txt"-file from your clipboard into the field labeled "File Path". After that, press on "Update Text".

![](https://i.imgur.com/NNQRK4o.png)

After that, you should be good to go.


# Connect with me!
[<img src="https://discordapp.com/assets/fc0b01fe10a0b8c602fb0106d8189d9b.png"  target="_blank">](https://discordapp.com/invite/H8nd4T4)
