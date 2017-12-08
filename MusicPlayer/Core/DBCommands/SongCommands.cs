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
    public class SongCommands
    {
        ArtistCommands artistCommands = null;
        TrackCommands trackCommands = null;

        SQLiteConnection dbConnection;

        private long _lastIDAssigned = 0;
        public long NextID
        {
            get { return ++_lastIDAssigned; }
        }

        public SongCommands()
        {
        }

        public void Initialize(
            SQLiteConnection dbConnection,
            ArtistCommands artistCommands,
            TrackCommands trackCommands)
        {
            this.dbConnection = dbConnection;

            this.artistCommands = artistCommands;
            this.trackCommands = trackCommands;

            _lastIDAssigned = 0;
        }

        /// <summary>
        /// Renaming And/Or Consolidating Songs
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(ICollection<long> songIDs, string newTitle)
        {
            List<long> songIDsCopy = new List<long>(songIDs);

            //Renaming (or Consolidating) a Song
            dbConnection.Open();

            long songID = _FindSongID_ByTitle_MatchSongArtist(
                songTitle: newTitle,
                oldSongID: songIDsCopy[0]);

            using (SQLiteTransaction updateSongTitles = dbConnection.BeginTransaction())
            {
                if (songID == -1)
                {
                    //New Song did not exist
                    //  we just need to update the first title, collapse the rest

                    //Pop off the front
                    songID = songIDsCopy[0];
                    songIDsCopy.RemoveAt(0);

                    //Update the song formerly in the front
                    _UpdateSongTitle_BySongID(
                        transaction: updateSongTitles,
                        songID: songID,
                        songTitle: newTitle);
                }

                if (songIDsCopy.Count > 0)
                {
                    //New song did exist, or we passed in more than one song

                    //Update track table to point at new song
                    _UpdateSongID_ForeignKeys(
                        transaction: updateSongTitles,
                        newSongID: songID,
                        oldSongIDs: songIDsCopy);

                    //Delete old IDs
                    _DeleteSongID(
                        transaction: updateSongTitles,
                        songIDs: songIDsCopy);
                }

                updateSongTitles.Commit();
            }

            dbConnection.Close();
        }


        /// <summary>
        /// Assinging Songs to a different artist
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateArtistName(ICollection<long> songIDs, string newArtistName)
        {
            List<long> songIDsCopy = new List<long>(songIDs);

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

                //Update the album and song ArtistIDs
                _ReassignArtistID(
                    transaction: updateArtistName,
                    artistID: artistID,
                    songIDs: songIDs);

                //Now, delete any old artists with no remaining albums
                _DeleteAllLeafs(
                    transaction: updateArtistName);

                updateArtistName.Commit();
            }

            dbConnection.Close();
        }

        #region Search Commands

        public long _FindSongID_ByTitle_MatchSongArtist(string songTitle, long oldSongID)
        {
            long newSongID = -1;

            SQLiteCommand findSongID_ByTitle_SameArtist = dbConnection.CreateCommand();
            findSongID_ByTitle_SameArtist.CommandType = System.Data.CommandType.Text;
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@newSongTitle", songTitle));
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@oldSongID", oldSongID));
            findSongID_ByTitle_SameArtist.CommandText =
                "SELECT song_id " +
                "FROM song " +
                "WHERE song_title=@newSongTitle COLLATE NOCASE AND " +
                    "artist_id=( " +
                        "SELECT artist_id " +
                        "FROM song " +
                        "WHERE song_id=@oldSongID " +
                    ");";

            //First, find out if the new song already exists (ie, consolidation)

            using (SQLiteDataReader reader = findSongID_ByTitle_SameArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    newSongID = (long)reader["song_id"];
                }
            }

            return newSongID;
        }

        public long _FindArtistID_BySongID(long songID)
        {
            long artistID = -1;

            SQLiteCommand findSongID_ByTitle_SameArtist = dbConnection.CreateCommand();
            findSongID_ByTitle_SameArtist.CommandType = System.Data.CommandType.Text;
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@songID", songID));
            findSongID_ByTitle_SameArtist.CommandText =
                "SELECT artist_id " +
                "FROM song " +
                "WHERE song_id=@songID);";

            using (SQLiteDataReader reader = findSongID_ByTitle_SameArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    artistID = (long)reader["artist_id"];
                }
            }

            return artistID;
        }

        public string _GetPlaylistName(long songID)
        {
            string songName = "";
            string artistName = "";
            long artistID = -1;

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.song_title AS song_title, " +
                    "artist.artist_name AS artist_name, " +
                    "recording.artist_id AS artist_id " +
                "FROM recording " +
                "LEFT JOIN song ON recording.song_id=song.song_id " +
                "LEFT JOIN artist ON recording.artist_id=artist.artist_id " +
                "WHERE recording.song_id=@songID;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    songName = (string)reader["song_title"];
                    artistName = (string)reader["artist_name"];
                    artistID = (long)reader["artist_id"];

                    while (reader.Read())
                    {
                        //Look for different artists among the return values
                        if (artistID != (long)reader["artist_id"])
                        {
                            artistName = "Various";
                            break;
                        }
                    }
                }
            }

            return string.Format("{0} - {1}", artistName, songName);
        }

        #endregion  //Search Commands

        #region Lookup Commands

        public void _PopulateLookup(
            Dictionary<(long, string), long> artistID_SongTitleDict)
        {
            SQLiteCommand loadSongs = dbConnection.CreateCommand();
            loadSongs.CommandType = System.Data.CommandType.Text;
            loadSongs.CommandText =
                "SELECT " +
                    "song.song_id AS song_id, " +
                    "song.song_title AS song_title, " +
                    "recording.artist_id AS artist_id " +
                "FROM song " +
                "LEFT JOIN recording ON song.song_id=recording.song_id;";

            using (SQLiteDataReader reader = loadSongs.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];
                    string songTitle = (string)reader["song_title"];
                    long artistID = (long)reader["artist_id"];

                    var key = (artistID, songTitle.ToLowerInvariant());

                    if (!artistID_SongTitleDict.ContainsKey(key))
                    {
                        artistID_SongTitleDict.Add(key, songID);
                    }

                    if (songID > _lastIDAssigned)
                    {
                        _lastIDAssigned = songID;
                    }
                }
            }
        }

        #endregion  //Lookup Commands

        #region Update Commands

        public void _UpdateSongTitle_BySongID(
            SQLiteTransaction transaction,
            long songID,
            string songTitle)
        {
            SQLiteCommand updateSongTitle_BySongID = dbConnection.CreateCommand();
            updateSongTitle_BySongID.Transaction = transaction;
            updateSongTitle_BySongID.CommandType = System.Data.CommandType.Text;
            updateSongTitle_BySongID.Parameters.Add(new SQLiteParameter("@songTitle", songTitle));
            updateSongTitle_BySongID.Parameters.Add(new SQLiteParameter("@songID", songID));
            updateSongTitle_BySongID.CommandText =
                "UPDATE song " +
                    "SET song.song_title=@songTitle " +
                    "WHERE song.song_id=@songID;";

            updateSongTitle_BySongID.ExecuteNonQuery();
        }

        public void _UpdateSongID_ForeignKeys(
            SQLiteTransaction transaction,
            long newSongID,
            ICollection<long> oldSongIDs)
        {
            trackCommands._RemapSongID(
                transaction: transaction,
                newSongID: newSongID,
                oldSongIDs: oldSongIDs);
        }

        public void _UpdateArtistID_ByAlbumID(
            SQLiteTransaction transaction,
            long artistID,
            ICollection<long> albumIDs)
        {
            throw new Exception("Now nonsense");

            SQLiteCommand updateArtistID_ByAlbumID = dbConnection.CreateCommand();
            updateArtistID_ByAlbumID.Transaction = transaction;
            updateArtistID_ByAlbumID.CommandType = System.Data.CommandType.Text;
            updateArtistID_ByAlbumID.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            updateArtistID_ByAlbumID.Parameters.Add("@albumID", DbType.Int64);
            updateArtistID_ByAlbumID.CommandText =
                "UPDATE song " +
                "SET song.artist_id=@artistID " +
                "WHERE song_id IN ( " +
                    "SELECT track.song_id " +
                    "FROM track " +
                    "WHERE track.album_id=@albumID );";
            foreach (long id in albumIDs)
            {
                updateArtistID_ByAlbumID.Parameters["@albumID"].Value = id;
                updateArtistID_ByAlbumID.ExecuteNonQuery();
            }
        }

        public void _ReassignArtistID(
            SQLiteTransaction transaction,
            long artistID,
            ICollection<long> songIDs)
        {
            throw new Exception("Now nonsense");

            SQLiteCommand remapArtistID = dbConnection.CreateCommand();
            remapArtistID.Transaction = transaction;
            remapArtistID.CommandType = System.Data.CommandType.Text;
            remapArtistID.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            remapArtistID.Parameters.Add("@songIDs", DbType.Int64);
            remapArtistID.CommandText =
                "UPDATE song " +
                    "SET song.artist_id=@artistID " +
                    "WHERE song.song_id=@songIDs;";
            foreach (long id in songIDs)
            {
                remapArtistID.Parameters["@songIDs"].Value = id;
                remapArtistID.ExecuteNonQuery();
            }
        }

        public void _RemapArtistID(
            SQLiteTransaction transaction,
            long newArtistID,
            ICollection<long> oldArtistIDs)
        {
            throw new Exception("Now nonsense");

            SQLiteCommand remapArtistID = dbConnection.CreateCommand();
            remapArtistID.Transaction = transaction;
            remapArtistID.CommandType = System.Data.CommandType.Text;
            remapArtistID.Parameters.Add(new SQLiteParameter("@newArtistID", newArtistID));
            remapArtistID.Parameters.Add("@oldArtistID", DbType.Int64);
            remapArtistID.CommandText =
                "UPDATE song " +
                    "SET song.artist_id=@newArtistID " +
                    "WHERE song.artist_id=@oldArtistID;";
            foreach (long id in oldArtistIDs)
            {
                remapArtistID.Parameters["@oldArtistID"].Value = id;
                remapArtistID.ExecuteNonQuery();
            }
        }

        #endregion // Update Commands

        #region Create Commands

        public void _CreateSongTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createSongTable = dbConnection.CreateCommand();
            createSongTable.Transaction = transaction;
            createSongTable.CommandType = System.Data.CommandType.Text;
            createSongTable.CommandText =
                "CREATE TABLE IF NOT EXISTS song (" +
                    "song_id INTEGER PRIMARY KEY, " +
                    "song_title TEXT);";
            createSongTable.ExecuteNonQuery();

            SQLiteCommand createSongWeightTable = dbConnection.CreateCommand();
            createSongWeightTable.Transaction = transaction;
            createSongWeightTable.CommandType = System.Data.CommandType.Text;
            createSongWeightTable.CommandText =
                "CREATE TABLE IF NOT EXISTS song_weight (" +
                    "song_id INTEGER PRIMARY KEY, " +
                    "weight REAL);";
            createSongWeightTable.ExecuteNonQuery();

            SQLiteCommand createSongTitleIndex = dbConnection.CreateCommand();
            createSongTitleIndex.Transaction = transaction;
            createSongTitleIndex.CommandType = System.Data.CommandType.Text;
            createSongTitleIndex.CommandText =
                    "CREATE INDEX IF NOT EXISTS idx_song_songTitle ON song (song_title COLLATE NOCASE);";
            createSongTitleIndex.ExecuteNonQuery();
        }

        #endregion //Create Commands

        #region Insert Commands

        public long _CreateSong(
            SQLiteTransaction transaction,
            string songTitle)
        {
            long songID = NextID;

            SQLiteCommand writeSong = dbConnection.CreateCommand();
            writeSong.Transaction = transaction;
            writeSong.CommandType = System.Data.CommandType.Text;
            writeSong.CommandText =
                "INSERT INTO song " +
                    "(song_id, song_title) VALUES " +
                    "(@songID, @songTitle);";
            writeSong.Parameters.Add(new SQLiteParameter("@songID", songID));
            writeSong.Parameters.Add(new SQLiteParameter("@songTitle", songTitle));
            writeSong.ExecuteNonQuery();

            return songID;
        }

        public void _BatchCreateSong(
            SQLiteTransaction transaction,
            ICollection<SongData> newSongRecords)
        {
            SQLiteCommand writeSong = dbConnection.CreateCommand();
            writeSong.Transaction = transaction;
            writeSong.CommandType = System.Data.CommandType.Text;
            writeSong.CommandText =
                "INSERT INTO song " +
                    "(song_id, song_title) VALUES " +
                    "(@songID, @songTitle);";
            writeSong.Parameters.Add(new SQLiteParameter("@songID", -1));
            writeSong.Parameters.Add(new SQLiteParameter("@songTitle", ""));
            foreach (SongData song in newSongRecords)
            {
                writeSong.Parameters["@songID"].Value = song.songID;
                writeSong.Parameters["@songTitle"].Value = song.songTitle;
                writeSong.ExecuteNonQuery();
            }
        }


        #endregion //Insert Commands

        #region Delete Commands

        public void _DeleteSongID(
            SQLiteTransaction transaction,
            ICollection<long> songIDs)
        {
            SQLiteCommand deleteSong_BySongID = dbConnection.CreateCommand();
            deleteSong_BySongID.Transaction = transaction;
            deleteSong_BySongID.CommandType = System.Data.CommandType.Text;
            deleteSong_BySongID.Parameters.Add("@songID", DbType.Int64);
            deleteSong_BySongID.CommandText =
                "DELETE FROM song " +
                "WHERE song.song_id=@songID;";
            foreach (long id in songIDs)
            {
                deleteSong_BySongID.Parameters["@songID"].Value = id;
                deleteSong_BySongID.ExecuteNonQuery();
            }
        }

        public void _DeleteAllLeafs(
            SQLiteTransaction transaction)
        {
            SQLiteCommand deleteLeafs = dbConnection.CreateCommand();
            deleteLeafs.Transaction = transaction;
            deleteLeafs.CommandType = System.Data.CommandType.Text;
            deleteLeafs.CommandText =
                "DELETE FROM song " +
                "WHERE song_id IN ( " +
                    "SELECT song.song_id " +
                    "FROM song " +
                    "LEFT JOIN recording ON song.song_id=recording.song_id " +
                    "WHERE recording.song_id IS NULL );";
            deleteLeafs.ExecuteNonQuery();
        }

        #endregion // Delete Commands
    }
}
