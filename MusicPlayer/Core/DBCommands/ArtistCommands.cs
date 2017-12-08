﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Core.DBCommands
{
    public class ArtistCommands
    {
        AlbumCommands albumCommands = null;
        SongCommands songCommands = null;

        SQLiteConnection dbConnection;

        private long _lastIDAssigned = 0;
        public long NextID
        {
            get { return ++_lastIDAssigned; }
        }

        public ArtistCommands()
        {
        }

        public void Initialize(
            SQLiteConnection dbConnection,
            AlbumCommands albumCommands,
            SongCommands songCommands)
        {
            this.dbConnection = dbConnection;

            this.albumCommands = albumCommands;
            this.songCommands = songCommands;

            _lastIDAssigned = 0;
        }

        #region High Level Commands

        public void UpdateArtistName(ICollection<long> artistIDs, string newArtistName)
        {
            List<long> artistIDsCopy = new List<long>(artistIDs);

            //Assigning Albums to a different artist
            dbConnection.Open();

            //First, find out if the new artist exists
            long artistID = _FindArtist_ByName(newArtistName);

            using (SQLiteTransaction updateArtist = dbConnection.BeginTransaction())
            {
                if (artistID == -1)
                {
                    //Update an Artist's name, because it doesn't exist

                    //Pop off the front
                    artistID = artistIDsCopy[0];
                    artistIDsCopy.RemoveAt(0);

                    //Update the song formerly in the front
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

                    //Now, delete any old artists with no remaining songs
                    _DeleteAllLeafs(
                        transaction: updateArtist);
                }

                updateArtist.Commit();
            }

            dbConnection.Close();
        }

        #endregion // High Level Commands

        #region Search Commands

        public long _FindArtist_ByName(string artistName)
        {
            long artistID = -1;

            SQLiteCommand findArtist = dbConnection.CreateCommand();
            findArtist.CommandType = System.Data.CommandType.Text;
            findArtist.Parameters.Add(new SQLiteParameter("@artistName", artistName));
            findArtist.CommandText =
                "SELECT artist_id " +
                "FROM artist " +
                "WHERE artist.artist_name=@artistName COLLATE NOCASE " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = findArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    artistID = (long)reader["artist_id"];
                }
            }

            return artistID;
        }

        public string _GetPlaylistName(long artistID, long songID)
        {
            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.song_title AS song_title, " +
                    "artist.artist_name AS artist_name, " +
                "FROM artist " +
                "LEFT JOIN song ON song.song_id=@songID " +
                "WHERE artist.artist_id=@artistID;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));
            readTracks.Parameters.Add(new SQLiteParameter("@artistID", artistID));

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
            Dictionary<string, long> artistNameDict)
        {
            SQLiteCommand loadArtists = dbConnection.CreateCommand();
            loadArtists.CommandType = System.Data.CommandType.Text;
            loadArtists.CommandText =
                "SELECT artist_id, artist_name " +
                "FROM artist;";

            using (SQLiteDataReader reader = loadArtists.ExecuteReader())
            {
                while (reader.Read())
                {
                    string artistName = (string)reader["artist_name"];
                    long artistID = (long)reader["artist_id"];

                    if (!artistNameDict.ContainsKey(artistName.ToLowerInvariant()))
                    {
                        artistNameDict.Add(artistName.ToLowerInvariant(), artistID);
                    }

                    if (artistID > _lastIDAssigned)
                    {
                        _lastIDAssigned = artistID;
                    }
                }
            }
        }

        #endregion  //Lookup Commands

        #region Update Commands

        public void _UpdateArtistName_ByArtistID(
            SQLiteTransaction transaction,
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
                    "SET artist.artist_name=@artistName " +
                    "WHERE artist.artist_id=@artistID;";

            updateArtistName_ByArtistID.ExecuteNonQuery();
        }

        public void _RemapForeignKeys(
            SQLiteTransaction transaction,
            long newArtistID,
            ICollection<long> oldArtistIDs)
        {
            songCommands._RemapArtistID(
                transaction: transaction,
                newArtistID: newArtistID,
                oldArtistIDs: oldArtistIDs);

            albumCommands._RemapArtistID(
                transaction: transaction,
                newArtistID: newArtistID,
                oldArtistIDs: oldArtistIDs);
        }

        #endregion // Update Commands

        #region Create Commands

        public void _CreateArtistTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createArtistTable = dbConnection.CreateCommand();
            createArtistTable.Transaction = transaction;
            createArtistTable.CommandType = System.Data.CommandType.Text;
            createArtistTable.CommandText =
                "CREATE TABLE IF NOT EXISTS artist (" +
                    "artist_id INTEGER PRIMARY KEY, " +
                    "artist_name TEXT);";
            createArtistTable.ExecuteNonQuery();

            SQLiteCommand createWeightTable = dbConnection.CreateCommand();
            createWeightTable.Transaction = transaction;
            createWeightTable.CommandType = System.Data.CommandType.Text;
            createWeightTable.CommandText =
                "CREATE TABLE IF NOT EXISTS artist_weight (" +
                    "artist_id INTEGER PRIMARY KEY, " +
                    "weight REAL);";
            createWeightTable.ExecuteNonQuery();
        }

        #endregion //Create Commands

        #region Insert Commands

        public long _CreateArtist(
            SQLiteTransaction transaction,
            string artistName)
        {
            long artistID = NextID;
            
            SQLiteCommand createArtist = dbConnection.CreateCommand();
            createArtist.Transaction = transaction;
            createArtist.CommandType = System.Data.CommandType.Text;
            createArtist.CommandText = 
                "INSERT INTO artist " +
                    "(artist_id, artist_name) VALUES " +
                    "(@artistID, @artistName);";
            createArtist.Parameters.Add(new SQLiteParameter("@artistID", artistID));
            createArtist.Parameters.Add(new SQLiteParameter("@artistName", artistName));
            createArtist.ExecuteNonQuery();

            return artistID;
        }

        public void _BatchCreateArtist(
            SQLiteTransaction transaction,
            ICollection<ArtistData> newArtistRecords)
        {
            SQLiteCommand writeArtist = dbConnection.CreateCommand();
            writeArtist.Transaction = transaction;
            writeArtist.CommandType = System.Data.CommandType.Text;
            writeArtist.CommandText =
                "INSERT INTO artist " +
                    "(artist_id, artist_name) VALUES " +
                    "(@artistID, @artistName);";
            writeArtist.Parameters.Add("@artistID", DbType.Int64);
            writeArtist.Parameters.Add("@artistName", DbType.AnsiString);

            foreach (ArtistData artist in newArtistRecords)
            {
                writeArtist.Parameters["@artistID"].Value = artist.artistID;
                writeArtist.Parameters["@artistName"].Value = artist.artistName;
                writeArtist.ExecuteNonQuery();
            }
        }


        #endregion //Insert Commands

        #region Delete Commands

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
                "WHERE artist.artist_id=@artistID;";
            foreach (long id in artistIDs)
            {
                deleteArtist_ByArtistID.Parameters["@artistID"].Value = id;
                deleteArtist_ByArtistID.ExecuteNonQuery();
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
                "WHERE artist_id IN ( " +
                    "SELECT artist.artist_id " +
                    "FROM artist " +
                    "LEFT JOIN recording ON artist.artist_id=recording.artist_id " +
                    "WHERE recording.artist_id IS NULL );";
            deleteLeafs.ExecuteNonQuery();
        }

        #endregion // Delete Commands
    }
}