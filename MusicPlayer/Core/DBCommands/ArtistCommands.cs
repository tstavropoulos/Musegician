using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using Musegician.Deredundafier;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class ArtistCommands
    {
        AlbumCommands albumCommands = null;
        SongCommands songCommands = null;
        RecordingCommands recordingCommands = null;

        MusegicianData db = null;

        public ArtistCommands()
        {
        }

        public void Initialize(
            MusegicianData db,
            AlbumCommands albumCommands,
            SongCommands songCommands,
            RecordingCommands recordingCommands)
        {
            this.db = db;

            this.albumCommands = albumCommands;
            this.songCommands = songCommands;
            this.recordingCommands = recordingCommands;
        }

        #region High Level Commands

        public void UpdateArtistName(IEnumerable<long> artistIDs, string newArtistName)
        {
            List<long> artistIDsCopy = new List<long>(artistIDs);
            
            //First, find out if the new artist exists
            long artistID = _FindArtist_ByName(newArtistName);

            if (artistID == -1)
            {
                //Update an Artist's name, because it doesn't exist

                //Pop off the front
                artistID = artistIDsCopy[0];
                artistIDsCopy.RemoveAt(0);

                //Update the artist formerly in the front
                _UpdateArtistName_ByArtistID(
                    transaction: updateArtist,
                    artistID: artistID,
                    artistName: newArtistName);
            }

            if (artistIDsCopy.Count > 0)
            {
                //For the remaining artists, Remap foreign keys
                _RemapForeignKeys(
                    transaction: updateArtist,
                    newArtistID: artistID,
                    oldArtistIDs: artistIDsCopy);

                //Now, delete any old artists with no remaining recordings
                _DeleteAllLeafs(
                    transaction: updateArtist);
            }

            updateArtist.Commit();
        }

        public IEnumerable<Artist> GeneratArtistList()
        {
            return (from artist in db.Artists
                    orderby (artist.Name.StartsWith("The ") ? artist.Name.Substring(4) : artist.Name)
                    select artist);
        }

        public IList<DeredundafierDTO> GetDeredundancyTargets()
        {
            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();

            dbConnection.Open();

            SQLiteCommand findTargets = dbConnection.CreateCommand();
            findTargets.CommandType = System.Data.CommandType.Text;
            findTargets.CommandText =
                "SELECT name " +
                "FROM artist " +
                "GROUP BY name COLLATE NOCASE " +
                "HAVING count(*) > 1;";

            using (SQLiteDataReader reader = findTargets.ExecuteReader())
            {
                while (reader.Read())
                {
                    targets.Add(_GetDeredunancyTarget((string)reader["name"]));
                }
            }

            dbConnection.Close();

            return targets;
        }

        public void Merge(IEnumerable<long> ids)
        {
            List<long> artistIDsCopy = new List<long>(ids);

            long artistID = artistIDsCopy[0];
            artistIDsCopy.RemoveAt(0);

            dbConnection.Open();

            using (SQLiteTransaction transaction = dbConnection.BeginTransaction())
            {
                _RemapForeignKeys(
                    transaction: transaction,
                    newArtistID: artistID,
                    oldArtistIDs: artistIDsCopy);

                _DeleteArtistID(
                    transaction: transaction,
                    artistIDs: artistIDsCopy);

                transaction.Commit();
            }

            dbConnection.Close();
        }

        #endregion High Level Commands
        #region Search Commands

        public long _FindArtist_ByName(string artistName)
        {
            long artistID = -1;

            SQLiteCommand findArtist = dbConnection.CreateCommand();
            findArtist.CommandType = System.Data.CommandType.Text;
            findArtist.Parameters.Add(new SQLiteParameter("@artistName", artistName));
            findArtist.CommandText =
                "SELECT id " +
                "FROM artist " +
                "WHERE name=@artistName COLLATE NOCASE " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = findArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    artistID = (long)reader["id"];
                }
            }

            return artistID;
        }

        private DeredundafierDTO _GetDeredunancyTarget(string name)
        {
            DeredundafierDTO target = new DeredundafierDTO()
            {
                Name = name
            };

            SQLiteCommand findOptions = dbConnection.CreateCommand();
            findOptions.CommandType = System.Data.CommandType.Text;
            findOptions.CommandText =
                "SELECT id, name " +
                "FROM artist " +
                "WHERE name=@artistName COLLATE NOCASE;";
            findOptions.Parameters.Add(new SQLiteParameter("@artistName", name));

            //Match them all up
            using (SQLiteDataReader innerReader = findOptions.ExecuteReader())
            {
                while (innerReader.Read())
                {
                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = (string)innerReader["name"],
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

        private IList<DeredundafierDTO> _GetDeredundancyTrackExamples(long artistID)
        {
            List<DeredundafierDTO> examples = new List<DeredundafierDTO>();

            SQLiteCommand readRecordings = dbConnection.CreateCommand();
            readRecordings.CommandType = System.Data.CommandType.Text;
            readRecordings.CommandText =
                "SELECT " +
                    "recording.id AS recording_id, " +
                    "artist.name || ' - ' || album.title || ' - ' || track.title AS title " +
                "FROM artist " +
                "LEFT JOIN recording ON recording.artist_id=artist.id " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "WHERE artist.id=@artistID;";
            readRecordings.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readRecordings.ExecuteReader())
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
        //Dictionary<string, long> artistNameDict)
        //{
        //    SQLiteCommand loadArtists = dbConnection.CreateCommand();
        //    loadArtists.CommandType = System.Data.CommandType.Text;
        //    loadArtists.CommandText =
        //        "SELECT id, name " +
        //        "FROM artist;";

        //    using (SQLiteDataReader reader = loadArtists.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            string artistName = (string)reader["name"];
        //            long artistID = (long)reader["id"];

        //            if (!artistNameDict.ContainsKey(artistName.ToLowerInvariant()))
        //            {
        //                artistNameDict.Add(artistName.ToLowerInvariant(), artistID);
        //            }
        //        }
        //    }
        //}

        //#endregion Lookup Commands
        #region Update Commands

        public void _UpdateArtistName_ByArtistID(
            MusegicianData db,
            long artistID,
            string artistName)
        {

            SQLiteCommand updateArtistName_ByArtistID = dbConnection.CreateCommand();
            updateArtistName_ByArtistID.Transaction = transaction;
            updateArtistName_ByArtistID.CommandType = System.Data.CommandType.Text;
            updateArtistName_ByArtistID.Parameters.Add(new SQLiteParameter("@artistName", artistName));
            updateArtistName_ByArtistID.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            updateArtistName_ByArtistID.CommandText =
                "UPDATE artist " +
                    "SET name=@artistName " +
                    "WHERE id=@artistID;";

            updateArtistName_ByArtistID.ExecuteNonQuery();
        }

        public void _RemapForeignKeys(
            SQLiteTransaction transaction,
            long newArtistID,
            ICollection<long> oldArtistIDs)
        {
            recordingCommands._RemapArtistID(
                transaction: transaction,
                newArtistID: newArtistID,
                oldArtistIDs: oldArtistIDs);
        }

        #endregion Update Commands
        #region Insert Commands

        public long _CreateArtist(
            SQLiteTransaction transaction,
            string artistName)
        {
            long artistID = -1;

            SQLiteCommand createArtist = dbConnection.CreateCommand();
            createArtist.Transaction = transaction;
            createArtist.CommandType = System.Data.CommandType.Text;
            createArtist.CommandText =
                "INSERT INTO artist " +
                    "(id, name) VALUES " +
                    "(@artistID, @artistName);";
            createArtist.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            createArtist.Parameters.Add(new SQLiteParameter("@artistName", artistName));
            createArtist.ExecuteNonQuery();

            return artistID;
        }


        #endregion Insert Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allArtists = from artist in db.Artists select artist;
            db.Artists.RemoveRange(allArtists);
        }

        public void _DeleteArtistID(
            SQLiteTransaction transaction,
            ICollection<long> artistIDs)
        {
            SQLiteCommand deleteArtist_ByArtistID = dbConnection.CreateCommand();
            deleteArtist_ByArtistID.Transaction = transaction;
            deleteArtist_ByArtistID.CommandType = System.Data.CommandType.Text;
            deleteArtist_ByArtistID.Parameters.Add("@artistID", DbType.Int64);
            deleteArtist_ByArtistID.CommandText =
                "DELETE FROM artist " +
                "WHERE id=@artistID;";

            SQLiteCommand deleteArtistWeight_ByArtistID = dbConnection.CreateCommand();
            deleteArtistWeight_ByArtistID.Transaction = transaction;
            deleteArtistWeight_ByArtistID.CommandType = System.Data.CommandType.Text;
            deleteArtistWeight_ByArtistID.Parameters.Add("@artistID", DbType.Int64);
            deleteArtistWeight_ByArtistID.CommandText =
                "DELETE FROM artist_weight " +
                "WHERE artist_id=@artistID;";

            foreach (long id in artistIDs)
            {
                deleteArtist_ByArtistID.Parameters["@artistID"].Value = id;
                deleteArtist_ByArtistID.ExecuteNonQuery();

                deleteArtistWeight_ByArtistID.Parameters["@artistID"].Value = id;
                deleteArtistWeight_ByArtistID.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Dekete all artists with no recordings pointing at them
        /// </summary>
        /// <param name="transaction"></param>
        public void _DeleteAllLeafs(
            SQLiteTransaction transaction)
        {
            SQLiteCommand deleteLeafs = dbConnection.CreateCommand();
            deleteLeafs.Transaction = transaction;
            deleteLeafs.CommandType = System.Data.CommandType.Text;
            deleteLeafs.CommandText =
                "DELETE FROM artist " +
                "WHERE id IN ( " +
                    "SELECT artist.id " +
                    "FROM artist " +
                    "LEFT JOIN recording ON artist.id=recording.artist_id " +
                    "WHERE recording.artist_id IS NULL );";
            deleteLeafs.ExecuteNonQuery();
        }

        #endregion Delete Commands
    }
}
