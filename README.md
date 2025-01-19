# SpotifyAdMuter
Simple tool written in C# to mute the audio of the Spotify application on Windows Desktop when an ad is playing. (works in Spotify Free version)
I came across other tools, but they didn't seem to work anymore so decided to create my own version.

## How it works
The tool works by checking the application name of the spotify process. The songname will be displayed here in case a song is playing, else the adname will be shown.

The tool has 2 modes. Simple and advanced
### Simple mode
In simple mode it will just check the process name for a dash (-). Every played song will have this since the artist and songname will be seperated by it, while almost all ads will lack this symbol in the name.
This is the fastest way and will require no calls to the Spotify API, and is all local (this will also work without an internet connection)
### Advanced mode
In advanced mode, along with checking for dashes it will, on every songchange, check if the song is an existing song (and not the name of an ad) thru the Spotify API. 
This will be more reliable since it will also filter out ads that include a hyphen in the name. In case of performance / delay between the options I didn't notice any difference.

## configuration
I included a config.ini file to control several options:

### check_frequency:
The delay in miliseconds (ms) between each check. Lower = worse performance, higher = longer delay between checks. 500 is the default option and worked perfectly for me without taking too much performance.
### show_toast:
Weather to show a toast popup message to the user to let you know spotify was muted. I added this since in case I would get confused why it stopped playing music.
### advanced_mode:
toggle between advanced & simple mode
### client_id & client_secret: 
used to call the spotify API to check the songs. These are only required if advanced mode is used. These can be aquired by creating an application on the spotify developer portal.
