# SR-Music
An open-source custom music client built, to work with SRS VoIP for DCS.

## Getting Started
To obtain a copy of this application, head on over to the [releases tab](https://github.com/AlexEvans747/SR-Music/releases) and downloaded the latest version (do not download the source code).  Extract the contents of the .zip file anywhere you'd like; however, please ensure that you do not move the executable outside of the inner SR-Music folder.  Instead, create a shortcut and place it wherever you'd like to start the application from.

This app was built to work with DCS-SimpleRadioStandalone (SRS).  If you do not have a copy of SRS, you can obtain the latest version [here](https://github.com/ciribob/DCS-SimpleRadioStandalone/releases).

Once you have done the above, go ahead and start up the SRS client, SRS server, and SR-Music applications.

![Alt text](images/Screenshot1.PNG?raw=true "Optional Title")

You will be presented with a connection splash screen.  Ensure that the SRS server is running, then enter the server port, as shown in the below image.

#### Remote Connections & Multiplayer
You will notice that the "IP Address" box is disabled.  This was done to prevent users from connecting to remote SRS servers and spamming audio over in-game frequencies.  Currently, only the SRS server admin can control the SR-Music app.  Clients should connect to SRS as normal, and will hear the music that the SRS server admin selects.

![Alt text](images/Screenshot2.PNG?raw=true "Optional Title")

Once connected, the splash screen will disappear and you will be presented with the main application window as shown below.

![Alt text](images/Screenshot3.PNG?raw=true "Optional Title")

#### Connection Issues***

If you cannot connect to the SRS server, you may want to check whether or not "Line of Sight" or "Distance Limit" is enabled.  Because the music played via SR-Music is not tied to a specific in-game unit, SR-Music will force a disconnect if these features are turned on.

![Alt text](images/Screenshot4.PNG?raw=true "Optional Title")

SR-Music offers 4 custom radio stations, which each can be set to a specific frequency/modulation, renamed, and also provided with a different set of songs.  The column on the left is used to navigate between the 4 stations.  Each station has a Player, Queue (Not Yet Implemented), and Settings tab.

To change the frequency of a station, you can use the slider in one of the following ways:

* Drag the slider thumb to the left or right
* Click to the left or right of the slider thumb for +/- 1 MHz
* Use the left or right arrow key on your keyboard for +/- 0.1 MHz

Please note that stations cannot use the same frequency.  If you try and increment from 201.000 MHz to 202.000 MHz, by clicking to the right of the slider thumb, the frequency will not change.

![Alt text](images/Screenshot6.PNG?raw=true "Optional Title")

In addition to setting the frequency and modulation, you will also need to select a valid music directory using the browse button, as shown below.  If a valid folder is not selected, you will presented with a warning when clicking play within the Player tab.

### ***Audio tracks must be of .MP3 format!!!***

![Alt text](images/Screenshot5.PNG?raw=true "Optional Title")

Once you have set your station settings to your liking, swap over to the Player tab and click play!  You can use the media controls to skip both backwards and forwards between tracks.  You can also enable track repeat.  Because the Queue tab is not yet implemented, shuffle has been permanently enabled.  With shuffle on, the order of songs will be re-shuffled once you reach the end of the number of tracks listed.

![Alt text](images/Screenshot7.PNG?raw=true "Optional Title")

You are now ready to take to the skies in DCS, and rock out to the Top Gun soundtrack with your friends via SRS and SR-Music! 🎧🎶

## Help
**Please join the SR-Music discord server: https://discord.gg/BPmtn5**

If you run into any issues and need help, please feel free to reach out to me directly on Discord (Ghostrider#2610).  You can also try the general or support channels on the discord server.

Additionally, if you encounter any bugs, please ensure to report them to the feedback channel on the discord server!
