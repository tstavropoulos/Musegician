﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Musegician.Database;
using Musegician.Playlist;

using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician
{
    public partial class FileManager : IPlaylistTransferRequestHandler
    {
        private IPlaylistTransferRequestHandler ThisTransfer => this;

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetSongData(
            Recording recording)
        {
            return ThisTransfer.GetSongData(
                song: recording.Song,
                exclusiveRecording: recording);
        }

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetSongData(
            Song song,
            Artist exclusiveArtist,
            Recording exclusiveRecording)
        {
            List<PlaylistSong> songData = new List<PlaylistSong>();

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
                title: playlistName);

            foreach (PlaylistRecording recording in GetRecordingList(
                song: song,
                exclusiveArtist: exclusiveArtist,
                exclusiveRecording: exclusiveRecording))
            {
                recording.PlaylistSong = newSong;
            }

            songData.Add(newSong);

            return songData;
        }

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetAlbumData(
            Album album,
            bool deep)
        {
            List<PlaylistSong> songData = new List<PlaylistSong>();

            if (deep)
            {
                var songs = (from track in album.Tracks
                              orderby track.DiscNumber, track.TrackNumber
                              select track.Recording.Song).Distinct();

                foreach (Song song in songs)
                {
                    songData.AddRange(ThisTransfer.GetSongData(song: song));
                }
            }
            else
            {
                var tracks = from track in album.Tracks
                             orderby track.DiscNumber, track.TrackNumber
                             select track;

                foreach(Track track in tracks)
                {
                    PlaylistSong newSong = new PlaylistSong(
                        song: track.Recording.Song,
                        title: $"{track.Recording.Artist} - {track.Recording.Song.Title}");

                    PlaylistRecording newRecording = new PlaylistRecording(
                        recording: track.Recording,
                        title: $"{track.Recording.Artist} - {track.Album.Title} - {track.Title}");
                    newRecording.PlaylistSong = newSong;

                    songData.Add(newSong);
                }
            }

            return songData;
        }

        IEnumerable<PlaylistSong> IPlaylistTransferRequestHandler.GetArtistData(
            Artist artist,
            bool deep)
        {
            List<PlaylistSong> artistData = new List<PlaylistSong>();

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
                    title: playlistName);

                foreach (PlaylistRecording recording in GetRecordingList(
                        song: song,
                        exclusiveArtist: deep ? null : artist))
                {
                    recording.PlaylistSong = newSong;
                }

                artistData.Add(newSong);
            }


            return artistData;
        }

        string IPlaylistTransferRequestHandler.GetDefaultPlaylistName(
            LibraryContext context,
            BaseData data)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                    return ((Artist)data).Name;
                case LibraryContext.Album:
                    return ((Album)data).Title;
                case LibraryContext.Song:
                case LibraryContext.Track:
                case LibraryContext.Recording:
                    //Blank default name for Single songs, tracks, and recordings
                    return "";
                case LibraryContext.MAX:
                default:
                    throw new Exception("Unexpected LibraryContext: " + context);
            }
        }

        IEnumerable<PlaylistRecording> GetRecordingList(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null)
        {
            if (exclusiveRecording != null)
            {
                Track track = exclusiveRecording.Tracks.First();
                return new PlaylistRecording[] { new PlaylistRecording(
                    recording: exclusiveRecording,
                    title: $"{exclusiveRecording.Artist.Name} - {track.Album.Title} - {track.Title}") { Weight = 1.0 } };
            }
            else if (exclusiveArtist != null)
            {
                return (from recording in song.Recordings
                        where recording.Artist == exclusiveArtist
                        select new PlaylistRecording(
                            recording: recording,
                            title: $"{exclusiveArtist.Name} - {recording.Tracks.First().Album.Title} - {recording.Tracks.First().Title}"));
            }

            return (from recording in song.Recordings
                    select new PlaylistRecording(
                        recording: recording,
                        title: $"{recording.Artist.Name} - {recording.Tracks.First().Album.Title} - {recording.Tracks.First().Title}"));
        }
    }
}
