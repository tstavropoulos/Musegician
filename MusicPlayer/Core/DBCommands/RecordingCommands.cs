using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class RecordingCommands
    {
        private readonly MusegicianData db;

        public RecordingCommands(MusegicianData db)
        {
            this.db = db;
        }

        #region High Level Commands

        /// <summary>
        /// Splitting, Renaming, And/Or Consolidating Recordings by Song Title
        /// </summary>
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
                //New Song did not exist

                //If we are updating the name of every recording of one song,
                //  then just update the name
                if (recordings.Select(x => x.Song).Distinct().Count() == 1)
                {
                    Song sourceSong = recordings.First().Song;

                    //If the recordings exactly match artist
                    if (sourceSong.Recordings.Except(recordings.Distinct()).Count() == 0)
                    {
                        sourceSong.Title = newTitle;

                        db.SaveChanges();

                        //We are done - bail
                        return;
                    }
                }

                matchingSong = new Song()
                {
                    Title = newTitle,
                    Weight = -1.0,
                    SongGuid = Guid.NewGuid(),
                    SongGuidTimestamp = Epoch.Time
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
                from song in db.Songs.Local
                where song.Recordings.Count == 0
                select song);

            db.SaveChanges();
        }

        /// <summary>
        /// Assigning Tracks to a different artist
        /// </summary>
        public void UpdateArtistName(IEnumerable<Recording> recordings, string newArtistName)
        {
            //Renaming (or Consolidating) a Song
            Artist matchingArtist = db.Artists
                .Where(x => x.Name == newArtistName)
                .FirstOrDefault();

            //Create new artist if it doesn't exist
            if (matchingArtist == null)
            {
                //New Artist did not exist

                //If we are updating the artist name of every recording of one Artist,
                //  then just update the name
                if (recordings.Select(x => x.Artist).Distinct().Count() == 1)
                {
                    Artist sourceArtist = recordings.First().Artist;

                    //If the recordings exactly match artist
                    if (sourceArtist.Recordings.Except(recordings.Distinct()).Count() == 0)
                    {
                        sourceArtist.Name = newArtistName;

                        db.SaveChanges();

                        //We are done - bail
                        return;
                    }
                }

                matchingArtist = new Artist()
                {
                    Name = newArtistName,
                    Weight = -1.0,
                    ArtistGuid = Guid.NewGuid(),
                    ArtistGuidTimestamp = Epoch.Time
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
                from artist in db.Artists.Local
                where artist.Recordings.Count == 0
                select artist);

            db.SaveChanges();
        }

        /// <summary>
        /// Assigning the recording to a different Album, creating a new Album or
        /// renaming an existing Album
        /// </summary>
        public void UpdateAlbumTitle(IEnumerable<Recording> recordings, string newAlbumTitle)
        {
            //Is there an album currently by the same artist with the same name?
            Album matchingAlbum = recordings
                .Select(x=>x.Artist).Distinct()
                .SelectMany(x => x.Recordings)
                .Select(x => x.Album).Distinct()
                .Where(x => x.Title == newAlbumTitle).FirstOrDefault();

            if (matchingAlbum == null)
            {
                //New Album did not exist

                //If we are updating the name of every recording on the album,
                //  then just update the name
                if (recordings.Select(x=>x.Album).Distinct().Count() == 1)
                {
                    Album sourceAlbum = recordings.First().Album;
                    //If the recordings exactly match album
                    if (sourceAlbum.Recordings.Except(recordings.Distinct()).Count() == 0)
                    {
                        sourceAlbum.Title = newAlbumTitle;

                        db.SaveChanges();

                        return;
                    }
                }

                //We need to create the new album
                matchingAlbum = new Album()
                {
                    Title = newAlbumTitle,
                    Weight = -1.0,
                    Year = 0,
                    AlbumGuid = Guid.NewGuid(),
                    AlbumGuidTimestamp = Epoch.Time
                };

                db.Albums.Add(matchingAlbum);
            }

            //Update tracks to point at new album
            foreach (Recording recording in recordings)
            {
                recording.Album = matchingAlbum;
            }

            db.SaveChanges();

            //Delete leafs
            db.Albums.RemoveRange(db.Albums.Where(x => x.Recordings.Count == 0));

            db.SaveChanges();
        }

        public void UpdateRecordingType(IEnumerable<Recording> recordings, RecordingType newRecordingType)
        {
            foreach (Recording recording in recordings)
            {
                recording.RecordingType = newRecordingType;
            }

            db.SaveChanges();
        }

        public void UpdateRecordingTitle(IEnumerable<Recording> recordings, string newRecordingTitle)
        {
            foreach (Recording recording in recordings)
            {
                recording.Title = newRecordingTitle;
            }

            db.SaveChanges();
        }

        public void UpdateYear(IEnumerable<Recording> recordings, int newYear)
        {
            foreach (Album album in recordings.Select(x => x.Album).Distinct())
            {
                album.Year = newYear;
            }

            db.SaveChanges();
        }

        public void UpdateTrackNumber(IEnumerable<Recording> recordings, int newTrackNumber)
        {
            foreach (Recording recording in recordings)
            {
                recording.TrackNumber = newTrackNumber;
            }

            db.SaveChanges();
        }

        public void UpdateDiscNumber(IEnumerable<Recording> recordings, int newDisc)
        {
            foreach (Recording recording in recordings)
            {
                recording.DiscNumber = newDisc;
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
