using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Musegician.DataStructures;
using Musegician.Playlist;

using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician
{
    public partial class FileManager : IPlaylistTransferRequestHandler
    {
        #region IPlaylistTransferRequestHandler

        private IPlaylistTransferRequestHandler ThisTransfer
        {
            get { return this; }
        }

        List<SongDTO> IPlaylistTransferRequestHandler.GetSongDataFromRecordingID(
            long recordingID)
        {
            RecordingData data = recordingCommands.GetData(
                recordingID: recordingID);

            if (!data.RecordFound())
            {
                return null;
            }

            return ThisTransfer.GetSongData(
                songID: data.songID,
                exclusiveRecordingID: recordingID);
        }

        List<SongDTO> IPlaylistTransferRequestHandler.GetSongData(
            long songID,
            long exclusiveArtistID,
            long exclusiveRecordingID)
        {
            List<SongDTO> songData = new List<SongDTO>();

            dbConnection.Open();

            string playlistName;

            if (exclusiveRecordingID != -1)
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

            songData.Add(new SongDTO(
                songID: songID,
                title: playlistName));

            foreach (RecordingDTO data in GetRecordingList(
                                            songID: songID,
                                            exclusiveArtistID: exclusiveArtistID,
                                            exclusiveRecordingID: exclusiveRecordingID))
            {
                songData[0].Children.Add(data);
            }

            dbConnection.Close();

            return songData;
        }

        List<SongDTO> IPlaylistTransferRequestHandler.GetAlbumData(
            long albumID,
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

        List<SongDTO> IPlaylistTransferRequestHandler.GetArtistData(
            long artistID,
            bool deep)
        {
            List<SongDTO> artistData = new List<SongDTO>();

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
                        playlistName = artistCommands._GetPlaylistSongName(
                            artistID: artistID,
                            songID: songID);
                    }
                    else
                    {
                        playlistName = songCommands._GetPlaylistSongName(
                            songID: songID);
                    }


                    if (reader["weight"].GetType() != typeof(DBNull))
                    {
                        weight = (double)reader["weight"];
                    }

                    SongDTO newSong = new SongDTO(
                        songID: songID,
                        title: playlistName)
                    {
                        Weight = weight
                    };

                    foreach (RecordingDTO recording in GetRecordingList(
                            songID: songID,
                            exclusiveArtistID: deep ? artistID : -1))
                    {
                        newSong.Children.Add(recording);
                    }

                    artistData.Add(newSong);
                }
            }

            dbConnection.Close();

            return artistData;
        }

        string IPlaylistTransferRequestHandler.GetDefaultPlaylistName(LibraryContext context, long id)
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

        List<RecordingDTO> GetRecordingList(
            long songID,
            long exclusiveArtistID = -1,
            long exclusiveRecordingID = -1)
        {
            return recordingCommands._GetRecordingList(
                songID: songID,
                exclusiveArtistID: exclusiveArtistID,
                exclusiveRecordingID: exclusiveRecordingID);
        }

        #endregion IPlaylistTransferRequestHandler
    }
}
