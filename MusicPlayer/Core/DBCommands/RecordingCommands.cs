using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class RecordingCommands
    {
        MusegicianData db = null;

        public RecordingCommands(MusegicianData db)
        {
            this.db = db;
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
                    Weight = -1.0,
                    SongGuid = Guid.NewGuid()
                };

                db.Songs.Add(matchingSong);
            }

            //Update recordings
            foreach (Recording recording in recordings)
            {
                recording.Song = matchingSong;
            }

            //Update playlists
            foreach (PlaylistSong playlistSong in
                (from recording in recordings
                 join plRec in db.PlaylistRecordings on recording.Id equals plRec.RecordingId
                 select plRec.PlaylistSong)
                .Distinct())
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
                    Weight = -1.0,
                    ArtistGuid = Guid.NewGuid()
                };

                db.Artists.Add(matchingArtist);
            }

            //Update recordings
            foreach (Recording recording in recordings)
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
            foreach (Recording recording in recordings)
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
