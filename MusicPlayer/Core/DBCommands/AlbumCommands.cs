using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using TagLib;
using Musegician.Deredundafier;
using Musegician.Database;
using System.Drawing;

namespace Musegician.Core.DBCommands
{
    public class AlbumCommands
    {
        private readonly string consoleDiv = "---------------------------------------";
        private readonly MusegicianData db;

        public AlbumCommands(MusegicianData db)
        {
            this.db = db;
        }

        private static readonly Regex greedyParenPattern = new Regex(@"(\s*?[\(\[].*[\)\]])");

        #region High Level Commands

        public IEnumerable<DeredundafierDTO> GetDeepDeredundancyTargets()
        {
            //Map of Album Titles to lists of albums with this title
            Dictionary<string, List<Album>> map = new Dictionary<string, List<Album>>();

            foreach (Album album in db.Albums)
            {
                string title = album.Title.ToLowerInvariant();
                title.Trim();

                if (greedyParenPattern.IsMatch(title))
                {
                    title = greedyParenPattern.Replace(title, "");
                }

                //Reset name if this fully deletes it.
                if (title == "")
                {
                    title = album.Title.ToLowerInvariant();
                }

                //Add base name
                if (!map.ContainsKey(title))
                {
                    map.Add(title, new List<Album>());
                }
                map[title].Add(album);
            }

            TextInfo titleFormatter = new CultureInfo("en-US", false).TextInfo;


            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();
            foreach (var albumSet in map)
            {
                if (albumSet.Value.Count < 2)
                {
                    continue;
                }

                DeredundafierDTO target = new DeredundafierDTO()
                {
                    Name = titleFormatter.ToTitleCase(albumSet.Key)
                };
                targets.Add(target);

                foreach (Album album in albumSet.Value)
                {
                    List<Artist> artists = album.Recordings.Select(t => t.Artist).Distinct().ToList();

                    string artistName;

                    if (artists.Count == 0)
                    {
                        Console.WriteLine($"Failed to find artist for album: ({album.Id},{album.Title}).  Skipping.");
                        continue;
                    }
                    else if (artists.Count == 1)
                    {
                        artistName = artists[0].Name;
                    }
                    else
                    {
                        artistName = "Various";
                    }

                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = $"{artistName} - {album.Title}",
                        Data = album,
                        IsChecked = false
                    };

                    target.Children.Add(selector);

                    foreach (Recording recording in album.Recordings)
                    {
                        selector.Children.Add(new DeredundafierDTO()
                        {
                            Name = $"{recording.Artist.Name} - {recording.Title}",
                            Data = recording
                        });
                    }
                }
            }

            return targets;
        }

        public IEnumerable<DeredundafierDTO> GetDeredundancyTargets()
        {
            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();

            var deredundancyAlbumSets =
                (from albums in db.Albums
                 group albums by albums.Title into albumSets
                 where albumSets.Count() > 1
                 select albumSets);

            foreach (var albumSet in deredundancyAlbumSets)
            {
                DeredundafierDTO target = new DeredundafierDTO()
                {
                    Name = albumSet.Key
                };

                targets.Add(target);

                foreach (Album album in albumSet)
                {
                    List<Artist> artists = album.Recordings.Select(t => t.Artist).Distinct().ToList();

                    string artistName;

                    if (artists.Count == 0)
                    {
                        Console.WriteLine($"Failed to find artist for album: ({album.Id},{album.Title}).  Skipping.");
                        continue;
                    }
                    else if (artists.Count == 1)
                    {
                        artistName = artists[0].Name;
                    }
                    else
                    {
                        artistName = "Various";
                    }

                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = $"{artistName} - {album.Title}",
                        Data = album,
                        IsChecked = false
                    };

                    target.Children.Add(selector);

                    foreach (Recording recording in album.Recordings)
                    {
                        selector.Children.Add(new DeredundafierDTO()
                        {
                            Name = $"{recording.Artist.Name} - {recording.Title}",
                            Data = recording
                        });
                    }
                }
            }

            return targets;
        }

        public void UpdateAlbumTitle(IEnumerable<Album> albums, string newAlbumTitle)
        {
            List<Album> albumsCopy = new List<Album>(albums);

            //First, find out if the new album exists in a limited pool
            Album matchingAlbum = albumsCopy.SelectMany(x => x.Recordings)
                .Select(x => x.Artist).Distinct()
                .SelectMany(x => x.Recordings)
                .Select(x => x.Album).Distinct()
                .Where(x => x.Title == newAlbumTitle).FirstOrDefault();

            if (matchingAlbum == null)
            {
                //Update an Album's name, because it doesn't exist

                //Sort on Guid Timestamp so that the merged result uses the oldest Guid
                albumsCopy.Sort((x, y) => x.AlbumGuidTimestamp.CompareTo(y.AlbumGuidTimestamp));

                //Pop off the front
                matchingAlbum = albumsCopy[0];
                albumsCopy.RemoveAt(0);

                //Update the album formerly in the front
                matchingAlbum.Title = newAlbumTitle;
            }

            if (albumsCopy.Count > 0)
            {
                foreach (Recording recording in
                    (from album in albumsCopy
                     join recording in db.Recordings on album.Id equals recording.AlbumId
                     select recording))
                {
                    recording.Album = matchingAlbum;
                }

                //Delete orphans
                db.Albums.RemoveRange(albumsCopy);
            }

            db.SaveChanges();
        }

        public void UpdateYear(IEnumerable<Album> albums, int newYear)
        {
            foreach (Album album in albums)
            {
                album.Year = newYear;
            }

            db.SaveChanges();
        }

        public void Merge(IEnumerable<BaseData> data)
        {
            List<Album> albumsCopy = new List<Album>(data.Cast<Album>().Distinct());

            //Sort on Guid Timestamp so that the merged result uses the oldest Guid
            albumsCopy.Sort((x,y) => x.AlbumGuidTimestamp.CompareTo(y.AlbumGuidTimestamp));

            Album matchingAlbum = albumsCopy[0];
            albumsCopy.RemoveAt(0);

            //For the remaining artists, Remap foreign keys
            foreach (Recording recording in
                (from album in albumsCopy
                 join recording in db.Recordings on album.Id equals recording.AlbumId
                 select recording))
            {
                recording.Album = matchingAlbum;
            }

            //Now, delete any old artists with no remaining recordings
            db.Albums.RemoveRange(albumsCopy);

            db.SaveChanges();
        }

        #endregion High Level Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allAlbums = from album in db.Albums select album;
            db.Albums.RemoveRange(allAlbums);
        }

        #endregion Delete Commands

        public void SetAlbumArt(Album album, string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new IOException($"File not found: {path}");
            }

            try
            {
                using (Bitmap loadedBitmap = new Bitmap(path))
                {
                    //If image can be loaded...
                    if (loadedBitmap != null)
                    {
                        album.Image = System.IO.File.ReadAllBytes(path);
                        album.Thumbnail = FileManager.CreateThumbnail(loadedBitmap);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception when trying to load Bitmap stream: {e}");

                return;
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
                        string tagName = audioFile.Tag.Album.ToLowerInvariant();
                        string lowerAlbumTitle = album.Title.ToLowerInvariant();

                        if (tagName != lowerAlbumTitle && !tagName.Contains(lowerAlbumTitle))
                        {
                            Console.WriteLine($"Album Title doesn't match. Skipping : {album.Title} / {tagName}");
                            continue;
                        }

                        audioFile.Mode = TagLib.File.AccessMode.Write;
                        audioFile.Tag.Pictures = new IPicture[1] { new Picture(album.Image) };
                        audioFile.Save();
                    }
                }
                catch (UnsupportedFormatException)
                {
                    Console.WriteLine($"{consoleDiv}\nSkipping UNSUPPORTED FILE: {recording.Filename}\n");
                    continue;
                }
                catch (CorruptFileException)
                {
                    Console.WriteLine($"{consoleDiv}\nSkipping CORRUPT FILE: {recording.Filename}\n");
                    continue;
                }
                catch (IOException)
                {
                    Console.WriteLine($"{consoleDiv}\nSkipping Writing Tag To FILE IN USE: {recording.Filename}\n");
                    continue;
                }

                break;
            }

            db.SaveChanges();

        }
    }
}
