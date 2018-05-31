using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using Musegician.DataStructures;
using TagLib;
using Musegician.Deredundafier;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class AlbumCommands
    {
        ArtistCommands artistCommands = null;
        SongCommands songCommands = null;
        TrackCommands trackCommands = null;
        RecordingCommands recordingCommands = null;

        MusegicianData db = null;

        public AlbumCommands()
        {
        }

        public void Initialize(
            MusegicianData db,
            ArtistCommands artistCommands,
            SongCommands songCommands,
            TrackCommands trackCommands,
            RecordingCommands recordingCommands)
        {
            this.db = db;

            this.artistCommands = artistCommands;
            this.songCommands = songCommands;
            this.trackCommands = trackCommands;
            this.recordingCommands = recordingCommands;
        }

        #region High Level Commands

        

        //public IEnumerable<Song> GetAlbumData(
        //    long albumID)
        //{
        //    List<SongDTO> albumData = new List<SongDTO>();

        //    dbConnection.Open();

        //    SQLiteCommand readTracks = dbConnection.CreateCommand();
        //    readTracks.CommandType = System.Data.CommandType.Text;
        //    readTracks.CommandText =
        //        "SELECT " +
        //            "song.id AS song_id, " +
        //            "song.title AS song_title, " +
        //            "artist.name AS artist_name," +
        //            "recording.id AS recording_id, " +
        //            "song_weight.weight AS weight " +
        //        "FROM track " +
        //        "LEFT JOIN recording ON track.recording_id=recording.id " +
        //        "LEFT JOIN album ON track.album_id=album.id " +
        //        "LEFT JOIN song ON recording.song_id=song.id " +
        //        "LEFT JOIN artist ON recording.artist_id=artist.id " +
        //        "LEFT JOIN song_weight ON song.id=song_weight.song_id " +
        //        "WHERE track.album_id=@albumID " +
        //        "ORDER BY track.disc_number ASC, track.track_number ASC;";
        //    readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

        //    using (SQLiteDataReader reader = readTracks.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            long songID = (long)reader["song_id"];

        //            SongDTO newSong = new SongDTO(
        //                songID: songID,
        //                title: _GetPlaylistSongName(
        //                    albumID: albumID,
        //                    songID: songID));

        //            if (reader["weight"].GetType() != typeof(DBNull))
        //            {
        //                newSong.Weight = (double)reader["weight"];
        //            }

        //            RecordingDTO recording = recordingCommands._GetRecording(
        //                recordingID: (long)reader["recording_id"],
        //                albumID: albumID);

        //            if (recording != null)
        //            {
        //                newSong.Children.Add(recording);
        //            }


        //            albumData.Add(newSong);
        //        }
        //    }

        //    dbConnection.Close();

        //    return albumData;
        //}

        //public List<SongDTO> GetAlbumDataDeep(
        //    long albumID)
        //{
        //    List<SongDTO> albumData = new List<SongDTO>();

        //    dbConnection.Open();

        //    SQLiteCommand readTracks = dbConnection.CreateCommand();
        //    readTracks.CommandType = System.Data.CommandType.Text;
        //    readTracks.CommandText =
        //        "SELECT " +
        //            "song.id AS song_id, " +
        //            "song.title AS song_title, " +
        //            "artist.name AS artist_name " +
        //        "FROM track " +
        //        "LEFT JOIN recording ON track.recording_id=recording.id " +
        //        "LEFT JOIN album ON track.album_id=album.id " +
        //        "LEFT JOIN song ON recording.song_id=song.id " +
        //        "LEFT JOIN artist ON recording.artist_id=artist.id " +
        //        "WHERE track.album_id=@albumID ORDER BY track.disc_number ASC, track.track_number ASC;";
        //    readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

        //    using (SQLiteDataReader reader = readTracks.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            long songID = (long)reader["song_id"];

        //            SongDTO newSong = new SongDTO(
        //                songID: songID,
        //                title: songCommands._GetPlaylistSongName(
        //                    songID: songID));

        //            foreach (RecordingDTO recording in recordingCommands._GetRecordingList(
        //                    songID: songID))
        //            {
        //                newSong.Children.Add(recording);
        //            }


        //            albumData.Add(newSong);
        //        }
        //    }

        //    dbConnection.Close();

        //    return albumData;
        //}

        public IList<DeredundafierDTO> GetDeredundancyTargets()
        {
            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();

            dbConnection.Open();

            SQLiteCommand findTargets = dbConnection.CreateCommand();
            findTargets.CommandType = System.Data.CommandType.Text;
            findTargets.CommandText =
                "SELECT title " +
                "FROM album " +
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
            List<long> albumIDsCopy = new List<long>(ids);

            long albumID = albumIDsCopy[0];
            albumIDsCopy.RemoveAt(0);

            dbConnection.Open();

            using (SQLiteTransaction transaction = dbConnection.BeginTransaction())
            {
                _UpdateForeignKeys(
                    transaction: transaction,
                    newAlbumID: albumID,
                    oldAlbumIDs: albumIDsCopy);

                _DeleteAlbumID(
                    transaction: transaction,
                    albumIDs: albumIDsCopy);

                transaction.Commit();
            }

            dbConnection.Close();
        }

        public void UpdateAlbumTitle(
            IEnumerable<long> albumIDs,
            string newAlbumTitle)
        {
            List<long> albumIDsCopy = new List<long>(albumIDs);

            dbConnection.Open();

            //First, find out if the new album exists
            long albumID = _FindAlbum_ByName_MatchArtist(
                albumTitle: newAlbumTitle,
                oldAlbumID: albumIDsCopy[0]);


            using (SQLiteTransaction updateAlbum = dbConnection.BeginTransaction())
            {
                if (albumID == -1)
                {
                    //Update an Album's name, because it doesn't exist

                    //Pop off the front
                    albumID = albumIDsCopy[0];
                    albumIDsCopy.RemoveAt(0);

                    //Update the album formerly in the front
                    _UpdateAlbumTitle_ByAlbumID(
                        transaction: updateAlbum,
                        albumID: albumID,
                        albumTitle: newAlbumTitle);
                }

                if (albumIDsCopy.Count > 0)
                {
                    //For the remaining artists, Remap foreign keys
                    _UpdateForeignKeys(
                        transaction: updateAlbum,
                        newAlbumID: albumID,
                        oldAlbumIDs: albumIDsCopy);

                    //Now, delete any old artists with no remaining recordings
                    _DeleteAllLeafs(
                        transaction: updateAlbum);
                }

                updateAlbum.Commit();
            }

            dbConnection.Close();
        }

        public void UpdateYear(IEnumerable<long> albumIDs, long newYear)
        {
            dbConnection.Open();

            using (SQLiteTransaction updateAlbumYears = dbConnection.BeginTransaction())
            {
                foreach (long albumID in albumIDs)
                {
                    _UpdateAlbumYear(
                        transaction: updateAlbumYears,
                        albumID: albumID,
                        albumYear: newYear);
                }

                updateAlbumYears.Commit();
            }

            dbConnection.Close();
        }


        #endregion High Level Commands
        #region Search Commands

        public long _FindAlbum_ByName_MatchArtist(string albumTitle, long oldAlbumID)
        {
            long albumID = -1;

            SQLiteCommand findArtist = dbConnection.CreateCommand();
            findArtist.CommandType = System.Data.CommandType.Text;
            findArtist.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));
            findArtist.Parameters.Add(new SQLiteParameter("@albumID", oldAlbumID));
            findArtist.CommandText =
                "SELECT album.id " +
                "FROM recording " +
                "LEFT JOIN track ON recording.id=track.recording_id " +
                "LEFT JOIN album ON track.album_id=album.id " +
                "WHERE " +
                    "recording.artist_id IN ( " +
                        "SELECT recording.artist_id " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.id " +
                        "WHERE track.album_id=@albumID ) AND " +
                    "album.title=@albumTitle COLLATE NOCASE " +
                "LIMIT 1;";

            using (SQLiteDataReader reader = findArtist.ExecuteReader())
            {
                if (reader.Read())
                {
                    albumID = (long)reader["id"];
                }
            }

            return albumID;
        }

        public byte[] _GetArt(long albumID)
        {
            byte[] image = null;

            SQLiteCommand getAlbumArt = dbConnection.CreateCommand();
            getAlbumArt.CommandType = System.Data.CommandType.Text;
            getAlbumArt.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            getAlbumArt.CommandText =
                "SELECT image " +
                "FROM album_art " +
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

        private DeredundafierDTO _GetDeredunancyTarget(string albumTitle)
        {
            DeredundafierDTO target = new DeredundafierDTO()
            {
                Name = albumTitle
            };

            SQLiteCommand findOptions = dbConnection.CreateCommand();
            findOptions.CommandType = System.Data.CommandType.Text;
            findOptions.CommandText =
                "SELECT " +
                    "album.id AS id, " +
                    "album.title AS album_title," +
                    "CASE WHEN COUNT(artist.id) > 1 THEN 'Various Artists' " +
                        "ELSE MAX(artist.name) END artist_name " +
                "FROM album " +
                "LEFT JOIN artist ON artist.id IN ( " +
                    "SELECT recording.artist_id " +
                    "FROM track " +
                    "LEFT JOIN recording ON track.recording_id=recording.id " +
                    "WHERE track.album_id=album.id ) " +
                "WHERE album.title=@albumTitle COLLATE NOCASE " +
                "GROUP BY album.id;";
            findOptions.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));

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
                            (string)innerReader["album_title"]),
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

        private IList<DeredundafierDTO> _GetDeredundancyTrackExamples(long albumID)
        {
            List<DeredundafierDTO> examples = new List<DeredundafierDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "recording.id AS recording_id, " +
                    "artist.name || ' - ' || track.title AS title " +
                "FROM track " +
                "LEFT JOIN recording ON track.recording_id=recording.id " +
                "LEFT JOIN artist ON recording.artist_id=artist.id " +
                "WHERE track.album_id=@albumID;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

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
        //    Dictionary<(long, string), long> artistID_AlbumTitleDict,
        //    HashSet<long> albumArt)
        //{
        //    SQLiteCommand loadAlbums = dbConnection.CreateCommand();
        //    loadAlbums.CommandType = System.Data.CommandType.Text;
        //    loadAlbums.CommandText =
        //        "SELECT " +
        //            "album.id AS album_id, " +
        //            "album.title AS title, " +
        //            "artist.id AS artist_id " +
        //        "FROM album " +
        //        "LEFT JOIN artist ON artist.id IN ( " +
        //            "SELECT recording.artist_id " +
        //            "FROM track " +
        //            "LEFT JOIN recording ON track.recording_id=recording.id " +
        //            "WHERE track.album_id=album.id);";

        //    using (SQLiteDataReader reader = loadAlbums.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            long albumID = (long)reader["album_id"];
        //            long artistID = (long)reader["artist_id"];
        //            string albumTitle = (string)reader["title"];

        //            var key = (artistID, albumTitle.ToLowerInvariant());

        //            if (!artistID_AlbumTitleDict.ContainsKey(key))
        //            {
        //                artistID_AlbumTitleDict.Add(key, albumID);
        //            }
        //        }
        //    }

        //    SQLiteCommand loadAlbumArt = dbConnection.CreateCommand();
        //    loadAlbumArt.CommandType = System.Data.CommandType.Text;
        //    loadAlbumArt.CommandText =
        //        "SELECT album_id " +
        //        "FROM album_art; ";

        //    using (SQLiteDataReader reader = loadAlbumArt.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            long albumID = (long)reader["album_id"];

        //            if (!albumArt.Contains(albumID))
        //            {
        //                albumArt.Add(albumID);
        //            }
        //        }
        //    }
        //}

        //#endregion Lookup Commands
        #region Update Commands

        public void _UpdateAlbumYear(
            SQLiteTransaction transaction,
            long albumID,
            long albumYear)
        {
            SQLiteCommand updateAlbumYear = dbConnection.CreateCommand();
            updateAlbumYear.Transaction = transaction;
            updateAlbumYear.CommandType = System.Data.CommandType.Text;
            updateAlbumYear.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            updateAlbumYear.Parameters.Add(new SQLiteParameter("@albumYear", albumYear));
            updateAlbumYear.CommandText =
                "UPDATE album " +
                    "SET year=@albumYear " +
                    "WHERE id=@albumID;";
            updateAlbumYear.ExecuteNonQuery();
        }

        public void _UpdateAlbumTitle_ByAlbumID(
            SQLiteTransaction transaction,
            long albumID,
            string albumTitle)
        {
            SQLiteCommand updateAlbumTitle_ByAlbumID = dbConnection.CreateCommand();
            updateAlbumTitle_ByAlbumID.Transaction = transaction;
            updateAlbumTitle_ByAlbumID.CommandType = System.Data.CommandType.Text;
            updateAlbumTitle_ByAlbumID.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));
            updateAlbumTitle_ByAlbumID.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            updateAlbumTitle_ByAlbumID.CommandText =
                "UPDATE album " +
                    "SET title=@albumTitle " +
                    "WHERE id=@albumID;";

            updateAlbumTitle_ByAlbumID.ExecuteNonQuery();
        }

        public void _UpdateForeignKeys(
            SQLiteTransaction transaction,
            long newAlbumID,
            ICollection<long> oldAlbumIDs)
        {
            trackCommands._RemapAlbumID(
                transaction: transaction,
                newAlbumID: newAlbumID,
                oldAlbumIDs: oldAlbumIDs);
        }

        #endregion Update Commands
        #region Insert Commands

        public long _CreateAlbum(
            SQLiteTransaction transaction,
            string albumTitle,
            long albumYear = 0,
            double albumWeight = double.NaN)
        {
            long albumID = -1;

            SQLiteCommand writeAlbum = dbConnection.CreateCommand();
            writeAlbum.Transaction = transaction;
            writeAlbum.CommandType = System.Data.CommandType.Text;
            writeAlbum.CommandText =
                "INSERT INTO album " +
                    "(id, title, year, weight) VALUES " +
                    "(@albumID, @albumTitle, @albumYear, @albumWeight);";
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumTitle", albumTitle));
            writeAlbum.Parameters.Add(new SQLiteParameter("@albumYear", albumYear));
            writeAlbum.Parameters.Add("@albumWeight", DbType.Double);
            writeAlbum.Parameters["@albumWeight"].Value = double.IsNaN(albumWeight) ? null : (object)albumWeight;
            writeAlbum.ExecuteNonQuery();

            return albumID;
        }

        public void _CreateArt(
            SQLiteTransaction transaction,
            long albumID,
            byte[] imageData)
        {
            long albumArtID = -1;

            SQLiteCommand writeArt = dbConnection.CreateCommand();
            writeArt.Transaction = transaction;
            writeArt.CommandType = System.Data.CommandType.Text;
            writeArt.CommandText =
                "INSERT OR REPLACE INTO album_art " +
                    "(id, album_id, image) VALUES " +
                    "(@albumArtID, @albumID, @image);";
            writeArt.Parameters.Add(new SQLiteParameter("@albumArtID", albumArtID));
            writeArt.Parameters.Add(new SQLiteParameter("@albumID", albumID));
            writeArt.Parameters.Add(new SQLiteParameter("@image", imageData));

            writeArt.ExecuteNonQuery();
        }

        #endregion Insert Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allAlbums = from album in db.Albums select album;
            db.Albums.RemoveRange(allAlbums);
        }

        public void _DeleteAlbumID(
            SQLiteTransaction transaction,
            ICollection<long> albumIDs)
        {
            SQLiteCommand deleteAlbum_ByAlbumID = dbConnection.CreateCommand();
            deleteAlbum_ByAlbumID.Transaction = transaction;
            deleteAlbum_ByAlbumID.CommandType = System.Data.CommandType.Text;
            deleteAlbum_ByAlbumID.Parameters.Add("@albumID", DbType.Int64);
            deleteAlbum_ByAlbumID.CommandText =
                "DELETE FROM album " +
                "WHERE album.id=@albumID;";

            SQLiteCommand deleteAlbumArt_ByAlbumID = dbConnection.CreateCommand();
            deleteAlbumArt_ByAlbumID.Transaction = transaction;
            deleteAlbumArt_ByAlbumID.CommandType = System.Data.CommandType.Text;
            deleteAlbumArt_ByAlbumID.Parameters.Add("@albumID", DbType.Int64);
            deleteAlbumArt_ByAlbumID.CommandText =
                "DELETE FROM album_art " +
                "WHERE album_id=@albumID;";

            foreach (long id in albumIDs)
            {
                deleteAlbum_ByAlbumID.Parameters["@albumID"].Value = id;
                deleteAlbum_ByAlbumID.ExecuteNonQuery();

                deleteAlbumArt_ByAlbumID.Parameters["@albumID"].Value = id;
                deleteAlbumArt_ByAlbumID.ExecuteNonQuery();
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


                    TagLib.File audioFile = TagLib.File.Create(filename);

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

    }
}
