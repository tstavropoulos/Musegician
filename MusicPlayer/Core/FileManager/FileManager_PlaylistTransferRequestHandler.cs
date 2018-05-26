using System;
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
        #region IPlaylistTransferRequestHandler

        private IPlaylistTransferRequestHandler ThisTransfer => this;

        IEnumerable<Song> IPlaylistTransferRequestHandler.GetSongData(
            Recording recording)
        {
            return ThisTransfer.GetSongData(
                song: recording.Song,
                exclusiveRecording: recording);
        }

        IEnumerable<Song> IPlaylistTransferRequestHandler.GetSongData(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null)
        {
            List<Song> songData = new List<Song>();

            dbConnection.Open();

            string playlistName;

            if (exclusiveRecording != -1)
            {
                playlistName = recordingCommands._GetPlaylistName(
                    recordingID: exclusiveRecordingID);
            }
            else if (exclusiveArtistID != -1)
            {
                playlistName = artistCommands._GetPlaylistSongName(
                    artistID: exclusiveArtistID,
                    songID: songID);
            }
            else
            {
                playlistName = songCommands._GetPlaylistSongName(
                    songID: songID);
            }

            songData.Add(new Song(
                songID: songID,
                title: playlistName));

            foreach (Recording data in GetRecordingList(
                songID: songID,
                exclusiveArtistID: exclusiveArtistID,
                exclusiveRecordingID: exclusiveRecordingID))
            {
                songData[0].Children.Add(data);
            }

            dbConnection.Close();

            return songData;
        }

        IEnumerable<Song> IPlaylistTransferRequestHandler.GetAlbumData(
            Album album,
            bool deep)
        {
            if (deep)
            {
                return albumCommands.GetAlbumDataDeep(
                    albumID: albumID);
            }
            else
            {
                return albumCommands.GetAlbumData(
                    albumID: albumID);
            }
        }

        IEnumerable<Song> IPlaylistTransferRequestHandler.GetArtistData(
            Artist artist,
            bool deep)
        {
            List<Song> artistData = new List<Song>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.id AS id, " +
                    "song_weight.weight AS weight " +
                "FROM song " +
                "LEFT JOIN song_weight ON song.id=song_weight.song_id " +
                "WHERE id IN ( " +
                    "SELECT song_id " +
                    "FROM recording " +
                    "WHERE artist_id=@artistID ) " +
                "ORDER BY title ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["id"];
                    string playlistName;

                    double weight = double.NaN;

                    if (deep)
                    {
                        playlistName = songCommands._GetPlaylistSongName(
                            songID: songID);
                    }
                    else
                    {
                        playlistName = artistCommands._GetPlaylistSongName(
                            artistID: artistID,
                            songID: songID);
                    }


                    if (reader["weight"].GetType() != typeof(DBNull))
                    {
                        weight = (double)reader["weight"];
                    }

                    Song newSong = new Song(
                        songID: songID,
                        title: playlistName)
                    {
                        Weight = weight
                    };

                    foreach (Recording recording in GetRecordingList(
                            songID: songID,
                            exclusiveArtistID: deep ? -1 : artistID))
                    {
                        newSong.Children.Add(recording);
                    }

                    artistData.Add(newSong);
                }
            }

            dbConnection.Close();

            return artistData;
        }

        string IPlaylistTransferRequestHandler.GetDefaultPlaylistName(LibraryContext context, BaseData data)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                    return artistCommands.GetArtistName(id);
                case LibraryContext.Album:
                    return albumCommands.GetAlbumTitle(id);
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

        IEnumerable<Recording> GetRecordingList(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null)
        {
            return recordingCommands._GetRecordingList(
                song: song,
                exclusiveArtist: exclusiveArtist,
                exclusiveRecording: exclusiveRecording);
        }

        #endregion IPlaylistTransferRequestHandler
    }
}
