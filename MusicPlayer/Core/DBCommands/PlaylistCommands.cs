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
    public class PlaylistCommands
    {
        SQLiteConnection dbConnection;

        private long _lastPlaylistIDAssigned = 0;
        public long NextPlaylistID
        {
            get { return ++_lastPlaylistIDAssigned; }
        }

        private long _lastPlaylistSongIDAssigned = 0;
        public long NextPlaylistSongID
        {
            get { return ++_lastPlaylistSongIDAssigned; }
        }

        private long _lastPlaylistRecordingIDAssigned = 0;
        public long NextPlaylistRecordingID
        {
            get { return ++_lastPlaylistRecordingIDAssigned; }
        }

        public PlaylistCommands()
        {
        }

        public void Initialize(
            SQLiteConnection dbConnection)
        {
            this.dbConnection = dbConnection;

            _lastPlaylistIDAssigned = 0;
            _lastPlaylistSongIDAssigned = 0;
            _lastPlaylistRecordingIDAssigned = 0;
        }

        #region High Level Commands

        public List<PlaylistData> GetPlaylistInfo()
        {
            List<PlaylistData> playlists = new List<PlaylistData>();

            dbConnection.Open();

            SQLiteCommand loadPlaylists = dbConnection.CreateCommand();
            loadPlaylists.CommandType = System.Data.CommandType.Text;
            loadPlaylists.CommandText =
                "SELECT " +
                    "playlists.id AS id, " +
                    "playlists.title AS title, " +
                    "COUNT(playlist_songs.id) AS count " +
                "FROM playlists " +
                "INNER JOIN playlist_songs ON playlists.id=playlist_songs.playlist_id " +
                "GROUP BY playlists.id " +
                "ORDER BY playlists.title;";

            using (SQLiteDataReader reader = loadPlaylists.ExecuteReader())
            {
                while (reader.Read())
                {
                    playlists.Add(new PlaylistData()
                    {
                        id = (long)reader["id"],
                        title = (string)reader["title"],
                        count = (long)reader["count"]
                    });
                }
            }

            dbConnection.Close();

            return playlists;
        }

        public List<SongDTO> LoadPlaylist(long playlistID)
        {
            List<SongDTO> songs = new List<SongDTO>();

            dbConnection.Open();

            SQLiteCommand loadPlaylist = dbConnection.CreateCommand();
            loadPlaylist.CommandType = System.Data.CommandType.Text;
            loadPlaylist.CommandText =
                "SELECT * " +
                "FROM playlist_songs " +
                "WHERE playlist_id=@ID " +
                "ORDER BY number;";
            loadPlaylist.Parameters.Add(new SQLiteParameter("@ID", playlistID));

            using (SQLiteDataReader reader = loadPlaylist.ExecuteReader())
            {
                while (reader.Read())
                {
                    long playlistSongID = (long)reader["id"];

                    SongDTO newPlaylistSong = new SongDTO(
                        songID: (long)reader["song_id"],
                        title: (string)reader["title"]);

                    newPlaylistSong.Weight = (double)reader["weight"];

                    foreach (RecordingDTO recording in _LoadPlaylistRecordings(playlistSongID))
                    {
                        newPlaylistSong.Children.Add(recording);
                    }

                    songs.Add(newPlaylistSong);
                }
            }

            dbConnection.Close();

            return songs;
        }

        public void SavePlaylist(string title, ICollection<SongDTO> songs)
        {
            dbConnection.Open();

            //Find if the playlist exists
            long playlistID = _FindPlaylist(title);

            using (SQLiteTransaction transaction = dbConnection.BeginTransaction())
            {
                if (playlistID != -1)
                {
                    _DeepDeletePlaylist(
                        transaction: transaction,
                        id: playlistID);
                }

                playlistID = _CreatePlaylist(
                    transaction: transaction,
                    title: title,
                    playlistID: playlistID);

                List<PlaylistSongData> playlistSongs = new List<PlaylistSongData>();
                List<PlaylistRecordingData> playlistRecordings = new List<PlaylistRecordingData>();

                TranslateSongData(
                    playlistID: playlistID,
                    songs: songs,
                    playlistSongs: playlistSongs,
                    playlistRecordings: playlistRecordings);

                _BatchCreatePlaylistSongs(
                    transaction: transaction,
                    playlistSongs: playlistSongs);

                _BatchCreatePlaylistRecording(
                    transaction: transaction,
                    playlistRecordings: playlistRecordings);

                transaction.Commit();
            }


            dbConnection.Close();
        }

        public void TranslateSongData(
            long playlistID,
            ICollection<SongDTO> songs,
            ICollection<PlaylistSongData> playlistSongs,
            ICollection<PlaylistRecordingData> playlistRecordings)
        {
            int position = -1;
            foreach (SongDTO songData in songs)
            {
                ++position;

                long songID = NextPlaylistSongID;

                playlistSongs.Add(new PlaylistSongData()
                {
                    id = songID,
                    playlistID = playlistID,
                    title = songData.Name,
                    songID = songData.ID,
                    number = position,
                    weight = songData.Weight
                });

                foreach (RecordingDTO recordingData in songData.Children)
                {
                    playlistRecordings.Add(new PlaylistRecordingData()
                    {
                        id = NextPlaylistRecordingID,
                        playlistSongID = songID,
                        recordingID = recordingData.ID,
                        weight = recordingData.Weight
                    });
                }
            }
        }

        public long FindPlaylist(string title)
        {
            dbConnection.Open();

            long id = _FindPlaylist(title);

            dbConnection.Close();

            return id;
        }


        #endregion // High Level Commands

        #region Search Commands

        private long _FindPlaylist(string title)
        {
            long id = -1;

            SQLiteCommand findPlaylist = dbConnection.CreateCommand();
            findPlaylist.CommandType = System.Data.CommandType.Text;
            findPlaylist.CommandText =
                "SELECT id " +
                "FROM playlists " +
                "WHERE title=@title COLLATE NOCASE " +
                "LIMIT 1;";
            findPlaylist.Parameters.Add(new SQLiteParameter("@title", title));

            using (SQLiteDataReader reader = findPlaylist.ExecuteReader())
            {
                if (reader.Read())
                {
                    id = (long)reader["id"];
                }
            }

            return id;
        }

        public List<RecordingDTO> _LoadPlaylistRecordings(long playlistSongID)
        {
            List<RecordingDTO> recordings = new List<RecordingDTO>();

            SQLiteCommand loadPlaylistRecordings = dbConnection.CreateCommand();
            loadPlaylistRecordings.CommandType = System.Data.CommandType.Text;
            loadPlaylistRecordings.CommandText =
                "SELECT " +
                    "playlist_recordings.recording_id AS recording_id, " +
                    "playlist_recordings.weight AS weight, " +
                    "artist.artist_name AS artist_name, " +
                    "album.album_title AS album_title, " +
                    "track.track_title AS track_title " +
                "FROM playlist_recordings " +
                "LEFT JOIN recording ON playlist_recordings.recording_id=recording.recording_id " +
                "LEFT JOIN artist ON recording.artist_id=artist.artist_id " +
                "LEFT JOIN track ON track.track_id IN ( " +
                    "SELECT track.track_id " +
                    "FROM track " +
                    "LEFT JOIN album ON track.album_id=album.album_id " +
                    "WHERE track.recording_id=recording.recording_id " +
                    "ORDER BY album.album_year ASC " +
                    "LIMIT 1) " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "WHERE playlist_song_id=@ID;";
            loadPlaylistRecordings.Parameters.Add(new SQLiteParameter("@ID", playlistSongID));

            using (SQLiteDataReader reader = loadPlaylistRecordings.ExecuteReader())
            {
                while (reader.Read())
                {
                    recordings.Add(new RecordingDTO()
                    {
                        ID = (long)reader["recording_id"],
                        Name = string.Format(
                                "{0} - {1} - {2}",
                                (string)reader["artist_name"],
                                (string)reader["album_title"],
                                (string)reader["track_title"]),
                        Weight = (double)reader["weight"]
                    });
                }
            }


            return recordings;
        }

        #endregion  //Search Commands

        #region Initialization Commands

        public void _InitializeValues()
        {
            SQLiteCommand loadPlaylists = dbConnection.CreateCommand();
            loadPlaylists.CommandType = System.Data.CommandType.Text;
            loadPlaylists.CommandText =
                "SELECT id " +
                "FROM playlists " +
                "ORDER BY id DESC " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = loadPlaylists.ExecuteReader())
            {
                if (reader.Read())
                {
                    _lastPlaylistIDAssigned = (long)reader["id"];
                }
            }

            SQLiteCommand loadPlaylistSongs = dbConnection.CreateCommand();
            loadPlaylistSongs.CommandType = System.Data.CommandType.Text;
            loadPlaylistSongs.CommandText =
                "SELECT id " +
                "FROM playlist_songs " +
                "ORDER BY id DESC " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = loadPlaylistSongs.ExecuteReader())
            {
                if (reader.Read())
                {
                    _lastPlaylistSongIDAssigned = (long)reader["id"];
                }
            }

            SQLiteCommand loadPlaylistRecordings = dbConnection.CreateCommand();
            loadPlaylistRecordings.CommandType = System.Data.CommandType.Text;
            loadPlaylistRecordings.CommandText =
                "SELECT id " +
                "FROM playlist_recordings " +
                "ORDER BY id DESC " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = loadPlaylistRecordings.ExecuteReader())
            {
                if (reader.Read())
                {
                    _lastPlaylistRecordingIDAssigned = (long)reader["id"];
                }
            }
        }

        #endregion  //Lookup Commands

        #region Update Commands

        #endregion // Update Commands

        #region Create Commands

        public void _CreatePlaylistTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createPlaylistsTable = dbConnection.CreateCommand();
            createPlaylistsTable.Transaction = transaction;
            createPlaylistsTable.CommandType = System.Data.CommandType.Text;
            createPlaylistsTable.CommandText =
                "CREATE TABLE IF NOT EXISTS playlists (" +
                    "id INTEGER PRIMARY KEY, " +
                    "title TEXT);";
            createPlaylistsTable.ExecuteNonQuery();

            SQLiteCommand createPlaylistSongsTable = dbConnection.CreateCommand();
            createPlaylistSongsTable.Transaction = transaction;
            createPlaylistSongsTable.CommandType = System.Data.CommandType.Text;
            createPlaylistSongsTable.CommandText =
                "CREATE TABLE IF NOT EXISTS playlist_songs (" +
                    "id INTEGER PRIMARY KEY, " +
                    "playlist_id INTEGER REFERENCES playlists, " +
                    "title TEXT, " +
                    "song_id INTEGER REFERENCES song, " +
                    "number INTEGER, " +
                    "weight REAL);";
            createPlaylistSongsTable.ExecuteNonQuery();

            SQLiteCommand createPlaylistRecordingsTable = dbConnection.CreateCommand();
            createPlaylistRecordingsTable.Transaction = transaction;
            createPlaylistRecordingsTable.CommandType = System.Data.CommandType.Text;
            createPlaylistRecordingsTable.CommandText =
                "CREATE TABLE IF NOT EXISTS playlist_recordings (" +
                    "id INTEGER PRIMARY KEY, " +
                    "playlist_song_id INTEGER REFERENCES playlist_songs, " +
                    "recording_id INTEGER REFERENCES recording, " +
                    "weight REAL);";
            createPlaylistRecordingsTable.ExecuteNonQuery();

            SQLiteCommand createPlaylistIDIndex = dbConnection.CreateCommand();
            createPlaylistIDIndex.Transaction = transaction;
            createPlaylistIDIndex.CommandType = System.Data.CommandType.Text;
            createPlaylistIDIndex.CommandText =
                "CREATE INDEX IF NOT EXISTS " +
                    "idx_playlistsongs_playlistid ON playlist_songs (playlist_id);";
            createPlaylistIDIndex.ExecuteNonQuery();

            SQLiteCommand createPlaylistRecordingIDIndex = dbConnection.CreateCommand();
            createPlaylistRecordingIDIndex.Transaction = transaction;
            createPlaylistRecordingIDIndex.CommandType = System.Data.CommandType.Text;
            createPlaylistRecordingIDIndex.CommandText =
                "CREATE INDEX IF NOT EXISTS " +
                    "idx_playlistrecordings_playlistsongid ON playlist_recordings (playlist_song_id);";
            createPlaylistRecordingIDIndex.ExecuteNonQuery();
        }

        #endregion //Create Commands

        #region Insert Commands

        public long _CreatePlaylist(
            SQLiteTransaction transaction,
            string title,
            long playlistID = -1)
        {
            if (playlistID == -1)
            {
                playlistID = NextPlaylistID;
            }

            SQLiteCommand createPlaylist = dbConnection.CreateCommand();
            createPlaylist.Transaction = transaction;
            createPlaylist.CommandType = System.Data.CommandType.Text;
            createPlaylist.CommandText =
                "INSERT INTO playlists " +
                    "(id, title) VALUES " +
                    "(@id, @title);";
            createPlaylist.Parameters.Add(new SQLiteParameter("@id", playlistID));
            createPlaylist.Parameters.Add(new SQLiteParameter("@title", title));

            createPlaylist.ExecuteNonQuery();

            return playlistID;
        }

        public long _CreatePlaylistSong(
            SQLiteTransaction transaction,
            long playlistID,
            string title,
            long songID,
            long number,
            double weight)
        {
            long playlistSongID = NextPlaylistSongID;

            SQLiteCommand createPlaylistSong = dbConnection.CreateCommand();
            createPlaylistSong.Transaction = transaction;
            createPlaylistSong.CommandType = System.Data.CommandType.Text;
            createPlaylistSong.CommandText =
                "INSERT INTO playlist_songs " +
                    "(id, playlist_id, title, song_id, number, weight) VALUES " +
                    "(@id, @playlistID, @title, @songID, @number, @weight);";

            createPlaylistSong.Parameters.Add(new SQLiteParameter("@id", playlistSongID));
            createPlaylistSong.Parameters.Add(new SQLiteParameter("@playlistID", playlistID));
            createPlaylistSong.Parameters.Add(new SQLiteParameter("@title", title));
            createPlaylistSong.Parameters.Add(new SQLiteParameter("@songID", songID));
            createPlaylistSong.Parameters.Add(new SQLiteParameter("@number", number));
            createPlaylistSong.Parameters.Add(new SQLiteParameter("@weight", weight));

            createPlaylistSong.ExecuteNonQuery();

            return playlistSongID;
        }

        public void _BatchCreatePlaylistSongs(
            SQLiteTransaction transaction,
            ICollection<PlaylistSongData> playlistSongs)
        {
            SQLiteCommand createPlaylistSongs = dbConnection.CreateCommand();
            createPlaylistSongs.Transaction = transaction;
            createPlaylistSongs.CommandType = System.Data.CommandType.Text;
            createPlaylistSongs.CommandText =
                "INSERT INTO playlist_songs " +
                    "(id, playlist_id, title, song_id, number, weight) VALUES " +
                    "(@id, @playlistID, @title, @songID, @number, @weight);";

            createPlaylistSongs.Parameters.Add("@id", DbType.Int64);
            createPlaylistSongs.Parameters.Add("@playlistID", DbType.Int64);
            createPlaylistSongs.Parameters.Add("@title", DbType.String);
            createPlaylistSongs.Parameters.Add("@songID", DbType.Int64);
            createPlaylistSongs.Parameters.Add("@number", DbType.Int64);
            createPlaylistSongs.Parameters.Add("@weight", DbType.Double);

            foreach (PlaylistSongData data in playlistSongs)
            {
                createPlaylistSongs.Parameters["@id"].Value = data.id;
                createPlaylistSongs.Parameters["@playlistID"].Value = data.playlistID;
                createPlaylistSongs.Parameters["@title"].Value = data.title;
                createPlaylistSongs.Parameters["@songID"].Value = data.songID;
                createPlaylistSongs.Parameters["@number"].Value = data.number;
                createPlaylistSongs.Parameters["@weight"].Value = data.weight;

                createPlaylistSongs.ExecuteNonQuery();
            }
        }

        public long _CreatePlaylistRecording(
            SQLiteTransaction transaction,
            long playlistSongID,
            long recordingID,
            double weight)
        {
            long playlistRecordingID = NextPlaylistRecordingID;

            SQLiteCommand createPlaylistRecording = dbConnection.CreateCommand();
            createPlaylistRecording.Transaction = transaction;
            createPlaylistRecording.CommandType = System.Data.CommandType.Text;
            createPlaylistRecording.CommandText =
                "INSERT INTO playlist_recordings " +
                    "(id, playlist_song_id, recording_id, weight) VALUES " +
                    "(@id, @playlistSongID, @recordingID, @weight);";

            createPlaylistRecording.Parameters.Add(new SQLiteParameter("@id", playlistRecordingID));
            createPlaylistRecording.Parameters.Add(new SQLiteParameter("@playlistSongID", playlistSongID));
            createPlaylistRecording.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));
            createPlaylistRecording.Parameters.Add(new SQLiteParameter("@weight", weight));

            createPlaylistRecording.ExecuteNonQuery();

            return playlistRecordingID;
        }

        public void _BatchCreatePlaylistRecording(
            SQLiteTransaction transaction,
            ICollection<PlaylistRecordingData> playlistRecordings)
        {

            SQLiteCommand createPlaylistRecordings = dbConnection.CreateCommand();
            createPlaylistRecordings.Transaction = transaction;
            createPlaylistRecordings.CommandType = System.Data.CommandType.Text;
            createPlaylistRecordings.CommandText =
                "INSERT INTO playlist_recordings " +
                    "(id, playlist_song_id, recording_id, weight) VALUES " +
                    "(@id, @playlistSongID, @recordingID, @weight);";

            createPlaylistRecordings.Parameters.Add("@id", DbType.Int64);
            createPlaylistRecordings.Parameters.Add("@playlistSongID", DbType.Int64);
            createPlaylistRecordings.Parameters.Add("@recordingID", DbType.Int64);
            createPlaylistRecordings.Parameters.Add("@weight", DbType.Double);

            foreach (PlaylistRecordingData data in playlistRecordings)
            {
                createPlaylistRecordings.Parameters["@id"].Value = data.id;
                createPlaylistRecordings.Parameters["@playlistSongID"].Value = data.playlistSongID;
                createPlaylistRecordings.Parameters["@recordingID"].Value = data.recordingID;
                createPlaylistRecordings.Parameters["@weight"].Value = data.weight;

                createPlaylistRecordings.ExecuteNonQuery();
            }
        }

        #endregion //Insert Commands

        #region Delete Commands

        public void _DeepDeletePlaylist(SQLiteTransaction transaction, long id)
        {
            SQLiteCommand deletePlaylistRecordings = dbConnection.CreateCommand();
            deletePlaylistRecordings.Transaction = transaction;
            deletePlaylistRecordings.CommandType = System.Data.CommandType.Text;
            deletePlaylistRecordings.CommandText =
                "DELETE FROM playlist_recordings " +
                "WHERE playlist_song_id IN ( " +
                    "SELECT id " +
                    "FROM playlist_songs " +
                    "WHERE playlist_id=@id);";
            deletePlaylistRecordings.Parameters.Add(new SQLiteParameter("@id", id));
            deletePlaylistRecordings.ExecuteNonQuery();

            SQLiteCommand deletePlaylistSongs = dbConnection.CreateCommand();
            deletePlaylistSongs.Transaction = transaction;
            deletePlaylistSongs.CommandType = System.Data.CommandType.Text;
            deletePlaylistSongs.CommandText =
                "DELETE FROM playlist_songs " +
                "WHERE playlist_id=@id;";
            deletePlaylistSongs.Parameters.Add(new SQLiteParameter("@id", id));
            deletePlaylistSongs.ExecuteNonQuery();

            SQLiteCommand deletePlaylist = dbConnection.CreateCommand();
            deletePlaylist.Transaction = transaction;
            deletePlaylist.CommandType = System.Data.CommandType.Text;
            deletePlaylist.CommandText =
                "DELETE FROM playlists " +
                "WHERE id=@id;";
            deletePlaylist.Parameters.Add(new SQLiteParameter("@id", id));
            deletePlaylist.ExecuteNonQuery();
        }

        #endregion // Delete Commands
    }
}
