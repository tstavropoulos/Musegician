using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using Musegician.DataStructures;
using Musegician.Deredundafier;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class SongCommands
    {
        ArtistCommands artistCommands = null;
        TrackCommands trackCommands = null;
        RecordingCommands recordingCommands = null;
        PlaylistCommands playlistCommands = null;

        MusegicianData db = null;

        public SongCommands()
        {
        }

        public void Initialize(
            MusegicianData db,
            ArtistCommands artistCommands,
            TrackCommands trackCommands,
            RecordingCommands recordingCommands,
            PlaylistCommands playlistCommands)
        {
            this.db = db;

            this.artistCommands = artistCommands;
            this.trackCommands = trackCommands;
            this.recordingCommands = recordingCommands;
            this.playlistCommands = playlistCommands;
        }

        #region High Level Commands

        /// <summary>
        /// Renaming And/Or Consolidating Songs
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(IEnumerable<long> songIDs, string newTitle)
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
                    _UpdateForeignKeys(
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

        public List<SongDTO> GenerateArtistSongList(long artistID, string artistName)
        {
            List<SongDTO> songList = new List<SongDTO>();

            dbConnection.Open();

            SQLiteCommand readSongs = dbConnection.CreateCommand();
            readSongs.CommandType = System.Data.CommandType.Text;
            readSongs.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            readSongs.CommandText =
                "SELECT id, title " +
                "FROM song " +
                "WHERE id IN ( " +
                    "SELECT song_id " +
                    "FROM recording " +
                    "WHERE artist_id=@artistID ) " +
                "ORDER BY title ASC;";

            using (SQLiteDataReader reader = readSongs.ExecuteReader())
            {
                while (reader.Read())
                {
                    songList.Add(new SongDTO(
                        songID: (long)reader["id"],
                        title: string.Format(
                            "{0} - {1}",
                            artistName,
                            (string)reader["title"])));
                }
            }

            dbConnection.Close();

            return songList;
        }

        public void UpdateWeights(IList<(long songID, double weight)> values)
        {
            dbConnection.Open();

            SQLiteCommand updateWeight = dbConnection.CreateCommand();
            updateWeight.CommandType = System.Data.CommandType.Text;
            updateWeight.CommandText =
                "INSERT OR REPLACE INTO song_weight " +
                "(song_id, weight) VALUES " +
                "(@songID, @weight);";
            updateWeight.Parameters.Add("@songID", DbType.Int64);
            updateWeight.Parameters.Add("@weight", DbType.Double);

            foreach (var value in values)
            {
                updateWeight.Parameters["@songID"].Value = value.songID;
                updateWeight.Parameters["@weight"].Value = value.weight;
                updateWeight.ExecuteNonQuery();
            }

            dbConnection.Close();
        }

        public IList<DeredundafierDTO> GetDeredundancyTargets()
        {
            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();

            dbConnection.Open();

            SQLiteCommand findTargets = dbConnection.CreateCommand();
            findTargets.CommandType = System.Data.CommandType.Text;
            findTargets.CommandText =
                "SELECT title " +
                "FROM song " +
                "GROUP BY title COLLATE NOCASE " +
                "HAVING count(*) > 1;";

            using (SQLiteDataReader reader = findTargets.ExecuteReader())
            {
                while (reader.Read())
                {
                    targets.Add(_GetDeredunancyTarget((string)reader["title"]));
                }
            }

            dbConnection.Close();

            return targets;
        }

        public void Merge(IEnumerable<long> ids)
        {
            List<long> songIDsCopy = new List<long>(ids);

            long songID = songIDsCopy[0];
            songIDsCopy.RemoveAt(0);

            dbConnection.Open();

            using (SQLiteTransaction transaction = dbConnection.BeginTransaction())
            {
                _UpdateForeignKeys(
                    transaction: transaction,
                    newSongID: songID,
                    oldSongIDs: songIDsCopy);

                _DeleteSongID(
                    transaction: transaction,
                    songIDs: songIDsCopy);

                transaction.Commit();
            }

            dbConnection.Close();
        }

        #endregion High Level Commands
        #region Search Commands

        public long _FindSongID_ByTitle_MatchSongArtist(string songTitle, long oldSongID)
        {
            long newSongID = -1;

            SQLiteCommand findSongID_ByTitle_SameArtist = dbConnection.CreateCommand();
            findSongID_ByTitle_SameArtist.CommandType = System.Data.CommandType.Text;
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@newSongTitle", songTitle));
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@oldSongID", oldSongID));
            findSongID_ByTitle_SameArtist.CommandText =
                "SELECT id " +
                "FROM song " +
                "WHERE title=@newSongTitle COLLATE NOCASE AND " +
                    "id IN ( " +
                        "SELECT song_id " +
                        "FROM recording " +
                        "WHERE artist_id IN ( " +
                            "SELECT artist_id " +
                            "FROM recording " +
                            "WHERE song_id=@oldSongID " +
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

        public long _FindArtistID_BySongID(long songID)
        {
            long artistID = -1;

            SQLiteCommand findSongID_ByTitle_SameArtist = dbConnection.CreateCommand();
            findSongID_ByTitle_SameArtist.CommandType = System.Data.CommandType.Text;
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@songID", songID));
            findSongID_ByTitle_SameArtist.CommandText =
                "SELECT artist_id " +
                "FROM song " +
                "WHERE id=@songID);";

            using (SQLiteDataReader reader = findSongID_ByTitle_SameArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    artistID = (long)reader["artist_id"];
                }
            }

            return artistID;
        }

        private DeredundafierDTO _GetDeredunancyTarget(string songTitle)
        {
            DeredundafierDTO target = new DeredundafierDTO()
            {
                Name = songTitle
            };

            SQLiteCommand findOptions = dbConnection.CreateCommand();
            findOptions.CommandType = System.Data.CommandType.Text;
            findOptions.CommandText =
                "SELECT " +
                    "song.id AS id, " +
                    "song.title AS song_title, " +
                    "CASE WHEN COUNT(artist.id) > 1 THEN 'Various Artists' " +
                        "ELSE MAX(artist.name) END artist_name " +
                "FROM song " +
                "LEFT JOIN artist ON artist.id IN ( " +
                    "SELECT artist_id " +
                    "FROM recording " +
                    "WHERE song_id=song.id ) " +
                "WHERE song.title=@songTitle COLLATE NOCASE " +
                "GROUP BY song.id;";
            findOptions.Parameters.Add(new SQLiteParameter("@songTitle", songTitle));

            //Match them all up
            using (SQLiteDataReader innerReader = findOptions.ExecuteReader())
            {
                while (innerReader.Read())
                {
                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = string.Format(
                            "{0} - {1}",
                            (string)innerReader["artist_name"],
                            (string)innerReader["song_title"]),
                        ID = (long)innerReader["id"],
                        IsChecked = false
                    };

                    target.Children.Add(selector);

                    foreach (DeredundafierDTO data in _GetDeredundancyTrackExamples(selector.ID))
                    {
                        selector.Children.Add(data);
                    }
                }
            }

            return target;
        }

        private IList<DeredundafierDTO> _GetDeredundancyTrackExamples(long songID)
        {
            List<DeredundafierDTO> examples = new List<DeredundafierDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "recording.id AS recording_id, " +
                    "artist.name || ' - ' || album.title || ' - ' || track.title AS title " +
                "FROM recording " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "WHERE recording.song_id=@songID;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    examples.Add(new DeredundafierDTO()
                    {
                        Name = (string)reader["title"],
                        ID = (long)reader["recording_id"]
                    });
                }
            }

            return examples;
        }

        #endregion Search Commands
        //#region Lookup Commands

        //public void _PopulateLookup(
        //    Dictionary<(long, string), long> artistID_SongTitleDict)
        //{
        //    SQLiteCommand loadSongs = dbConnection.CreateCommand();
        //    loadSongs.CommandType = System.Data.CommandType.Text;
        //    loadSongs.CommandText =
        //        "SELECT " +
        //            "song.id AS song_id, " +
        //            "song.title AS title, " +
        //            "recording.artist_id AS artist_id " +
        //        "FROM song " +
        //        "LEFT JOIN recording ON song.id=recording.song_id;";

        //    using (SQLiteDataReader reader = loadSongs.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            long songID = (long)reader["song_id"];
        //            string songTitle = (string)reader["title"];
        //            long artistID = (long)reader["artist_id"];

        //            var key = (artistID, songTitle.ToLowerInvariant());

        //            if (!artistID_SongTitleDict.ContainsKey(key))
        //            {
        //                artistID_SongTitleDict.Add(key, songID);
        //            }
        //        }
        //    }
        //}

        //#endregion Lookup Commands
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
                    "SET title=@songTitle " +
                    "WHERE id=@songID;";

            updateSongTitle_BySongID.ExecuteNonQuery();
        }

        public void _UpdateForeignKeys(
            SQLiteTransaction transaction,
            long newSongID,
            ICollection<long> oldSongIDs)
        {
            recordingCommands._RemapSongID(
                transaction: transaction,
                newSongID: newSongID,
                oldSongIDs: oldSongIDs);

            //Update Playlist keys
            playlistCommands._RemapSongID(
                transaction: transaction,
                newSongID: newSongID,
                oldSongIDs: oldSongIDs);
        }

        #endregion Update Commands
        #region Insert Commands

        public long _CreateSong(
            SQLiteTransaction transaction,
            string songTitle)
        {
            long songID = -1;

            SQLiteCommand writeSong = dbConnection.CreateCommand();
            writeSong.Transaction = transaction;
            writeSong.CommandType = System.Data.CommandType.Text;
            writeSong.CommandText =
                "INSERT INTO song " +
                    "(id, title) VALUES " +
                    "(@songID, @songTitle);";
            writeSong.Parameters.Add(new SQLiteParameter("@songID", songID));
            writeSong.Parameters.Add(new SQLiteParameter("@songTitle", songTitle));
            writeSong.ExecuteNonQuery();

            return songID;
        }


        #endregion Insert Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allSongs = from song in db.Songs select song;
            db.Songs.RemoveRange(allSongs);
        }

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
                "WHERE id=@songID;";

            SQLiteCommand deleteSongWeight_BySongID = dbConnection.CreateCommand();
            deleteSongWeight_BySongID.Transaction = transaction;
            deleteSongWeight_BySongID.CommandType = System.Data.CommandType.Text;
            deleteSongWeight_BySongID.Parameters.Add("@songID", DbType.Int64);
            deleteSongWeight_BySongID.CommandText =
                "DELETE FROM song_weight " +
                "WHERE song_id=@songID;";

            foreach (long id in songIDs)
            {
                deleteSong_BySongID.Parameters["@songID"].Value = id;
                deleteSong_BySongID.ExecuteNonQuery();

                deleteSongWeight_BySongID.Parameters["@songID"].Value = id;
                deleteSongWeight_BySongID.ExecuteNonQuery();
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
                "WHERE id IN ( " +
                    "SELECT song.id " +
                    "FROM song " +
                    "LEFT JOIN recording ON song.id=recording.song_id " +
                    "WHERE recording.song_id IS NULL );";
            deleteLeafs.ExecuteNonQuery();
        }

        #endregion Delete Commands
    }
}
