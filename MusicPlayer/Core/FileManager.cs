using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using MusicPlayer.DataStructures;

namespace MusicPlayer
{
    class FileManager
    {
        private List<string> supportedFileTypes = new List<string>() { "*.mp3" };
        private List<string> foundMusic = new List<string>();

        private const string songDBFilename = "SongDB.sqlite";

        private SQLiteConnection dbConnection = null;

        private Dictionary<int, ArtistData> artistDict = new Dictionary<int, ArtistData>();
        private Dictionary<string, int> artistReverseLookupDict = new Dictionary<string, int>();

        private Dictionary<int, SongData> songDict = new Dictionary<int, SongData>();
        private Dictionary<string, int> songFilenameReverseLookupDict = new Dictionary<string, int>();

        private Dictionary<int, AlbumData> albumDict = new Dictionary<int, AlbumData>();
        private Dictionary<Tuple<int, string>, int> albumTitleReverseLookupDict = new Dictionary<Tuple<int, string>, int>();

        private Dictionary<int, TrackData> trackDict = new Dictionary<int, TrackData>();

        private int lastArtistIDAssigned = 0;
        private int lastSongIDAssigned = 0;
        private int lastAlbumIDAssigned = 0;
        private int lastTrackIDAssigned = 0;

        private List<ArtistData> pendingArtistAdditions = new List<ArtistData>();
        private List<AlbumData> pendingAlbumAdditions = new List<AlbumData>();
        private List<TrackData> pendingTrackAdditions = new List<TrackData>();
        private List<SongData> pendingSongAdditions = new List<SongData>();

        public List<ArtistDTO> artistList = new List<ArtistDTO>();

        public FileManager()
        {
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
                        "album_name TEXT, " +
                        "album_year TEXT);";

                string makeSongTable =
                    "CREATE TABLE song (" +
                        "song_id INTEGER PRIMARY KEY, " +
                        "artist_id INTEGER REFERENCES artist, " +
                        "song_filename TEXT, " +
                        "song_title TEXT, " +
                        "live BOOLEAN);";

                string makeTrackTable =
                    "CREATE TABLE track (" +
                        "track_id INTEGER PRIMARY KEY, " +
                        "song_id INTEGER REFERENCES song, " +
                        "album_id INTEGER REFERENCES album, " +
                        "track_number INTEGER);";

                dbConnection.Open();
                //Set Up Tables
                new SQLiteCommand(makeArtistsTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeAlbumTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeSongTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeTrackTable, dbConnection).ExecuteNonQuery();

                dbConnection.Close();
            }
            else
            {
                //Load Artists
                string getArtists = "SELECT * FROM artist ORDER BY artist_id ASC;";

                artistDict.Clear();
                artistReverseLookupDict.Clear();

                dbConnection.Open();

                using (SQLiteDataReader reader = new SQLiteCommand(getArtists, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {

                        string artistName = (string)reader["artist_name"];
                        int artistID = (int)(long)reader["artist_id"];

                        artistDict.Add(artistID,
                            new ArtistData()
                            {
                                artistID = artistID,
                                artistName = artistName
                            });

                        if (!artistReverseLookupDict.ContainsKey(artistName))
                        {
                            artistReverseLookupDict.Add(artistName, artistID);
                        }
                        else
                        {
                            Console.WriteLine(
                                "Warning, duplicated artist name.  Possible error: " + artistName);
                        }

                        if (artistID > lastArtistIDAssigned)
                        {
                            lastArtistIDAssigned = artistID;
                        }
                    }
                }

                //Load Albums
                string getAlbums = "SELECT * FROM album;";

                albumDict.Clear();
                albumTitleReverseLookupDict.Clear();

                using (SQLiteDataReader reader = new SQLiteCommand(getAlbums, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int albumID = (int)(long)reader["album_id"];
                        int artistID = (int)(long)reader["artist_id"];
                        string albumTitle = (string)reader["album_name"];

                        albumDict.Add(albumID,
                            new AlbumData()
                            {
                                albumID = albumID,
                                artistID = artistID,
                                albumTitle = albumTitle,
                                albumYear = (string)reader["album_year"]
                            });

                        albumTitleReverseLookupDict.Add(new Tuple<int, string>(artistID, albumTitle), albumID);

                        if (albumID > lastAlbumIDAssigned)
                        {
                            lastAlbumIDAssigned = albumID;
                        }
                    }
                }

                //Load Songs
                string getSongs = "SELECT * FROM song ORDER BY song_id ASC;";

                songFilenameReverseLookupDict.Clear();
                songDict.Clear();

                using (SQLiteDataReader reader = new SQLiteCommand(getSongs, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int songID = (int)(long)reader["song_id"];
                        string fileName = (string)reader["song_filename"];
                        bool valid = File.Exists(fileName);

                        songDict.Add(songID,
                            new SongData()
                            {
                                songID = songID,
                                artistID = (int)(long)reader["artist_id"],
                                fileName = fileName,
                                songTitle = (string)reader["song_title"],
                                live = (bool)reader["live"],
                                valid = valid
                            });

                        songFilenameReverseLookupDict.Add(fileName, songID);

                        if (songID > lastSongIDAssigned)
                        {
                            lastSongIDAssigned = songID;
                        }
                    }
                }

                //Load up Tracks
                string getTracks = "SELECT * FROM track;";

                trackDict.Clear();

                using (SQLiteDataReader reader = new SQLiteCommand(getTracks, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int trackID = (int)(long)reader["track_id"];

                        trackDict.Add(trackID,
                            new TrackData()
                            {
                                trackID = trackID,
                                songID = (int)(long)reader["song_id"],
                                albumID = (int)(long)reader["album_id"],
                                trackNumber = (int)(long)reader["track_number"]
                            });

                        if (trackID > lastTrackIDAssigned)
                        {
                            lastTrackIDAssigned = trackID;
                        }
                    }
                }

                dbConnection.Close();
            }
        }

        //Allow folders 2 or 3 deep
        public void OpenDirectory(string path)
        {
            foundMusic = new List<string>();
            foreach (string extension in supportedFileTypes)
            {
                foundMusic.AddRange(Directory.GetFiles(path, extension));
            }

            foreach (string bandDirectory in Directory.GetDirectories(path))
            {
                string bandPath = Path.Combine(path, bandDirectory);
                foreach (string extension in supportedFileTypes)
                {
                    foundMusic.AddRange(Directory.GetFiles(bandPath, extension));
                }

                foreach (string albumDirectory in Directory.GetDirectories(bandPath))
                {
                    string albumPath = Path.Combine(bandPath, albumDirectory);
                    foreach (string extension in supportedFileTypes)
                    {
                        foundMusic.AddRange(Directory.GetFiles(albumPath, extension));
                    }
                }
            }

            foreach (string songFilename in foundMusic)
            {
                LoadFileData(songFilename);
            }


            if (pendingArtistAdditions.Count > 0 ||
                pendingAlbumAdditions.Count > 0 ||
                pendingTrackAdditions.Count > 0 ||
                pendingSongAdditions.Count > 0)
            {
                dbConnection.Open();

                //Add Artists
                using (var writeTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeArtist = dbConnection.CreateCommand();
                    writeArtist.Transaction = writeTransaction;
                    writeArtist.CommandType = System.Data.CommandType.Text;
                    writeArtist.CommandText = "INSERT INTO artist " +
                        "(artist_id, artist_name) VALUES " +
                        "(@artistID, @artistName);";
                    writeArtist.Parameters.Add(new SQLiteParameter("@artistID", -1));
                    writeArtist.Parameters.Add(new SQLiteParameter("@artistName", ""));

                    foreach (ArtistData artist in pendingArtistAdditions)
                    {
                        writeArtist.Parameters["@artistID"].Value = artist.artistID;
                        writeArtist.Parameters["@artistName"].Value = artist.artistName;
                        writeArtist.ExecuteNonQuery();
                    }

                    writeTransaction.Commit();
                    pendingArtistAdditions.Clear();
                }

                //Add Albums
                using (var writeTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeAlbum = dbConnection.CreateCommand();
                    writeAlbum.Transaction = writeTransaction;
                    writeAlbum.CommandType = System.Data.CommandType.Text;
                    writeAlbum.CommandText = "INSERT INTO album " +
                        "(album_id, artist_id, album_name, album_year) VALUES " +
                        "(@albumID, @artistID, @albumTitle, @albumYear);";
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumID", -1));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@artistID", -1));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumTitle", ""));
                    writeAlbum.Parameters.Add(new SQLiteParameter("@albumYear", ""));

                    foreach (AlbumData album in pendingAlbumAdditions)
                    {
                        writeAlbum.Parameters["@albumID"].Value = album.albumID;
                        writeAlbum.Parameters["@artistID"].Value = album.artistID;
                        writeAlbum.Parameters["@albumTitle"].Value = album.albumTitle;
                        writeAlbum.Parameters["@albumYear"].Value = album.albumYear;
                        writeAlbum.ExecuteNonQuery();
                    }

                    writeTransaction.Commit();
                    pendingAlbumAdditions.Clear();
                }

                //Add Tracks
                using (var writeTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeTrack = dbConnection.CreateCommand();
                    writeTrack.Transaction = writeTransaction;
                    writeTrack.CommandType = System.Data.CommandType.Text;
                    writeTrack.CommandText = "INSERT INTO track " +
                        "(track_id, song_id, album_id, track_number) VALUES " +
                        "(@trackID, @songID, @albumID, @trackNumber);";
                    writeTrack.Parameters.Add(new SQLiteParameter("@trackID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@songID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@albumID", -1));
                    writeTrack.Parameters.Add(new SQLiteParameter("@trackNumber", -1));

                    foreach (TrackData track in pendingTrackAdditions)
                    {
                        writeTrack.Parameters["@trackID"].Value = track.trackID;
                        writeTrack.Parameters["@songID"].Value = track.songID;
                        writeTrack.Parameters["@albumID"].Value = track.albumID;
                        writeTrack.Parameters["@trackNumber"].Value = track.trackNumber;
                        writeTrack.ExecuteNonQuery();
                    }

                    writeTransaction.Commit();
                    pendingTrackAdditions.Clear();
                }

                //Add Songs
                using (var writeTransaction = dbConnection.BeginTransaction())
                {
                    SQLiteCommand writeSong = dbConnection.CreateCommand();
                    writeSong.Transaction = writeTransaction;
                    writeSong.CommandType = System.Data.CommandType.Text;
                    writeSong.CommandText = "INSERT INTO song " +
                        "(song_id, artist_id, song_filename, song_title, live) VALUES " +
                        "(@songID, @artistID, @songFilename, @songTitle, @live);";
                    writeSong.Parameters.Add(new SQLiteParameter("@songID", -1));
                    writeSong.Parameters.Add(new SQLiteParameter("@artistID", -1));
                    writeSong.Parameters.Add(new SQLiteParameter("@songFilename", ""));
                    writeSong.Parameters.Add(new SQLiteParameter("@songTitle", ""));
                    writeSong.Parameters.Add(new SQLiteParameter("@live", false));
                    foreach (SongData song in pendingSongAdditions)
                    {
                        writeSong.Parameters["@songID"].Value = song.songID;
                        writeSong.Parameters["@artistID"].Value = song.artistID;
                        writeSong.Parameters["@songFilename"].Value = song.fileName;
                        writeSong.Parameters["@songTitle"].Value = song.songTitle;
                        writeSong.Parameters["@live"].Value = song.live;
                        writeSong.ExecuteNonQuery();
                    }

                    writeTransaction.Commit();
                    pendingSongAdditions.Clear();
                }

                dbConnection.Close();
            }
        }

        private void LoadFileData(string path)
        {
            if (songFilenameReverseLookupDict.ContainsKey(path))
            {
                Console.WriteLine("File already found in db: " + path);
                return;
            }

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

            int artistID = -1;
            if (!artistReverseLookupDict.ContainsKey(artistName))
            {
                artistID = ++lastArtistIDAssigned;
                ArtistData newArtist = new ArtistData()
                {
                    artistID = artistID,
                    artistName = artistName
                };

                artistReverseLookupDict.Add(artistName, artistID);
                artistDict.Add(lastArtistIDAssigned, newArtist);
                pendingArtistAdditions.Add(newArtist);
            }
            else
            {
                artistID = artistReverseLookupDict[artistName];
            }

            int songID = ++lastSongIDAssigned;

            songDict.Add(songID, new SongData()
            {
                songID = songID,
                artistID = artistID,
                fileName = path,
                songTitle = musicFile.Tag.Title,
                live = false
            });
            songFilenameReverseLookupDict.Add(path, songID);
            pendingSongAdditions.Add(songDict[songID]);

            string albumTitle = "UNDEFINED";
            if (!string.IsNullOrEmpty(musicFile.Tag.Album))
            {
                albumTitle = musicFile.Tag.Album;
            }

            int albumID = -1;
            Tuple<int, string> albumTuple = new Tuple<int, string>(artistID, albumTitle);
            if (albumTitleReverseLookupDict.ContainsKey(albumTuple))
            {
                albumID = albumTitleReverseLookupDict[albumTuple];
            }
            else
            {
                albumID = ++lastAlbumIDAssigned;
                albumDict.Add(albumID, new AlbumData()
                {
                    albumID = albumID,
                    artistID = artistID,
                    albumTitle = albumTitle,
                    albumYear = musicFile.Tag.Year.ToString()
                });
                albumTitleReverseLookupDict.Add(albumTuple, albumID);
                pendingAlbumAdditions.Add(albumDict[albumID]);
            }

            int trackID = ++lastTrackIDAssigned;

            trackDict.Add(trackID, new TrackData
            {
                trackID = trackID,
                songID = songID,
                albumID = albumID,
                trackNumber = (int)musicFile.Tag.Track
            });
            pendingTrackAdditions.Add(trackDict[trackID]);
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
                        artistID: (int)(long)reader["artist_id"],
                        artistName: (string)reader["artist_name"]));
                }
            }

            dbConnection.Close();

            return artistList;
        }

        private ArtistDTO GenerateArtist(int artistID, string artistName)
        {
            List<AlbumDTO> albumList = new List<AlbumDTO>();

            SQLiteCommand readAlbums = dbConnection.CreateCommand();
            readAlbums.CommandType = System.Data.CommandType.Text;
            readAlbums.CommandText =
                "SELECT album_id, album_name, album_year " +
                "FROM album " +
                "WHERE artist_id = @artistID ORDER BY album_year ASC;";
            readAlbums.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readAlbums.ExecuteReader())
            {
                while (reader.Read())
                {
                    string albumName = (string)reader["album_name"];
                    string albumYear = (string)reader["album_year"];

                    albumList.Add(GenerateAlbum(
                        albumID: (int)(long)reader["album_id"],
                        albumTitle: String.Format("{0} ({1})", albumName, albumYear)));
                }
            }

            return new ArtistDTO(artistID, artistName, albumList);
        }

        private AlbumDTO GenerateAlbum(int albumID, string albumTitle)
        {
            List<SongDTO> songList = new List<SongDTO>();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT song.song_id as song_id, song.song_title AS song_title " +
                "FROM track " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "LEFT JOIN song ON track.song_id=song.song_id " +
                "WHERE track.album_id = @albumID ORDER BY track.track_number ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    songList.Add(new SongDTO()
                    {
                        SongID = (int)(long)reader["song_id"],
                        Title = (string)reader["song_title"]
                    });
                }
            }

            return new AlbumDTO(albumID, albumTitle, songList);
        }

        public PlaylistData GetSongData(int songID)
        {
            if (songDict.ContainsKey(songID))
            {
                return new PlaylistData()
                {
                    songTitle = songDict[songID].songTitle,
                    artistName = artistDict[songDict[songID].artistID].artistName,
                    songID = songID
                };
            }

            return new PlaylistData();
        }

        public PlayData GetPlayData(int songID)
        {
            if (songDict.ContainsKey(songID))
            {
                return new PlayData
                {
                    songTitle = songDict[songID].songTitle,
                    artistName = artistDict[songDict[songID].artistID].artistName,
                    fileName = songDict[songID].fileName
                };
            }

            return new PlayData();
        }
    }
}
