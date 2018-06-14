using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class TrackCommands
    {
        MusegicianData db = null;

        public TrackCommands(MusegicianData db)
        {
            this.db = db;
        }

        #region High Level Commands

        /// <summary>
        /// Splitting, Renaming, And/Or Consolidating Tracks by Song Title
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(IEnumerable<Track> tracks, string newTitle)
        {
            //Are there any songs by the indicated artists with the same name?
            Song matchingSong = tracks.SelectMany(x => x.Recording.Artist.Recordings)
                .Select(x => x.Song)
                .Distinct()
                .Where(x => x.Title == newTitle)
                .FirstOrDefault();

            if (matchingSong == null)
            {
                //New Song did not exist
                //  We need to create the new song
                matchingSong = new Song()
                {
                    Title = newTitle,
                    Weight = -1.0,
                    SongGuid = Guid.NewGuid()
                };

                db.Songs.Add(matchingSong);
            }

            var recordingSet = tracks.Select(x => x.Recording).Distinct();

            //Update track table to point at new song
            foreach (Recording recording in recordingSet)
            {
                recording.Song = matchingSong;
            }

            foreach (PlaylistSong plSong in 
                (from recording in recordingSet
                 join plRecording in db.PlaylistRecordings on recording.Id equals plRecording.RecordingId
                 select plRecording.PlaylistSong).Distinct())
            {
                plSong.Song = matchingSong;
            }

            //Delete Leafs
            db.Songs.RemoveRange(
                from song in db.Songs
                where song.Recordings.Count == 0
                select song);

            db.SaveChanges();
        }

        /// <summary>
        /// Assigning Tracks to a different artist
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateArtistName(IEnumerable<Track> tracks, string newArtistName)
        {
            //Moving (or Creating) tracks under a new artist
            Artist matchingArtist = db.Artists.Where(x => x.Name == newArtistName).FirstOrDefault();

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

            var recordingSet = tracks.Select(x => x.Recording).Distinct();

            //Update track table to point at new Artist
            foreach (Recording recording in recordingSet)
            {
                recording.Artist = matchingArtist;
            }

            //Delete Leafs
            db.Artists.RemoveRange(db.Artists.Where(x => x.Recordings.Count == 0));

            db.SaveChanges();
        }

        public void UpdateAlbumTitle(IEnumerable<Track> tracks, string newAlbumTitle)
        {
            //Is there an album currently by the same artist with the same name?
            Album matchingAlbum = tracks.SelectMany(x => x.Recording.Artist.Recordings)
                .Distinct()
                .SelectMany(x => x.Tracks)
                .Select(x => x.Album)
                .Where(x => x.Title == newAlbumTitle).FirstOrDefault();

            if (matchingAlbum == null)
            {
                //New Album did not exist
                //  We need to create the new album
                matchingAlbum = new Album()
                {
                    Title = newAlbumTitle,
                    Weight = -1.0,
                    Year = 0,
                    AlbumGuid = Guid.NewGuid()
                };

                db.Albums.Add(matchingAlbum);
            }

            //Update tracks to point at new album
            foreach (Track track in tracks)
            {
                track.Album = matchingAlbum;
            }

            db.SaveChanges();

            //Delete leafs
            db.Albums.RemoveRange(db.Albums.Where(x => x.Tracks.Count == 0));

            db.SaveChanges();

        }

        public void UpdateTrackTitle(IEnumerable<Track> tracks, string newTrackTitle)
        {
            foreach (Track track in tracks)
            {
                track.Title = newTrackTitle;
            }

            db.SaveChanges();
        }

        public void UpdateTrackNumber(IEnumerable<Track> tracks, int newTrackNumber)
        {
            foreach (Track track in tracks)
            {
                track.TrackNumber = newTrackNumber;
            }

            db.SaveChanges();
        }

        public void UpdateDisc(IEnumerable<Track> tracks, int newDisc)
        {
            foreach (Track track in tracks)
            {
                track.DiscNumber = newDisc;
            }

            db.SaveChanges();
        }

        public void UpdateLive(IEnumerable<Track> tracks, bool newLiveValue)
        {
            foreach (Track track in tracks)
            {
                track.Recording.Live = newLiveValue;
            }

            db.SaveChanges();
        }

        #endregion High Level Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allTracks = from track in db.Tracks select track;
            db.Tracks.RemoveRange(allTracks);
        }

        #endregion Delete Commands
    }
}
