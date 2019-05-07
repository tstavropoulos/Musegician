using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using Musegician.Core.DBCommands;
using Musegician.Database;
using System.Text;

using MusegicianTag = Musegician.Core.MusegicianTag;
using LoadingUpdater = Musegician.LoadingDialog.LoadingDialog.LoadingUpdater;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Musegician
{
    #region Exceptions

    public class LibraryContextException : Exception
    {
        public LibraryContextException(string message) : base(message) { }
    }

    #endregion Exceptions

    public sealed partial class FileManager : IDisposable
    {
        #region Data

        private readonly List<string> supportedFileTypes = new List<string>() { "*.mp3", "*.flac", "*.ogg" };
        private readonly string[] songNameDelimiter = new string[] { " - " };
        private readonly string consoleDiv = "---------------------------------------";

        #region RegEx

        /// <summary>
        /// Identifies text of the form (Live*) or [Live*].
        /// </summary>
        private const string livePatternA = @"(\s*?[\(\[][Ll]ive.*?[\)\]])";
        /// <summary>
        /// Identifies text of the form (Bootleg*) or [Bootleg*].
        /// </summary>
        private const string livePatternB = @"(\s*?[\(\[][Bb]ootleg.*?[\)\]])";
        /// <summary>
        /// Identifies text of the form (At The*) or [At The*].
        /// </summary>
        private const string livePatternC = @"(\s*?[\(\[][Aa]t [Tt]he.*?[\)\]])";
        /// <summary>
        /// Identifies text of the form (Acoustic*) or [Acoustic*].
        /// Ex: Sting - A Day In The Life (Acoustic)
        /// </summary>
        private const string acousticPattern = @"(\s*?[\(\[][Aa]coustic.*?[\)\]])";
        /// <summary>
        /// Identifies text of the form (Explicit*) or [Explicit*].
        /// Ex: Queens of the Stone Age - Song For The Dead [Explicit]
        /// </summary>
        private const string explicitCleanupPattern = @"(\s*?[\(\[][Ee]xplicit.*?[\)\]])";
        /// <summary>
        /// Identifies text of the form (Album*) or [Album*].
        /// Ex: [Album Version]
        /// </summary>
        private const string albumVersionCleanupPattern = @"(\s*?[\(\[][Aa]lbum.*?[\)\]])";
        /// <summary>
        /// Identifies text of the form (Disc #) or [Disc #].
        /// Ex: Physical Graffiti (Disc 1)
        /// </summary>
        private const string discNumberPattern = @"(\s*?[\(\[][Dd]isc.*?\d+[\)\]])";
        /// <summary>
        /// Captures and extracts numbers
        /// </summary>
        private const string numberExtractor = @"(\d+)";

        #endregion RegEx

        private RecordingCommands recordingCommands = null;
        private TrackCommands trackCommands = null;
        private SongCommands songCommands = null;
        private AlbumCommands albumCommands = null;
        private ArtistCommands artistCommands = null;

        private PlaylistCommands playlistCommands = null;

        /// <summary>
        /// A long-running Database reference
        /// </summary>
        private MusegicianData db = null;

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
            trackCommands = new TrackCommands(db);
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
                trackCommands._DropTable();

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
                                 join track in db.Tracks on album.Id equals track.AlbumId
                                 select new { track.Recording.Artist, Album = album }).Distinct())
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

                //Lookup loading is FAST - skipDB threshold is therefore low
                bool skipDB = newMusic.Count >= 10;

                if (skipDB)
                {
                    updater.SetSubtitle("Reading Database...");
                    PopulateLookups(lookups);
                }

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
                        lookups: lookups,
                        skipDB: skipDB);
                }


                db.SaveChanges();
            }
            finally
            {
                db.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        private void LoadFileData(
            string path,
            Lookups lookups,
            bool skipDB)
        {
            TagLib.File file = null;

            try
            {
                file = TagLib.File.Create(path);

                MusegicianTag musegicianTag = null;
                bool newMusegicianTag = false;

                // Reading custom Musegician frame
                if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3TagRead)
                {
                    TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(id3TagRead, "Musegician/Meta", true);

                    if (frame.PrivateData != null)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTag));
                        musegicianTag = serializer.Deserialize(new MemoryStream(frame.PrivateData.Data)) as MusegicianTag;
                    }
                }

                if (musegicianTag == null)
                {
                    musegicianTag = new MusegicianTag()
                    {
                        Live = false,
                        AlbumGuid = Guid.Empty,
                        ArtistGuid = Guid.Empty,
                        SongGuid = Guid.Empty
                    };

                    newMusegicianTag = true;
                }

                TagLib.Tag tag = file.Tag;

                //Handle Artist
                string artistName = "UNDEFINED";
                if (!string.IsNullOrEmpty(tag.JoinedPerformers))
                {
                    artistName = tag.JoinedPerformers;
                }

                Artist artist = null;

                if (!newMusegicianTag)
                {
                    //Tag was loaded
                    //Find in lookups by Guid
                    if (lookups.ArtistGuidLookup.ContainsKey(musegicianTag.ArtistGuid))
                    {
                        artist = lookups.ArtistGuidLookup[musegicianTag.ArtistGuid];
                    }

                    //Find in DB by Guid
                    if (artist == null && !skipDB)
                    {
                        artist = db.Artists
                            .Where(x => x.ArtistGuid == musegicianTag.ArtistGuid)
                            .FirstOrDefault();

                        if (artist != null)
                        {
                            //If we found it, add it to the lookup
                            lookups.ArtistGuidLookup.Add(musegicianTag.ArtistGuid, artist);
                        }
                    }
                }
                else
                {
                    //Tag is new
                    //Find in lookups by name
                    if (lookups.ArtistNameLookup.ContainsKey(artistName.ToLowerInvariant()))
                    {
                        artist = lookups.ArtistNameLookup[artistName.ToLowerInvariant()];
                    }

                    //Find in DB by name
                    if (artist == null && !skipDB)
                    {
                        artist = db.Artists
                            .Where(x => x.Name == artistName)
                            .FirstOrDefault();

                        if (artist != null)
                        {
                            //If we found it, add it to the lookup
                            lookups.ArtistNameLookup.Add(artistName.ToLowerInvariant(), artist);
                        }
                    }

                    //If artist was found, populate ArtistGuid into tag
                    musegicianTag.ArtistGuid = artist?.ArtistGuid ?? Guid.NewGuid();
                }

                //Create
                if (artist == null)
                {
                    artist = new Artist()
                    {
                        Name = artistName,
                        Weight = -1.0,
                        ArtistGuid = musegicianTag.ArtistGuid
                    };

                    db.Artists.Add(artist);
                    lookups.ArtistGuidLookup.Add(musegicianTag.ArtistGuid, artist);
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
                string songTitle = "";

                if (!newMusegicianTag)
                {
                    //Tag was loaded
                    //Find in lookups by Guid
                    if (lookups.SongGuidLookup.ContainsKey(musegicianTag.SongGuid))
                    {
                        song = lookups.SongGuidLookup[musegicianTag.SongGuid];
                    }

                    //Find in DB by Guid
                    if (song == null && !skipDB)
                    {
                        song = db.Songs
                            .Where(x => x.SongGuid == musegicianTag.SongGuid)
                            .FirstOrDefault();

                        if (song != null)
                        {
                            //If we found it, add it to the lookup
                            lookups.SongGuidLookup.Add(song.SongGuid, song);
                        }
                    }

                    if (song == null)
                    {
                        //Clean up title for record creation
                        CleanUpSongTitle(trackTitle, out songTitle);
                    }
                }
                else
                {
                    //Tag is new
                    //we must search clean up name and search
                    musegicianTag.Live |= CleanUpSongTitle(trackTitle, out songTitle);

                    //find in lookups by (artist, name)
                    if (lookups.SongNameLookup.ContainsKey((artist, songTitle.ToLowerInvariant())))
                    {
                        song = lookups.SongNameLookup[(artist, songTitle.ToLowerInvariant())];
                    }

                    if (song == null && !skipDB)
                    {
                        //Find in db by name, matching artist
                        song = db.Recordings.Where(x => x.ArtistId == artist.Id)
                            .Select(x => x.Song)
                            .Distinct()
                            .Where(x => x.Title == songTitle)
                            .FirstOrDefault();

                        if (song != null)
                        {
                            //If we found it, add it to the lookup
                            lookups.SongNameLookup.Add((artist, songTitle), song);
                        }
                    }


                    musegicianTag.SongGuid = song?.SongGuid ?? Guid.NewGuid();
                }

                if (song == null)
                {
                    //Create the song
                    song = new Song()
                    {
                        Title = songTitle,
                        Weight = -1.0,
                        SongGuid = musegicianTag.SongGuid
                    };

                    db.Songs.Add(song);
                    lookups.SongGuidLookup.Add(musegicianTag.SongGuid, song);
                    if (!lookups.SongNameLookup.ContainsKey((artist, songTitle.ToLowerInvariant())))
                    {
                        lookups.SongNameLookup.Add((artist, songTitle.ToLowerInvariant()), song);
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

                if (!newMusegicianTag)
                {
                    //Tag was loaded
                    //Find in lookups by Guid
                    if (lookups.AlbumGuidLookup.ContainsKey(musegicianTag.AlbumGuid))
                    {
                        album = lookups.AlbumGuidLookup[musegicianTag.AlbumGuid];
                    }

                    if (album == null && !skipDB)
                    {
                        //Find in DB by Guid
                        album = db.Albums
                            .Where(x => x.AlbumGuid == musegicianTag.AlbumGuid)
                            .FirstOrDefault();

                        if (album != null)
                        {
                            //If we found it, add it to the lookup
                            lookups.AlbumGuidLookup.Add(album.AlbumGuid, album);
                        }
                    }

                    if (album == null)
                    {
                        //Clean up title for record creation
                        CleanUpAlbumTitle(albumTitle, out cleanAlbumTitle, ref discNumber);
                    }
                }
                else
                {
                    //Tag is new
                    //We must search and clean up the name
                    musegicianTag.Live |= CleanUpAlbumTitle(albumTitle, out cleanAlbumTitle, ref discNumber);

                    //search in lookups for (artist,albumname)
                    if (lookups.AlbumNameLookup.ContainsKey((artist, cleanAlbumTitle.ToLowerInvariant())))
                    {
                        album = lookups.AlbumNameLookup[(artist, cleanAlbumTitle.ToLowerInvariant())];
                    }

                    if (album == null && !skipDB)
                    {
                        //Search in db for albums featuring artist, with matching name
                        album = db.Tracks.Where(x => x.Recording.ArtistId == artist.Id)
                            .Select(x => x.Album)
                            .Distinct()
                            .Where(x => x.Title == cleanAlbumTitle).FirstOrDefault();

                        if (album != null)
                        {
                            //If we found it, add it to the lookup
                            lookups.AlbumNameLookup.Add((artist, cleanAlbumTitle.ToLowerInvariant()), album);
                        }
                    }


                    musegicianTag.AlbumGuid = album?.AlbumGuid ?? Guid.NewGuid();
                }

                if (album == null)
                {
                    album = new Album()
                    {
                        Title = cleanAlbumTitle,
                        Year = (int)tag.Year,
                        Weight = -1.0,
                        Image = null,
                        AlbumGuid = musegicianTag.AlbumGuid
                    };

                    db.Albums.Add(album);
                    lookups.AlbumGuidLookup.Add(musegicianTag.AlbumGuid, album);
                    if (!lookups.AlbumNameLookup.ContainsKey((artist, cleanAlbumTitle.ToLowerInvariant())))
                    {
                        lookups.AlbumNameLookup.Add((artist, cleanAlbumTitle.ToLowerInvariant()), album);
                    }
                }

                if (tag.Pictures.Length > 0 && album.Image == null)
                {
                    //Try to open it
                    if (LoadImage(file.Tag.Pictures[0].Data.Data) != null)
                    {
                        album.Image = file.Tag.Pictures[0].Data.Data;
                    }
                }

                Recording recording = new Recording()
                {
                    Artist = artist,
                    Song = song,
                    Filename = path,
                    Live = musegicianTag.Live
                };
                db.Recordings.Add(recording);

                Track track = new Track()
                {
                    Album = album,
                    Recording = recording,
                    Title = trackTitle,
                    TrackNumber = (int)tag.Track,
                    DiscNumber = discNumber,
                    Weight = -1.0
                };
                db.Tracks.Add(track);

                if (newMusegicianTag && Settings.Instance.CreateMusegicianTags)
                {
                    if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3TagWrite)
                    {
                        file.Mode = TagLib.File.AccessMode.Write;

                        TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(id3TagWrite, "Musegician/Meta", true);

                        StringWriter data = new StringWriter(new StringBuilder());
                        XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTag));
                        serializer.Serialize(data, musegicianTag);

                        frame.PrivateData = Encoding.Unicode.GetBytes(data.ToString());

                        file.Save();
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

        public void SetAlbumArt(Album album, string path)
        {
            albumCommands.SetAlbumArt(album, path);
        }

        public BitmapImage GetAlbumArtForRecording(Recording recording)
        {
            return LoadImage(recording.Tracks.First().Album.Image);
        }

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

        /// <summary>
        /// Generates songtitle from Tracktitle, returns whether it thinks the song is a live recording
        /// </summary>
        /// <returns>If the song is live</returns>
        private static bool CleanUpSongTitle(string trackTitle, out string songTitle)
        {
            bool live = false;
            songTitle = trackTitle;

            if (Regex.IsMatch(songTitle, explicitCleanupPattern))
            {
                songTitle = Regex.Replace(songTitle, explicitCleanupPattern, "");
            }

            if (Regex.IsMatch(songTitle, albumVersionCleanupPattern))
            {
                songTitle = Regex.Replace(songTitle, albumVersionCleanupPattern, "");
            }

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

            if (Regex.IsMatch(songTitle, livePatternC))
            {
                live = true;
                songTitle = Regex.Replace(songTitle, livePatternC, "");
            }

            if (Regex.IsMatch(songTitle, acousticPattern))
            {
                //Do we want acoustic tracks marked live?  I don't think so, in general
                //live = true;
                songTitle = Regex.Replace(songTitle, acousticPattern, "");
            }

            return live;
        }

        private static bool CleanUpAlbumTitle(string loadedTitle, out string cleanAlbumTitle, ref int discNumber)
        {
            bool live = false;
            cleanAlbumTitle = loadedTitle;
            if (Regex.IsMatch(cleanAlbumTitle, livePatternA))
            {
                live = true;
                //Lets leave this expression in the title
                //cleanAlbumTitle = Regex.Replace(cleanAlbumTitle, livePatternA, "");
            }

            if (Regex.IsMatch(cleanAlbumTitle, livePatternB))
            {
                live = true;
                //Lets leave this expression in the title
                //cleanAlbumTitle = Regex.Replace(cleanAlbumTitle, livePatternB, "");
            }

            if (Regex.IsMatch(cleanAlbumTitle, livePatternC))
            {
                live = true;
                //Let's leave this expression in the album title
                //cleanAlbumTitle = Regex.Replace(cleanAlbumTitle, livePatternC, "");
            }

            if (Regex.IsMatch(cleanAlbumTitle, discNumberPattern))
            {
                string discString = Regex.Match(cleanAlbumTitle, discNumberPattern).Captures[0].ToString();
                discNumber = int.Parse(Regex.Match(discString, numberExtractor).Captures[0].ToString());
                cleanAlbumTitle = Regex.Replace(cleanAlbumTitle, discNumberPattern, "");
            }

            return live;
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
                recordings = data.Select(x => x as Recording)
                    .Distinct();
            }
            else if (firstDatum is Song)
            {
                recordings = data.Select(x => x as Song)
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Album)
            {
                recordings = data.Select(x => x as Album)
                    .SelectMany(x => x.Tracks)
                    .Select(x => x.Recording)
                    .Distinct();
            }
            else if (firstDatum is Artist)
            {
                recordings = data.Select(x => x as Artist)
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Track)
            {
                recordings = data.Select(x => x as Track)
                    .Select(x => x.Recording)
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

                MusegicianTag musegicianTag = null;
                using (TagLib.File file = TagLib.File.Create(recording.Filename))
                {
                    if (file.GetTag(TagLib.TagTypes.Id3v2, true) is TagLib.Id3v2.Tag id3Tag)
                    {
                        TagLib.Id3v2.PrivateFrame frame = TagLib.Id3v2.PrivateFrame.Get(id3Tag, "Musegician/Meta", true);

                        XmlSerializer serializer = new XmlSerializer(typeof(MusegicianTag));

                        if (frame.PrivateData != null)
                        {
                            musegicianTag = serializer.Deserialize(new MemoryStream(frame.PrivateData.Data)) as MusegicianTag;
                        }

                        bool write = false;

                        if (musegicianTag == null)
                        {
                            write = true;

                            musegicianTag = new MusegicianTag()
                            {
                                ArtistGuid = Guid.Empty,
                                SongGuid = Guid.Empty,
                                AlbumGuid = Guid.Empty,
                                Live = false
                            };
                        }

                        if (musegicianTag.ArtistGuid != recording.Artist.ArtistGuid)
                        {
                            write = true;
                            musegicianTag.ArtistGuid = recording.Artist.ArtistGuid;
                        }

                        if (musegicianTag.AlbumGuid != recording.Tracks.First().Album.AlbumGuid)
                        {
                            write = true;
                            musegicianTag.AlbumGuid = recording.Tracks.First().Album.AlbumGuid;
                        }

                        if (musegicianTag.SongGuid != recording.Song.SongGuid)
                        {
                            write = true;
                            musegicianTag.SongGuid = recording.Song.SongGuid;
                        }

                        if (musegicianTag.Live != recording.Live)
                        {
                            write = true;
                            musegicianTag.Live = recording.Live;
                        }

                        if (write)
                        {
                            file.Mode = TagLib.File.AccessMode.Write;

                            StringWriter stringData = new StringWriter(new StringBuilder());
                            serializer.Serialize(stringData, musegicianTag);

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
                recordings = data.Select(x => x as Recording)
                    .Distinct();
            }
            else if (firstDatum is Song)
            {
                recordings = data.Select(x => x as Song)
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Album)
            {
                recordings = data.Select(x => x as Album)
                    .SelectMany(x => x.Tracks)
                    .Select(x => x.Recording)
                    .Distinct();
            }
            else if (firstDatum is Artist)
            {
                recordings = data.Select(x => x as Artist)
                    .SelectMany(x => x.Recordings)
                    .Distinct();
            }
            else if (firstDatum is Track)
            {
                recordings = data.Select(x => x as Track)
                    .Select(x => x.Recording)
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
                            (file.Tag.Performers.Length > 1 && String.Join("/", file.Tag.Performers) != recording.Artist.Name))
                        {
                            write = true;
                            file.Tag.Performers = new string[] { recording.Artist.Name };
                        }

                        if (file.Tag.AlbumArtists.Length == 0 ||
                            (file.Tag.AlbumArtists.Length == 1 && file.Tag.AlbumArtists[0] != recording.Artist.Name) ||
                            (file.Tag.AlbumArtists.Length > 1 && String.Join("/", file.Tag.AlbumArtists) != recording.Artist.Name))
                        {
                            write = true;
                            file.Tag.AlbumArtists = new string[] { recording.Artist.Name };
                        }

                        Track firstTrack = recording.Tracks.First();

                        if (file.Tag.Album != firstTrack.Album.Title)
                        {
                            write = true;
                            file.Tag.Album = firstTrack.Album.Title;
                        }

                        if (file.Tag.Title != firstTrack.Title)
                        {
                            write = true;
                            file.Tag.Title = firstTrack.Title;
                        }

                        if (file.Tag.Year != firstTrack.Album.Year)
                        {
                            write = true;
                            file.Tag.Year = (uint)firstTrack.Album.Year;
                        }

                        if (file.Tag.Track != firstTrack.TrackNumber)
                        {
                            write = true;
                            file.Tag.Track = (uint)firstTrack.TrackNumber;
                        }

                        if (file.Tag.Disc != firstTrack.DiscNumber)
                        {
                            write = true;
                            file.Tag.Disc = (uint)firstTrack.DiscNumber;
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

                foreach (Recording recording in album.Tracks
                    .OrderBy(x => x.DiscNumber)
                    .ThenBy(x => x.TrackNumber)
                    .Select(x => x.Recording))
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
                                    if (LoadImage(audioFile.Tag.Pictures[0].Data.Data) != null)
                                    {
                                        album.Image = audioFile.Tag.Pictures[0].Data.Data;

                                        modifiedRecords++;

                                        break;
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

        public void CleanChildlessRecords()
        {
            //Remove orphaned recordings
            db.Recordings.RemoveRange(
                from recording in db.Recordings
                where recording.Tracks.Count == 0
                select recording);

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
                where album.Tracks.Count == 0
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
                if (disposing)
                {
                    db?.Dispose();
                    db = null;
                }

                // TODO: set large fields to null.

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
