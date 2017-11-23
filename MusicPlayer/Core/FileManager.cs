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

        private Dictionary<int, Artist> artistDict = new Dictionary<int, Artist>();
        private Dictionary<string, int> artistReverseLookupDict = new Dictionary<string, int>();

        private Dictionary<int, SongData> songDict = new Dictionary<int, SongData>();
        private Dictionary<string, int> songFilenameReverseLookupDict = new Dictionary<string, int>();

        private int lastArtistIDAssigned = 1;
        private int lastSongIDAssigned = 0;

        private List<Artist> pendingArtistAdditions = new List<Artist>();
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
                    "CREATE TABLE album_simple (" +
                        "album_id INTEGER, " +
                        "artist_id INTEGER REFERENCES artist, " +
                        "album_name TEXT, " +
                        "album_year TEXT);";

                string makeSongTable =
                    "CREATE TABLE song_simple (" +
                        "song_id INTEGER, " +
                        "artist_id INTEGER REFERENCES artist, " +
                        "song_filename TEXT, " +
                        "song_name TEXT, " +
                        "live BOOLEAN);";

                string makeTrackTable =
                    "CREATE TABLE track_simple (" +
                        "track_simple_id INTEGER, " +
                        "song_id INTEGER REFERENCES song_simple, " +
                        "album_id INTEGER REFERENCES album_simple, " +
                        "songNumber INTEGER);";

                string addUndefiniedArtist =
                    "INSERT INTO artist (artist_id, artist_name) VALUES " +
                        "(1, 'UNDEFINED');";

                dbConnection.Open();
                //Set Up Tables
                new SQLiteCommand(makeArtistsTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeAlbumTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeSongTable, dbConnection).ExecuteNonQuery();
                new SQLiteCommand(makeTrackTable, dbConnection).ExecuteNonQuery();

                //Add Null Entries
                new SQLiteCommand(addUndefiniedArtist, dbConnection).ExecuteNonQuery();


                dbConnection.Close();
            }
            else
            {
                //Load up artists
                string getArtists = "SELECT * FROM artist ORDER BY artist_id ASC;";

                artistDict.Clear();
                artistReverseLookupDict.Clear();

                dbConnection.Open();

                using (SQLiteDataReader reader = new SQLiteCommand(getArtists, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {

                        string artistName = (string)reader["artist_name"];
                        var temp = reader["artist_id"];
                        int artistID = (int)(long)reader["artist_id"];

                        artistDict.Add(artistID,
                            new Artist()
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

                //Load up songs
                string getSongs = "SELECT * FROM song_simple ORDER BY song_id ASC;";

                songFilenameReverseLookupDict.Clear();
                songDict.Clear();

                string lastFile = "";

                using (SQLiteDataReader reader = new SQLiteCommand(getSongs, dbConnection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int songID = (int)(long)reader["song_id"];
                        string fileName = (string)reader["song_filename"];
                        int artistID = (int)(long)reader["artist_id"];
                        string songName = (string)reader["song_name"];
                        bool live = (bool)reader["live"];

                        songDict.Add(songID,
                            new SongData()
                            {
                                songID = songID,
                                artistID = artistID,
                                fileName = fileName,
                                songName = songName,
                                live = live
                            });

                        songFilenameReverseLookupDict.Add(fileName, songID);

                        if (songID > lastSongIDAssigned)
                        {
                            lastSongIDAssigned = songID;
                        }

                        lastFile = fileName;
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


            if (pendingArtistAdditions.Count > 0 || pendingSongAdditions.Count > 0)
            {
                dbConnection.Open();

                SQLiteCommand writeArtist = dbConnection.CreateCommand();
                writeArtist.CommandType = System.Data.CommandType.Text;
                writeArtist.CommandText = "INSERT INTO artist (artist_id, artist_name) VALUES " +
                    "(@artistID, @artistName);";
                writeArtist.Parameters.Add(new SQLiteParameter("@artistID", -1));
                writeArtist.Parameters.Add(new SQLiteParameter("@artistName", ""));

                foreach (Artist artist in pendingArtistAdditions)
                {
                    writeArtist.Parameters["@artistID"].Value = artist.artistID;
                    writeArtist.Parameters["@artistName"].Value = artist.artistName;
                    writeArtist.ExecuteNonQuery();
                }

                SQLiteCommand writeSong = dbConnection.CreateCommand();
                writeSong.CommandType = System.Data.CommandType.Text;
                writeSong.CommandText = "INSERT INTO song_simple " +
                    "(song_id, artist_id, song_filename, song_name, live) VALUES " +
                    "(@songID, @artistID, @songFilename, @songName, @live);";
                writeSong.Parameters.Add(new SQLiteParameter("@songID", -1));
                writeSong.Parameters.Add(new SQLiteParameter("@artistID", -1));
                writeSong.Parameters.Add(new SQLiteParameter("@songFilename", ""));
                writeSong.Parameters.Add(new SQLiteParameter("@songName", ""));
                writeSong.Parameters.Add(new SQLiteParameter("@live", false));
                foreach (SongData song in pendingSongAdditions)
                {
                    writeSong.Parameters["@songID"].Value = song.songID;
                    writeSong.Parameters["@artistID"].Value = song.artistID;
                    writeSong.Parameters["@songFilename"].Value = song.fileName;
                    writeSong.Parameters["@songName"].Value = song.songName;
                    writeSong.Parameters["@live"].Value = song.live;
                    writeSong.ExecuteNonQuery();
                }

                pendingArtistAdditions.Clear();
                pendingSongAdditions.Clear();

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

            int artistID = 1;

            //Handle Artist
            if (musicFile.Tag.JoinedPerformers != "")
            {
                if (!artistReverseLookupDict.ContainsKey(musicFile.Tag.JoinedPerformers))
                {
                    artistID = ++lastArtistIDAssigned;
                    Artist newArtist = new Artist()
                    {
                        artistID = artistID,
                        artistName = musicFile.Tag.JoinedPerformers
                    };

                    artistReverseLookupDict.Add(musicFile.Tag.JoinedPerformers, artistID);
                    artistDict.Add(lastArtistIDAssigned, newArtist);
                    pendingArtistAdditions.Add(newArtist);
                }
                else
                {
                    artistID = artistReverseLookupDict[musicFile.Tag.JoinedPerformers];
                }
            }
            else
            {
                Console.WriteLine("Track does not have a performer: " + musicFile.Name);
            }

            int songID = ++lastSongIDAssigned;

            songDict.Add(songID, new SongData()
            {
                songID = songID,
                artistID = artistID,
                fileName = path,
                songName = musicFile.Tag.Title,
                live = false
            });

            songFilenameReverseLookupDict.Add(path, songID);

            pendingSongAdditions.Add(songDict[songID]);
        }

        public List<ArtistDTO> GenerateArtistList()
        {
            List<ArtistDTO> artistList = new List<ArtistDTO>();

            dbConnection.Open();

            SQLiteCommand readSongs = dbConnection.CreateCommand();
            readSongs.CommandType = System.Data.CommandType.Text;
            readSongs.CommandText = "SELECT * FROM song_simple WHERE artist_id = @artistID ORDER BY song_name ASC;";
            readSongs.Parameters.Add(new SQLiteParameter("@artistID", -1));

            foreach (Artist artist in artistDict.Values)
            {
                readSongs.Parameters["@artistID"].Value = artist.artistID;

                List<SongDTO> songList = new List<SongDTO>();

                using (SQLiteDataReader reader = readSongs.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        songList.Add(new SongDTO()
                        {
                            SongID = (int)(long)reader["song_id"],
                            Title = (string)reader["song_name"]
                        });
                    }
                }

                if (artist.artistID == 1 && songList.Count == 0)
                {
                    //Skip adding UNDEFINED if it's not used
                    continue;
                }
                artistList.Add(new ArtistDTO(artist.artistID, artist.artistName, songList));
            }

            dbConnection.Close();

            return artistList;
        }


        public PlayData GetPlayData(int songID)
        {
            if (songDict.ContainsKey(songID))
            {
                return new PlayData
                {
                    songName = songDict[songID].songName,
                    artistName = artistDict[songDict[songID].artistID].artistName,
                    fileName = songDict[songID].fileName
                };
            }

            return new PlayData();
        }
    }
}
