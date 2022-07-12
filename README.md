![forthebadge](https://forthebadge.com/images/badges/made-with-c-sharp.svg) ![forthebadge](https://forthebadge.com/images/badges/built-with-love.svg) ![forthebadge](https://forthebadge.com/images/badges/60-percent-of-the-time-works-every-time.svg) 

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6S167PLK)

### What is Songify?

Songify fetches the current playing song from Spotify, YouTube (Chrome) and many more players and saves it to a text file. We also have a widget which allows you to add it to your stream. 

#### Features?

* Gets the currently playing song and saves it to a text file. We also support splitting Artist and Title in seperate files
* Has a progress bar HTML file to be added as an OBS browser source, with customizable colors, [How To Setup!](https://github.com/songify-rocks/Songify/blob/master/README.md/#progress-bar-setup)
* Automatically start with windows
* Minimize to the system tray
* Custom output string
* Upload Song info to use with most common chat bots
* Integration with Twitch Chat
* Song History
* Spotify Songrequests
* Download the album cover
* Custom pause text
* and more

#### FAQ
The FAQ can be found here https://songify.rocks/faq.html

#### Troubleshooting
If you don't see anything happen in the Live Output try switching songs and make sure this option is turned **off**!
![](https://i.imgur.com/VUoPNbZ.png)

#### Screenshots
![](http://songify.bloemacher.com/img/Songify_Slim_1.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_2.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_3.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_4.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_5.png)  
![](http://songify.bloemacher.com/img/Songify_Slim_6.png)


### How to set up
This small guide will help you to set up Songify Slim with some of the popular streaming applications.

#### Initial Setup
This initial process is the same for all streaming applications that support text files.
First, start Songify Slim and choose your desired music service.

![](https://i.imgur.com/uEHboqi.png)

Next, go into the Songify Slim Settings and find the output tab.
Click on the clipboard icon to copy the path of the text file to your clipboard.

![](https://i.imgur.com/3tKtHwD.png)

#### OBS Studio / StreamLabs OBS (SLOBS)
This part of the guide applies to OBS Studio and StreamLabs OBS.

First, add a new "Text (GDI+)" source to your scene and give it a name. After that, the property window for your text source will open. Once it opens, tick the "Read from file" checkbox.

![](https://i.imgur.com/JVjKvDt.png)

Next, click on "Browse" and find the "Songify.txt"-File that is located in the same directory as your Songify Slim application. The path to the file will still be copied to your clipboard, so you can use that to speed up the process. After finding the file, the preview windows should update and show the same information that Songify Slim is showing.

#### XSplit Broadcaster
This part of the guide applies to XSplit Broadcaster.

First, add a new Text source to your scene. This should open up a popup property window.  
Under "Content", click on "Use Custom Script", then click "Edit Script".

![](https://i.imgur.com/vM7ZLA3.png)

Then, from the Template drop down menu, select "Load Text from Local File" and paste the path of the "Songify.txt"-file from your clipboard into the field labeled "File Path". After that, press on "Update Text".

![](https://i.imgur.com/NNQRK4o.png)

After that, you should be good to go.

#### How to get Twitch Reward ID

Fill out the Twitch credentials inside the Integration tab (hit the "?" Button to open a website that generates the oAuth Token for you).

![](http://songify.bloemacher.com/img/songify_reward_1.png)

Connect to Twitch by clicking the Twitch Icon on the main window of Songify, if the icon turns green you successfully connected to Twitch.

![](http://songify.bloemacher.com/img/songify_reward_2.png)

Enable message logging (Spotify SR tab in Settings)

![](http://songify.bloemacher.com/img/songify_reward_4.png)

Trigger the reward you want to have as your songrequest reward. The reward ID will automatically be filled in the textbox.

![](http://songify.bloemacher.com/img/songify_reward_3.png)

![](http://songify.bloemacher.com/img/songify_reward_5.png)

You can now turn off message logging.

#### Progress Bar Setup
First, add a new "Browser" source to your scene and give it a name. After that, the property window for your Browser source will open. Once it opens, tick the "Local file" checkbox

![image](https://user-images.githubusercontent.com/69881381/177660378-58eac6b7-2840-486f-a6c1-b9f495e6f92d.png)

Click on "Browse", then go to your Songify folder and select "progress.html", adjust your width & height and your progress bar should be good to go

![obs64_wj858QeXLq](https://user-images.githubusercontent.com/69881381/177661338-6d97c565-dbb7-48f9-8bb2-0ba386ae2a55.gif)

To change colors, open the "progress.html" file via Notepad or any code editor, in line 41 & 42 you can edit the class name using the names listed above (from line 9 to 39), line 41 is the progress bar background color and line 42 is the color of the progress bar itself, after you edit the color make sure to click on your browser source in OBS and click "Refresh" under your preview to see the changes. If you want the progress bar without the background colored, set the class in line 41 in the html file to "prog-black", and in OBS right-click the browser source and set its blending mode to screen.
#### Connect with us!
[<img src="http://songify.bloemacher.com/img/discord.png"  target="_blank">](https://discordapp.com/invite/H8nd4T4)
