# Musegician

Pronouced like a portmanteau of Magician and Musician - the name _Musegician_ was designed to feel like extruding a mouthful of gravel through gritted teeth.

The initial goal of this project was to provide a relatively lightweight musicplayer with more intelligent shuffle behavior.  On initially importing a music library, some simple heuristics will attempt to determine unique songs and artists, and identify live recordings, though there is an expanding set of tools available to manually update this data.  Live recordings end up nested under their associated song, along with the studio recording, and the "song" itself can be added to playlists.  There is a modifiable, global default probability when playing a given song that a live recording will be substituted in its place, and this weighting value is also individually modifiable.  Additionally, songs themselves have modifiable global- and playlist-specific weights allowing you to tune the distributions without needing to actually cull songs from your music collection.

If you're interested in giving it a try, just grab it from [The Releases Page](https://github.com/tstavropoulos/Musegician/releases).  Unfortunately, because the UI framework it was developed in (WPF) is only supported on Windows, it is limited to that platform at this time.  The Automated installer is still under active development, and may have somewhat inconsistent results in settings up the dependencies.  You may need to manually install [SQL Server 2017 Express](https://go.microsoft.com/fwlink/?linkid=853017), and make sure you check the box to install SqlLocalDB.

The intelligent import scheme implicitly depends on good quality metadata tags, but there exist a number of tools at this point to administer the data once it has been imported.

## Features

_Musegician_ is currently a full-featured MP3 player with several possible views.

The Classic Library and Playlist browser...  
![Classic Simple Screenshot](README_Screenshots/ClassicSimple.gif)

And an always-on-top Tiny Player.  
![Tiny Player](README_Screenshots/TinyPlayer.gif)

You should be able to use the media buttons on your keyboard to manipulate the playing track.  The classic player also allows you to look at your library without showing the albums, or even a view that just lists all of your albums.

### Live Songs and Weights

What's different about it is that it understands the concept of a "song", and can cluster together different recordings of the same song.  It allows you to set a relative preference between different songs and recordings, making one play more frequently than another.  It also supports the concept of "live recordings", and by default sets the weight of live recordings to a lower value.  
![Live recordings with context highlighting](README_Screenshots/LiveAndContext.PNG)

You can independently manipulate the weights of different recordings, away from their default values, by pressing the + and - keys.  
![Independent track manipulation](README_Screenshots/CustomizeTrackWeight.gif)

You can select multiple tracks at once, modifying all their weights simultaneously, or modifying their data in the Database and even push these updates to their ID3 tags.  
![Multiedit](README_Screenshots/MultiEdit.PNG)

### Playlists

When songs are added to a playlist, their weights are carried over automatically (based on context).  
![Pushing Weights](README_Screenshots/PushProbabilities.gif)

But songs in the playlist also retain their independence, allowing you to create different playlists that have different weights for the same songs.  
![Playlist Weight Independence](README_Screenshots/PlaylistWeightIndependence.gif)

You can Load Playlists...  
![Load Playlists](README_Screenshots/LoadPlaylist.png)

and Save Playlists.  
![Save Playlists](README_Screenshots/SavePlaylist.png)

The TinyPlayer now even features access to the playlist!  
![TinyPlayer Playlists](README_Screenshots/TinyPlaylist.gif)


### Playback

Built into the player is an Equalizer.  
![Equalizer](README_Screenshots/Equalizer.gif)

### Directory View

Now features the Directory View!  
![Directory View](README_Screenshots/DirectoryView.png)

Open the explorer directly to any directory or file from any view, or jump to a song in the library from the playlist's context menu.

### Spatialization

Leveraging the latest only slightly outdated techniques from auditory research, Musegician can spatialize auditory recordings, simulating listening to the music from speakers in your physical environment.  
![Spatialization](README_Screenshots/Spatialization.gif)

This tends to sounds great on older tracks, and occasionally bizarre on more recent ones.

### Music Driller

Currently rather feature-light, the Music Driller lets you loop over pieces of songs in your library.  
![Music Driller](README_Screenshots/MusicDriller.gif)

Ideal if you are learning an instrument and want to practice a tricky part.  Forthcomming features will expand on this functionality.

### Installer

It's packaged with an installer that will handle the installation of the requirements for you.  Just grab it from [The Releases Page](https://github.com/tstavropoulos/Musegician/releases).  Windows SmartScan will likely flag it, but clicking "More Information" on the warning that pops up will reveal a button that lets you install anyway.

## Development

Requirements:
* Visual Studio 2017
* .NET 4.7.1 framework (or better)  [Available Here](https://www.microsoft.com/net/download/thank-you/net471-developer-pack)
* WiX for the Installer Project.  [WiX Installer Here](https://github.com/wixtoolset/wix3/releases), and [VS2017 Extension Here](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension)

## About The Developer

I like Music.  I like the artistic vision that Studio Recordings represent.  I like occasionally listening to live recordings of music.  I do not like it when a music player's Shuffle results in too many live recordings playing.  I hate it when a music player's Shuffle plays two different recordings of the same song back-to-back.  I would like to fix that.
