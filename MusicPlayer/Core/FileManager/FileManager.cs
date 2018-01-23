using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Media.Imaging;
using Musegician.DataStructures;
using Musegician.Core.DBCommands;

namespace Musegician
{
    #region Exceptions

    public class LibraryContextException : Exception
    {
        public LibraryContextException(string message) : base(message) { }
    }

    #endregion Exceptions

    public partial class FileManager
    {
        #region Data

        private readonly List<string> supportedFileTypes = new List<string>() { "*.mp3", "*.flac", "*.ogg" };
        private readonly string[] songNameDelimiter = new string[] { " - " };

        private const string livePatternA = @"(\s*?[\(\[][Ll]ive.*?[\)\]])";
        private const string livePatternB = @"(\s*?[\(\[][Bb]ootleg.*?[\)\]])";
        private const string explicitCleanupPattern = @"(\s*?[\(\[][Ee]xplicit.*?[\)\]])";
        private const string albumVersionCleanupPattern = @"(\s*?[\(\[][Aa]lbum.*?[\)\]])";
        private const string discNumberPattern = @"(\s*?[\(\[][Dd]isc.*?\d+[\)\]])";
        private const string numberExtractor = @"(\d+)";

        private const string songDBFilename = "SongDB.sqlite";

        private SQLiteConnection dbConnection = null;

        private RecordingCommands recordingCommands = null;
        private TrackCommands trackCommands = null;
        private SongCommands songCommands = null;
        private AlbumCommands albumCommands = null;
        private ArtistCommands artistCommands = null;

        private PlaylistCommands playlistCommands = null;

        #endregion Data
        #region Inner Classes

        private struct DBRecords
        {
            public List<ArtistData> artists;
            public List<AlbumData> albums;
            public List<SongData> songs;
            public List<RecordingData> recordings;
            public List<TrackData> tracks;
            public List<ArtData> art;

            public bool HasRecords()
            {
                return (artists.Count > 0 ||
                    albums.Count > 0 ||
                    tracks.Count > 0 ||
                    songs.Count > 0 ||
                    recordings.Count > 0 ||
                    art.Count > 0);
            }
        }

        private struct DBBuilderLookups
        {
            public Dictionary<string, long> artistName;
            public Dictionary<ValueTuple<long, string>, long> artistID_AlbumTitle;
            public Dictionary<ValueTuple<long, string>, long> artistID_SongTitle;
            public HashSet<string> loadedFilenames;
            public HashSet<long> loadedAlbumArt;
        }

        #endregion Inner Classes
        #region Event RebuildNotifier

        private EventHandler _rebuildNotifier;

        #endregion Event RebuildNotifier
        #region Singleton Implementation

        private static object m_lock = new object();
        private static volatile FileManager _instance;
        public static FileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (m_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FileManager();
                            _instance.OpenDB();
                            _instance.Initialize();
                        }
                    }
                }
                return _instance;
            }
        }

        private FileManager()
        {
            recordingCommands = new RecordingCommands();
            trackCommands = new TrackCommands();
            songCommands = new SongCommands();
            albumCommands = new AlbumCommands();
            artistCommands = new ArtistCommands();

            playlistCommands = new PlaylistCommands();
        }

        #endregion Singleton Implementation
        
        public void DropDB()
        {
            if (dbConnection != null)
            {
                dbConnection.Open();

                using (SQLiteTransaction dropTablesTransaction = dbConnection.BeginTransaction())
                {
                    //Set Up Tables
                    artistCommands._DropTable(dropTablesTransaction);
                    albumCommands._DropTable(dropTablesTransaction);
                    songCommands._DropTable(dropTablesTransaction);
                    recordingCommands._DropTable(dropTablesTransaction);
                    trackCommands._DropTable(dropTablesTransaction);

                    playlistCommands._DropTable(dropTablesTransaction);

                    dropTablesTransaction.Commit();
                }

                dbConnection.Close();
            }
            else
            {
                try
                {
                    string dbPath = Path.Combine(FileUtility.GetDataPath(), songDBFilename);

                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }

                    OpenDB();

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

            Initialize();
        }

        public void OpenDB()
        {
            string dbPath = Path.Combine(FileUtility.GetDataPath(), songDBFilename);

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            dbConnection = new SQLiteConnection(String.Format(
                "Data Source=\"{0}\";Version=3;",
                dbPath));
        }

        public void Initialize()
        {
            recordingCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                songCommands: songCommands);

            trackCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                songCommands: songCommands,
                albumCommands: albumCommands,
                recordingCommands: recordingCommands);

            songCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                trackCommands: trackCommands,
                recordingCommands: recordingCommands,
                playlistCommands: playlistCommands);

            albumCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                songCommands: songCommands,
                trackCommands: trackCommands,
                recordingCommands: recordingCommands);

            artistCommands.Initialize(
                dbConnection: dbConnection,
                albumCommands: albumCommands,
                songCommands: songCommands,
                recordingCommands: recordingCommands);

            playlistCommands.Initialize(
                dbConnection: dbConnection);
            
            dbConnection.Open();

            using (SQLiteTransaction createTablesTransaction = dbConnection.BeginTransaction())
            {
                //Set Up Tables
                artistCommands._CreateArtistTables(createTablesTransaction);
                albumCommands._CreateAlbumTables(createTablesTransaction);
                songCommands._CreateSongTables(createTablesTransaction);
                recordingCommands._CreateRecordingTables(createTablesTransaction);
                trackCommands._CreateTrackTables(createTablesTransaction);

                playlistCommands._CreatePlaylistTables(createTablesTransaction);

                createTablesTransaction.Commit();
            }

            dbConnection.Close();

            InitializeCommands();
        }


        /// <summary>
        /// Load in LastIDAssigned for all command classes
        /// </summary>
        private void InitializeCommands()
        {
            dbConnection.Open();

            //Initialize Artists
            artistCommands._InitializeValues();

            //Initialize Albums
            albumCommands._InitializeValues();

            //Initialize Songs
            songCommands._InitializeValues();

            //Initialize Recordings
            recordingCommands._InitializeValues();

            //Initialize Tracks
            trackCommands._InitializeValues();

            //Initialize Playlists
            playlistCommands._InitializeValues();

            dbConnection.Close();
        }

        private void LoadLibraryDictionaries(DBBuilderLookups lookups)
        {
            dbConnection.Open();

            //Load Artists
            artistCommands._PopulateLookup(
                artistNameDict: lookups.artistName);

            //Load Albums
            albumCommands._PopulateLookup(
                artistID_AlbumTitleDict: lookups.artistID_AlbumTitle,
                albumArt: lookups.loadedAlbumArt);

            //Load Songs
            songCommands._PopulateLookup(
                artistID_SongTitleDict: lookups.artistID_SongTitle);

            //Load Recordings
            recordingCommands._PopulateLookup(
                loadedFilenames: lookups.loadedFilenames);

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
                loadedFilenames = new HashSet<string>(),
                loadedAlbumArt = new HashSet<long>()
            };

            DBRecords newRecords = new DBRecords()
            {
                albums = new List<AlbumData>(),
                artists = new List<ArtistData>(),
                songs = new List<SongData>(),
                tracks = new List<TrackData>(),
                recordings = new List<RecordingData>(),
                art = new List<ArtData>()
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

                using (SQLiteTransaction writeRecordsTransaction = dbConnection.BeginTransaction())
                {
                    //Add Artists
                    artistCommands._BatchCreateArtist(
                        transaction: writeRecordsTransaction,
                        newArtistRecords: newRecords.artists);

                    //Add Albums
                    albumCommands._BatchCreateAlbum(
                        transaction: writeRecordsTransaction,
                        newAlbumRecords: newRecords.albums);

                    albumCommands._BatchCreateArt(
                        transaction: writeRecordsTransaction,
                        newArtRecords: newRecords.art);

                    //Add Tracks
                    trackCommands._BatchCreateTracks(
                        transaction: writeRecordsTransaction,
                        newTrackRecords: newRecords.tracks);

                    //Add Songs
                    songCommands._BatchCreateSong(
                        transaction: writeRecordsTransaction,
                        newSongRecords: newRecords.songs);

                    // Add Recordings
                    recordingCommands._BatchCreateRecording(
                        transaction: writeRecordsTransaction,
                        newRecordingRecords: newRecords.recordings);

                    writeRecordsTransaction.Commit();
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

            TagLib.Tag tag = file.Tag;

            // How to write custom frames
            //
            //TagLib.Id3v2.Tag thing = file.GetTag(TagLib.TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;
            //if (thing != null)
            //{
            //    TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(thing, "Musegician/TAGID", true);
            //    frame.PrivateData = System.Text.Encoding.Unicode.GetBytes("testValue");
            //}


            //Handle Artist
            string artistName = "UNDEFINED";
            if (!string.IsNullOrEmpty(tag.JoinedPerformers))
            {
                artistName = tag.JoinedPerformers;
            }

            long artistID = -1;
            if (!lookups.artistName.ContainsKey(artistName))
            {
                artistID = artistCommands.NextID;

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
            if (!string.IsNullOrEmpty(tag.Title))
            {
                songTitle = tag.Title;
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
            if (!string.IsNullOrEmpty(tag.Album))
            {
                albumTitle = tag.Album;
            }

            long discNumber = 1;
            if (tag.Disc != 0)
            {
                discNumber = tag.Disc;
            }

            //Copy the track title before we gut it
            string trackTitle = songTitle;

            if (Regex.IsMatch(songTitle, explicitCleanupPattern))
            {
                songTitle = Regex.Replace(songTitle, explicitCleanupPattern, "");
            }

            if (Regex.IsMatch(songTitle, albumVersionCleanupPattern))
            {
                songTitle = Regex.Replace(songTitle, albumVersionCleanupPattern, "");
            }

            bool live = false;
            if (Regex.IsMatch(songTitle, livePatternA))
            {
                live = true;
                songTitle = Regex.Replace(songTitle, livePatternA, "");
            }

            if (Regex.IsMatch(songTitle, livePatternB))
            {
                live = true;
                songTitle = Regex.Replace(songTitle, livePatternB, "");
            }

            if (Regex.IsMatch(albumTitle, livePatternA))
            {
                live = true;
                albumTitle = Regex.Replace(albumTitle, livePatternA, "");
            }

            if (Regex.IsMatch(albumTitle, livePatternB))
            {
                live = true;
                albumTitle = Regex.Replace(albumTitle, livePatternB, "");
            }

            if (Regex.IsMatch(albumTitle, discNumberPattern))
            {
                string discString = Regex.Match(albumTitle, discNumberPattern).Captures[0].ToString();
                discNumber = long.Parse(Regex.Match(discString, numberExtractor).Captures[0].ToString());
                albumTitle = Regex.Replace(albumTitle, discNumberPattern, "");
            }

            long songID = -1;
            var songLookupKey = (artistID, songTitle.ToLowerInvariant());
            if (!lookups.artistID_SongTitle.ContainsKey(songLookupKey))
            {
                songID = songCommands.NextID;

                lookups.artistID_SongTitle.Add(songLookupKey, songID);
                newRecords.songs.Add(new SongData()
                {
                    songID = songID,
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
                albumID = albumCommands.NextID;

                lookups.artistID_AlbumTitle.Add(albumTuple, albumID);
                newRecords.albums.Add(new AlbumData()
                {
                    albumID = albumID,
                    albumTitle = albumTitle,
                    albumYear = tag.Year
                });
            }

            if (tag.Pictures.Length > 0 && !lookups.loadedAlbumArt.Contains(albumID))
            {
                lookups.loadedAlbumArt.Add(albumID);

                newRecords.art.Add(new ArtData()
                {
                    albumArtID = albumCommands.NextAlbumArtID,
                    albumID = albumID,
                    image = file.Tag.Pictures[0].Data.Data
                });
            }

            long recordingID = recordingCommands.NextID;
            newRecords.recordings.Add(new RecordingData
            {
                recordingID = recordingID,
                artistID = artistID,
                songID = songID,
                filename = path,
                live = live,
                valid = true
            });

            long trackID = trackCommands.NextID;
            newRecords.tracks.Add(new TrackData
            {
                trackID = trackID,
                albumID = albumID,
                recordingID = recordingID,
                trackTitle = trackTitle,
                trackNumber = tag.Track,
                discNumber = discNumber
            });
        }

        public void SetAlbumArt(long albumID, string path)
        {
            albumCommands.SetAlbumArt(albumID, path);
        }

        public BitmapImage GetAlbumArtForRecording(long recordingID)
        {
            return albumCommands.GetAlbumArtForRecording(recordingID);
        }
    }
}
