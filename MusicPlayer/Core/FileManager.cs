using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using MusicPlayer.DataStructures;
using MusicPlayer.Library;
using System.Windows;

namespace MusicPlayer
{
    public class FileManager
    {
        private readonly List<string> supportedFileTypes = new List<string>() { "*.mp3" };
        private readonly string[] songNameDelimiter = new string[] { " - " };

        private const string livePattern = @"(\s*?[\(\[][Ll]ive.*?[\)\]])";

        private const string songDBFilename = "SongDB.sqlite";

        private SQLiteConnection dbConnection = null;

        private long lastArtistIDAssigned = 0;
        private long lastSongIDAssigned = 0;
        private long lastAlbumIDAssigned = 0;
        private long lastTrackIDAssigned = 0;
        private long lastRecordingIDAssigned = 0;

        private struct DBRecords
        {
            public List<ArtistData> artists;
            public List<AlbumData> albums;
            public List<SongData> songs;
            public List<RecordingData> recordings;
            public List<TrackData> tracks;

            public bool HasRecords()
            {
                return (artists.Count > 0 ||
                    albums.Count > 0 ||
                    tracks.Count > 0 ||
                    songs.Count > 0 ||
                    recordings.Count > 0);
            }
        }

        private struct DBBuilderLookups
        {
            public Dictionary<string, long> artistName;
            public Dictionary<ValueTuple<long, string>, long> artistID_AlbumTitle;
            public Dictionary<ValueTuple<long, string>, long> artistID_SongTitle;
            public HashSet<string> loadedFilenames;
        }


        public FileManager()
        {
        }

        public void DropDB()
        {
            if (dbConnection != null)
            {
                dbConnection.Close();
                dbConnection = null;
            }

            try
            {
                string dbPath = Path.Combine(FileUtility.GetDataPath(), songDBFilename);
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                Initialize();

            }
            catch (Exception e)
            {
                MessageBox.Show(
                    messageBoxText: string.Format(
                        "There was an error of type ({0}) attempting to delete the database file.\n\n{1}",
                        e.GetType().ToString(),
                        e.Message),
                    caption: "Cannot Delete Database",
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Error);
            }
        }

        public void Initialize()
        {
            string dbPath = Path.Combine(FileUtility.GetDataPath(), songDBFilename);

            bool newDB = false;

            if (!File.Exists(dbPath))
            {
                newDB = true;
                SQLiteConnection.CreateFile(dbPath);
            }

            dbConnection = new SQLiteConnection(String.Format(
                "Data Source=\"{0}\";Version=3;",
                dbPath));

            if (newDB)
            {
                string makeArtistsTable =
                    "CREATE TABLE artist (" +
                        "artist_id INTEGER PRIMARY KEY, " +
                        "artist_name TEXT);";

                string makeAlbumTable =
                    "CREATE TABLE album (" +
                        "album_id INTEGER PRIMARY KEY, " +
                        "artist_id INTEGER REFERENCES artist, " +
                        "album_title TEXT, " +
                        "album_year INTEGER, " +
                        "album_art_filename TEXT);";

                string makeSongTable =
                    "CREATE TABLE song (" +
                        "song_id INTEGER PRIMARY KEY, " +
                        "artist_id INTEGER REFERENCES artist, " +
                        "song_title TEXT);";

                string makeRecordingTable =
                    "CREATE TABLE recording (" +
                        "recording_id INTEGER PRIMARY KEY, " +
                        "filename TEXT, " +
                        "live BOOLEAN);";

                string makeTrackTable =
                    "CREATE TABLE track (" +
                        "track_id INTEGER PRIMARY KEY, " +
                        "song_id INTEGER REFERENCES song, " +
                        "album_id INTEGER REFERENCES album, " +
                        "recording_id INTEGER REFERENCES recording, " +
                        "track_title TEXT, " +
                        "track_number INTEGER);";

                string makeTrackWeightTable =
                    "CREATE TABLE track_weight (" +
                        "track_id INTEGER PRIMARY KEY, " +
                        "weight REAL);";

                string makeSongWeightTable =
                    "CREATE TABLE song_weight (" +
                        "song_id INTEGER PRIMARY KEY, " +
                        "weight REAL);";

                string makeAlbumWeightTable =
                    "CREATE TABLE album_weight (" +
                        "album_id INTEGER PRIMARY KEY, " +
                        "weight REAL);";

                string makeArtistWeightTable =
                    "CREATE TABLE artist_weight (" +
                        "artist_id INTEGER PRIMARY KEY, " +
                        "weight REAL);";

                string makeTrackSongIDIndex =
                    "CREATE INDEX idx_track_songid ON track (song_id);";

                dbConnection.Open();
                //Set Up Tables
                new SQLiteCommand(makeArtistsTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeAlbumTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeSongTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeRecordingTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeTrackTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeTrackWeightTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeSongWeightTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeAlbumWeightTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeArtistWeightTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeTrackSongIDIndex, dbConnection).ExecuteNonQuery();

                dbConnection.Close();
            }
        }

        private void LoadLibraryDictionaries(DBBuilderLookups lookups)
        {
            dbConnection.Open();

            //Load Artists
            {
                string getArtists = "SELECT * FROM artist;";
                using (SQLiteDataReader reader = new SQLiteCommand(getArtists, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string artistName = (string)reader["artist_name"];
                        long artistID = (long)reader["artist_id"];

                        if (!lookups.artistName.ContainsKey(artistName.ToLowerInvariant()))
                        {
                            lookups.artistName.Add(artistName.ToLowerInvariant(), artistID);
                        }
                        else
                        {
                            Console.WriteLine(
                                "Warning, duplicated artist name.  Possible error: " + artistName.ToLowerInvariant());
                        }

                        if (artistID > lastArtistIDAssigned)
                        {
                            lastArtistIDAssigned = artistID;
                        }
                    }
                }
            }

            //Load Albums
            {
                string getAlbums = "SELECT * FROM album;";
                using (SQLiteDataReader reader = new SQLiteCommand(getAlbums, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long albumID = (long)reader["album_id"];
                        long artistID = (long)reader["artist_id"];
                        string albumTitle = (string)reader["album_title"];

                        lookups.artistID_AlbumTitle.Add((artistID, albumTitle.ToLowerInvariant()), albumID);

                        if (albumID > lastAlbumIDAssigned)
                        {
                            lastAlbumIDAssigned = albumID;
                        }
                    }
                }
            }

            //Load Songs
            {
                string getSongs = "SELECT * FROM song;";
                using (SQLiteDataReader reader = new SQLiteCommand(getSongs, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long songID = (long)reader["song_id"];
                        string songTitle = (string)reader["song_title"];
                        long artistID = (long)reader["artist_id"];

                        if (!lookups.artistID_SongTitle.ContainsKey((artistID, songTitle.ToLowerInvariant())))
                        {
                            lookups.artistID_SongTitle.Add((artistID, songTitle.ToLowerInvariant()), songID);
                        }

                        if (songID > lastSongIDAssigned)
                        {
                            lastSongIDAssigned = songID;
                        }
                    }
                }
            }

            //Load Recordings
            {
                string getRecordings = "SELECT * FROM recording;";
                using (SQLiteDataReader reader = new SQLiteCommand(getRecordings, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long recordingID = (long)reader["recording_id"];
                        string filename = (string)reader["filename"];
                        bool valid = File.Exists(filename);

                        lookups.loadedFilenames.Add(filename);

                        if (recordingID > lastRecordingIDAssigned)
                        {
                            lastRecordingIDAssigned = recordingID;
                        }
                    }
                }
            }

            //Load Tracks
            {
                string getTracks = "SELECT * FROM track;";
                using (SQLiteDataReader reader = new SQLiteCommand(getTracks, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long trackID = (long)reader["track_id"];

                        if (trackID > lastTrackIDAssigned)
                        {
                            lastTrackIDAssigned = trackID;
                        }
                    }
                }
            }

            dbConnection.Close();
        }

        public void AddMusicDirectory(string path, List<string> newMusic, HashSet<string> loadedFilenames)
        {
            foreach (string extension in supportedFileTypes)
            {
                newMusic.AddRange(
                     from filename
                     in Directory.GetFiles(path, extension)
                     where !loadedFilenames.Contains(filename)
                     select filename);
            }

            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                AddMusicDirectory(subDirectory, newMusic, loadedFilenames);
            }
        }

        public void AddDirectoryToLibrary(string path)
        {
            List<string> newMusic = new List<string>();

            DBBuilderLookups lookups = new DBBuilderLookups()
            {
                artistName = new Dictionary<string, long>(),
                artistID_AlbumTitle = new Dictionary<ValueTuple<long, string>, long>(),
                artistID_SongTitle = new Dictionary<ValueTuple<long, string>, long>(),
                loadedFilenames = new HashSet<string>()
            };

            DBRecords newRecords = new DBRecords()
            {
                albums = new List<AlbumData>(),
                artists = new List<ArtistData>(),
                songs = new List<SongData>(),
                tracks = new List<TrackData>(),
                recordings = new List<RecordingData>()
            };


            LoadLibraryDictionaries(lookups);

            AddMusicDirectory(path, newMusic, lookups.loadedFilenames);

            foreach (string songFilename in newMusic)
            {
                LoadFileData(
                    path: songFilename,
                    lookups: lookups,
                    newRecords: newRecords);
            }

            if (newRecords.HasRecords())
            {
                dbConnection.Open();

                //Add Artists
                using (SQLiteTransaction writeArtistsTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeArtist = dbConnection.CreateCommand();
                    writeArtist.Transaction = writeArtistsTransaction;
                    writeArtist.CommandType = System.Data.CommandType.Text;
                    writeArtist.CommandText = "INSERT INTO artist " +
                        "(artist_id, artist_name) VALUES " +
                        "(@artistID, @artistName);";
                    writeArtist.Parameters.Add(new SQLiteParameter("@artistID", -1));
                    writeArtist.Parameters.Add(new SQLiteParameter("@artistName", ""));

                    foreach (ArtistData artist in newRecords.artists)
                    {
                        writeArtist.Parameters["@artistID"].Value = artist.artistID;
                        writeArtist.Parameters["@artistName"].Value = artist.artistName;
                        writeArtist.ExecuteNonQuery();
                    }

                    writeArtistsTransaction.Commit();
                }

                //Add Albums
                using (SQLiteTransaction writeAlbumsTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeAlbum = dbConnection.CreateCommand();
                    writeAlbum.Transaction = writeAlbumsTransaction;
                    writeAlbum.CommandType = System.Data.CommandType.Text;
                    writeAlbum.CommandText = "INSERT INTO album " +
                        "(album_id, artist_id, album_title, album_year, album_art_filename) VALUES " +
                        "(@albumID, @artistID, @albumTitle, @albumYear, @albumArtFilename);";
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumID", -1));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@artistID", -1));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumTitle", ""));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumYear", 0));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumArtFilename", ""));

                    foreach (AlbumData album in newRecords.albums)
                    {
                        writeAlbum.Parameters["@albumID"].Value = album.albumID;
                        writeAlbum.Parameters["@artistID"].Value = album.artistID;
                        writeAlbum.Parameters["@albumTitle"].Value = album.albumTitle;
                        writeAlbum.Parameters["@albumYear"].Value = album.albumYear;
                        writeAlbum.Parameters["@albumArtFilename"].Value = album.albumArtFilename;
                        writeAlbum.ExecuteNonQuery();
                    }

                    writeAlbumsTransaction.Commit();
                }

                //Add Tracks
                using (SQLiteTransaction writeTracksTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeTrack = dbConnection.CreateCommand();
                    writeTrack.Transaction = writeTracksTransaction;
                    writeTrack.CommandType = System.Data.CommandType.Text;
                    writeTrack.CommandText = "INSERT INTO track " +
                        "(track_id, song_id, album_id, recording_id, track_title, track_number) VALUES " +
                        "(@trackID, @songID, @albumID, @recordingID, @trackTitle, @trackNumber);";
                    writeTrack.Parameters.Add(new SQLiteParameter("@trackID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@songID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@albumID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@recordingID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@trackTitle", ""));
                    writeTrack.Parameters.Add(new SQLiteParameter("@trackNumber", -1));

                    foreach (TrackData track in newRecords.tracks)
                    {
                        writeTrack.Parameters["@trackID"].Value = track.trackID;
                        writeTrack.Parameters["@songID"].Value = track.songID;
                        writeTrack.Parameters["@albumID"].Value = track.albumID;
                        writeTrack.Parameters["@recordingID"].Value = track.recordingID;
                        writeTrack.Parameters["@trackTitle"].Value = track.trackTitle;
                        writeTrack.Parameters["@trackNumber"].Value = track.trackNumber;
                        writeTrack.ExecuteNonQuery();
                    }

                    writeTracksTransaction.Commit();
                }

                //Add Songs
                using (SQLiteTransaction writeSongsTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeSong = dbConnection.CreateCommand();
                    writeSong.Transaction = writeSongsTransaction;
                    writeSong.CommandType = System.Data.CommandType.Text;
                    writeSong.CommandText = "INSERT INTO song " +
                        "(song_id, artist_id, song_title) VALUES " +
                        "(@songID, @artistID, @songTitle);";
                    writeSong.Parameters.Add(new SQLiteParameter("@songID", -1));
                    writeSong.Parameters.Add(new SQLiteParameter("@artistID", -1));
                    writeSong.Parameters.Add(new SQLiteParameter("@songTitle", ""));
                    foreach (SongData song in newRecords.songs)
                    {
                        writeSong.Parameters["@songID"].Value = song.songID;
                        writeSong.Parameters["@artistID"].Value = song.artistID;
                        writeSong.Parameters["@songTitle"].Value = song.songTitle;
                        writeSong.ExecuteNonQuery();
                    }

                    writeSongsTransaction.Commit();
                }

                // Add Recordings
                using (SQLiteTransaction writeRecordingsTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeRecordings = dbConnection.CreateCommand();
                    writeRecordings.Transaction = writeRecordingsTransaction;
                    writeRecordings.CommandType = System.Data.CommandType.Text;
                    writeRecordings.CommandText = "INSERT INTO recording " +
                        "(recording_id, filename, live) VALUES " +
                        "(@recordingID, @filename, @live);";
                    writeRecordings.Parameters.Add(new SQLiteParameter("@recordingID", -1));
                    writeRecordings.Parameters.Add(new SQLiteParameter("@filename", ""));
                    writeRecordings.Parameters.Add(new SQLiteParameter("@live", false));

                    foreach (RecordingData recording in newRecords.recordings)
                    {
                        writeRecordings.Parameters["@recordingID"].Value = recording.recordingID;
                        writeRecordings.Parameters["@filename"].Value = recording.filename;
                        writeRecordings.Parameters["@live"].Value = recording.live;
                        writeRecordings.ExecuteNonQuery();
                    }

                    writeRecordingsTransaction.Commit();
                }

                dbConnection.Close();
            }
        }

        private void LoadFileData(
            string path,
            DBBuilderLookups lookups,
            DBRecords newRecords)
        {
            TagLib.File file = null;

            try
            {
                file = TagLib.File.Create(path);
            }
            catch (TagLib.UnsupportedFormatException)
            {
                Console.WriteLine("UNSUPPORTED FILE: " + path);
                Console.WriteLine(String.Empty);
                Console.WriteLine("---------------------------------------");
                Console.WriteLine(String.Empty);
                return;
            }

            var musicFile = file as TagLib.Mpeg.AudioFile;
            if (musicFile == null)
            {
                Console.WriteLine("NOT AN MPEG FILE: " + path);
                Console.WriteLine(String.Empty);
                Console.WriteLine("---------------------------------------");
                Console.WriteLine(String.Empty);
                return;
            }

            //Handle Artist
            string artistName = "UNDEFINED";
            if (!string.IsNullOrEmpty(musicFile.Tag.JoinedPerformers))
            {
                artistName = musicFile.Tag.JoinedPerformers;
            }

            long artistID = -1;
            if (!lookups.artistName.ContainsKey(artistName))
            {
                artistID = ++lastArtistIDAssigned;

                lookups.artistName.Add(artistName, artistID);
                newRecords.artists.Add(new ArtistData()
                {
                    artistID = artistID,
                    artistName = artistName
                });
            }
            else
            {
                artistID = lookups.artistName[artistName];
            }

            string songTitle = "UNDEFINED";
            if (!string.IsNullOrEmpty(musicFile.Tag.Title))
            {
                songTitle = musicFile.Tag.Title;
            }
            else
            {
                string[] nameParts =
                    Path.GetFileNameWithoutExtension(path)
                    .Split(songNameDelimiter, StringSplitOptions.RemoveEmptyEntries);

                if (nameParts.Length == 2)
                {
                    // Presumed "${Artist} - ${Title}"
                    songTitle = nameParts[1].Trim(' ');
                }
                else if (nameParts.Length == 3)
                {
                    // Presumed "${Artist} - ${Album} - ${Title}" or
                    //          "${Album} - ${Number} - ${Title}" ??
                    songTitle = nameParts[2].Trim(' ');
                }
                else
                {
                    //Who knows?  just use path
                    songTitle = Path.GetFileNameWithoutExtension(path);
                }
            }

            string albumTitle = "UNDEFINED";
            if (!string.IsNullOrEmpty(musicFile.Tag.Album))
            {
                albumTitle = musicFile.Tag.Album;
            }

            //Copy the track title before we gut it
            string trackTitle = songTitle;

            bool live = false;
            if (Regex.IsMatch(songTitle, livePattern))
            {
                live = true;
                songTitle = Regex.Replace(songTitle, livePattern, "");
            }

            if (Regex.IsMatch(albumTitle, livePattern))
            {
                live = true;
                albumTitle = Regex.Replace(albumTitle, livePattern, "");
            }

            long songID = -1;
            var songLookupKey = (artistID, songTitle.ToLowerInvariant());
            if (!lookups.artistID_SongTitle.ContainsKey(songLookupKey))
            {
                songID = ++lastSongIDAssigned;

                lookups.artistID_SongTitle.Add(songLookupKey, songID);
                newRecords.songs.Add(new SongData()
                {
                    songID = songID,
                    artistID = artistID,
                    songTitle = songTitle
                });
            }
            else
            {
                songID = lookups.artistID_SongTitle[songLookupKey];
            }

            long albumID = -1;
            var albumTuple = (artistID, albumTitle.ToLowerInvariant());
            if (lookups.artistID_AlbumTitle.ContainsKey(albumTuple))
            {
                albumID = lookups.artistID_AlbumTitle[albumTuple];
            }
            else
            {
                albumID = ++lastAlbumIDAssigned;

                lookups.artistID_AlbumTitle.Add(albumTuple, albumID);
                newRecords.albums.Add(new AlbumData()
                {
                    albumID = albumID,
                    artistID = artistID,
                    albumTitle = albumTitle,
                    albumYear = musicFile.Tag.Year,
                    albumArtFilename = ""
                });
            }

            long recordingID = ++lastRecordingIDAssigned;
            newRecords.recordings.Add(new RecordingData
            {
                recordingID = recordingID,
                filename = path,
                live = live,
                valid = true
            });

            long trackID = ++lastTrackIDAssigned;
            newRecords.tracks.Add(new TrackData
            {
                trackID = trackID,
                songID = songID,
                albumID = albumID,
                recordingID = recordingID,
                trackTitle = trackTitle,
                trackNumber = musicFile.Tag.Track
            });
        }

        public List<ArtistDTO> GenerateArtistList()
        {
            List<ArtistDTO> artistList = new List<ArtistDTO>();

            dbConnection.Open();

            SQLiteCommand readArtists = dbConnection.CreateCommand();
            readArtists.CommandType = System.Data.CommandType.Text;
            readArtists.CommandText =
                "SELECT artist_id, artist_name " +
                "FROM artist ORDER BY artist_name ASC;";
            using (SQLiteDataReader reader = readArtists.ExecuteReader())
            {
                while (reader.Read())
                {
                    artistList.Add(GenerateArtist(
                        artistID: (long)reader["artist_id"],
                        artistName: (string)reader["artist_name"]));
                }
            }

            dbConnection.Close();

            return artistList;
        }

        private ArtistDTO GenerateArtist(long artistID, string artistName)
        {
            List<AlbumDTO> albumList = new List<AlbumDTO>();

            SQLiteCommand readAlbums = dbConnection.CreateCommand();
            readAlbums.CommandType = System.Data.CommandType.Text;
            readAlbums.CommandText =
                "SELECT album_id, album_title, album_year " +
                "FROM album " +
                "WHERE artist_id=@artistID ORDER BY album_year ASC;";
            readAlbums.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readAlbums.ExecuteReader())
            {
                while (reader.Read())
                {
                    string albumName = (string)reader["album_title"];
                    long albumYear = (long)reader["album_year"];

                    albumList.Add(GenerateAlbum(
                        albumID: (long)reader["album_id"],
                        albumTitle: String.Format("{0} ({1})", albumName, albumYear)));
                }
            }

            return new ArtistDTO(artistID, artistName, albumList);
        }

        private AlbumDTO GenerateAlbum(long albumID, string albumTitle)
        {
            List<SongDTO> songList = new List<SongDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "track.song_id AS song_id, " +
                    "song.song_title AS song_title, " +
                    "track.track_number AS track_number " +
                "FROM track " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "LEFT JOIN song ON track.song_id=song.song_id " +
                "WHERE track.album_id=@albumID ORDER BY track.track_number ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    songList.Add(GenerateSong(
                        songID: (long)reader["song_id"],
                        songTitle: String.Format(
                            "{0}. {1}",
                            ((long)reader["track_number"]).ToString("D2"),
                            (string)reader["song_title"]),
                        albumID: albumID));
                }
            }

            return new AlbumDTO(albumID, albumTitle, songList);
        }

        private SongDTO GenerateSong(long songID, string songTitle, long albumID)
        {
            List<RecordingDTO> recordingList = new List<RecordingDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "track.track_id AS track_id, " +
                    "track.track_title AS track_title, " +
                    "track.album_id AS album_id, " +
                    "track.recording_id AS recording_id, " +
                    "recording.live AS live, " +
                    "track_weight.weight AS weight, " +
                    "album.album_title AS album_title, " +
                    "artist.artist_name AS artist_name " +
                "FROM track " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "LEFT JOIN artist ON album.artist_id=artist.artist_id " +
                "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                "LEFT JOIN track_weight ON track.track_id=track_weight.track_id " +
                "WHERE track.song_id=@songID ORDER BY recording.live ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            long trackID = -1;
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
                    if(albumID == (long)reader["album_id"])
                    {
                        albumMatch = true;
                        trackID = (long)reader["track_id"];
                    }

                    recordingList.Add(new RecordingDTO
                    {
                        RecordingID = (long)reader["recording_id"],
                        Title = string.Format(
                            "{0} - {1} - {2}",
                            (string)reader["artist_name"],
                            (string)reader["album_title"],
                            (string)reader["track_title"]),
                        IsHome = albumMatch,
                        Live = (bool)reader["live"],
                        Weight = weight
                    });
                }
            }

            return new SongDTO(songID, trackID, songTitle, recordingList);
        }

        private List<Playlist.RecordingDTO> GetRecordingList(long songID, long overrideRecordingID = -1)
        {
            List<Playlist.RecordingDTO> recordingData = new List<Playlist.RecordingDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "recording.recording_id AS recording_id, " +
                    "track.track_title AS track_title, " +
                    "track_weight.weight AS weight, " +
                    "recording.live AS live, " +
                    "album.album_title AS album_title, " +
                    "artist.artist_name AS artist_name " +
                "FROM recording " +
                "LEFT JOIN track ON recording.recording_id=track.recording_id " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "LEFT JOIN artist ON album.artist_id=artist.artist_id " +
                "LEFT JOIN track_weight ON track.track_id=track_weight.track_id " +
                "WHERE track.song_id=@songID;";

            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long recordingID = (long)reader["recording_id"];
                    double weight = double.NaN;

                    if (overrideRecordingID != -1)
                    {
                        weight = (recordingID == overrideRecordingID) ? 1.0 : 0.0;
                    }
                    else
                    {
                        if (reader["weight"].GetType() != typeof(DBNull))
                        {
                            weight = (double)reader["weight"];
                        }
                    }

                    recordingData.Add(new Playlist.RecordingDTO()
                    {
                        Title = string.Format(
                            "{0} - {1} - {2}",
                            (string)reader["artist_name"],
                            (string)reader["album_title"],
                            (string)reader["track_title"]),
                        RecordingID = recordingID,
                        Weight = weight,
                        Live = (bool)reader["Live"]
                    });
                }
            }

            return recordingData;
        }

        public List<Playlist.SongDTO> GetSongDataFromRecordingID(long recordingID)
        {
            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT track.song_id AS song_id " +
                "FROM track " +
                "WHERE track.recording_id=@recordingID " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            long songID = -1;

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    songID = (long)reader["song_id"];
                }
            }

            dbConnection.Close();

            if (songID == -1)
            {
                return null;
            }

            return GetSongData(songID, recordingID);
        }

        public List<Playlist.SongDTO> GetSongData(long songID, long overrideRecordingID = -1)
        {
            List<Playlist.SongDTO> songData = new List<Playlist.SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT song.song_title AS song_title, artist.artist_name AS artist_name " +
                "FROM song " +
                "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                "WHERE song.song_id=@songID " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    songData.Add(new Playlist.SongDTO(
                        id: songID,
                        title: string.Format("{0} - {1}",
                            (string)reader["artist_name"], (string)reader["song_title"]),
                        recordings: GetRecordingList(songID, overrideRecordingID)));
                }
            }

            dbConnection.Close();

            return songData;
        }

        public List<Playlist.SongDTO> GetAlbumData(long albumID)
        {
            List<Playlist.SongDTO> albumData = new List<Playlist.SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT song.song_id AS song_id, song.song_title AS song_title, artist.artist_name AS artist_name " +
                "FROM track " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "LEFT JOIN song ON track.song_id=song.song_id " +
                "LEFT JOIN artist ON album.artist_id=artist.artist_id " +
                "WHERE track.album_id=@albumID ORDER BY track.track_number ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];

                    albumData.Add(new Playlist.SongDTO(
                        id: songID,
                        title: string.Format("{0} - {1}",
                            (string)reader["artist_name"], (string)reader["song_title"]),
                        recordings: GetRecordingList(songID)));
                }
            }

            dbConnection.Close();

            return albumData;
        }

        public List<Playlist.SongDTO> GetArtistData(long artistID)
        {
            List<Playlist.SongDTO> artistData = new List<Playlist.SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT song.song_id AS song_id, song.song_title AS song_title, artist.artist_name AS artist_name " +
                "FROM song " +
                "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                "WHERE song.artist_id=@artistID ORDER BY song.song_title ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];

                    artistData.Add(new Playlist.SongDTO(
                        id: songID,
                        title: string.Format("{0} - {1}",
                            (string)reader["artist_name"], (string)reader["song_title"]),
                        recordings: GetRecordingList(songID)));
                }
            }

            dbConnection.Close();

            return artistData;
        }

        public PlayData GetSongPlayData(long songID)
        {
            PlayData playData = new PlayData();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "track.track_title AS track_title, " +
                    "artist.artist_name AS artist_name, " +
                    "recording.filename AS filename " +
                "FROM track " +
                "LEFT JOIN song ON track.song_id=song.song_id " +
                "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                "WHERE track.song_id=@songID ORDER BY recording.live " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@songID", songID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    playData.songTitle = (string)reader["track_title"];
                    playData.artistName = (string)reader["artist_name"];
                    playData.filename = (string)reader["filename"];
                }
            }

            dbConnection.Close();

            return playData;
        }

        public PlayData GetTrackPlayData(long trackID)
        {
            PlayData playData = new PlayData();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT track.track_title AS track_title, artist.artist_name AS artist_name, recording.filename AS filename " +
                "FROM track " +
                "LEFT JOIN song ON track.song_id=song.song_id " +
                "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                "WHERE track.track_id=@trackID " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@trackID", trackID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    playData.songTitle = (string)reader["track_title"];
                    playData.artistName = (string)reader["artist_name"];
                    playData.filename = (string)reader["filename"];
                }
            }

            dbConnection.Close();

            return playData;
        }

        public PlayData GetRecordingPlayData(long recordingID)
        {
            PlayData playData = new PlayData();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "track.track_title AS track_title, " +
                    "artist.artist_name AS artist_name, " +
                    "recording.filename AS filename " +
                "FROM recording " +
                "LEFT JOIN track ON recording.recording_id=track.recording_id " +
                "LEFT JOIN song ON track.song_id=song.song_id " +
                "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                "WHERE recording.recording_id=@recordingID " +
                "LIMIT 1;";
            readTracks.Parameters.Add(new SQLiteParameter("@recordingID", recordingID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    playData.songTitle = (string)reader["track_title"];
                    playData.artistName = (string)reader["artist_name"];
                    playData.filename = (string)reader["filename"];
                }
            }

            dbConnection.Close();

            return playData;
        }

        public List<TagData> GetTagData(LibraryContext context, long id)
        {
            List<TagData> tagList = new List<TagData>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.Parameters.Add(new SQLiteParameter("@ID", id));

            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "artist_name " +
                            "FROM artist " +
                            "WHERE artist_id=@ID " +
                            "LIMIT 1;";
                    }
                    break;
                case LibraryContext.Album:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "artist.artist_name AS artist_name, " +
                                "album.album_title AS album_title, " +
                                "album.album_year AS album_year " +
                            "FROM album " +
                            "LEFT JOIN artist ON album.artist_id=artist.artist_id " +
                            "WHERE album.album_id=@ID " +
                            "LIMIT 1;";
                    }
                    break;
                case LibraryContext.Song:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.song_title AS song_title, " +
                                "artist.artist_name AS artist_name, " +
                                "album.album_title AS album_title, " +
                                "album.album_year AS album_year, " +
                            "FROM song " +
                            "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                            "LEFT JOIN track ON song.song_id=track.song_id " +
                            "LEFT JOIN album ON track.album_id=album.album_id " +
                            "WHERE song.song_id=@ID " +
                            "LIMIT 1;";
                    }
                    break;
                case LibraryContext.Track:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.song_title AS song_title, " +
                                "artist.artist_name AS artist_name, " +
                                "album.album_title AS album_title, " +
                                "album.album_year AS album_year, " +
                                "track.track_title AS track_title, " +
                                "track.track_number AS track_number, " +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM track " +
                            "LEFT JOIN song ON track.song_id=song.song_id " +
                            "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                            "LEFT JOIN album ON track.album_id=album.album_id " +
                            "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                            "WHERE track.track_id=@ID " +
                            "LIMIT 1;";
                    }
                    break;
                case LibraryContext.Recording:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.song_title AS song_title, " +
                                "track.track_title AS track_title, " +
                                "track.track_number AS track_number, " +
                                "artist.artist_name AS artist_name, " +
                                "album.album_title AS album_title, " +
                                "album.album_year AS album_year, " +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM recording " +
                            "LEFT JOIN track ON recording.recording_id=track.recording_id " +
                            "LEFT JOIN song ON track.song_id=song.song_id " +
                            "LEFT JOIN artist ON song.artist_id=artist.artist_id " +
                            "LEFT JOIN album ON track.album_id=album.album_id " +
                            "WHERE recording.recording_id=@ID " +
                            "LIMIT 1;";
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    dbConnection.Close();
                    throw new Exception("Unexpected LibraryContext: " + context);
            }

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagViewable
                        {
                            _CurrentValue = (string)reader["filename"],
                            recordType = TagEditor.MusicRecord.Filename
                        });

                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["track_title"],
                            NewValue = (string)reader["track_title"],
                            recordType = TagEditor.MusicRecord.TrackTitle,
                            tagType = TagEditor.ID3TagType.Title
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["track_number"],
                            _newValue = (long)reader["track_number"],
                            recordType = TagEditor.MusicRecord.TrackNumber,
                            tagType = TagEditor.ID3TagType.Track
                        });

                        tagList.Add(new TagDataBool()
                        {
                            _currentValue = (bool)reader["live"],
                            NewValue = (bool)reader["live"],
                            recordType = TagEditor.MusicRecord.Live
                        });
                    }

                    if (context == LibraryContext.Song ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["song_title"],
                            NewValue = (string)reader["song_title"],
                            recordType = TagEditor.MusicRecord.SongTitle
                        });
                    }

                    if (context == LibraryContext.Album ||
                        context == LibraryContext.Song ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["album_title"],
                            NewValue = (string)reader["album_title"],
                            recordType = TagEditor.MusicRecord.AlbumTitle,
                            tagType = TagEditor.ID3TagType.Album
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["album_year"],
                            _newValue = (long)reader["album_year"],
                            recordType = TagEditor.MusicRecord.AlbumYear,
                            tagType = TagEditor.ID3TagType.Year
                        });
                    }

                    if (context == LibraryContext.Artist ||
                        context == LibraryContext.Album ||
                        context == LibraryContext.Song ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["artist_name"],
                            NewValue = (string)reader["artist_name"],
                            recordType = TagEditor.MusicRecord.ArtistName,
                            tagType = TagEditor.ID3TagType.Performer,
                            tagTypeIndex = 0
                        });
                    }
                }
            }

            dbConnection.Close();

            return tagList;
        }

        public List<string> GetAffectedFiles(LibraryContext context, long id)
        {
            List<string> affectedFiles = new List<string>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.Parameters.Add(new SQLiteParameter("@ID", id));

            switch (context)
            {
                case LibraryContext.Artist:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM song " +
                        "LEFT JOIN track ON song.song_id=track.song_id " +
                        "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                        "WHERE song.artist_id=@ID;";
                    break;
                case LibraryContext.Album:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                        "WHERE track.album_id=@ID;";
                    break;
                case LibraryContext.Song:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                        "WHERE track.song_id=@ID;";
                    break;
                case LibraryContext.Track:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                        "WHERE track.track_id=@ID;";
                    break;
                case LibraryContext.Recording:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM recording " +
                        "WHERE recording.recording_id=@ID;";
                    break;
                case LibraryContext.MAX:
                default:
                    dbConnection.Close();
                    throw new Exception("Unexpected LibraryContext: " + context);
            }


            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    affectedFiles.Add((string)reader["filename"]);
                }
            }

            dbConnection.Close();
            
            return affectedFiles;
        }
    }
}
