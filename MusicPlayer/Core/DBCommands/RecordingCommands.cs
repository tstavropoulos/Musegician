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
    public class RecordingCommands
    {
        SQLiteConnection dbConnection;

        private long _lastIDAssigned = 0;
        public long NextID
        {
            get { return ++_lastIDAssigned; }
        }

        public RecordingCommands()
        {
        }

        public void Initialize(SQLiteConnection dbConnection)
        {
            this.dbConnection = dbConnection;

            _lastIDAssigned = 0;
        }

        #region High Level Commands

        public RecordingData GetData(long recordingID)
        {
            dbConnection.Open();

            RecordingData recordingData = _GetData(recordingID);

            dbConnection.Close();

            return recordingData;
        }

        public long GetSongID(long recordingID)
        {
            dbConnection.Open();

            long songID = _GetData(recordingID).songID;

            dbConnection.Close();

            return songID;
        }

        public long GetArtistID(long recordingID)
        {
            dbConnection.Open();

            long artistID = _GetData(recordingID).artistID;

            dbConnection.Close();

            return artistID;
        }

        #endregion // High Level Commands

        #region Search Commands

        public RecordingData _GetData(long recordingID)
        {
            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT * " +
                "FROM recording " +
                "WHERE recording_id=@recordingID " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new RecordingData()
                    {
                        recordingID = recordingID,
                        artistID = (long)reader["artist_id"],
                        songID = (long)reader["song_id"],
                        filename = (string)reader["filename"],
                        live = (bool)reader["live"],
                        valid = System.IO.File.Exists((string)reader["filename"])
                    };
                }
            }

            return RecordingData.Invalid;
        }

        public string _GetPlaylistName(long recordingID)
        {
            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.song_title AS song_title, " +
                    "artist.artist_name AS artist_name " +
                "FROM recording " +
                "LEFT JOIN song ON recording.song_id=song.song_id " +
                "LEFT JOIN artist ON recording.artist_id=artist.artist_id " +
                "WHERE recording.recording_id=@recordingID;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    return string.Format(
                        "{0} - {1}",
                        (string)reader["artist_name"],
                        (string)reader["song_title"]);

                }
            }

            return "INVALID";
        }


        #endregion  //Search Commands

        #region Lookup Commands

        public void _PopulateLookup(
            HashSet<string> loadedFilenames)
        {
            SQLiteCommand loadRecordings = dbConnection.CreateCommand();
            loadRecordings.CommandType = System.Data.CommandType.Text;
            loadRecordings.CommandText =
                "SELECT recording_id, filename " +
                "FROM recording;";

            using (SQLiteDataReader reader = loadRecordings.ExecuteReader())
            {
                while (reader.Read())
                {
                    long recordingID = (long)reader["recording_id"];
                    string filename = (string)reader["filename"];
                    bool valid = System.IO.File.Exists(filename);

                    loadedFilenames.Add(filename);

                    if (recordingID > _lastIDAssigned)
                    {
                        _lastIDAssigned = recordingID;
                    }
                }
            }
        }

        #endregion  //Lookup Commands

        #region Update Commands

        #endregion // Update Commands

        #region Create Commands

        public void _CreateRecordingTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createRecordingTable = dbConnection.CreateCommand();
            createRecordingTable.Transaction = transaction;
            createRecordingTable.CommandType = System.Data.CommandType.Text;
            createRecordingTable.CommandText =
                "CREATE TABLE IF NOT EXISTS recording (" +
                    "recording_id INTEGER PRIMARY KEY, " +
                    "artist_id INTEGER references artist, " +
                    "song_id INTEGER references song, " +
                    "filename TEXT, " +
                    "live BOOLEAN);";
            createRecordingTable.ExecuteNonQuery();

            SQLiteCommand createSongIDIndex = dbConnection.CreateCommand();
            createSongIDIndex.Transaction = transaction;
            createSongIDIndex.CommandType = System.Data.CommandType.Text;
            createSongIDIndex.CommandText =
                    "CREATE INDEX IF NOT EXISTS idx_recording_songid ON recording (song_id);";
            createSongIDIndex.ExecuteNonQuery();

            SQLiteCommand createArtistIDIndex = dbConnection.CreateCommand();
            createArtistIDIndex.Transaction = transaction;
            createArtistIDIndex.CommandType = System.Data.CommandType.Text;
            createArtistIDIndex.CommandText =
                    "CREATE INDEX IF NOT EXISTS idx_recording_artistid ON recording (artist_id);";
            createArtistIDIndex.ExecuteNonQuery();
        }

        #endregion //Create Commands

        #region Insert Commands

        public long _CreateRecording(
            SQLiteTransaction transaction,
            string filename,
            long artistID,
            long songID,
            bool live = false)
        {
            long recordingID = NextID;

            SQLiteCommand createRecording = dbConnection.CreateCommand();
            createRecording.Transaction = transaction;
            createRecording.CommandType = System.Data.CommandType.Text;
            createRecording.CommandText =
                "INSERT INTO recording " +
                    "(recording_id, artist_id, song_id, filename, live) VALUES " +
                    "(@recordingID, @artistID, @songID, @filename, @live);";
            createRecording.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));
            createRecording.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            createRecording.Parameters.Add(new SQLiteParameter("@songID", songID));
            createRecording.Parameters.Add(new SQLiteParameter("@filename", filename));
            createRecording.Parameters.Add(new SQLiteParameter("@live", live));

            createRecording.ExecuteNonQuery();

            return recordingID;
        }

        public void _BatchCreateRecording(
            SQLiteTransaction transaction,
            ICollection<RecordingData> newRecordingRecords)
        {
            SQLiteCommand createRecordings = dbConnection.CreateCommand();
            createRecordings.Transaction = transaction;
            createRecordings.CommandType = System.Data.CommandType.Text;
            createRecordings.CommandText =
                "INSERT INTO recording " +
                    "(recording_id, artist_id, song_id, filename, live) VALUES " +
                    "(@recordingID, @artistID, @songID, @filename, @live);";
            createRecordings.Parameters.Add("@recordingID", DbType.Int64);
            createRecordings.Parameters.Add("@artistID", DbType.Int64);
            createRecordings.Parameters.Add("@songID", DbType.Int64);
            createRecordings.Parameters.Add("@filename", DbType.AnsiString);
            createRecordings.Parameters.Add("@live", DbType.Boolean);

            foreach (RecordingData recording in newRecordingRecords)
            {
                createRecordings.Parameters["@recordingID"].Value = recording.recordingID;
                createRecordings.Parameters["@artistID"].Value = recording.artistID;
                createRecordings.Parameters["@songID"].Value = recording.songID;
                createRecordings.Parameters["@filename"].Value = recording.filename;
                createRecordings.Parameters["@live"].Value = recording.live;
                createRecordings.ExecuteNonQuery();
            }
        }


        #endregion //Insert Commands

        #region Delete Commands

        public void _DeleteRecordingID(
            SQLiteTransaction updateTransaction,
            ICollection<long> recordingIDs)
        {
            SQLiteCommand deleteRecording_ByRecordingID = dbConnection.CreateCommand();
            deleteRecording_ByRecordingID.Transaction = updateTransaction;
            deleteRecording_ByRecordingID.CommandType = System.Data.CommandType.Text;
            deleteRecording_ByRecordingID.Parameters.Add("@recordingID", DbType.Int64);
            deleteRecording_ByRecordingID.CommandText =
                "DELETE FROM recording " +
                "WHERE recording.recording_id=@recordingID;";
            foreach (long id in recordingIDs)
            {
                deleteRecording_ByRecordingID.Parameters["@recordingID"].Value = id;
                deleteRecording_ByRecordingID.ExecuteNonQuery();
            }
        }

        #endregion // Delete Commands
    }
}
