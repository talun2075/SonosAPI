# SonosAPI / PlaylistGenerator / Nanoleaf Aurora API / Amazon Dash Button

# Motivation, Information and Used Ideas
I love the Sonos Player but Sonos miss some specials, that I want to have so I wrote it for me.
First the Base of this Project where the Intel UPNP C# Lib, the Taglib-sharp and the Jishi Lib (https://github.com/jishi/Jishi.Intel.SonosUPnP)


I´m not native english speaker so much Comments are written in German and much Stuff in here is for me but maybe there a some folks, that say, hey this is cool
and we can work on it thogether. If so I will translate the comments.

# So what will you found on this Project.

The Backend is a ASP MVC API Project wich will Discover the Sonos Player and hold and read the needed Informations. 
Then you have a Javascript Based Frontend with Responsive Design (for Apple Devices). 

# What is the different to the normal Sonos Controler? 
I read, with the taglib, the Metainformation of your local Song. If you tagged it for Example with Lyric, Rating (popm) or the Recording Label, you got it on the Frontend. If you not have tagged it jet, no Problem.
On the Frontend you can set Rating for the Mood,Situation,Tempo and there a two Star Ratings. 
I wrote it for My Wife and me.

I think the last special is the export of one Playlist to a M3U Playlist.

# File Formats
I Support MP3,FLAC and M4A

# Supported Tags
Artist, Title ,Album ,Recording Label, Mood, Situation, Tempo, POPM (Rating),Year, Genre, Composer


#Other Stuff
In the Project there are two more none directly Sonos Projects.


# Playlist Generator
For my rated Songs I have wrote a ugly WPF Playlistgenerator with only German Frontend wich can generate Playlists for me like
all Songs with Rating > 3 AND NOT Genre Christmas AND Tempo FAST AND Situation Party. This as Excample. You can use the DLL without Frontend ;-)

# Amazon Dash Buttons
The last Project ist a Amazon Dashbutton Watcher that call on Button-Click the SonosAPI and Make some Stuff on my House.
This is a Windows Console App, that is Designed to Reset the IIS where the Sonos API installed and Call the Front End of the Sonos API on Errors. 

# NEW Nanoleaf Aurrora as Open API Connection. 
Add the Nanoleaf Aurora Open API to Use it as App in my Web. 
(Use http://kid53.blogspot.de/2013/03/css-power-button.html as Power Button for the Aurora)



So and now some last Words. Is my first git/github Project. I hope it help someone to save Time.
If you have Questions, I hope we can find the awnser. 
Have fun
