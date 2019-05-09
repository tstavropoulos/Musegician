using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using Musegician.Deredundafier;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class ArtistCommands
    {
        private readonly MusegicianData db;

        public ArtistCommands(MusegicianData db)
        {
            this.db = db;
        }

        #region High Level Commands

        public void UpdateArtistName(IEnumerable<Artist> artists, string newArtistName)
        {
            List<Artist> artistsCopy = new List<Artist>(artists);

            //First, find out if the new artist exists
            Artist matchingArtist = db.Artists
                .Where(x => x.Name == newArtistName)
                .FirstOrDefault();

            if (matchingArtist == null)
            {
                //Update an Artist's name, because it doesn't exist

                //Pop off the front
                matchingArtist = artistsCopy[0];
                artistsCopy.RemoveAt(0);

                matchingArtist.Name = newArtistName;
            }

            if (artistsCopy.Count > 0)
            {
                //For the remaining artists, Remap foreign keys
                foreach (Recording recording in
                    (from artist in artistsCopy
                     join recording in db.Recordings on artist.Id equals recording.ArtistId
                     select recording).Distinct())
                {
                    recording.Artist = matchingArtist;
                }

                //Now, delete any old artists with no remaining recordings
                db.Artists.RemoveRange(artistsCopy);
            }

            db.SaveChanges();
        }

        public IEnumerable<Artist> GeneratArtistList()
        {
            return (from artist in db.Artists
                    orderby (artist.Name.StartsWith("The ") ? artist.Name.Substring(4) : artist.Name)
                    select artist);
        }

        private const string greedyParenPattern = @"(\s*?[\(\[].*[\)\]])";

        public IEnumerable<DeredundafierDTO> GetDeepDeredundancyTargets()
        {
            Dictionary<string, List<Artist>> map = new Dictionary<string, List<Artist>>();

            foreach (Artist artist in db.Artists)
            {
                string name = artist.Name.ToLowerInvariant();
                name.Trim();

                if (Regex.IsMatch(name, greedyParenPattern))
                {
                    name = Regex.Replace(name, greedyParenPattern, "");
                }

                //Reset name if this fully deletes it.
                if (name == "")
                {
                    name = artist.Name.ToLowerInvariant();
                }

                //Add base name
                if (!map.ContainsKey(name))
                {
                    map.Add(name, new List<Artist>());
                }
                map[name].Add(artist);
            }

            TextInfo titleFormatter = new CultureInfo("en-US", false).TextInfo;

            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();
            foreach (var set in map)
            {
                if (set.Value.Count < 2)
                {
                    continue;
                }

                DeredundafierDTO target = new DeredundafierDTO()
                {
                    Name = titleFormatter.ToTitleCase(set.Key)
                };
                targets.Add(target);

                foreach (Artist artist in set.Value)
                {
                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = artist.Name,
                        Data = artist,
                        IsChecked = false
                    };

                    target.Children.Add(selector);

                    foreach (Recording recording in artist.Recordings.Take(10))
                    {
                        selector.Children.Add(new DeredundafierDTO()
                        {
                            Name = $"{recording.Artist.Name} - {recording.Album.Title} - {recording.Title}",
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

            var artistNameSetsQ = (from artist in db.Artists
                                   group artist by artist.Name into artistNameSets
                                   where artistNameSets.Count() > 1
                                   select artistNameSets);

            foreach (var set in artistNameSetsQ)
            {
                DeredundafierDTO target = new DeredundafierDTO()
                {
                    Name = set.Key
                };
                targets.Add(target);

                foreach (Artist artist in set)
                {
                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = artist.Name,
                        Data = artist,
                        IsChecked = false
                    };

                    target.Children.Add(selector);

                    foreach (Recording recording in artist.Recordings.Take(10))
                    {
                        selector.Children.Add(new DeredundafierDTO()
                        {
                            Name = $"{recording.Artist.Name} - {recording.Album.Title} - {recording.Title}",
                            Data = recording
                        });
                    }
                }
            }

            return targets;
        }

        public IEnumerable<DeredundafierDTO> GetCompositeArtistTargets()
        {
            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();

            Dictionary<string, List<Artist>> artistsByName = new Dictionary<string, List<Artist>>();
            Dictionary<Artist, List<string>> splitArtistsByName = new Dictionary<Artist, List<string>>();

            foreach (Artist artist in db.Artists)
            {
                string name = artist.Name.Trim().ToLowerInvariant();

                //Add base name
                if (!artistsByName.ContainsKey(name))
                {
                    artistsByName.Add(name, new List<Artist>());
                }
                artistsByName[name].Add(artist);

                var subNames = name
                    .Split(new string[] { "&", ",", ";", " with ", "feat. ", " featuring " }, options: StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x));

                if (subNames.Count() > 1)
                {
                    splitArtistsByName.Add(artist, subNames.ToList());
                }
            }

            foreach (var kvp in splitArtistsByName)
            {
                List<Artist> matchingArtists = new List<Artist>();

                foreach (string subName in kvp.Value)
                {
                    if (artistsByName.ContainsKey(subName))
                    {
                        matchingArtists.AddRange(artistsByName[subName]);
                    }
                }

                foreach (Artist artist in kvp.Key.GroupOf.Select(x => x.Member).Distinct())
                {
                    matchingArtists.Add(artist);
                }

                var reducedSet = matchingArtists.Distinct();

                if (reducedSet.Count() == 0)
                {
                    continue;
                }

                DeredundafierDTO target = new DeredundafierDTO()
                {
                    Name = kvp.Key.Name,
                    Data = kvp.Key
                };

                foreach (Artist artist in reducedSet)
                {
                    target.Children.Add(new SelectorDTO()
                    {
                        Name = artist.Name,
                        Data = artist,
                        IsChecked = kvp.Key.GroupOf.Select(x => x.Member).Contains(artist)
                    });
                }

                targets.Add(target);
            }

            return targets;
        }

        public void Merge(IEnumerable<BaseData> data)
        {
            List<Artist> artistsCopy = new List<Artist>(data.Cast<Artist>().Distinct());

            Artist matchingArtist = artistsCopy[0];
            artistsCopy.RemoveAt(0);

            //For the remaining artists, Remap foreign keys
            foreach (Recording recording in
                (from artist in artistsCopy
                 join recording in db.Recordings on artist.Id equals recording.ArtistId
                 select recording).Distinct())
            {
                recording.Artist = matchingArtist;
            }

            //Now, delete any old artists with no remaining recordings
            db.Artists.RemoveRange(artistsCopy);

            db.SaveChanges();
        }

        public void CreateCompositeArtist(IEnumerable<BaseData> data)
        {
            throw new NotImplementedException();
        }

        #endregion High Level Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allArtists = from artist in db.Artists select artist;
            db.Artists.RemoveRange(allArtists);
        }

        #endregion Delete Commands
    }
}
