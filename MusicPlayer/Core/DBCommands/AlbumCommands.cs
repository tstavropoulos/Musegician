using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using MusicPlayer.DataStructures;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using TagLib;

namespace MusicPlayer.Core.DBCommands
{
    public class AlbumCommands
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

        public AlbumCommands()
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

        #region High Level Commands

        public List<AlbumDTO> GenerateArtistAlbumList(long artistID, string artistName)
        {
            List<AlbumDTO> albumList = new List<AlbumDTO>();

            dbConnection.Open();

            SQLiteCommand readAlbums = dbConnection.CreateCommand();
            readAlbums.CommandType = System.Data.CommandType.Text;
            readAlbums.CommandText =
                "SELECT id, title, year " +
                "FROM album " +
                "WHERE id IN ( " +
                    "SELECT track.album_id " +
                    "FROM recording " +
                    "LEFT JOIN track ON recording.id=track.recording_id " +
                    "WHERE recording.artist_id=@artistID ) " +
                "ORDER BY year ASC;";
            readAlbums.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readAlbums.ExecuteReader())
            {
                while (reader.Read())
                {
                    long albumID = (long)reader["id"];

                    albumList.Add(new AlbumDTO(
                        albumID: albumID,
                        albumTitle: String.Format(
                            "{0} ({1})",
                            (string)reader["title"],
                            ((long)reader["year"]).ToString()),
                        albumArt: LoadImage(_GetArt(albumID))));
                }
            }

            dbConnection.Close();

            return albumList;
        }

        public List<AlbumDTO> GenerateAlbumList()
        {
            List<AlbumDTO> albumList = new List<AlbumDTO>();

            dbConnection.Open();

            SQLiteCommand readAlbums = dbConnection.CreateCommand();
            readAlbums.CommandType = System.Data.CommandType.Text;
            readAlbums.CommandText =
                "SELECT id, title " +
                "FROM album " +
                "ORDER BY title ASC;";
            using (SQLiteDataReader reader = readAlbums.ExecuteReader())
            {
                while (reader.Read())
                {
                    long albumID = (long)reader["id"];

                    albumList.Add(new AlbumDTO(
                        albumID: albumID,
                        albumTitle: (string)reader["title"],
                        albumArt: LoadImage(_GetArt(albumID))));
                }
            }

            dbConnection.Close();

            return albumList;
        }

        public BitmapImage GetAlbumArtForRecording(long recordingID)
        {
            dbConnection.Open();

            long albumID = recordingCommands._GetAlbumID(recordingID);

            BitmapImage image = LoadImage(_GetArt(albumID));

            dbConnection.Close();

            return image;
        }

        public string GetAlbumTitle(long albumID)
        {
            string albumName = "";

            dbConnection.Open();

            SQLiteCommand findArtist = dbConnection.CreateCommand();
            findArtist.CommandType = System.Data.CommandType.Text;
            findArtist.CommandText =
                "SELECT title " +
                "FROM album " +
                "WHERE id=@albumID;";
            findArtist.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            using (SQLiteDataReader reader = findArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    albumName = (string)reader["title"];
                }
            }

            dbConnection.Close();

            return albumName;
        }

        public List<SongDTO> GetAlbumData(
            long albumID)
        {
            List<SongDTO> albumData = new List<SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.id AS song_id, " +
                    "song.title AS song_title, " +
                    "artist.name AS artist_name," +
                    "recording.id AS recording_id, " +
                    "song_weight.weight AS weight " +
                "FROM track " +
                "LEFT JOIN recording ON track.recording_id=recording.id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "LEFT JOIN song ON recording.song_id=song.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "LEFT JOIN song_weight ON song.id=song_weight.song_id " +
                "WHERE track.album_id=@albumID " +
                "ORDER BY track.disc_number ASC, track.track_number ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];

                    SongDTO newSong = new SongDTO(
                        songID: songID,
                        title: _GetPlaylistSongName(
                            albumID: albumID,
                            songID: songID));

                    if (reader["weight"].GetType() != typeof(DBNull))
                    {
                        newSong.Weight = (double)reader["weight"];
                    }

                    RecordingDTO recording = recordingCommands._GetRecording(
                        recordingID: (long)reader["recording_id"],
                        albumID: albumID);

                    if (recording != null)
                    {
                        newSong.Children.Add(recording);
                    }


                    albumData.Add(newSong);
                }
            }

            dbConnection.Close();

            return albumData;
        }

        public List<SongDTO> GetAlbumDataDeep(
            long albumID)
        {
            List<SongDTO> albumData = new List<SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.id AS song_id, " +
                    "song.title AS song_title, " +
                    "artist.name AS artist_name " +
                "FROM track " +
                "LEFT JOIN recording ON track.recording_id=recording.id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "LEFT JOIN song ON recording.song_id=song.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "WHERE track.album_id=@albumID ORDER BY track.disc_number ASC, track.track_number ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];

                    SongDTO newSong = new SongDTO(
                        songID: songID,
                        title: songCommands._GetPlaylistSongName(
                            songID: songID));

                    foreach (RecordingDTO recording in recordingCommands._GetRecordingList(
                            songID: songID))
                    {
                        newSong.Children.Add(recording);
                    }


                    albumData.Add(newSong);
                }
            }

            dbConnection.Close();

            return albumData;
        }

        public void UpdateWeights(IList<(long albumID, double weight)> values)
        {
            dbConnection.Open();

            SQLiteCommand updateWeight = dbConnection.CreateCommand();
            updateWeight.CommandType = System.Data.CommandType.Text;
            updateWeight.CommandText =
                "INSERT OR REPLACE INTO album_weight " +
                "(album_id, weight) VALUES " +
                "(@albumID, @weight);";

            updateWeight.Parameters.Add("@albumID", DbType.Int64);
            updateWeight.Parameters.Add("@weight", DbType.Double);

            foreach(var value in values)
            {
                updateWeight.Parameters["@albumID"].Value = value.albumID;
                updateWeight.Parameters["@weight"].Value = value.weight;
                updateWeight.ExecuteNonQuery();
            }

            dbConnection.Close();
        }


        #endregion High Level Commands
        #region Search Commands

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

        public string _GetPlaylistSongName(long albumID, long songID)
        {
            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.title AS title, " +
                    "artist.name AS name " +
                "FROM album " +
                "LEFT JOIN track ON album.id=track.album_id " +
                "LEFT JOIN recording ON track.recording_id=recording.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "LEFT JOIN song ON recording.song_id=song.id " +
                "WHERE album.id=@albumID AND song.id=@songID;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

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

        #endregion Search Commands
        #region Initialization Commands

        public void _InitializeValues()
        {
            SQLiteCommand loadAlbums = dbConnection.CreateCommand();
            loadAlbums.CommandType = System.Data.CommandType.Text;
            loadAlbums.CommandText =
                "SELECT id " +
                "FROM album " +
                "ORDER BY id DESC " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = loadAlbums.ExecuteReader())
            {
                if (reader.Read())
                {
                    _lastIDAssigned = (long)reader["id"];
                }
            }
        }

        #endregion Initialization Commands
        #region Lookup Commands

        public void _PopulateLookup(
            Dictionary<(long, string), long> artistID_AlbumTitleDict,
            HashSet<long> albumArt)
        {
            SQLiteCommand loadAlbums = dbConnection.CreateCommand();
            loadAlbums.CommandType = System.Data.CommandType.Text;
            loadAlbums.CommandText =
                "SELECT " +
                    "album.id AS album_id, " +
                    "album.title AS title, " +
                    "artist.id AS artist_id " +
                "FROM album " +
                "LEFT JOIN artist ON artist.id IN ( " +
                    "SELECT recording.artist_id " +
                    "FROM track " +
                    "LEFT JOIN recording ON track.recording_id= recording.id " +
                    "WHERE track.album_id= album.id); ";

            using (SQLiteDataReader reader = loadAlbums.ExecuteReader())
            {
                while (reader.Read())
                {
                    long albumID = (long)reader["album_id"];
                    long artistID = (long)reader["artist_id"];
                    string albumTitle = (string)reader["title"];

                    var key = (artistID, albumTitle.ToLowerInvariant());

                    if (!artistID_AlbumTitleDict.ContainsKey(key))
                    {
                        artistID_AlbumTitleDict.Add(key, albumID);
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

        #endregion Lookup Commands
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
                    "SET album.title=@albumTitle " +
                    "WHERE album.id=@albumID;";

            updateAlbumTitle_ByAlbumID.ExecuteNonQuery();
        }

        #endregion Update Commands
        #region Create Commands

        public void _CreateAlbumTables(SQLiteTransaction transaction)
        {
            SQLiteCommand createAlbumTable = dbConnection.CreateCommand();
            createAlbumTable.Transaction = transaction;
            createAlbumTable.CommandType = System.Data.CommandType.Text;
            createAlbumTable.CommandText =
                "CREATE TABLE IF NOT EXISTS album (" +
                    "id INTEGER PRIMARY KEY, " +
                    "title TEXT, " +
                    "year INTEGER);";
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

        #endregion Create Commands
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
                    "(id, title, year) VALUES " +
                    "(@albumID, @albumTitle, @albumYear);";
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumYear", albumYear));
            writeAlbum.ExecuteNonQuery();

            return albumID;
        }

        public void _CreateArt(
            SQLiteTransaction transaction,
            long albumID,
            byte[] imageData)
        {
            SQLiteCommand writeArt = dbConnection.CreateCommand();
            writeArt.Transaction = transaction;
            writeArt.CommandType = System.Data.CommandType.Text;
            writeArt.CommandText =
                "INSERT OR REPLACE INTO art " +
                    "(album_id, image) VALUES " +
                    "(@albumID, @image);";
            writeArt.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            writeArt.Parameters.Add(new SQLiteParameter("@image", imageData));
            
            writeArt.ExecuteNonQuery();
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
                    "(id, title, year) VALUES " +
                    "(@albumID, @albumTitle, @albumYear);";
            writeAlbum.Parameters.Add("@albumID", DbType.Int64);
            writeAlbum.Parameters.Add("@albumTitle", DbType.String);
            writeAlbum.Parameters.Add("@albumYear", DbType.Int64);
            writeAlbum.Parameters.Add("@albumArtFilename", DbType.String);

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

        #endregion Insert Commands
        #region Delete Commands

        public void _DropTable(
            SQLiteTransaction transaction)
        {
            SQLiteCommand dropAlbumTable = dbConnection.CreateCommand();
            dropAlbumTable.Transaction = transaction;
            dropAlbumTable.CommandType = System.Data.CommandType.Text;
            dropAlbumTable.CommandText =
                "DROP TABLE IF EXISTS album;";
            dropAlbumTable.ExecuteNonQuery();

            SQLiteCommand dropAlbumWeightTable = dbConnection.CreateCommand();
            dropAlbumWeightTable.Transaction = transaction;
            dropAlbumWeightTable.CommandType = System.Data.CommandType.Text;
            dropAlbumWeightTable.CommandText =
                "DROP TABLE IF EXISTS album_weight;";
            dropAlbumWeightTable.ExecuteNonQuery();

            SQLiteCommand dropAlbumArtTable = dbConnection.CreateCommand();
            dropAlbumArtTable.Transaction = transaction;
            dropAlbumArtTable.CommandType = System.Data.CommandType.Text;
            dropAlbumArtTable.CommandText =
                "DROP TABLE IF EXISTS art;";
            dropAlbumArtTable.ExecuteNonQuery();
        }

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
                "WHERE album.id=@albumID;";
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
                "WHERE id IN ( " +
                    "SELECT album.id " +
                    "FROM album " +
                    "LEFT JOIN track ON album.id=track.album_id " +
                    "WHERE track.album_id IS NULL );";
            deleteLeafs.ExecuteNonQuery();
        }

        #endregion Delete Commands

        public void SetAlbumArt(long albumID, string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new IOException("File not found: " + path);
            }

            byte[] imageData = System.IO.File.ReadAllBytes(path);

            dbConnection.Open();



            using (SQLiteTransaction transaction = dbConnection.BeginTransaction())
            {
                _CreateArt(
                    transaction: transaction,
                    albumID: albumID,
                    imageData: imageData);

                transaction.Commit();
            }

            //Update the first song on the album (whose id3 tag points to the album) with the art
            _PushAlbumArtToTag(albumID, imageData);

            dbConnection.Close();

        }

        //Update the first song on the album (whose id3 tag points to the album) with the art
        public void _PushAlbumArtToTag(long albumID, byte[] imageData)
        {

            SQLiteCommand readFiles = dbConnection.CreateCommand();
            readFiles.CommandType = System.Data.CommandType.Text;
            readFiles.CommandText =
                "SELECT recording.filename AS filename, album.title AS title " +
                "FROM album " +
                "LEFT JOIN track ON album.id=track.album_id " +
                "LEFT JOIN recording ON track.recording_id=recording.id " +
                "WHERE album.id=@albumID " +
                "ORDER BY track.track_number ASC;";
            readFiles.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readFiles.ExecuteReader())
            {
                while (reader.Read())
                {
                    string albumTitle = ((string)reader["title"]).ToLowerInvariant();
                    string filename = (string)reader["filename"];

                    TagLib.Mpeg.AudioFile audioFile = TagLib.File.Create(filename) as TagLib.Mpeg.AudioFile;

                    if (audioFile == null)
                    {
                        Console.WriteLine("Unable to open file: " + filename);
                        continue;
                    }

                    string tagName = audioFile.Tag.Album.ToLowerInvariant();

                    if (tagName != albumTitle && !tagName.Contains(albumTitle))
                    {
                        Console.WriteLine("Album Title doesn't match. Skipping : " + albumTitle + " / " + tagName);
                        continue;
                    }

                    audioFile.Tag.Pictures = new IPicture[1] { new Picture(imageData) };
                    audioFile.Save();
                    break;
                }
            }
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            BitmapImage image = new BitmapImage();
            using (MemoryStream mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
    }
}
