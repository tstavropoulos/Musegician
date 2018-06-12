using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;
using Musegician.Playlist;

namespace Musegician
{
    public partial class FileManager : IPlaylistTransferRequestHandler
    {
        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetSongData(
            Recording recording)
        {
            PlaylistSong[] values = new PlaylistSong[]
            {
                GetSongData(
                    song: recording.Song,
                    exclusiveRecording: recording)
            };

            db.SaveChanges();

            return values;
        }

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetSongData(
            Song song,
            Artist exclusiveArtist,
            Recording exclusiveRecording)
        {
            List<PlaylistSong> songData = new List<PlaylistSong>
            {
                GetSongData(
                    song: song,
                    exclusiveArtist: exclusiveArtist,
                    exclusiveRecording: exclusiveRecording)
            };

            db.SaveChanges();

            return songData;
        }

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetAlbumData(
            Album album,
            bool deep)
        {
            List<PlaylistSong> songData = new List<PlaylistSong>();

            try
            {
                db.Configuration.AutoDetectChangesEnabled = false;

                if (deep)
                {
                    var songs = (from track in album.Tracks
                                 orderby track.DiscNumber, track.TrackNumber
                                 select track.Recording.Song).Distinct();

                    foreach (Song song in songs)
                    {
                        songData.Add(GetSongData(song: song));
                    }
                }
                else
                {
                    Database.Playlist currentPlaylist = GetCurrentPlaylist();

                    var tracks = from track in album.Tracks
                                 orderby track.DiscNumber, track.TrackNumber
                                 select track;

                    foreach (Track track in tracks)
                    {
                        PlaylistSong newSong = new PlaylistSong(
                            song: track.Recording.Song,
                            title: $"{track.Recording.Artist.Name} - {track.Recording.Song.Title}")
                        {
                            Playlist = currentPlaylist,
                            Weight = track.Weight
                        };
                        db.PlaylistSongs.Add(newSong);

                        PlaylistRecording newRecording = new PlaylistRecording(
                            recording: track.Recording,
                            title: $"{track.Recording.Artist.Name} - {track.Album.Title} - {track.Title}")
                        { Weight = 1.0 };

                        newRecording.PlaylistSong = newSong;
                        db.PlaylistRecordings.Add(newRecording);

                        songData.Add(newSong);
                    }
                }
            }
            finally
            {
                db.Configuration.AutoDetectChangesEnabled = true;
            }

            db.SaveChanges();

            return songData;
        }

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetArtistData(
            Artist artist,
            bool deep)
        {
            List<PlaylistSong> artistData = new List<PlaylistSong>();

            try
            {
                db.Configuration.AutoDetectChangesEnabled = false;

                Database.Playlist currentPlaylist = GetCurrentPlaylist();

                var songs = (from recording in artist.Recordings
                             orderby recording.Song.Title ascending
                             select recording.Song).Distinct();

                foreach (Song song in songs)
                {
                    string playlistName;

                    if (deep)
                    {
                        List<Artist> artists = new List<Artist>(
                            (from recording in song.Recordings
                             select recording.Artist).Distinct());

                        if (artists.Count == 1)
                        {
                            playlistName = $"{artists[0].Name} - {song.Title}";
                        }
                        else
                        {
                            playlistName = $"Various - {song.Title}";
                        }
                    }
                    else
                    {
                        playlistName = $"{artist.Name} - {song.Title}";
                    }

                    PlaylistSong newSong = new PlaylistSong(
                        song: song,
                        title: playlistName)
                    {
                        Playlist = currentPlaylist,
                        Weight = song.Weight
                    };
                    db.PlaylistSongs.Add(newSong);

                    foreach (PlaylistRecording recording in GetRecordingList(
                            song: song,
                            exclusiveArtist: deep ? null : artist))
                    {
                        recording.PlaylistSong = newSong;
                        db.PlaylistRecordings.Add(recording);
                    }

                    artistData.Add(newSong);
                }
            }
            finally
            {
                db.Configuration.AutoDetectChangesEnabled = true;
            }


            db.SaveChanges();

            return artistData;
        }

        string IPlaylistTransferRequestHandler.GetDefaultPlaylistName(BaseData data)
        {
            if (data is Artist artist)
            {
                return artist.Name;
            }
            else if (data is Album album)
            {
                return album.Title;
            }

            return "";
        }

        PlaylistSong GetSongData(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null)
        {
            Database.Playlist currentPlaylist = GetCurrentPlaylist();

            string playlistName;

            if (exclusiveRecording != null)
            {
                playlistName = $"{exclusiveRecording.Artist.Name} - {exclusiveRecording.Song.Title}";
            }
            else if (exclusiveArtist != null)
            {
                playlistName = $"{exclusiveArtist.Name} - {song.Title}";
            }
            else
            {
                List<Artist> artists = new List<Artist>(
                    (from recording in song.Recordings
                     select recording.Artist).Distinct());

                if (artists.Count == 1)
                {
                    playlistName = $"{artists[0].Name} - {song.Title}";
                }
                else
                {
                    playlistName = $"Various - {song.Title}";
                }
            }

            PlaylistSong newSong = new PlaylistSong(
                song: song,
                title: playlistName)
            {
                Playlist = currentPlaylist,
                Weight = song.Weight
            };
            db.PlaylistSongs.Add(newSong);

            foreach (PlaylistRecording recording in GetRecordingList(
                song: song,
                exclusiveArtist: exclusiveArtist,
                exclusiveRecording: exclusiveRecording))
            {
                recording.PlaylistSong = newSong;
                db.PlaylistRecordings.Add(recording);
            }

            return newSong;
        }

        IEnumerable<PlaylistRecording> GetRecordingList(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null)
        {
            if (exclusiveRecording != null)
            {
                Track track = exclusiveRecording.Tracks.First();
                return new PlaylistRecording[] {
                    new PlaylistRecording(
                        recording: exclusiveRecording,
                        title: $"{exclusiveRecording.Artist.Name} - {track.Album.Title} - {track.Title}")
                    { Weight = 1.0 } };
            }
            else if (exclusiveArtist != null)
            {
                return (from recording in song.Recordings
                        where recording.Artist.Id == exclusiveArtist.Id
                        select new PlaylistRecording(
                            recording: recording,
                            title: $"{exclusiveArtist.Name} - {recording.Tracks.First().Album.Title} - {recording.Tracks.First().Title}")
                        { Weight = recording.Tracks.First().Weight });
            }

            return (from recording in song.Recordings
                    select new PlaylistRecording(
                        recording: recording,
                        title: $"{recording.Artist.Name} - {recording.Tracks.First().Album.Title} - {recording.Tracks.First().Title}")
                    { Weight = recording.Tracks.First().Weight });
        }
    }
}
