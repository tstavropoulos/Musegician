﻿using System;
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
using MusicPlayer.TagEditor;
using MusicPlayer.Core.DBCommands;

namespace MusicPlayer
{
    public class FileManager : ILibraryRequestHandler
    {
        private readonly List<string> supportedFileTypes = new List<string>() { "*.mp3" };
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


        public FileManager()
        {
            recordingCommands = new RecordingCommands();
            trackCommands = new TrackCommands();
            songCommands = new SongCommands();
            albumCommands = new AlbumCommands();
            artistCommands = new ArtistCommands();
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

            recordingCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                songCommands: songCommands);

            trackCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                songCommands: songCommands,
                recordingCommands: recordingCommands);

            songCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                trackCommands: trackCommands,
                recordingCommands: recordingCommands);

            albumCommands.Initialize(
                dbConnection: dbConnection,
                artistCommands: artistCommands,
                songCommands: songCommands);

            artistCommands.Initialize(
                dbConnection: dbConnection,
                albumCommands: albumCommands,
                songCommands: songCommands,
                recordingCommands: recordingCommands);

            if (newDB)
            {
                dbConnection.Open();

                using (SQLiteTransaction createTablesTransaction = dbConnection.BeginTransaction())
                {
                    //Set Up Tables
                    artistCommands._CreateArtistTables(createTablesTransaction);
                    albumCommands._CreateAlbumTables(createTablesTransaction);
                    songCommands._CreateSongTables(createTablesTransaction);
                    recordingCommands._CreateRecordingTables(createTablesTransaction);
                    trackCommands._CreateTrackTables(createTablesTransaction);

                    createTablesTransaction.Commit();

                }

                dbConnection.Close();
            }
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

            //Load Tracks
            trackCommands._PopulateLookup();

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

            long discNumber = 1;
            if (musicFile.Tag.Disc != 0)
            {
                discNumber = musicFile.Tag.Disc;
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
                    albumYear = musicFile.Tag.Year
                });
            }

            if (musicFile.Tag.Pictures.Length > 0 && !lookups.loadedAlbumArt.Contains(albumID))
            {
                lookups.loadedAlbumArt.Add(albumID);

                newRecords.art.Add(new ArtData()
                {
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
                trackNumber = musicFile.Tag.Track,
                discNumber = discNumber
            });
        }

        List<ArtistDTO> ILibraryRequestHandler.GenerateArtistList()
        {
            return artistCommands.GeneratArtistList();
        }

        List<AlbumDTO> ILibraryRequestHandler.GenerateArtistAlbumList(long artistID, string artistName)
        {
            return albumCommands.GenerateArtistAlbumList(artistID, artistName);
        }

        List<SongDTO> ILibraryRequestHandler.GenerateAlbumSongList(long artistID, long albumID)
        {

            return songCommands.GenerateAlbumSongList(artistID, albumID);
        }

        List<RecordingDTO> ILibraryRequestHandler.GenerateSongRecordingList(long songID, long albumID)
        {
            return recordingCommands.GenerateSongRecordingList(songID, albumID);
        }

        List<AlbumDTO> ILibraryRequestHandler.GenerateAlbumList()
        {
            return albumCommands.GenerateAlbumList();
        }

        List<SongDTO> ILibraryRequestHandler.GenerateArtistSongList(long artistID, string artistName)
        {
            return songCommands.GenerateArtistSongList(artistID, artistName);
        }

        private List<RecordingDTO> GetRecordingList(
            long songID,
            long exclusiveArtistID = -1,
            long exclusiveRecordingID = -1)
        {
            return recordingCommands._GetRecordingList(songID, exclusiveArtistID, exclusiveRecordingID);
        }

        public List<SongDTO> GetSongDataFromRecordingID(long recordingID)
        {
            RecordingData data = recordingCommands.GetData(
                recordingID: recordingID);

            if (!data.RecordFound())
            {
                return null;
            }

            return GetSongData(
                songID: data.songID,
                exclusiveRecordingID: recordingID);
        }

        public List<SongDTO> GetSongData(
            long songID,
            long exclusiveArtistID = -1,
            long exclusiveRecordingID = -1)
        {
            List<SongDTO> songData = new List<SongDTO>();

            dbConnection.Open();

            string playlistName;

            if (exclusiveRecordingID != -1)
            {
                playlistName = recordingCommands._GetPlaylistName(
                    recordingID: exclusiveRecordingID);
            }
            else if (exclusiveArtistID != -1)
            {
                playlistName = artistCommands._GetPlaylistName(
                    artistID: exclusiveArtistID,
                    songID: songID);
            }
            else
            {
                playlistName = songCommands._GetPlaylistName(
                    songID: songID);
            }

            songData.Add(new SongDTO(
                songID: songID,
                title: playlistName));

            foreach (RecordingDTO data in GetRecordingList(
                                            songID: songID,
                                            exclusiveArtistID: exclusiveArtistID,
                                            exclusiveRecordingID: exclusiveRecordingID))
            {
                songData[0].Children.Add(data);
            }

            dbConnection.Close();

            return songData;
        }

        public List<SongDTO> GetAlbumData(
            long albumID,
            long exclusiveArtistID = -1)
        {
            List<SongDTO> albumData = new List<SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT " +
                    "song.song_id AS song_id, " +
                    "song.song_title AS song_title, " +
                    "artist.artist_name AS artist_name " +
                "FROM track " +
                "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                "LEFT JOIN album ON track.album_id=album.album_id " +
                "LEFT JOIN song ON recording.song_id=song.song_id " +
                "LEFT JOIN artist ON recording.artist_id=artist.artist_id " +
                "WHERE track.album_id=@albumID ORDER BY track.disc_number ASC, track.track_number ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@albumID", albumID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];
                    string playlistName;

                    if (exclusiveArtistID != -1)
                    {
                        playlistName = artistCommands._GetPlaylistName(
                            artistID: exclusiveArtistID,
                            songID: songID);
                    }
                    else
                    {
                        playlistName = songCommands._GetPlaylistName(
                            songID: songID);
                    }

                    SongDTO newSong = new SongDTO(
                        songID: songID,
                        title: playlistName);

                    foreach (RecordingDTO recording in GetRecordingList(
                            songID: songID,
                            exclusiveArtistID: exclusiveArtistID))
                    {
                        newSong.Children.Add(recording);
                    }


                    albumData.Add(newSong);
                }
            }

            dbConnection.Close();

            return albumData;
        }

        public List<SongDTO> GetArtistData(long artistID, bool exclusive = false)
        {
            List<SongDTO> artistData = new List<SongDTO>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.CommandText =
                "SELECT song_id " +
                "FROM song " +
                "WHERE song_id IN ( " +
                    "SELECT song_id " +
                    "FROM recording " +
                    "WHERE artist_id=@artistID ) " +
                "ORDER BY song_title ASC;";
            readTracks.Parameters.Add(new SQLiteParameter("@artistID", artistID));

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    long songID = (long)reader["song_id"];
                    string playlistName;

                    if (exclusive)
                    {
                        playlistName = artistCommands._GetPlaylistName(
                            artistID: artistID,
                            songID: songID);
                    }
                    else
                    {
                        playlistName = songCommands._GetPlaylistName(
                            songID: songID);
                    }

                    SongDTO newSong = new SongDTO(
                        songID: songID,
                        title: playlistName);

                    foreach (RecordingDTO recording in GetRecordingList(
                            songID: songID,
                            exclusiveArtistID: exclusive ? artistID : -1))
                    {
                        newSong.Children.Add(recording);
                    }

                    artistData.Add(newSong);
                }
            }

            dbConnection.Close();

            return artistData;
        }

        public void SetAlbumArt(long albumID, string path)
        {
            albumCommands.SetAlbumArt(albumID, path);



        }

        public PlayData GetRecordingPlayData(long recordingID)
        {
            return recordingCommands.GetRecordingPlayData(recordingID);
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
                            "SELECT artist_name " +
                            "FROM artist " +
                            "WHERE artist_id=@ID;";
                    }
                    break;
                case LibraryContext.Album:
                    {
                        readTracks.CommandText =
                            "SELECT album_title, album_year " +
                            "FROM album " +
                            "WHERE album_id=@ID;";
                    }
                    break;
                case LibraryContext.Song:
                    {
                        readTracks.CommandText =
                            "SELECT song_title " +
                            "FROM song " +
                            "WHERE song_id=@ID;";
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
                            "LEFT JOIN recording ON track.recording_id=recording.recording_id " +
                            "LEFT JOIN song ON recording.song_id=song.song_id " +
                            "LEFT JOIN artist ON recording.artist_id=artist.artist_id " +
                            "LEFT JOIN album ON track.album_id=album.album_id " +
                            "WHERE track.track_id=@ID;";
                    }
                    break;
                case LibraryContext.Recording:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.song_title AS song_title, " +
                                "artist.artist_name AS artist_name, " +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM recording " +
                            "LEFT JOIN song ON recording.song_id=song.song_id " +
                            "LEFT JOIN artist ON recording.artist_id=artist.artist_id " +
                            "WHERE recording.recording_id=@ID;";
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
                        tagList.Add(new TagViewable()
                        {
                            _CurrentValue = (string)reader["filename"],
                            recordType = MusicRecord.Filename
                        });

                        tagList.Add(new TagDataBool()
                        {
                            _currentValue = (bool)reader["live"],
                            NewValue = (bool)reader["live"],
                            recordType = MusicRecord.Live
                        });
                    }

                    if (context == LibraryContext.Track)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["track_title"],
                            NewValue = (string)reader["track_title"],
                            recordType = MusicRecord.TrackTitle,
                            tagType = ID3TagType.Title
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["track_number"],
                            _newValue = (long)reader["track_number"],
                            recordType = MusicRecord.TrackNumber,
                            tagType = ID3TagType.Track
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
                            recordType = MusicRecord.SongTitle
                        });
                    }

                    if (context == LibraryContext.Album ||
                        context == LibraryContext.Track)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["album_title"],
                            NewValue = (string)reader["album_title"],
                            recordType = MusicRecord.AlbumTitle,
                            tagType = ID3TagType.Album
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["album_year"],
                            _newValue = (long)reader["album_year"],
                            recordType = MusicRecord.AlbumYear,
                            tagType = ID3TagType.Year
                        });
                    }

                    if (context == LibraryContext.Artist ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["artist_name"],
                            NewValue = (string)reader["artist_name"],
                            recordType = MusicRecord.ArtistName,
                            tagType = ID3TagType.Performer,
                            tagTypeIndex = 0
                        });
                    }
                }
            }

            dbConnection.Close();

            return tagList;
        }

        /// <summary>
        /// Identifies all of the files potentially requiring ID3 tag updates
        /// </summary>
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
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.artist_id=@ID;";
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
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.song_id=@ID;";
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
                        "SELECT filename " +
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

        public class LibraryContextException : Exception
        {
            public LibraryContextException(string message) : base(message) { }
        }

        /// <summary>
        /// Make sure to translate the ID to the right context before calling this method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <param name="record"></param>
        /// <param name="newString"></param>
        /// <exception cref="LibraryContextException"/>
        public void UpdateRecord(LibraryContext context, IList<long> ids, MusicRecord record, string newString)
        {
            if (ids.Count == 0)
            {
                throw new InvalidOperationException(string.Format(
                    "Found 0 records to modify for LibraryContext {0}, MusicRecord {1}",
                    context.ToString(),
                    record.ToString()));
            }

            switch (record)
            {
                case MusicRecord.SongTitle:
                    {
                        switch (context)
                        {
                            case LibraryContext.Song:
                                {
                                    //Renaming (or Consolidating) Songs
                                    songCommands.UpdateSongTitle(
                                        songIDs: ids,
                                        newTitle: newString);
                                }
                                break;
                            case LibraryContext.Track:
                                {
                                    //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                                    trackCommands.UpdateSongTitle(
                                        trackIDs: ids,
                                        newTitle: newString);
                                }
                                break;
                            case LibraryContext.Recording:
                                {
                                    //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                                    recordingCommands.UpdateSongTitle(
                                        recordingIDs: ids,
                                        newTitle: newString);
                                }
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                case MusicRecord.ArtistName:
                    {
                        switch (context)
                        {
                            case LibraryContext.Artist:
                                {
                                    //Renaming and collapsing Artists
                                    artistCommands.UpdateArtistName(
                                        artistIDs: ids,
                                        newArtistName: newString);
                                }
                                break;
                            case LibraryContext.Track:
                                {
                                    //Assinging Songs to a different artist
                                    trackCommands.UpdateArtistName(
                                        trackIDs: ids,
                                        newArtistName: newString);
                                }
                                break;
                            case LibraryContext.Recording:
                                {
                                    //Assinging Songs to a different artist
                                    recordingCommands.UpdateArtistName(
                                        recordingIDs: ids,
                                        newArtistName: newString);
                                }
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }

                    }
                    break;
                case MusicRecord.AlbumTitle:
                    {
                        switch (context)
                        {
                            case LibraryContext.Album:
                                //Renaming an album
                                throw new NotImplementedException();
                                break;
                            case LibraryContext.Track:
                                //Assigning a track to a different album
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                case MusicRecord.TrackTitle:
                    {
                        switch (context)
                        {
                            case LibraryContext.Track:
                                //Renaming a track
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newString.GetType().ToString(),
                        record.ToString()));
            }
        }

        public void UpdateRecord(LibraryContext context, IList<long> ids, MusicRecord record, long newLong)
        {
            switch (record)
            {
                case MusicRecord.TrackNumber:
                    {
                        switch (context)
                        {
                            case LibraryContext.Track:
                                //Updating the track number of a track
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                case MusicRecord.AlbumYear:
                    {
                        switch (context)
                        {
                            case LibraryContext.Album:
                                //Updating the year that an album was produced
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newLong.GetType().ToString(),
                        record.ToString()));
            }
        }

        public void UpdateRecord(LibraryContext context, IList<long> ids, MusicRecord record, bool newBool)
        {
            switch (record)
            {
                case MusicRecord.Live:
                    {
                        //Update Recording Live Status Weight
                        if (context != LibraryContext.Recording)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newBool.GetType().ToString(),
                        record.ToString()));
            }
        }

        public void UpdateRecord(LibraryContext context, IList<long> ids, MusicRecord record, double newDouble)
        {
            switch (record)
            {
                case MusicRecord.ArtistWeight:
                    {
                        //Update Artist Weight
                        if (context != LibraryContext.Artist)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                case MusicRecord.AlbumWeight:
                    {
                        //Update Album Weight
                        if (context != LibraryContext.Album)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                case MusicRecord.SongWeight:
                    {
                        //Update Album Weight
                        if (context != LibraryContext.Song)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                case MusicRecord.TrackWeight:
                    {
                        //Update Track Weight
                        if (context != LibraryContext.Track)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newDouble.GetType().ToString(),
                        record.ToString()));
            }
        }
    }
}
