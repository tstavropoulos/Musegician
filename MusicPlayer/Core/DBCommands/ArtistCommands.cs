using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using Musegician.Deredundafier;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class ArtistCommands
    {
        AlbumCommands albumCommands = null;
        SongCommands songCommands = null;
        RecordingCommands recordingCommands = null;

        MusegicianData db = null;

        public ArtistCommands()
        {
        }

        public void Initialize(
            MusegicianData db,
            AlbumCommands albumCommands,
            SongCommands songCommands,
            RecordingCommands recordingCommands)
        {
            this.db = db;

            this.albumCommands = albumCommands;
            this.songCommands = songCommands;
            this.recordingCommands = recordingCommands;
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
                    (from recording in db.Recordings
                     where artistsCopy.Contains(recording.Artist)
                     select recording))
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
                            Name = $"{recording.Artist} - {recording.Tracks.First().Album.Title} - {recording.Tracks.First().Title}",
                            Data = recording
                        });
                    }
                }
            }

            return targets;
        }

        public void Merge(IEnumerable<BaseData> data)
        {
            List<Artist> artistsCopy = new List<Artist>(data.Select(x => x as Artist).Distinct());

            Artist matchingArtist = artistsCopy[0];
            artistsCopy.RemoveAt(0);

            //For the remaining artists, Remap foreign keys
            foreach (Recording recording in
                (from recording in db.Recordings
                 where artistsCopy.Contains(recording.Artist)
                 select recording))
            {
                recording.Artist = matchingArtist;
            }

            //Now, delete any old artists with no remaining recordings
            db.Artists.RemoveRange(artistsCopy);

            db.SaveChanges();
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
