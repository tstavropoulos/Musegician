# Musegician

Pronouced like a portmanteau of Magician and Musician - Musegician was designed to feel like extruding a mouthful of gravel through gritted teeth.

The goal of this project is to provide a relatively lightweight and zippy musicplayer with more intelligent playlists.  Where appropriate and discernable, live recordings will end up nested under their associated studio recording, and the "song" itself is added to playlists.  There will be a modifiable, global probability when playing a given song that a live recording will be substituted in its place, and this weighting value will be individually modifiable.  Additionally, songs themselves will have modifiable global- and playlist-specific weights allowing you to tune the distributions without needing to actually cull songs from your music collection.

This scheme implicitly depends on good quality metadata tags, and as such I am working on a good scheme for updating them.

## Development

Requirements:
* Visual Studio 2017
* .NET 4.7.1 framework (or better)  [Available Here](https://www.microsoft.com/net/download/thank-you/net471-developer-pack)

## About The Developer

I like Music.  I like the artistic vision that Studio Recordings represent.  I like occasionally listening to live recordings of music.  I do not like it when Shuffle results in too many live recordings playing.  I hate it when Shuffle plays two different recordings of the same song back-to-back.  I would like to fix that.