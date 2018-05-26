using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Musegician.DataStructures;
using Musegician.Core.DBCommands;
using Musegician.Database;

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
        #region Singleton Implementation

        public static FileManager Instance;

        public FileManager()
        {
            recordingCommands = new RecordingCommands();
            trackCommands = new TrackCommands();
            songCommands = new SongCommands();
            albumCommands = new AlbumCommands();
            artistCommands = new ArtistCommands();

            playlistCommands = new PlaylistCommands();

            db = new MusegicianData();

            Initialize();
        }

        #endregion Singleton Implementation

        public void DropDB()
        {
            artistCommands._DropTable();
            albumCommands._DropTable();
            songCommands._DropTable();
            recordingCommands._DropTable();
            trackCommands._DropTable();

            playlistCommands._DropTable();

            db.SaveChanges();

            Initialize();
        }

        public void Initialize()
        {
            recordingCommands.Initialize(
                db: db,
                artistCommands: artistCommands,
                songCommands: songCommands);

            trackCommands.Initialize(
                db: db,
                artistCommands: artistCommands,
                songCommands: songCommands,
                albumCommands: albumCommands,
                recordingCommands: recordingCommands);

            songCommands.Initialize(
                db: db,
                artistCommands: artistCommands,
                trackCommands: trackCommands,
                recordingCommands: recordingCommands,
                playlistCommands: playlistCommands);

            albumCommands.Initialize(
                db: db,
                artistCommands: artistCommands,
                songCommands: songCommands,
                trackCommands: trackCommands,
                recordingCommands: recordingCommands);

            artistCommands.Initialize(
                db: db,
                albumCommands: albumCommands,
                songCommands: songCommands,
                recordingCommands: recordingCommands);

            playlistCommands.Initialize(
                db: db);
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

            foreach (string songFilename in newMusic)
            {
                LoadFileData(
                    path: songFilename,
                    db: db);
            }

            db.SaveChanges();
        }

        private void LoadFileData(
            string path,
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

            Artist artist = db.Artists.Local
                .Where(x => x.Name == artistName)
                .FirstOrDefault();
            
            if (artist == null)
            {
                artist = new Artist()
                {
                    Name = artistName,
                    Weight = double.NaN
                };
                
                db.Artists.Add(artist);
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

            if (Regex.IsMatch(songTitle, acousticPattern))
            {
                //Do we want acoustic tracks marked live?  I don't think so, in general
                //live = true;
                songTitle = Regex.Replace(songTitle, acousticPattern, "");
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

            Song song = db.Songs.Local
                .Where(x => x.Title == songTitle)
                .FirstOrDefault();

            if (song == null)
            {
                song = new Song()
                {
                    Title = songTitle,
                    Weight = double.NaN
                };
                
                db.Songs.Add(song);
            }

            Album album = (from matchingTrack in db.Tracks.Local
                           where matchingTrack.Album.Title == albumTitle &&
                           matchingTrack.Recording.Artist == artist
                           select matchingTrack.Album).FirstOrDefault();

            if(album == null)
            {
                album = new Album()
                {
                    Title = albumTitle,
                    Year = (int)tag.Year,
                    Weight = double.NaN
                };
                
                db.Albums.Add(album);
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
                Live = live
            };
            db.Recordings.Add(recording);

            Track track = new Track()
            {
                Album = album,
                Recording = recording,
                Title = trackTitle,
                TrackNumber = (int)tag.Track,
                DiscNumber = (int)discNumber,
                Weight = double.NaN
            };
            db.Tracks.Add(track);
        }

        public void SetAlbumArt(long albumID, string path)
        {
            albumCommands.SetAlbumArt(albumID, path);
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
