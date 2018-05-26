using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using Musegician.DataStructures;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class RecordingCommands
    {
        ArtistCommands artistCommands = null;
        SongCommands songCommands = null;

        MusegicianData db = null;

        public RecordingCommands()
        {
        }

        public void Initialize(
            MusegicianData db,
            ArtistCommands artistCommands,
            SongCommands songCommands)
        {
            this.db = db;

            this.artistCommands = artistCommands;
            this.songCommands = songCommands;
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

        /// <summary>
        /// Splitting, Renaming, And/Or Consolidating Recordings by Song Title
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(IEnumerable<long> recordingIDs, string newTitle)
        {
            dbConnection.Open();

            //Is there a song currently by the same artist with the same name?
            long newSongID = _FindSongID_ByTitle_MatchSongArtist(
                songTitle: newTitle,
                recordingID: recordingIDs.ElementAt(0));


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

                //New song did exist, or we passed in more than one recording
                //Update recording table to point at new song
                _ReassignSongIDs(
                    transaction: updateSongTitles,
                    songID: newSongID,
                    recordingIDs: recordingIDs);

                updateSongTitles.Commit();
            }

            dbConnection.Close();
        }

        /// <summary>
        /// Assigning Tracks to a different artist
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateArtistName(IEnumerable<long> recordingIDs, string newArtistName)
        {
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
                _ReassignArtistIDs(
                    transaction: updateArtistName,
                    artistID: artistID,
                    recordingIDs: recordingIDs);

                //Now, delete any old artists with no remaining recordings
                artistCommands._DeleteAllLeafs(
                     transaction: updateArtistName);

                updateArtistName.Commit();
            }

            dbConnection.Close();
        }

        public List<RecordingDTO> GenerateSongRecordingList(long songID, long albumID)
        {
            List<RecordingDTO> recordingList = new List<RecordingDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "track.id AS track_id, " +
                    "track.title AS track_title, " +
                    "track.album_id AS album_id, " +
                    "track.recording_id AS recording_id, " +
                    "recording.live AS live, " +
                    "track_weight.weight AS weight, " +
                    "album.title AS album_title, " +
                    "artist.name AS artist_name " +
                "FROM recording " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "LEFT JOIN track_weight ON track.id=track_weight.track_id " +
                "WHERE recording.song_id=@songID ORDER BY recording.live ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    double weight = double.NaN;

                    if (reader["weight"].GetType() != typeof(DBNull))
                    {
                        weight = (double)reader["weight"];
                    }

                    bool albumMatch = false;
                    if (albumID == (long)reader["album_id"])
                    {
                        albumMatch = true;
                    }

                    recordingList.Add(new RecordingDTO
                    {
                        ID = (long)reader["recording_id"],
                        Name = string.Format(
                            "{0} - {1} - {2}",
                            (string)reader["artist_name"],
                            (string)reader["album_title"],
                            (string)reader["track_title"]),
                        IsHome = albumMatch,
                        Live = (bool)reader["live"],
                        Weight = weight,
                        TrackID = (long)reader["track_id"]
                    });
                }
            }

            dbConnection.Close();

            return recordingList;
        }

        public void UpdateLive(IEnumerable<long> recordingIDs, bool newLiveValue)
        {
            dbConnection.Open();
            
            using (SQLiteTransaction updateLive = dbConnection.BeginTransaction())
            {
                foreach (long recordingID in recordingIDs)
                {
                    _UpdateLiveValue(
                        transaction: updateLive,
                        recordingID: recordingID,
                        newLiveValue: newLiveValue);
                }

                updateLive.Commit();
            }

            dbConnection.Close();
        }

        public PlayData GetRecordingPlayData(long recordingID)
        {
            PlayData playData = new PlayData();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "track.title AS track_title, " +
                    "artist.name AS artist_name, " +
                    "recording.filename AS filename " +
                "FROM recording " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN song ON recording.song_id=song.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "WHERE recording.id=@recordingID;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    playData.songTitle = (string)reader["track_title"];
                    playData.artistName = (string)reader["artist_name"];
                    playData.filename = (string)reader["filename"];
                    playData.recordingID = recordingID;
                }
            }

            dbConnection.Close();

            return playData;
        }

        public List<DirectoryDTO> GetDirectories(string path)
        {
            List<DirectoryDTO> directoryList = new List<DirectoryDTO>();
            HashSet<string> directorySet = new HashSet<string>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT filename " +
                "FROM recording " +
                "WHERE filename LIKE @Path || '%' " +
                "ORDER BY filename COLLATE NOCASE ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@Path", path));

            string currentChunk = "";
            int directoryChunkDepth = -1;


            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    string relativepath;

                    if (path == "")
                    {
                        relativepath = ((string)reader["filename"]);
                    }
                    else
                    {
                        relativepath = ((string)reader["filename"]).Substring(path.Length);
                    }

                    //Contains Subdirectory
                    if (relativepath.Contains(System.IO.Path.DirectorySeparatorChar))
                    {
                        if (directoryChunkDepth < 0)
                        {
                            //Initialization
                            directoryChunkDepth = _CountDirectories(relativepath);
                            currentChunk = _GrabPathChunk(relativepath, directoryChunkDepth);
                        }

                        string relativeDirChunk = _GrabPathChunk(relativepath, directoryChunkDepth);

                        while (directoryChunkDepth > 1)
                        {
                            //Possible Directory Collapse Condition
                            if (currentChunk == relativeDirChunk)
                            {
                                //Matches the deep comparison, keep going
                                break;
                            }

                            //Otherwise I need to decrement directoryChunkDepth
                            --directoryChunkDepth;
                            //Update my chunks
                            currentChunk = _GrabPathChunk(currentChunk, directoryChunkDepth);
                            relativeDirChunk = _GrabPathChunk(relativepath, directoryChunkDepth);
                            //And add this old chunk if we've hit one (otherwise it's been skipped)
                            if (directoryChunkDepth == 1)
                            {
                                if (directorySet.Add(currentChunk))
                                {
                                    directoryList.Add(new DirectoryDTO(currentChunk));
                                }
                            }
                        }

                        if (directoryChunkDepth == 1)
                        {
                            //Branching - we will just add every subdir
                            if (directorySet.Add(relativeDirChunk))
                            {
                                directoryList.Add(new DirectoryDTO(relativeDirChunk));
                            }
                        }
                    }

                }
            }

            if (directoryChunkDepth > 1)
            {
                //Stash my one, multi-directory chunk
                directoryList.Add(new DirectoryDTO(currentChunk));
            }

            dbConnection.Close();


            return directoryList;
        }

        #endregion High Level Commands
        #region Search Commands

        public RecordingData _GetData(long recordingID)
        {
            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT * " +
                "FROM recording " +
                "WHERE id=@recordingID " +
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

        public RecordingDTO _GetRecording(long recordingID, long albumID)
        {
            RecordingDTO recording = null;

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "album.title AS album_title, " +
                    "artist.name AS artist_name, " +
                    "track.title AS track_title, " +
                    "recording.live AS live, " +
                    "track.id AS track_id " +
                "FROM recording " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "LEFT JOIN song ON recording.song_id=song.id " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "WHERE recording.id=@recordingID AND track.album_id=@albumID " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));


            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    recording = new RecordingDTO()
                    {
                        Name = string.Format(
                            "{0} - {1} - {2}",
                            (string)reader["artist_name"],
                            (string)reader["album_title"],
                            (string)reader["track_title"]),
                        ID = recordingID,
                        Weight = Double.NaN,
                        Live = (bool)reader["live"],
                        TrackID = (long)reader["track_id"]
                    };
                }
            }

            return recording;
        }

        public string _GetPlaylistName(long recordingID)
        {
            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.title AS title, " +
                    "artist.name AS name " +
                "FROM recording " +
                "LEFT JOIN song ON recording.song_id=song.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "WHERE recording.id=@recordingID;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    return string.Format(
                        "{0} - {1}",
                        (string)reader["name"],
                        (string)reader["title"]);

                }
            }

            return "INVALID";
        }

        public long _FindSongID_ByTitle_MatchSongArtist(string songTitle, long recordingID)
        {
            long newSongID = -1;

            SQLiteCommand findSongID_ByTitle_SameArtist = dbConnection.CreateCommand();
            findSongID_ByTitle_SameArtist.CommandType = System.Data.CommandType.Text;
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@songTitle", songTitle));
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));
            findSongID_ByTitle_SameArtist.CommandText =
                "SELECT id " +
                "FROM song " +
                "WHERE title=@songTitle COLLATE NOCASE AND " +
                    "id IN ( " +
                        "SELECT song_id " +
                        "FROM recording " +
                        "WHERE artist_id IN ( " +
                            "SELECT artist_id " +
                            "FROM recording " +
                            "WHERE recording_id=@recordingID " +
                        ")" +
                    ");";

            //First, find out if the new song already exists (ie, consolidation)

            using (SQLiteDataReader reader = findSongID_ByTitle_SameArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    newSongID = (long)reader["id"];
                }
            }

            return newSongID;
        }

        public List<RecordingDTO> _GetRecordingList(
            long songID,
            long exclusiveArtistID = -1,
            long exclusiveRecordingID = -1)
        {
            List<RecordingDTO> recordingData = new List<RecordingDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "recording.id AS recording_id, " +
                    "track.title AS track_title, " +
                    "track_weight.weight AS weight, " +
                    "recording.artist_id AS artist_id, " +
                    "recording.live AS live, " +
                    "album.title AS album_title, " +
                    "artist.name AS artist_name, " +
                    "track.id AS track_id " +
                "FROM recording " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "LEFT JOIN track_weight ON track.id=track_weight.track_id " +
                "WHERE recording.song_id=@songID;";

            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long recordingID = (long)reader["recording_id"];
                    double weight = double.NaN;

                    //ExclusiveRecordingID trumps ExclusiveArtistID
                    if (exclusiveRecordingID != -1)
                    {
                        if (recordingID != exclusiveRecordingID)
                        {
                            continue;
                        }

                        weight = 1.0;
                    }
                    else if (exclusiveArtistID != -1)
                    {
                        if ((long)reader["artist_id"] != exclusiveArtistID)
                        {
                            continue;
                        }

                        if (reader["weight"].GetType() != typeof(DBNull))
                        {
                            weight = (double)reader["weight"];
                        }
                    }
                    else
                    {
                        if (reader["weight"].GetType() != typeof(DBNull))
                        {
                            weight = (double)reader["weight"];
                        }
                    }


                    recordingData.Add(new RecordingDTO()
                    {
                        Name = string.Format(
                            "{0} - {1} - {2}",
                            (string)reader["artist_name"],
                            (string)reader["album_title"],
                            (string)reader["track_title"]),
                        ID = recordingID,
                        Weight = weight,
                        Live = (bool)reader["Live"],
                        TrackID = (long)reader["track_id"]
                    });
                }
            }

            return recordingData;
        }

        public long _GetAlbumID(long recordingID)
        {
            long albumID = -1;

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT album_id " +
                "FROM track " +
                "WHERE track.recording_id=@recordingID;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    albumID = (long)reader["album_id"];
                }
            }

            return albumID;
        }

        #endregion Search Commands
        //#region Lookup Commands

        //public void _PopulateLookup(
        //    HashSet<string> loadedFilenames)
        //{
        //    SQLiteCommand loadRecordings = dbConnection.CreateCommand();
        //    loadRecordings.CommandType = System.Data.CommandType.Text;
        //    loadRecordings.CommandText =
        //        "SELECT id, filename " +
        //        "FROM recording;";

        //    using (SQLiteDataReader reader = loadRecordings.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            long recordingID = (long)reader["id"];
        //            string filename = (string)reader["filename"];
        //            bool valid = System.IO.File.Exists(filename);

        //            loadedFilenames.Add(filename);
        //        }
        //    }
        //}

        //#endregion Lookup Commands
        #region Update Commands

        public void _UpdateLiveValue(
            SQLiteTransaction transaction,
            long recordingID,
            bool newLiveValue)
        {
            SQLiteCommand updateRecordingLive = dbConnection.CreateCommand();
            updateRecordingLive.Transaction = transaction;
            updateRecordingLive.CommandType = System.Data.CommandType.Text;
            updateRecordingLive.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));
            updateRecordingLive.Parameters.Add(new SQLiteParameter("@live", newLiveValue));
            updateRecordingLive.CommandText =
                "UPDATE recording " +
                    "SET live=@live " +
                    "WHERE id=@recordingID;";
            updateRecordingLive.ExecuteNonQuery();
        }

        public void _ReassignArtistIDs_ByTrackID(
            SQLiteTransaction transaction,
            long artistID,
            IEnumerable<long> trackIDs)
        {
            SQLiteCommand updateArtistID = dbConnection.CreateCommand();
            updateArtistID.Transaction = transaction;
            updateArtistID.CommandType = System.Data.CommandType.Text;
            updateArtistID.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            updateArtistID.Parameters.Add("@trackID", DbType.Int64);
            updateArtistID.CommandText =
                "UPDATE recording " +
                    "SET artist_id=@artistID " +
                    "WHERE id IN ( " +
                        "SELECT recording_id " +
                        "FROM track " +
                        "WHERE id=@trackID);";
            foreach (long id in trackIDs)
            {
                updateArtistID.Parameters["@trackID"].Value = id;
                updateArtistID.ExecuteNonQuery();
            }
        }

        public void _ReassignSongIDs_ByTrackID(
            SQLiteTransaction transaction,
            long songID,
            ICollection<long> trackIDs)
        {
            SQLiteCommand updateArtistID = dbConnection.CreateCommand();
            updateArtistID.Transaction = transaction;
            updateArtistID.CommandType = System.Data.CommandType.Text;
            updateArtistID.Parameters.Add(new SQLiteParameter("@songID", songID));
            updateArtistID.Parameters.Add("@trackID", DbType.Int64);
            updateArtistID.CommandText =
                "UPDATE recording " +
                    "SET song_id=@songID " +
                    "WHERE id IN ( " +
                        "SELECT recording_id " +
                        "FROM track " +
                        "WHERE id=@trackID);";
            foreach (long id in trackIDs)
            {
                updateArtistID.Parameters["@trackID"].Value = id;
                updateArtistID.ExecuteNonQuery();
            }
        }

        public void _ReassignSongIDs(
            SQLiteTransaction transaction,
            long songID,
            IEnumerable<long> recordingIDs)
        {
            SQLiteCommand updateArtistID = dbConnection.CreateCommand();
            updateArtistID.Transaction = transaction;
            updateArtistID.CommandType = System.Data.CommandType.Text;
            updateArtistID.Parameters.Add(new SQLiteParameter("@songID", songID));
            updateArtistID.Parameters.Add("@recordingID", DbType.Int64);
            updateArtistID.CommandText =
                "UPDATE recording " +
                    "SET song_id=@songID " +
                    "WHERE id=@recordingID;";
            foreach (long id in recordingIDs)
            {
                updateArtistID.Parameters["@recordingID"].Value = id;
                updateArtistID.ExecuteNonQuery();
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
                "UPDATE recording " +
                    "SET song_id=@newSongID " +
                    "WHERE song_id=@oldSongID;";
            foreach (long id in oldSongIDs)
            {
                remapSongID.Parameters["@oldSongID"].Value = id;
                remapSongID.ExecuteNonQuery();
            }
        }

        public void _RemapArtistID(
            SQLiteTransaction transaction,
            long newArtistID,
            ICollection<long> oldArtistIDs)
        {
            SQLiteCommand remapArtistID = dbConnection.CreateCommand();
            remapArtistID.Transaction = transaction;
            remapArtistID.CommandType = System.Data.CommandType.Text;
            remapArtistID.Parameters.Add(new SQLiteParameter("@newArtistID", newArtistID));
            remapArtistID.Parameters.Add("@oldArtistID", DbType.Int64);
            remapArtistID.CommandText =
                "UPDATE recording " +
                    "SET artist_id=@newArtistID " +
                    "WHERE artist_id=@oldArtistID;";
            foreach (long id in oldArtistIDs)
            {
                remapArtistID.Parameters["@oldArtistID"].Value = id;
                remapArtistID.ExecuteNonQuery();
            }
        }

        public void _ReassignArtistIDs(
            SQLiteTransaction transaction,
            long artistID,
            IEnumerable<long> recordingIDs)
        {
            SQLiteCommand remapArtistID = dbConnection.CreateCommand();
            remapArtistID.Transaction = transaction;
            remapArtistID.CommandType = System.Data.CommandType.Text;
            remapArtistID.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            remapArtistID.Parameters.Add("@recordingID", DbType.Int64);
            remapArtistID.CommandText =
                "UPDATE recording " +
                    "SET artist_id=@artistID " +
                    "WHERE id=@recordingID;";
            foreach (long id in recordingIDs)
            {
                remapArtistID.Parameters["@recordingID"].Value = id;
                remapArtistID.ExecuteNonQuery();
            }
        }

        #endregion Update Commands
        #region Insert Commands

        public long _CreateRecording(
            SQLiteTransaction transaction,
            string filename,
            long artistID,
            long songID,
            bool live = false)
        {
            long recordingID = -1;

            SQLiteCommand createRecording = dbConnection.CreateCommand();
            createRecording.Transaction = transaction;
            createRecording.CommandType = System.Data.CommandType.Text;
            createRecording.CommandText =
                "INSERT INTO recording " +
                    "(id, artist_id, song_id, filename, live) VALUES " +
                    "(@recordingID, @artistID, @songID, @filename, @live);";
            createRecording.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));
            createRecording.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            createRecording.Parameters.Add(new SQLiteParameter("@songID", songID));
            createRecording.Parameters.Add(new SQLiteParameter("@filename", filename));
            createRecording.Parameters.Add(new SQLiteParameter("@live", live));

            createRecording.ExecuteNonQuery();

            return recordingID;
        }


        #endregion Insert Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allRecordings = from recording in db.Recordings select recording;
            db.Recordings.RemoveRange(allRecordings);
        }

        public void _DeleteRecordingID(
            SQLiteTransaction transaction,
            ICollection<long> recordingIDs)
        {
            SQLiteCommand deleteRecording_ByRecordingID = dbConnection.CreateCommand();
            deleteRecording_ByRecordingID.Transaction = transaction;
            deleteRecording_ByRecordingID.CommandType = System.Data.CommandType.Text;
            deleteRecording_ByRecordingID.Parameters.Add("@recordingID", DbType.Int64);
            deleteRecording_ByRecordingID.CommandText =
                "DELETE FROM recording " +
                "WHERE recording.id=@recordingID;";
            foreach (long id in recordingIDs)
            {
                deleteRecording_ByRecordingID.Parameters["@recordingID"].Value = id;
                deleteRecording_ByRecordingID.ExecuteNonQuery();
            }
        }

        #endregion Delete Commands
        #region Helper Methods

        private string _GrabPathChunk(string path, int directories)
        {
            int index = 0;
            for (int i = 0; i < directories; i++)
            {
                index = path.IndexOf(System.IO.Path.DirectorySeparatorChar, index + 1);
                if (index == -1)
                {
                    return "";
                }
            }

            return path.Substring(0, index);
        }

        private int _CountDirectories(string path)
        {
            int index = 0;
            int count = -1;
            while (index > -1)
            {
                index = path.IndexOf(System.IO.Path.DirectorySeparatorChar, index + 1);
                ++count;
            }

            return count;

        }

        private bool _GetFileIsInDirectory(string path, string file)
        {
            file = file.ToLowerInvariant();
            path = path.ToLowerInvariant();

            if (file.StartsWith(path) && file.IndexOf(System.IO.Path.DirectorySeparatorChar, path.Length + 1) == -1)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
