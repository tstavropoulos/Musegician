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
    public class AlbumCommands
    {
        ArtistCommands artistCommands = null;
        SongCommands songCommands = null;

        SQLiteConnection dbConnection;

        private long _lastIDAssigned = 0;
        public long NextID
        {
            get { return ++_lastIDAssigned; }
        }

        public AlbumCommands()
        {
        }

        public void Initialize(
            SQLiteConnection dbConnection,
            ArtistCommands artistCommands,
            SongCommands songCommands)
        {
            this.dbConnection = dbConnection;

            this.artistCommands = artistCommands;
            this.songCommands = songCommands;

            _lastIDAssigned = 0;
        }

        #region High Level Commands

        public void UpdateArtistName(ICollection<long> albumIDs, string newArtistName)
        {
            //Assigning Albums to a different artist
            dbConnection.Open();

            //First, find out if the new artist exists
            long artistID = artistCommands._FindArtist_ByName(newArtistName);

            using (SQLiteTransaction updateAlbums = dbConnection.BeginTransaction())
            {
                if (artistID == -1)
                {
                    //Create a new Artist, because it doesn't exist
                    artistID = artistCommands._CreateArtist(
                        transaction: updateAlbums,
                        artistName: newArtistName);
                }

                //Update the album and song ArtistIDs
                _UpdateArtistID_ByAlbumID(
                    transaction: updateAlbums,
                    artistID: artistID,
                    albumIDs: albumIDs);

                //Now, delete any old artists with no remaining albums
                _DeleteAllLeafs(
                    transaction: updateAlbums);

                updateAlbums.Commit();
            }

            dbConnection.Close();
        }

        #endregion // High Level Commands

        #region Search Commands

        public long _FindArtistID_ByAlbumID(long albumID)
        {
            long artistID = -1;

            SQLiteCommand findSongID_ByTitle_SameArtist = dbConnection.CreateCommand();
            findSongID_ByTitle_SameArtist.CommandType = System.Data.CommandType.Text;
            findSongID_ByTitle_SameArtist.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            findSongID_ByTitle_SameArtist.CommandText =
                "SELECT artist_id " +
                "FROM song " +
                "WHERE song_id=@albumID);";

            using (SQLiteDataReader reader = findSongID_ByTitle_SameArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    artistID = (long)reader["artist_id"];
                }
            }

            return artistID;
        }

        public byte[] _GetArt(long albumID)
        {
            byte[] image = null;

            SQLiteCommand getAlbumArt = dbConnection.CreateCommand();
            getAlbumArt.CommandType = System.Data.CommandType.Text;
            getAlbumArt.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            getAlbumArt.CommandText =
                "SELECT image " +
                "FROM art " +
                "WHERE album_id=@albumID;";

            using (SQLiteDataReader reader = getAlbumArt.ExecuteReader())
            {
                if (reader.Read())
                {
                    image = (byte[])reader["image"];
                }
            }

            return image;
        }

        #endregion  //Search Commands


        #region Lookup Commands

        public void _PopulateLookup(
            Dictionary<(long, string), long> artistID_AlbumTitleDict,
            HashSet<long> albumArt)
        {
            SQLiteCommand loadAlbums = dbConnection.CreateCommand();
            loadAlbums.CommandType = System.Data.CommandType.Text;
            loadAlbums.CommandText =
                "SELECT " +
                    "album.album_id AS album_id, " +
                    "album.album_title AS album_title, " +
                    "artist.artist_id AS artist_id " +
                "FROM album " +
                "LEFT JOIN artist ON artist.artist_id IN ( " +
                    "SELECT recording.artist_id " +
                    "FROM track " +
                    "LEFT JOIN recording ON track.recording_id= recording.recording_id " +
                    "WHERE track.album_id= album.album_id); ";

            using (SQLiteDataReader reader = loadAlbums.ExecuteReader())
            {
                while (reader.Read())
                {
                    long albumID = (long)reader["album_id"];
                    long artistID = (long)reader["artist_id"];
                    string albumTitle = (string)reader["album_title"];

                    var key = (artistID, albumTitle.ToLowerInvariant());

                    if (!artistID_AlbumTitleDict.ContainsKey(key))
                    {
                        artistID_AlbumTitleDict.Add(key, albumID);
                    }

                    if (albumID > _lastIDAssigned)
                    {
                        _lastIDAssigned = albumID;
                    }
                }
            }

            SQLiteCommand loadAlbumArt = dbConnection.CreateCommand();
            loadAlbumArt.CommandType = System.Data.CommandType.Text;
            loadAlbumArt.CommandText =
                "SELECT album_id " +
                "FROM art; ";

            using (SQLiteDataReader reader = loadAlbumArt.ExecuteReader())
            {
                while (reader.Read())
                {
                    long albumID = (long)reader["album_id"];

                    if (!albumArt.Contains(albumID))
                    {
                        albumArt.Add(albumID);
                    }
                }
            }
        }

        #endregion  //Lookup Commands

        #region Update Commands

        public void _UpdateAlbumTitle_ByAlbumID(
            SQLiteTransaction updateTransaction,
            long albumID,
            string albumTitle)
        {
            SQLiteCommand updateAlbumTitle_ByAlbumID = dbConnection.CreateCommand();
            updateAlbumTitle_ByAlbumID.Transaction = updateTransaction;
            updateAlbumTitle_ByAlbumID.CommandType = System.Data.CommandType.Text;
            updateAlbumTitle_ByAlbumID.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));
            updateAlbumTitle_ByAlbumID.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            updateAlbumTitle_ByAlbumID.CommandText =
                "UPDATE album " +
                    "SET album.album_title=@albumTitle " +
                    "WHERE album.album_id=@albumID;";

            updateAlbumTitle_ByAlbumID.ExecuteNonQuery();
        }

        public void _UpdateArtistID_ByAlbumID(
            SQLiteTransaction transaction,
            long artistID,
            ICollection<long> albumIDs)
        {
            //Update the artistID of listed albums
            _ReassignArtistID(
                transaction: transaction,
                artistID: artistID,
                albumIDs: albumIDs);

            //Update the Songs that were affected
            songCommands._UpdateArtistID_ByAlbumID(
                transaction: transaction,
                artistID: artistID,
                albumIDs: albumIDs);
        }

        public void _ReassignArtistID(
            SQLiteTransaction transaction,
            long artistID,
            ICollection<long> albumIDs)
        {
            SQLiteCommand reassignArtistID = dbConnection.CreateCommand();
            reassignArtistID.Transaction = transaction;
            reassignArtistID.CommandType = System.Data.CommandType.Text;
            reassignArtistID.Parameters.Add(new SQLiteParameter("@newArtistID", artistID));
            reassignArtistID.Parameters.Add("@albumID", DbType.Int64);
            reassignArtistID.CommandText =
                "UPDATE album " +
                    "SET album.artist_id=@newArtistID " +
                    "WHERE album.album_id=@albumID;";
            foreach (long id in albumIDs)
            {
                reassignArtistID.Parameters["@albumID"].Value = id;
                reassignArtistID.ExecuteNonQuery();
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
                "UPDATE album " +
                    "SET album.artist_id=@newArtistID " +
                    "WHERE album.artist_id=@oldArtistID;";
            foreach (long id in oldArtistIDs)
            {
                remapArtistID.Parameters["@oldArtistID"].Value = id;
                remapArtistID.ExecuteNonQuery();
            }
        }

        #endregion // Update Commands

        #region Create Commands

        public void _CreateAlbumTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createAlbumTable = dbConnection.CreateCommand();
            createAlbumTable.Transaction = transaction;
            createAlbumTable.CommandType = System.Data.CommandType.Text;
            createAlbumTable.CommandText =
                "CREATE TABLE IF NOT EXISTS album (" +
                    "album_id INTEGER PRIMARY KEY, " +
                    "album_title TEXT, " +
                    "album_year INTEGER);";
            createAlbumTable.ExecuteNonQuery();

            SQLiteCommand createWeightTable = dbConnection.CreateCommand();
            createWeightTable.Transaction = transaction;
            createWeightTable.CommandType = System.Data.CommandType.Text;
            createWeightTable.CommandText =
                "CREATE TABLE IF NOT EXISTS album_weight (" +
                    "album_id INTEGER PRIMARY KEY, " +
                    "weight REAL);";
            createWeightTable.ExecuteNonQuery();


            SQLiteCommand createArtTable = dbConnection.CreateCommand();
            createArtTable.Transaction = transaction;
            createArtTable.CommandType = System.Data.CommandType.Text;
            createArtTable.CommandText =
                "CREATE TABLE IF NOT EXISTS art (" +
                    "album_id INTEGER PRIMARY KEY, " +
                    "image BLOB);";
            createArtTable.ExecuteNonQuery();
        }

        #endregion //Create Commands

        #region Insert Commands

        public long _CreateAlbum(
            SQLiteTransaction transaction,
            string albumTitle,
            long albumYear = 0)
        {
            long albumID = NextID;

            SQLiteCommand writeAlbum = dbConnection.CreateCommand();
            writeAlbum.Transaction = transaction;
            writeAlbum.CommandType = System.Data.CommandType.Text;
            writeAlbum.CommandText =
                "INSERT INTO album " +
                    "(album_id, album_title, album_year) VALUES " +
                    "(@albumID, @albumTitle, @albumYear);";
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumYear", albumYear));
            writeAlbum.ExecuteNonQuery();

            return albumID;
        }

        public void _BatchCreateAlbum(
            SQLiteTransaction transaction,
            ICollection<AlbumData> newAlbumRecords)
        {
            SQLiteCommand writeAlbum = dbConnection.CreateCommand();
            writeAlbum.Transaction = transaction;
            writeAlbum.CommandType = System.Data.CommandType.Text;
            writeAlbum.CommandText =
                "INSERT INTO album " +
                    "(album_id, album_title, album_year) VALUES " +
                    "(@albumID, @albumTitle, @albumYear);";
            writeAlbum.Parameters.Add("@albumID", DbType.Int64);
            writeAlbum.Parameters.Add("@albumTitle", DbType.AnsiString);
            writeAlbum.Parameters.Add("@albumYear", DbType.Int64);
            writeAlbum.Parameters.Add("@albumArtFilename", DbType.AnsiString);

            foreach (AlbumData album in newAlbumRecords)
            {
                writeAlbum.Parameters["@albumID"].Value = album.albumID;
                writeAlbum.Parameters["@albumTitle"].Value = album.albumTitle;
                writeAlbum.Parameters["@albumYear"].Value = album.albumYear;
                writeAlbum.ExecuteNonQuery();
            }
        }

        public void _BatchCreateArt(
            SQLiteTransaction transaction,
            ICollection<ArtData> newArtRecords)
        {
            SQLiteCommand writeArt = dbConnection.CreateCommand();
            writeArt.Transaction = transaction;
            writeArt.CommandType = System.Data.CommandType.Text;
            writeArt.CommandText =
                "INSERT INTO art " +
                    "(album_id, image) VALUES " +
                    "(@albumID, @image);";
            writeArt.Parameters.Add("@albumID", DbType.Int64);
            writeArt.Parameters.Add("@image", DbType.Binary);

            foreach (ArtData art in newArtRecords)
            {
                writeArt.Parameters["@albumID"].Value = art.albumID;
                writeArt.Parameters["@image"].Value = art.image;
                writeArt.ExecuteNonQuery();
            }
        }


        #endregion //Insert Commands

        #region Delete Commands

        public void _DeleteAlbumID(
            SQLiteTransaction updateTransaction,
            ICollection<long> albumIDs)
        {
            SQLiteCommand deleteAlbum_ByAlbumID = dbConnection.CreateCommand();
            deleteAlbum_ByAlbumID.Transaction = updateTransaction;
            deleteAlbum_ByAlbumID.CommandType = System.Data.CommandType.Text;
            deleteAlbum_ByAlbumID.Parameters.Add("@albumID", DbType.Int64);
            deleteAlbum_ByAlbumID.CommandText =
                "DELETE FROM album " +
                "WHERE album.album_id=@albumID;";
            foreach (long id in albumIDs)
            {
                deleteAlbum_ByAlbumID.Parameters["@albumID"].Value = id;
                deleteAlbum_ByAlbumID.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Delete all albums with no tracks pointing at them.
        /// </summary>
        public void _DeleteAllLeafs(
            SQLiteTransaction transaction)
        {
            SQLiteCommand deleteLeafs = dbConnection.CreateCommand();
            deleteLeafs.Transaction = transaction;
            deleteLeafs.CommandType = System.Data.CommandType.Text;
            deleteLeafs.CommandText =
                "DELETE FROM album " +
                "WHERE album_id IN ( " +
                    "SELECT album.album_id " +
                    "FROM album " +
                    "LEFT JOIN track ON album.album_id=track.album_id " +
                    "WHERE track.album_id IS NULL );";
            deleteLeafs.ExecuteNonQuery();
        }

        #endregion // Delete Commands
    }
}
