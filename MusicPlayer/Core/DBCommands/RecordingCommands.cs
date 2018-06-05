using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using Musegician.DataStructures;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class RecordingCommands
    {
        ArtistCommands artistCommands = null;
        SongCommands songCommands = null;

        MusegicianData db = null;

        public RecordingCommands()
        {
        }

        public void Initialize(
            MusegicianData db,
            ArtistCommands artistCommands,
            SongCommands songCommands)
        {
            this.db = db;

            this.artistCommands = artistCommands;
            this.songCommands = songCommands;
        }

        #region High Level Commands

        /// <summary>
        /// Splitting, Renaming, And/Or Consolidating Recordings by Song Title
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(IEnumerable<Recording> recordings, string newTitle)
        {
            //Is there a song currently by the same artist with the same name?
            Song matchingSong = recordings
                            .Select(x => x.Artist)
                            .SelectMany(x => x.Recordings)
                            .Select(x => x.Song)
                            .Distinct()
                            .Where(x => x.Title == newTitle)
                            .FirstOrDefault();

            if (matchingSong == null)
            {
                matchingSong = new Song()
                {
                    Title = newTitle,
                    Weight = double.NaN
                };

                db.Songs.Add(matchingSong);
            }

            //Update recordings
            foreach(Recording recording in recordings)
            {
                recording.Song = matchingSong;
            }

            //Update playlists
            foreach(PlaylistSong playlistSong in (db.PlaylistRecordings
                .Where(x => recordings.Contains(x.Recording))
                .Select(x=>x.PlaylistSong)
                .Distinct()))
            {
                playlistSong.Song = matchingSong;
            }

            //Remove leafs
            db.Songs.RemoveRange(
                (from song in db.Songs.Local
                 where song.Recordings.Count == 0
                 select song));

            db.SaveChanges();
        }

        /// <summary>
        /// Assigning Tracks to a different artist
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateArtistName(IEnumerable<Recording> recordings, string newArtistName)
        {
            //Renaming (or Consolidating) a Song
            Artist matchingArtist = db.Artists
                .Where(x => x.Name == newArtistName)
                .FirstOrDefault();

            //Create new artist if it doesn't exist
            if (matchingArtist == null)
            {
                matchingArtist = new Artist()
                {
                    Name = newArtistName,
                    Weight = double.NaN
                };

                db.Artists.Add(matchingArtist);
            }

            //Update recordings
            foreach(Recording recording in recordings)
            {
                recording.Artist = matchingArtist;
            }

            //Remove leafs
            db.Artists.RemoveRange(
                (from artist in db.Artists.Local
                 where artist.Recordings.Count == 0
                 select artist));

            db.SaveChanges();
        }
        

        public void UpdateLive(IEnumerable<Recording> recordings, bool newLiveValue)
        {
            foreach(Recording recording in recordings)
            {
                recording.Live = newLiveValue;
            }

            db.SaveChanges();
        }

        #endregion High Level Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allRecordings = from recording in db.Recordings select recording;
            db.Recordings.RemoveRange(allRecordings);
        }

        #endregion Delete Commands
    }
}
