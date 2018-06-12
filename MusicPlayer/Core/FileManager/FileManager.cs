using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using Musegician.Core.DBCommands;
using Musegician.Database;
using MusegicianTag = Musegician.Core.MusegicianTag;
using System.Text;

namespace Musegician
{
    #region Exceptions

    public class LibraryContextException : Exception
    {
        public LibraryContextException(string message) : base(message) { }
    }

    #endregion Exceptions

    public partial class FileManager : IDisposable
    {
        #region Data

        private readonly List<string> supportedFileTypes = new List<string>() { "*.mp3", "*.flac", "*.ogg" };
        private readonly string[] songNameDelimiter = new string[] { " - " };

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
            HashSet<string> loadedFilenames = new HashSet<string>();

            AddMusicDirectory(path, newMusic, loadedFilenames);

            Lookups lookups = new Lookups();
            try
            {
                db.Configuration.AutoDetectChangesEnabled = false;

                foreach (string songFilename in newMusic)
                {
                    LoadFileData(
                        path: songFilename,
                        lookups: lookups,
                        db: db);
                }
            }
            finally
            {
                db.Configuration.AutoDetectChangesEnabled = true;
            }

            db.SaveChanges();
        }

        private void LoadFileData(
            string path,
            Lookups lookups,
            MusegicianData db)
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
                //Find in DB by Guid
                artist = db.Artists
                    .Where(x => x.ArtistGuid == musegicianTag.ArtistGuid)
                    .FirstOrDefault();

                //Find in lookups by Guid
                if (artist == null &&
                    lookups.ArtistGuidLookup.ContainsKey(musegicianTag.ArtistGuid))
                {
                    artist = lookups.ArtistGuidLookup[musegicianTag.ArtistGuid];
                }
            }
            else
            {
                //Tag is new
                //Find in DB by name
                artist = db.Artists
                    .Where(x => x.Name == artistName)
                    .FirstOrDefault();

                //Find in lookups by name
                if (artist == null &&
                    lookups.ArtistNameLookup.ContainsKey(artistName.ToLowerInvariant()))
                {
                    artist = lookups.ArtistNameLookup[artistName.ToLowerInvariant()];
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
                //Find in DB by Guid
                song = db.Songs
                    .Where(x => x.SongGuid == musegicianTag.SongGuid)
                    .FirstOrDefault();

                //Find in lookups by Guid
                if (song == null &&
                    lookups.SongGuidLookup.ContainsKey(musegicianTag.SongGuid))
                {
                    song = lookups.SongGuidLookup[musegicianTag.SongGuid];
                }

                if (song == null)
                {
                    //Clean up title
                    CleanUpSongTitle(trackTitle, out songTitle);
                }
            }
            else
            {
                //Tag is new
                //we must search clean up name and search
                musegicianTag.Live |= CleanUpSongTitle(trackTitle, out songTitle);

                //Find in db by name, matching artist
                song = db.Recordings.Where(x => x.ArtistId == artist.Id)
                    .Select(x => x.Song)
                    .Distinct()
                    .Where(x => x.Title == songTitle)
                    .FirstOrDefault();

                //find in lookups by (artist, name)
                if (song == null &&
                    lookups.SongNameLookup.ContainsKey((artist, songTitle.ToLowerInvariant())))
                {
                    song = lookups.SongNameLookup[(artist, songTitle.ToLowerInvariant())];
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
                //Find in DB by Guid
                album = db.Albums
                    .Where(x => x.AlbumGuid == musegicianTag.AlbumGuid)
                    .FirstOrDefault();

                //Find in lookups by Guid
                if (album == null &&
                    lookups.AlbumGuidLookup.ContainsKey(musegicianTag.AlbumGuid))
                {
                    album = lookups.AlbumGuidLookup[musegicianTag.AlbumGuid];
                }

                if (album == null)
                {
                    //Clean up title
                    CleanUpAlbumTitle(albumTitle, out cleanAlbumTitle, ref discNumber);
                }
            }
            else
            {
                //Tag is new
                //We must search and clean up the name
                musegicianTag.Live |= CleanUpAlbumTitle(albumTitle, out cleanAlbumTitle, ref discNumber);

                //Search in db for albums featuring artist, with matching name
                album = db.Tracks.Where(x => x.Recording.ArtistId == artist.Id)
                    .Select(x => x.Album)
                    .Distinct()
                    .Where(x => x.Title == cleanAlbumTitle).FirstOrDefault();

                //search in lookups for (artist,albumname)
                if (album == null &&
                    lookups.AlbumNameLookup.ContainsKey((artist, cleanAlbumTitle.ToLowerInvariant())))
                {
                    album = lookups.AlbumNameLookup[(artist, cleanAlbumTitle.ToLowerInvariant())];
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

            if (newMusegicianTag)
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

        public void PushMusegicianTagsToFiles()
        {
            foreach (Recording recording in db.Recordings)
            {
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

                            StringWriter data = new StringWriter(new StringBuilder());
                            serializer.Serialize(data, musegicianTag);

                            frame.PrivateData = Encoding.Unicode.GetBytes(data.ToString());

                            file.Save();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed!  File: {recording.Filename}");
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
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
