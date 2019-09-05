using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Media.Imaging;
using Musegician.Core;
using Musegician.Core.DBCommands;
using Musegician.Database;

using LoadingUpdater = Musegician.LoadingDialog.LoadingDialog.LoadingUpdater;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Musegician
{
    #region Exceptions

    [Serializable]
    public class LibraryContextException : Exception
    {
        public LibraryContextException(string message) : base(message) { }
    }

    #endregion Exceptions

    public sealed partial class FileManager : IDisposable
    {
        #region Data

        private readonly List<string> supportedFileTypes = new List<string>() { "*.mp3", "*.flac", "*.ogg", "*.m4a" };
        private readonly string[] songNameDelimiter = new string[] { " - " };
        private readonly string consoleDiv = "---------------------------------------";

        #region RegEx

        /// <summary>
        /// Identifies text of the form (Live*) or [Live*].
        /// </summary>
        private readonly static Regex livePatternA = new Regex(@"(\s*?[\(\[][Ll]ive.*?[\)\]])");

        /// <summary>
        /// Identifies text of the form (Bootleg*) or [Bootleg*].
        /// </summary>
        private readonly static Regex livePatternB = new Regex(@"(\s*?[\(\[][Bb]ootleg.*?[\)\]])");

        /// <summary>
        /// Identifies text of the form (At The*) or [At The*].
        /// </summary>
        private readonly static Regex livePatternC = new Regex(@"(\s*?[\(\[][Aa]t [Tt]he.*?[\)\]])");

        /// <summary>
        /// Identifies text of the form [Unplugged] or (Unplugged).
        /// </summary>
        private readonly static Regex unpluggedPattern = new Regex(@"(\s*?[\(\[][Uu]nplugged[\)\]])");

        /// <summary>
        /// Identifies text of the form (Acoustic*) or [Acoustic*].
        /// Ex: Sting - A Day In The Life (Acoustic)
        /// </summary>
        private readonly static Regex acousticPattern = new Regex(@"(\s*?[\(\[][Aa]coustic.*?[\)\]])");

        /// <summary>
        /// Identifies text of the form (Explicit*) or [Explicit*].
        /// Ex: Queens of the Stone Age - Song For The Dead [Explicit]
        /// </summary>
        private readonly static Regex explicitCleanupPattern = new Regex(@"(\s*?[\(\[][Ee]xplicit.*?[\)\]])");

        /// <summary>
        /// Identifies text of the form (Album*) or [Album*].
        /// Ex: [Album Version]
        /// </summary>
        private readonly static Regex albumVersionCleanupPattern = new Regex(@"(\s*?[\(\[][Aa]lbum.*?[\)\]])");

        /// <summary>
        /// Identifies text of the form (Disc #) or [Disc #].
        /// Ex: Physical Graffiti (Disc 1)
        /// </summary>
        private readonly static Regex discNumberPattern = new Regex(@"(\s*?[\(\[][Dd]isc.*?\d+[\)\]])");

        /// <summary>
        /// Captures and extracts numbers
        /// </summary>
        private readonly static Regex numberExtractor = new Regex(@"(\d+)");

        #endregion RegEx

        private readonly RecordingCommands recordingCommands = null;
        private readonly SongCommands songCommands = null;
        private readonly AlbumCommands albumCommands = null;
        private readonly ArtistCommands artistCommands = null;

        private readonly PlaylistCommands playlistCommands = null;

        /// <summary>
        /// A long-running Database reference
        /// </summary>
        private readonly MusegicianData db = null;

        private static ImageCodecInfo thumbnailEncoder = null;
        private static EncoderParameters thumbnailEncoderParameters = null;

        #endregion Data
        #region Event RebuildNotifier

        private EventHandler _rebuildNotifier;

        #endregion Event RebuildNotifier
        #region Lookup Data

        private class Lookups
        {
            //Guid Lookups
            public Dictionary<Guid, Artist> ArtistGuidLookup { get; } = new Dictionary<Guid, Artist>();
            public Dictionary<Guid, Album> AlbumGuidLookup { get; } = new Dictionary<Guid, Album>();
            public Dictionary<Guid, Song> SongGuidLookup { get; } = new Dictionary<Guid, Song>();

            //Name Lookups
            public Dictionary<string, Artist> ArtistNameLookup { get; } = new Dictionary<string, Artist>();
            public Dictionary<(Artist, string), Song> SongNameLookup { get; } = new Dictionary<(Artist, string), Song>();
            public Dictionary<(Artist, string), Album> AlbumNameLookup { get; } = new Dictionary<(Artist, string), Album>();
        }

        #endregion Lookup Data
        #region Singleton Implementation

        public static FileManager Instance;

        public FileManager()
        {
            db = new MusegicianData();

            db.Database.CreateIfNotExists();

            recordingCommands = new RecordingCommands(db);
            songCommands = new SongCommands(db);
            albumCommands = new AlbumCommands(db);
            artistCommands = new ArtistCommands(db);

            playlistCommands = new PlaylistCommands(db);
        }

        #endregion Singleton Implementation

        public void DropDB()
        {
            try
            {
                db.Configuration.AutoDetectChangesEnabled = false;

                artistCommands._DropTable();
                albumCommands._DropTable();
                songCommands._DropTable();
                recordingCommands._DropTable();

                playlistCommands._DropTable();
            }
            finally
            {
                db.Configuration.AutoDetectChangesEnabled = true;
            }

            db.SaveChanges();
        }

        private void AddMusicDirectory(
            string path,
            List<string> newMusic,
            HashSet<string> loadedFilenames,
            LoadingUpdater updater)
        {
            foreach (string extension in supportedFileTypes)
            {
                newMusic.AddRange(
                     from filename
                     in Directory.GetFiles(path, extension, SearchOption.TopDirectoryOnly)
                     where !loadedFilenames.Contains(filename)
                     select filename);
            }

            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                //Only update for top-level directories to reduce unnecessary spam
                updater?.SetSubtitle($"Searching Directory {subDirectory}");
                //Note the recursive call sets updater null
                AddMusicDirectory(subDirectory, newMusic, loadedFilenames, null);
            }
        }

        private void PopulateFilenameHashtable(HashSet<string> loadedFilenames)
        {
            foreach (string filename in db.Recordings.Select(x => x.Filename))
            {
                loadedFilenames.Add(filename);
            }
        }

        private void PopulateLookups(Lookups lookups)
        {
            foreach (Artist artist in db.Artists)
            {
                lookups.ArtistGuidLookup.Add(artist.ArtistGuid, artist);
                lookups.ArtistNameLookup.Add(artist.Name.ToLowerInvariant(), artist);
            }

            foreach (Song song in db.Songs)
            {
                lookups.SongGuidLookup.Add(song.SongGuid, song);
            }

            foreach (Album album in db.Albums)
            {
                lookups.AlbumGuidLookup.Add(album.AlbumGuid, album);
            }

            foreach (var set in (from recording in db.Recordings
                                 select new { recording.Artist, recording.Song }).Distinct())
            {
                if (!lookups.SongNameLookup.ContainsKey((set.Artist, set.Song.Title.ToLowerInvariant())))
                {
                    lookups.SongNameLookup.Add((set.Artist, set.Song.Title.ToLowerInvariant()), set.Song);
                }
            }

            foreach (var set in (from album in db.Albums
                                 join recording in db.Recordings on album.Id equals recording.AlbumId
                                 select new { recording.Artist, Album = album }).Distinct())
            {
                if (!lookups.AlbumNameLookup.ContainsKey((set.Artist, set.Album.Title.ToLowerInvariant())))
                {
                    lookups.AlbumNameLookup.Add((set.Artist, set.Album.Title.ToLowerInvariant()), set.Album);
                }
            }
        }

        public void AddDirectoryToLibrary(
            LoadingUpdater updater,
            string path)
        {
            updater.SetTitle($"Adding Music Directory {path}");

            List<string> newMusic = new List<string>();
            HashSet<string> loadedFilenames = new HashSet<string>();
            Lookups lookups = new Lookups();
            try
            {
                db.Configuration.AutoDetectChangesEnabled = false;

                updater.SetSubtitle("Preparing Database...");
                PopulateFilenameHashtable(loadedFilenames);
                AddMusicDirectory(path, newMusic, loadedFilenames, updater);

                updater.SetSubtitle("Reading Database...");
                PopulateLookups(lookups);

                int count = newMusic.Count;
                updater.SetSubtitle($"Loading New Files...  (0/{count})");
                updater.SetLimit(count);

                int i = 0;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (string songFilename in newMusic)
                {
                    i++;
                    if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                    {
                        updater.SetProgress(i);
                        updater.SetSubtitle($"Loading New Files...  ({i}/{count})");
                        stopwatch.Restart();
                    }

                    LoadFileData(
                        path: songFilename,
                        lookups: lookups);
                }


                db.SaveChanges();
            }
            finally
            {
                db.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        private enum TagStatus
        {
            Found,
            UpdatedV1,
            Missing,
            MAX
        }

        private void LoadFileData(
            string path,
            Lookups lookups)
        {
            TagLib.File file = null;

            try
            {
                file = TagLib.File.Create(path);

                MusegicianTagV2 musegicianTagV2 = null;
                TagStatus tagStatus = TagStatus.Found;

                // Reading custom Musegician frame
                if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3TagRead)
                {
                    MusegicianTag musegicianTag = null;

                    TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(id3TagRead, "Musegician/Meta", true);
                    if (frame.PrivateData != null)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTag));
                        musegicianTag = serializer.Deserialize(new MemoryStream(frame.PrivateData.Data)) as MusegicianTag;
                    }

                    frame = TagLib.Id3v2.PrivateFrame.Get(id3TagRead, "Musegician/Met2", true);
                    if (frame.PrivateData != null)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTagV2));
                        musegicianTagV2 = serializer.Deserialize(new MemoryStream(frame.PrivateData.Data)) as MusegicianTagV2;
                    }

                    if (musegicianTagV2 == null && musegicianTag != null)
                    {
                        tagStatus = TagStatus.UpdatedV1;

                        //Convert old style tag
                        musegicianTagV2 = new MusegicianTagV2()
                        {
                            RecordingType = RecordingType.Standard,
                            ArtistGuid = musegicianTag.ArtistGuid,
                            ArtistTimestamp = Epoch.Time,
                            AlbumGuid = musegicianTag.AlbumGuid,
                            AlbumTimestamp = Epoch.Time,
                            SongGuid = musegicianTag.SongGuid,
                            SongTimestamp = Epoch.Time,
                            SongTitle = null
                        };
                    }
                }

                if (musegicianTagV2 == null)
                {
                    //Create new tag
                    musegicianTagV2 = new MusegicianTagV2()
                    {
                        RecordingType = RecordingType.Standard,
                        ArtistGuid = Guid.Empty,
                        ArtistTimestamp = 0,
                        AlbumGuid = Guid.Empty,
                        AlbumTimestamp = 0,
                        SongGuid = Guid.Empty,
                        SongTimestamp = 0,
                        SongTitle = null
                    };

                    tagStatus = TagStatus.Missing;
                }

                TagLib.Tag tag = file.Tag;

                //Handle Artist
                string artistName = "UNDEFINED";
                if (!string.IsNullOrEmpty(tag.JoinedPerformers))
                {
                    artistName = tag.JoinedPerformers;
                }

                Artist artist = null;

                switch (tagStatus)
                {
                    case TagStatus.Found:
                        //Find in lookups by Guid
                        if (lookups.ArtistGuidLookup.ContainsKey(musegicianTagV2.ArtistGuid))
                        {
                            artist = lookups.ArtistGuidLookup[musegicianTagV2.ArtistGuid];
                        }
                        break;

                    case TagStatus.UpdatedV1:
                        //Find in lookups by Guid
                        if (lookups.ArtistGuidLookup.ContainsKey(musegicianTagV2.ArtistGuid))
                        {
                            artist = lookups.ArtistGuidLookup[musegicianTagV2.ArtistGuid];

                            //Update tag timestamp
                            musegicianTagV2.ArtistTimestamp = artist.ArtistGuidTimestamp;
                        }
                        break;

                    case TagStatus.Missing:
                        //Find in lookups by name
                        if (lookups.ArtistNameLookup.ContainsKey(artistName.ToLowerInvariant()))
                        {
                            artist = lookups.ArtistNameLookup[artistName.ToLowerInvariant()];

                            //Populate ArtistGuid into tag
                            musegicianTagV2.ArtistGuid = artist.ArtistGuid;
                            musegicianTagV2.ArtistTimestamp = artist.ArtistGuidTimestamp;
                        }
                        else
                        {
                            //Populate ArtistGuid into tag
                            musegicianTagV2.ArtistGuid = Guid.NewGuid();
                            musegicianTagV2.ArtistTimestamp = Epoch.Time;
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected TagStatus: {tagStatus}");
                }

                //Create
                if (artist == null)
                {
                    artist = new Artist()
                    {
                        Name = artistName,
                        Weight = -1.0,
                        ArtistGuid = musegicianTagV2.ArtistGuid,
                        ArtistGuidTimestamp = musegicianTagV2.ArtistTimestamp
                    };

                    db.Artists.Add(artist);
                    lookups.ArtistGuidLookup.Add(musegicianTagV2.ArtistGuid, artist);
                    lookups.ArtistNameLookup.Add(artistName.ToLowerInvariant(), artist);
                }

                string trackTitle = "UNDEFINED";
                if (!string.IsNullOrEmpty(tag.Title))
                {
                    trackTitle = tag.Title;
                }
                else
                {
                    string[] nameParts =
                        Path.GetFileNameWithoutExtension(path)
                        .Split(songNameDelimiter, StringSplitOptions.RemoveEmptyEntries);

                    if (nameParts.Length == 2)
                    {
                        // Presumed "${Artist} - ${Title}"
                        trackTitle = nameParts[1].Trim(' ');
                    }
                    else if (nameParts.Length == 3)
                    {
                        // Presumed "${Artist} - ${Album} - ${Title}" or
                        //          "${Album} - ${Number} - ${Title}" ??
                        trackTitle = nameParts[2].Trim(' ');
                    }
                    else
                    {
                        //Who knows?  just use filename
                        trackTitle = Path.GetFileNameWithoutExtension(path);
                    }
                }

                Song song = null;

                switch (tagStatus)
                {
                    case TagStatus.Found:
                        //Find in lookups by Guid
                        if (lookups.SongGuidLookup.ContainsKey(musegicianTagV2.SongGuid))
                        {
                            song = lookups.SongGuidLookup[musegicianTagV2.SongGuid];

                            if (musegicianTagV2.SongTitle != song.Title)
                            {
                                //Potential Conflict
                                //Resolving in Database's favor for now
                                musegicianTagV2.SongTitle = song.Title;
                            }
                        }
                        break;

                    case TagStatus.UpdatedV1:
                        //Find in lookups by Guid
                        if (lookups.SongGuidLookup.ContainsKey(musegicianTagV2.SongGuid))
                        {
                            song = lookups.SongGuidLookup[musegicianTagV2.SongGuid];

                            musegicianTagV2.SongTitle = song.Title;
                            musegicianTagV2.SongTimestamp = song.SongGuidTimestamp;

                            if (musegicianTagV2.RecordingType == RecordingType.Standard)
                            {
                                //Only update RecordingType if the old one was the default
                                //To avoid most cases of resetting actual user updates
                                musegicianTagV2.RecordingType = CleanUpSongTitle(trackTitle, out _);
                            }

                        }
                        else
                        {
                            //Clean up title for record creation
                            RecordingType newRecordingType = CleanUpSongTitle(trackTitle, out string tempSongTitle);
                            if (musegicianTagV2.RecordingType == RecordingType.Standard)
                            {
                                //Only update RecordingType if the old one was the default
                                //To avoid most cases of resetting actual user updates
                                musegicianTagV2.RecordingType = newRecordingType;
                            }

                            musegicianTagV2.SongTitle = tempSongTitle;
                        }
                        break;

                    case TagStatus.Missing:
                        //we must search clean up name and search
                        musegicianTagV2.RecordingType = CleanUpSongTitle(trackTitle, out string songTitle);

                        //find in lookups by (artist, name)
                        if (lookups.SongNameLookup.ContainsKey((artist, songTitle.ToLowerInvariant())))
                        {
                            song = lookups.SongNameLookup[(artist, songTitle.ToLowerInvariant())];

                            musegicianTagV2.SongGuid = song.SongGuid;
                            musegicianTagV2.SongTimestamp = song.SongGuidTimestamp;
                        }
                        else
                        {
                            musegicianTagV2.SongGuid = Guid.NewGuid();
                            musegicianTagV2.SongTimestamp = Epoch.Time;
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected TagStatus: {tagStatus}");
                }

                //Just double check that the old data wasn't bad or incomplete...
                if (string.IsNullOrEmpty(musegicianTagV2.SongTitle))
                {
                    CleanUpSongTitle(trackTitle, out string songTitle);
                    musegicianTagV2.SongTitle = songTitle;
                }

                if (song == null)
                {
                    //Create the song
                    song = new Song()
                    {
                        Title = musegicianTagV2.SongTitle,
                        Weight = -1.0,
                        SongGuid = musegicianTagV2.SongGuid,
                        SongGuidTimestamp = musegicianTagV2.SongTimestamp
                    };

                    db.Songs.Add(song);
                    lookups.SongGuidLookup.Add(musegicianTagV2.SongGuid, song);
                    if (!lookups.SongNameLookup.ContainsKey((artist, musegicianTagV2.SongTitle.ToLowerInvariant())))
                    {
                        lookups.SongNameLookup.Add((artist, musegicianTagV2.SongTitle.ToLowerInvariant()), song);
                    }
                }

                Album album = null;
                string albumTitle = "UNDEFINED";
                if (!string.IsNullOrEmpty(tag.Album))
                {
                    albumTitle = tag.Album;
                }

                int discNumber = 1;
                if (tag.Disc != 0)
                {
                    discNumber = (int)tag.Disc;
                }

                string cleanAlbumTitle = "";

                switch (tagStatus)
                {
                    case TagStatus.Found:
                        //Find in lookups by Guid
                        if (lookups.AlbumGuidLookup.ContainsKey(musegicianTagV2.AlbumGuid))
                        {
                            album = lookups.AlbumGuidLookup[musegicianTagV2.AlbumGuid];
                        }
                        else
                        {
                            //Clean up title for record creation
                            CleanUpAlbumTitle(
                                loadedTitle: albumTitle,
                                currentRecordingType: musegicianTagV2.RecordingType,
                                cleanAlbumTitle: out cleanAlbumTitle,
                                discNumber: ref discNumber);
                        }
                        break;

                    case TagStatus.UpdatedV1:
                        //Find in lookups by Guid
                        if (lookups.AlbumGuidLookup.ContainsKey(musegicianTagV2.AlbumGuid))
                        {
                            album = lookups.AlbumGuidLookup[musegicianTagV2.AlbumGuid];
                            musegicianTagV2.AlbumTimestamp = album.AlbumGuidTimestamp;
                        }
                        else
                        {
                            //Clean up title for record creation
                            musegicianTagV2.RecordingType = CleanUpAlbumTitle(
                                loadedTitle: albumTitle,
                                currentRecordingType: musegicianTagV2.RecordingType,
                                cleanAlbumTitle: out cleanAlbumTitle,
                                discNumber: ref discNumber);
                        }
                        break;

                    case TagStatus.Missing:
                        //We must search and clean up the name
                        musegicianTagV2.RecordingType = CleanUpAlbumTitle(
                            loadedTitle: albumTitle,
                            currentRecordingType: musegicianTagV2.RecordingType,
                            cleanAlbumTitle: out cleanAlbumTitle,
                            discNumber: ref discNumber);

                        //search in lookups for (artist,albumname)
                        if (lookups.AlbumNameLookup.ContainsKey((artist, cleanAlbumTitle.ToLowerInvariant())))
                        {
                            album = lookups.AlbumNameLookup[(artist, cleanAlbumTitle.ToLowerInvariant())];


                            musegicianTagV2.AlbumGuid = album.AlbumGuid;
                            musegicianTagV2.AlbumTimestamp = album.AlbumGuidTimestamp;
                        }
                        else
                        {
                            musegicianTagV2.AlbumGuid = Guid.NewGuid();
                            musegicianTagV2.AlbumTimestamp = Epoch.Time;
                        }
                        break;

                    default:
                        throw new Exception($"Unexpected TagStatus: {tagStatus}");
                }


                if (album == null)
                {
                    album = new Album()
                    {
                        Title = cleanAlbumTitle,
                        Year = (int)tag.Year,
                        Weight = -1.0,
                        Image = null,
                        AlbumGuid = musegicianTagV2.AlbumGuid,
                        AlbumGuidTimestamp = musegicianTagV2.AlbumTimestamp
                    };

                    db.Albums.Add(album);
                    lookups.AlbumGuidLookup.Add(musegicianTagV2.AlbumGuid, album);
                    if (!lookups.AlbumNameLookup.ContainsKey((artist, cleanAlbumTitle.ToLowerInvariant())))
                    {
                        lookups.AlbumNameLookup.Add((artist, cleanAlbumTitle.ToLowerInvariant()), album);
                    }
                }

                if (tag.Pictures.Length > 0 && album.Image == null)
                {
                    //Try to open it
                    using (Bitmap loadedBitmap = LoadBitmap(file.Tag.Pictures[0].Data.Data))
                    {
                        //If image can be loaded...
                        if (loadedBitmap != null)
                        {
                            album.Image = file.Tag.Pictures[0].Data.Data;
                            album.Thumbnail = CreateThumbnail(loadedBitmap);
                        }
                    }
                }

                Recording recording = new Recording()
                {
                    Artist = artist,
                    Song = song,
                    Album = album,
                    Filename = path,
                    Title = trackTitle,
                    TrackNumber = (int)tag.Track,
                    DiscNumber = discNumber,
                    RecordingType = musegicianTagV2.RecordingType,
                    Weight = -1.0
                };
                db.Recordings.Add(recording);

                if (Settings.Instance.CreateMusegicianTags)
                {
                    switch (tagStatus)
                    {
                        case TagStatus.Found:
                            //Do Nothing
                            break;

                        case TagStatus.UpdatedV1:
                        case TagStatus.Missing:
                            //Create
                            if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3TagWrite)
                            {
                                file.Mode = TagLib.File.AccessMode.Write;

                                TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(id3TagWrite, "Musegician/Met2", true);

                                StringWriter data = new StringWriter(new StringBuilder());
                                XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTagV2));
                                serializer.Serialize(data, musegicianTagV2);

                                frame.PrivateData = Encoding.Unicode.GetBytes(data.ToString());

                                file.Save();
                            }
                            break;

                        default:
                            throw new Exception($"Unexpected TagStatus: {tagStatus}");
                    }

                }
            }
            catch (TagLib.UnsupportedFormatException)
            {
                Console.WriteLine($"{consoleDiv}\nSkipping UNSUPPORTED FILE: {path}\n");
                return;
            }
            catch (TagLib.CorruptFileException)
            {
                Console.WriteLine($"{consoleDiv}\nSkipping CORRUPT FILE: {path}\n");
                return;
            }
            catch (IOException)
            {
                Console.WriteLine($"{consoleDiv}\nSkipping Writing Tag To FILE IN USE: {path}\n");
                return;
            }
            catch (Exception e)
            {
                Exception excp = e;
                StringBuilder errorMessage = new StringBuilder();
                while (excp != null)
                {
                    errorMessage.Append(excp.Message);
                    excp = excp.InnerException;
                    if (excp != null)
                    {
                        errorMessage.Append("\n\t");
                    }
                }
                Console.WriteLine(
                    $"{consoleDiv}\nUnanticipated Exception for file: {path}\n{errorMessage.ToString()}\n");

                System.Windows.MessageBox.Show(
                    messageBoxText: $"Unanticipated Exception for file: {path}\n{consoleDiv}\n{errorMessage.ToString()}",
                    caption: "Unanticipated Exception");
                return;
            }
            finally
            {
                file?.Dispose();
            }
        }

        public void SetAlbumArt(Album album, string path) => albumCommands.SetAlbumArt(album, path);

        public BitmapImage GetAlbumArtForRecording(Recording recording) => LoadImage(recording.Album.Image);

        public static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            try
            {
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
            catch (NotSupportedException)
            {
                Console.WriteLine("Failed to load image.  Skipping.");
            }
            return null;
        }

        public static Bitmap LoadBitmap(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            try
            {
                Bitmap image;
                using (MemoryStream mem = new MemoryStream(imageData))
                {
                    mem.Position = 0;
                    image = new Bitmap(mem);
                }

                return image;
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("Failed to load image.  Skipping.");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Failed to load image.  Skipping.");
            }

            return null;
        }

        public static byte[] CreateThumbnail(Bitmap sourceImage)
        {
            if (thumbnailEncoder == null || thumbnailEncoderParameters == null)
            {
                thumbnailEncoder = ImageCodecInfo.GetImageDecoders()
                    .Where(x => x.FormatID == ImageFormat.Jpeg.Guid)
                    .First();

                thumbnailEncoderParameters = new EncoderParameters(1);
                thumbnailEncoderParameters.Param[0] = new EncoderParameter(
                    encoder: System.Drawing.Imaging.Encoder.Quality,
                    value: 10L);
            }

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Position = 0;
                    using (Bitmap thumbnailBitmap = new Bitmap(sourceImage, 100, 100))
                    {
                        thumbnailBitmap.Save(
                            stream: ms,
                            encoder: thumbnailEncoder,
                            encoderParams: thumbnailEncoderParameters);
                    }


                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception when trying to create thumbnail stream: {e}");
            }

            return null;
        }

        /// <summary>
        /// Generates songtitle from Tracktitle, returns guess at RecordingType
        /// </summary>
        /// <returns>Guess of RecordingType</returns>
        private static RecordingType CleanUpSongTitle(string trackTitle, out string songTitle)
        {
            RecordingType recordingType = RecordingType.Standard;
            songTitle = trackTitle;

            if (explicitCleanupPattern.IsMatch(songTitle))
            {
                songTitle = explicitCleanupPattern.Replace(songTitle, "");
            }

            if (albumVersionCleanupPattern.IsMatch(songTitle))
            {
                songTitle = albumVersionCleanupPattern.Replace(songTitle, "");
            }

            if (livePatternA.IsMatch(songTitle))
            {
                recordingType = RecordingType.Live;
                songTitle = livePatternA.Replace(songTitle, "");
            }

            if (livePatternB.IsMatch(songTitle))
            {
                recordingType = RecordingType.Live;
                songTitle = livePatternB.Replace(songTitle, "");
            }

            if (livePatternC.IsMatch(songTitle))
            {
                recordingType = RecordingType.Live;
                songTitle = livePatternC.Replace(songTitle, "");
            }

            if (acousticPattern.IsMatch(songTitle))
            {
                recordingType = RecordingType.Acoustic;
                songTitle = acousticPattern.Replace(songTitle, "");
            }

            return recordingType;
        }

        private static RecordingType CleanUpAlbumTitle(
            string loadedTitle,
            RecordingType currentRecordingType,
            out string cleanAlbumTitle,
            ref int discNumber)
        {
            cleanAlbumTitle = loadedTitle;
            if (livePatternA.IsMatch(cleanAlbumTitle))
            {
                currentRecordingType = RecordingType.Live;
                //Lets leave this expression in the title
                //cleanAlbumTitle = livePatternA.Replace(cleanAlbumTitle, "");
            }

            if (livePatternB.IsMatch(cleanAlbumTitle))
            {
                currentRecordingType = RecordingType.Live;
                //Lets leave this expression in the title
                //cleanAlbumTitle = livePatternB.Replace(cleanAlbumTitle, "");
            }

            if (livePatternC.IsMatch(cleanAlbumTitle))
            {
                currentRecordingType = RecordingType.Live;
                //Let's leave this expression in the album title
                //cleanAlbumTitle = livePatternC.Replace(cleanAlbumTitle, "");
            }

            if (unpluggedPattern.IsMatch(cleanAlbumTitle))
            {
                currentRecordingType = RecordingType.Acoustic;
                //Let's leave this expression in the album title
                //cleanAlbumTitle = unpluggedPattern.Replace(cleanAlbumTitle, "");
            }

            if (discNumberPattern.IsMatch(cleanAlbumTitle))
            {
                string discString = discNumberPattern.Match(cleanAlbumTitle).Captures[0].ToString();
                discNumber = int.Parse(numberExtractor.Match(discString).Captures[0].ToString());
                cleanAlbumTitle = discNumberPattern.Replace(cleanAlbumTitle, "");
            }

            return currentRecordingType;
        }

        public void PushMusegicianTagsToFile(
            LoadingUpdater updater,
            IEnumerable<BaseData> data = null)
        {
            updater.SetTitle("Pushing Musegician tags to file.");
            updater.SetSubtitle("Searching Recordings...");

            IEnumerable<Recording> recordings = null;

            if (data == null)
            {
                data = db.Recordings;
            }

            BaseData firstDatum = data.FirstOrDefault();

            if (firstDatum is Recording)
            {
                recordings = data.Cast<Recording>()
                    .Distinct();
            }
            else if (firstDatum is Song)
            {
                recordings = data.Cast<Song>()
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Album)
            {
                recordings = data.Cast<Album>()
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Artist)
            {
                recordings = data.Cast<Artist>()
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else
            {
                throw new ArgumentException($"Unexpected DataType {firstDatum}");
            }

            int count = recordings.Count();
            updater.SetLimit(count);
            updater.SetSubtitle($"Comparing and Updating Tags...  (0/{count})\n");

            int i = 0;
            int modifiedCount = 0;
            string modifiedString = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Recording recording in recordings)
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);
                    if (modifiedCount > 0)
                    {
                        modifiedString = $"Modified {modifiedCount} Files.";
                    }

                    updater.SetSubtitle($"Comparing and Updating Tags...  ({i}/{count})\n{modifiedString}");
                    stopwatch.Restart();
                }

                using (TagLib.File file = TagLib.File.Create(recording.Filename))
                {
                    if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3Tag)
                    {
                        MusegicianTagV2 musegicianTagV2 = null;
                        XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTagV2));

                        TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(id3Tag, "Musegician/Met2", true);
                        if (frame.PrivateData != null)
                        {
                            musegicianTagV2 = serializer.Deserialize(new MemoryStream(frame.PrivateData.Data)) as MusegicianTagV2;
                        }

                        bool write = false;

                        if (musegicianTagV2 == null)
                        {
                            write = true;

                            musegicianTagV2 = new MusegicianTagV2()
                            {
                                ArtistGuid = Guid.Empty,
                                ArtistTimestamp = 0,
                                SongGuid = Guid.Empty,
                                SongTimestamp = 0,
                                AlbumGuid = Guid.Empty,
                                AlbumTimestamp = 0,
                                RecordingType = RecordingType.Standard
                            };
                        }

                        if (musegicianTagV2.ArtistGuid != recording.Artist.ArtistGuid ||
                            musegicianTagV2.ArtistTimestamp != recording.Artist.ArtistGuidTimestamp)
                        {
                            write = true;
                            musegicianTagV2.ArtistGuid = recording.Artist.ArtistGuid;
                            musegicianTagV2.ArtistTimestamp = recording.Artist.ArtistGuidTimestamp;
                        }

                        if (musegicianTagV2.AlbumGuid != recording.Album.AlbumGuid ||
                            musegicianTagV2.AlbumTimestamp != recording.Album.AlbumGuidTimestamp)
                        {
                            write = true;
                            musegicianTagV2.AlbumGuid = recording.Album.AlbumGuid;
                            musegicianTagV2.AlbumTimestamp = recording.Album.AlbumGuidTimestamp;
                        }

                        if (musegicianTagV2.SongGuid != recording.Song.SongGuid ||
                            musegicianTagV2.SongTimestamp != recording.Song.SongGuidTimestamp)
                        {
                            write = true;
                            musegicianTagV2.SongGuid = recording.Song.SongGuid;
                            musegicianTagV2.SongTimestamp = recording.Song.SongGuidTimestamp;
                        }

                        if (musegicianTagV2.SongTitle != recording.Song.Title)
                        {
                            write = true;
                            musegicianTagV2.SongTitle = recording.Song.Title;
                        }

                        if (musegicianTagV2.RecordingType != recording.RecordingType)
                        {
                            write = true;
                            musegicianTagV2.RecordingType = recording.RecordingType;
                        }

                        if (write)
                        {
                            file.Mode = TagLib.File.AccessMode.Write;

                            StringWriter stringData = new StringWriter(new StringBuilder());
                            serializer.Serialize(stringData, musegicianTagV2);

                            frame.PrivateData = Encoding.Unicode.GetBytes(stringData.ToString());

                            file.Save();

                            modifiedCount++;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed!  File: {recording.Filename}");
                    }
                }
            }
        }

        public void PushID3TagsToFile(
            LoadingUpdater updater,
            IEnumerable<BaseData> data = null)
        {
            updater.SetTitle("Updating ID3 Tags");
            updater.SetSubtitle("Searching Recordings...");

            IEnumerable<Recording> recordings = null;

            if (data == null)
            {
                data = db.Recordings;
            }

            BaseData firstDatum = data.FirstOrDefault();

            if (firstDatum is Recording)
            {
                recordings = data.Cast<Recording>()
                    .Distinct();
            }
            else if (firstDatum is Song)
            {
                recordings = data.Cast<Song>()
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Album)
            {
                recordings = data.Cast<Album>()
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Artist)
            {
                recordings = data.Cast<Artist>()
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else
            {
                throw new ArgumentException($"Unexpected DataType {firstDatum}");
            }

            int count = recordings.Count();
            updater.SetLimit(count);
            updater.SetSubtitle($"Comparing and Updating Tags...  (0/{count})\n");

            int i = 0;
            int modifiedCount = 0;
            string modifiedString = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Recording recording in recordings)
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);
                    if (modifiedCount > 0)
                    {
                        modifiedString = $"Modified {modifiedCount} Files.";
                    }

                    updater.SetSubtitle($"Comparing and Updating Tags...  ({i}/{count})\n{modifiedString}");
                    stopwatch.Restart();
                }

                try
                {
                    using (TagLib.File file = TagLib.File.Create(recording.Filename))
                    {
                        bool write = false;
                        if (file.Tag.Performers.Length == 0 ||
                            (file.Tag.Performers.Length == 1 && file.Tag.Performers[0] != recording.Artist.Name) ||
                            (file.Tag.Performers.Length > 1 && string.Join("/", file.Tag.Performers) != recording.Artist.Name))
                        {
                            write = true;
                            file.Tag.Performers = new string[] { recording.Artist.Name };
                        }

                        if (file.Tag.AlbumArtists.Length == 0 ||
                            (file.Tag.AlbumArtists.Length == 1 && file.Tag.AlbumArtists[0] != recording.Artist.Name) ||
                            (file.Tag.AlbumArtists.Length > 1 && string.Join("/", file.Tag.AlbumArtists) != recording.Artist.Name))
                        {
                            write = true;
                            file.Tag.AlbumArtists = new string[] { recording.Artist.Name };
                        }

                        if (file.Tag.Album != recording.Album.Title)
                        {
                            write = true;
                            file.Tag.Album = recording.Album.Title;
                        }

                        if (file.Tag.Title != recording.Title)
                        {
                            write = true;
                            file.Tag.Title = recording.Title;
                        }

                        if (file.Tag.Year != recording.Album.Year)
                        {
                            write = true;
                            file.Tag.Year = (uint)recording.Album.Year;
                        }

                        if (file.Tag.Track != recording.TrackNumber)
                        {
                            write = true;
                            file.Tag.Track = (uint)recording.TrackNumber;
                        }

                        if (file.Tag.Disc != recording.DiscNumber)
                        {
                            write = true;
                            file.Tag.Disc = (uint)recording.DiscNumber;
                        }

                        if (write)
                        {
                            file.Mode = TagLib.File.AccessMode.Write;
                            file.Save();
                            modifiedCount++;
                        }
                    }
                }
                catch (TagLib.UnsupportedFormatException)
                {
                    Console.WriteLine($"{consoleDiv}\nSkipping UNSUPPORTED FILE: {recording.Filename}\n");
                    continue;
                }
                catch (TagLib.CorruptFileException)
                {
                    Console.WriteLine($"{consoleDiv}\nSkipping CORRUPT FILE: {recording.Filename}\n");
                    continue;
                }
                catch (IOException)
                {
                    Console.WriteLine($"{consoleDiv}\nSkipping Writing Tag To FILE IN USE: {recording.Filename}\n");
                    continue;
                }
            }
        }

        public void PushMusegicianAlbumArtToFile(
            LoadingUpdater updater,
            IEnumerable<BaseData> data = null)
        {
            if (data == null)
            {
                data = db.Albums;
            }

            updater.SetTitle("Updating ID3 Tags with Album Art");
            int count = data.Count();
            updater.SetSubtitle($"Comparing and Updating Albums...  (0/{count})\n");

            updater.SetLimit(count);


            int i = 0;
            int modifiedFiles = 0;
            int modifiedRecords = 0;
            string modifiedString = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Album album in data.OfType<Album>().Distinct())
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);
                    if (modifiedRecords > 0 && modifiedFiles > 0)
                    {
                        modifiedString = $"Modified {modifiedFiles} Files and {modifiedRecords} Records.";
                    }
                    else if (modifiedFiles > 0)
                    {
                        modifiedString = $"Modified {modifiedFiles} Files.";
                    }
                    else if (modifiedRecords > 0)
                    {
                        modifiedString = $"Modified {modifiedRecords} Records.";
                    }

                    updater.SetSubtitle($"Comparing and Updating Albums...  ({i}/{count})\n{modifiedString}");
                    stopwatch.Restart();
                }

                foreach (Recording recording in album.Recordings
                    .OrderBy(x => x.DiscNumber)
                    .ThenBy(x => x.TrackNumber))
                {
                    //Update the first song on the album (whose id3 tag points to the album) with the art
                    try
                    {
                        using (TagLib.File audioFile = TagLib.File.Create(recording.Filename))
                        {

                            string tagName = audioFile.Tag.Album?.ToLowerInvariant() ?? "undefined";
                            string lowerAlbumTitle = album.Title.ToLowerInvariant();

                            if (tagName != lowerAlbumTitle && !tagName.Contains(lowerAlbumTitle))
                            {
                                Console.WriteLine($"Album Title doesn't match. Skipping : {album.Title} / {tagName}");
                                continue;
                            }

                            if (album.Image == null || album.Image.Length == 0)
                            {
                                //See if any of the tracks have an image
                                if (audioFile.Tag.Pictures != null && audioFile.Tag.Pictures.Length > 0)
                                {
                                    using (Bitmap loadedBitmap = LoadBitmap(audioFile.Tag.Pictures[0].Data.Data))
                                    {
                                        //If image can be loaded...
                                        if (loadedBitmap != null)
                                        {
                                            album.Image = audioFile.Tag.Pictures[0].Data.Data;
                                            album.Thumbnail = CreateThumbnail(loadedBitmap);

                                            modifiedRecords++;

                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //See if the first matching track has the image stored
                                if (audioFile.Tag.Pictures == null ||
                                    audioFile.Tag.Pictures.Length == 0 ||
                                    audioFile.Tag.Pictures[0].Data.Data.Length != album.Image.Length)
                                {
                                    //No picture Or Mismatched Picture - Create/Update
                                    audioFile.Mode = TagLib.File.AccessMode.Write;
                                    audioFile.Tag.Pictures = new TagLib.IPicture[1] { new TagLib.Picture(album.Image) };
                                    audioFile.Save();

                                    modifiedFiles++;

                                    break;
                                }

                            }
                        }
                    }
                    catch (TagLib.UnsupportedFormatException)
                    {
                        Console.WriteLine($"{consoleDiv}\nSkipping UNSUPPORTED FILE: {recording.Filename}\n");
                        continue;
                    }
                    catch (TagLib.CorruptFileException)
                    {
                        Console.WriteLine($"{consoleDiv}\nSkipping CORRUPT FILE: {recording.Filename}\n");
                        continue;
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"{consoleDiv}\nSkipping Writing Tag To FILE IN USE: {recording.Filename}\n");
                        continue;
                    }
                }

            }
        }

        public void UpdateAlbumArtThumbnails(
            LoadingUpdater updater,
            IEnumerable<BaseData> data = null)
        {
            if (data == null)
            {
                data = db.Albums;
            }

            updater.SetTitle("Updating Album Art Thumbnails");
            int count = data.Count();
            updater.SetSubtitle($"Comparing and Updating Albums...  (0/{count})\n");

            updater.SetLimit(count);


            int i = 0;
            int modifiedFiles = 0;
            int modifiedRecords = 0;
            string modifiedString = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Album album in data.OfType<Album>().Distinct())
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);
                    if (modifiedRecords > 0 && modifiedFiles > 0)
                    {
                        modifiedString = $"Modified {modifiedFiles} Files and {modifiedRecords} Records.";
                    }
                    else if (modifiedFiles > 0)
                    {
                        modifiedString = $"Modified {modifiedFiles} Files.";
                    }
                    else if (modifiedRecords > 0)
                    {
                        modifiedString = $"Modified {modifiedRecords} Records.";
                    }

                    updater.SetSubtitle($"Comparing and Updating Albums...  ({i}/{count})\n{modifiedString}");
                    stopwatch.Restart();
                }

                if (album.Image != null && album.Image.Length > 0)
                {
                    using (Bitmap loadedBitmap = LoadBitmap(album.Image))
                    {
                        //If image can be loaded...
                        if (loadedBitmap != null)
                        {
                            album.Thumbnail = CreateThumbnail(loadedBitmap);
                        }
                    }
                }
            }

            db.SaveChanges();
        }

        public void CleanChildlessRecords()
        {
            db.Songs.RemoveRange(
                from song in db.Songs
                where song.Recordings.Count == 0
                select song);

            db.Artists.RemoveRange(
                from artist in db.Artists
                where artist.Recordings.Count == 0
                select artist);

            db.Albums.RemoveRange(
                from album in db.Albums
                where album.Recordings.Count == 0
                select album);

            db.PlaylistSongs.RemoveRange(
                from plSong in db.PlaylistSongs
                where plSong.PlaylistRecordings.Count == 0
                select plSong);

            db.Playlists.RemoveRange(
                from playlist in db.Playlists
                where playlist.PlaylistSongs.Count == 0
                select playlist);

            db.SaveChanges();
        }

        public void CleanupMissingFiles(
            LoadingUpdater updater)
        {
            updater.SetTitle("Cleaning Up Missing Files");

            int count = db.Recordings.Count();

            updater.SetSubtitle($"Searching Recordings For Missing Files...  (0/{count})\n");
            updater.SetLimit(count);

            List<Recording> missingRecordings = new List<Recording>();

            int i = 0;
            string modifiedString = "";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (Recording recording in db.Recordings)
            {
                i++;
                if (stopwatch.ElapsedMilliseconds > Settings.BarUpdatePeriod)
                {
                    updater.SetProgress(i);
                    if (missingRecordings.Count > 0)
                    {
                        modifiedString = $"Identified {missingRecordings.Count} Dead Records.";
                    }

                    updater.SetSubtitle($"Searching Recordings For Missing Files...  ({i}/{count})\n{modifiedString}");
                    stopwatch.Restart();
                }

                if (!File.Exists(recording.Filename))
                {
                    missingRecordings.Add(recording);
                }
            }

            if (missingRecordings.Count == 0)
            {
                return;
            }

            updater.SetSubtitle($"Removing {missingRecordings.Count} Missing Recordings...");

            db.PlaylistRecordings.RemoveRange(
                (from recording in missingRecordings
                 join plRec in db.PlaylistRecordings on recording.Id equals plRec.RecordingId
                 select plRec).Distinct());

            db.Recordings.RemoveRange(missingRecordings);
            db.SaveChanges();

            updater.SetSubtitle("Scrubbing Childless Database Entries...");
            CleanChildlessRecords();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                db.Dispose();
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
