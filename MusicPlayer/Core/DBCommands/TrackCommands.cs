using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Core.DBCommands
{
    public class TrackCommands
    {
        ArtistCommands artistCommands = null;
        SongCommands songCommands = null;
        RecordingCommands recordingCommands = null;

        SQLiteConnection dbConnection;

        private long _lastIDAssigned = 0;
        public long NextID
        {
            get { return ++_lastIDAssigned; }
        }

        public TrackCommands()
        {

        }

        public void Initialize(
            SQLiteConnection dbConnection,
            ArtistCommands artistCommands,
            SongCommands songCommands,
            RecordingCommands recordingCommands)
        {
            this.dbConnection = dbConnection;
            this.artistCommands = artistCommands;
            this.songCommands = songCommands;
            this.recordingCommands = recordingCommands;

            _lastIDAssigned = 0;
        }

        /// <summary>
        /// Splitting, Renaming, And/Or Consolidating Tracks by Song Title
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(ICollection<long> trackIDs, string newTitle)
        {
            List<long> trackIDsCopy = new List<long>(trackIDs);

            dbConnection.Open();

            //First, grab the current songID:
            long oldSongID = _FindSongID_ByTrackID(
                trackID: trackIDsCopy[0]);

            //Is there a song currently by the same artist with the same name?
            long newSongID = songCommands._FindSongID_ByTitle_MatchSongArtist(
                songTitle: newTitle,
                oldSongID: oldSongID);


            using (SQLiteTransaction updateSongTitles = dbConnection.BeginTransaction())
            {
                if (newSongID == -1)
                {
                    //New Song did not exist
                    //  We need to create the new song
                    newSongID = songCommands._CreateSong(
                        transaction: updateSongTitles,
                        songTitle: newTitle);
                }

                //New song did exist, or we passed in more than one track
                //Update track table to point at new song
                recordingCommands._ReassignSongIDs_ByTrackID(
                    transaction: updateSongTitles,
                    songID: newSongID,
                    trackIDs: trackIDsCopy);

                updateSongTitles.Commit();
            }

            dbConnection.Close();
        }



        /// <summary>
        /// Assigning Tracks to a different artist
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateArtistName(ICollection<long> trackIDs, string newArtistName)
        {
            List<long> songIDsCopy = new List<long>(trackIDs);

            //Renaming (or Consolidating) a Song
            dbConnection.Open();

            long artistID = artistCommands._FindArtist_ByName(
                artistName: newArtistName);

            using (SQLiteTransaction updateArtistName = dbConnection.BeginTransaction())
            {
                if (artistID == -1)
                {
                    //New Artist did not exist
                    //  We need to Create one

                    //Update the song formerly in the front
                    artistID = artistCommands._CreateArtist(
                        transaction: updateArtistName,
                        artistName: newArtistName);
                }

                //Update the recording ArtistIDs
                recordingCommands._ReassignArtistIDs_ByTrackID(
                    transaction: updateArtistName,
                    artistID: artistID,
                    trackIDs: trackIDs);

                //Now, delete any old artists with no remaining recordings
               artistCommands._DeleteAllLeafs(
                    transaction: updateArtistName);

                updateArtistName.Commit();
            }

            dbConnection.Close();
        }


        #region Search Commands

        public long _FindSongID_ByTrackID(long trackID)
        {
            long songID = -1;

            SQLiteCommand findSongID = dbConnection.CreateCommand();
            findSongID.CommandType = System.Data.CommandType.Text;
            findSongID.Parameters.Add(new SQLiteParameter("@trackID", trackID));
            findSongID.CommandText =
                "SELECT song_id " +
                "FROM track " +
                "WHERE track_id=@trackID;";

            using (SQLiteDataReader reader = findSongID.ExecuteReader())
            {
                if (reader.Read())
                {
                    songID = (long)reader["song_id"];
                }
            }

            return songID;
        }

        #endregion  // Search Commands

        #region Initialization Commands

        public void _InitializeValues()
        {
            SQLiteCommand loadSongs = dbConnection.CreateCommand();
            loadSongs.CommandType = System.Data.CommandType.Text;
            loadSongs.CommandText =
                "SELECT track_id " +
                "FROM track " +
                "ORDER BY track_id DESC " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = loadSongs.ExecuteReader())
            {
                if (reader.Read())
                {
                    _lastIDAssigned = (long)reader["track_id"];
                }
            }
        }

        #endregion //Initialization Commands

        #region Update Commands

        public void _ReassignAlbumID(
            SQLiteTransaction transaction,
            long albumID,
            ICollection<long> trackIDs)
        {
            SQLiteCommand remapSongID = dbConnection.CreateCommand();
            remapSongID.Transaction = transaction;
            remapSongID.CommandType = System.Data.CommandType.Text;
            remapSongID.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            remapSongID.Parameters.Add("@trackID", DbType.Int64);
            remapSongID.CommandText =
                "UPDATE track " +
                    "SET album_id=@albumID " +
                    "WHERE track_id=@trackID;";
            foreach (long id in trackIDs)
            {
                remapSongID.Parameters["@trackID"].Value = id;
                remapSongID.ExecuteNonQuery();
            }
        }

        public void _RemapAlbumID(
            SQLiteTransaction transaction,
            long newAlbumID,
            ICollection<long> oldAlbumIDs)
        {
            SQLiteCommand remapSongID = dbConnection.CreateCommand();
            remapSongID.Transaction = transaction;
            remapSongID.CommandType = System.Data.CommandType.Text;
            remapSongID.Parameters.Add(new SQLiteParameter("@newAlbumID", newAlbumID));
            remapSongID.Parameters.Add("@oldAlbumID", DbType.Int64);
            remapSongID.CommandText =
                "UPDATE track " +
                    "SET track.album_id=@newAlbumID " +
                    "WHERE track.album_id=@oldAlbumID;";
            foreach (long id in oldAlbumIDs)
            {
                remapSongID.Parameters["@oldAlbumID"].Value = id;
                remapSongID.ExecuteNonQuery();
            }
        }

        #endregion // Update Commands


        #region Create Commands

        public void _CreateTrackTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createTrackTable = dbConnection.CreateCommand();
            createTrackTable.Transaction = transaction;
            createTrackTable.CommandType = System.Data.CommandType.Text;
            createTrackTable.CommandText =
                "CREATE TABLE IF NOT EXISTS track (" +
                    "track_id INTEGER PRIMARY KEY, " +
                    "album_id INTEGER REFERENCES album, " +
                    "recording_id INTEGER REFERENCES recording, " +
                    "track_title TEXT, " +
                    "track_number INTEGER, " +
                    "disc_number INTEGER);";
            createTrackTable.ExecuteNonQuery();
            
            SQLiteCommand createTrackWeightTable = dbConnection.CreateCommand();
            createTrackWeightTable.Transaction = transaction;
            createTrackWeightTable.CommandType = System.Data.CommandType.Text;
            createTrackWeightTable.CommandText =
                "CREATE TABLE IF NOT EXISTS track_weight (" +
                    "track_id INTEGER PRIMARY KEY, " +
                    "weight REAL);";
            createTrackWeightTable.ExecuteNonQuery();
        }

        #endregion //Create Commands


        #region Insert Commands

        public void _BatchCreateTracks(
            SQLiteTransaction transaction,
            ICollection<TrackData> newTrackRecords)
        {
            SQLiteCommand writeTrack = dbConnection.CreateCommand();
            writeTrack.Transaction = transaction;
            writeTrack.CommandType = System.Data.CommandType.Text;
            writeTrack.CommandText = "INSERT INTO track " +
                "(track_id, album_id, recording_id, track_title, track_number, disc_number) VALUES " +
                "(@trackID, @albumID, @recordingID, @trackTitle, @trackNumber, @discNumber);";
            writeTrack.Parameters.Add("@trackID", DbType.Int64);
            writeTrack.Parameters.Add("@albumID", DbType.Int64);
            writeTrack.Parameters.Add("@recordingID", DbType.Int64);
            writeTrack.Parameters.Add("@trackTitle", DbType.String);
            writeTrack.Parameters.Add("@trackNumber", DbType.Int64);
            writeTrack.Parameters.Add("@discNumber", DbType.Int64);

            foreach (TrackData track in newTrackRecords)
            {
                writeTrack.Parameters["@trackID"].Value = track.trackID;
                writeTrack.Parameters["@albumID"].Value = track.albumID;
                writeTrack.Parameters["@recordingID"].Value = track.recordingID;
                writeTrack.Parameters["@trackTitle"].Value = track.trackTitle;
                writeTrack.Parameters["@trackNumber"].Value = track.trackNumber;
                writeTrack.Parameters["@discNumber"].Value = track.discNumber;

                writeTrack.ExecuteNonQuery();
            }
        }

        #endregion // Insert Commands
    }
}
