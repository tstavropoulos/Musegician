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
        SongCommands songCommands = null;

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
            SongCommands songCommands)
        {
            this.dbConnection = dbConnection;
            this.songCommands = songCommands;

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

            long artistID = songCommands._FindArtistID_BySongID(
                songID: oldSongID);

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
                _ReassignSongID(
                    transaction: updateSongTitles,
                    songID: newSongID,
                    trackIDs: trackIDsCopy);

                updateSongTitles.Commit();
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

        #region Lookup Commands

        public void _PopulateLookup()
        {
            SQLiteCommand loadTracks = dbConnection.CreateCommand();
            loadTracks.CommandType = System.Data.CommandType.Text;
            loadTracks.CommandText =
                "SELECT track_id " +
                "FROM track;";

            using (SQLiteDataReader reader = loadTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long trackID = (long)reader["track_id"];

                    if (trackID > _lastIDAssigned)
                    {
                        _lastIDAssigned = trackID;
                    }
                }
            }
        }

        #endregion  //Lookup Commands

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

        public void _ReassignSongID(
            SQLiteTransaction transaction,
            long songID,
            ICollection<long> trackIDs)
        {
            SQLiteCommand remapSongID = dbConnection.CreateCommand();
            remapSongID.Transaction = transaction;
            remapSongID.CommandType = System.Data.CommandType.Text;
            remapSongID.Parameters.Add(new SQLiteParameter("@songID", songID));
            remapSongID.Parameters.Add("@trackID", DbType.Int64);
            remapSongID.CommandText =
                "UPDATE track " +
                    "SET track.song_id=@songID " +
                    "WHERE track.track_id=@trackID;";
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

        public void _RemapSongID(
            SQLiteTransaction transaction,
            long newSongID,
            ICollection<long> oldSongIDs)
        {
            SQLiteCommand remapSongID = dbConnection.CreateCommand();
            remapSongID.Transaction = transaction;
            remapSongID.CommandType = System.Data.CommandType.Text;
            remapSongID.Parameters.Add(new SQLiteParameter("@newSongID", newSongID));
            remapSongID.Parameters.Add("@oldSongID", DbType.Int64);
            remapSongID.CommandText =
                "UPDATE track " +
                    "SET track.song_id=@newSongID " +
                    "WHERE track.song_id=@oldSongID;";
            foreach (long id in oldSongIDs)
            {
                remapSongID.Parameters["@oldSongID"].Value = id;
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
            writeTrack.Parameters.Add(new SQLiteParameter("@trackID", -1));
            writeTrack.Parameters.Add(new SQLiteParameter("@albumID", -1));
            writeTrack.Parameters.Add(new SQLiteParameter("@recordingID", -1));
            writeTrack.Parameters.Add(new SQLiteParameter("@trackTitle", ""));
            writeTrack.Parameters.Add(new SQLiteParameter("@trackNumber", -1));
            writeTrack.Parameters.Add(new SQLiteParameter("@discNumber", -1));

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
