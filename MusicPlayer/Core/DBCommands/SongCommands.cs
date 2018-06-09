﻿using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Deredundafier;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class SongCommands
    {
        MusegicianData db = null;

        public SongCommands(MusegicianData db)
        {
            this.db = db;
        }

        #region High Level Commands

        /// <summary>
        /// Renaming And/Or Consolidating Songs
        /// </summary>
        /// <param name="songIDs"></param>
        /// <param name="newTitle"></param>
        public void UpdateSongTitle(IEnumerable<Song> songs, string newTitle)
        {
            List<Song> songsCopy = new List<Song>(songs.Distinct());

            //Renaming (or Consolidating) a Song
            Song matchingSong = songsCopy.SelectMany(x => x.Recordings)
                .SelectMany(x => x.Artist.Recordings)
                .Select(x => x.Song)
                .Distinct()
                .Where(x => x.Title == newTitle)
                .FirstOrDefault();

            if (matchingSong == null)
            {
                matchingSong = songsCopy[0];
                songsCopy.RemoveAt(0);

                matchingSong.Title = newTitle;
            }

            if (songsCopy.Count > 0)
            {
                //New song did exist, or we passed in more than one song

                //Update track table to point at new song
                foreach (Recording recording in
                    (from song in songsCopy
                     join recording in db.Recordings on song.Id equals recording.SongId
                     select recording))
                {
                    recording.Song = matchingSong;
                }

                foreach (PlaylistSong playlistSong in
                    (from song in songsCopy
                     join plsong in db.PlaylistSongs on song.Id equals plsong.SongId
                     select plsong))
                {
                    playlistSong.Song = matchingSong;
                }

                db.Songs.RemoveRange(songsCopy);
            }

            db.SaveChanges();
        }

        public IList<DeredundafierDTO> GetDeredundancyTargets()
        {
            List<DeredundafierDTO> targets = new List<DeredundafierDTO>();

            var songTitleSetsQ = (from song in db.Songs
                                  group song by song.Title into songTitleSets
                                  where songTitleSets.Count() > 1
                                  select songTitleSets);

            foreach (var set in songTitleSetsQ)
            {
                DeredundafierDTO target = new DeredundafierDTO()
                {
                    Name = set.Key
                };

                targets.Add(target);

                foreach (Song song in set)
                {
                    var artists = song.Recordings.Select(x => x.Artist).Distinct();

                    string artistName = "Various";
                    if (artists.Count() == 1)
                    {
                        artistName = artists.First().Name;
                    }

                    DeredundafierDTO selector = new SelectorDTO()
                    {
                        Name = $"{artistName} - {song.Title}",
                        Data = song,
                        IsChecked = false
                    };

                    target.Children.Add(selector);

                    foreach (Recording recording in song.Recordings)
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
            List<Song> songsCopy = new List<Song>(data.Select(x => x as Song).Distinct());

            Song matchingSong = songsCopy[0];
            songsCopy.RemoveAt(0);

            //For the remaining artists, Remap foreign keys
            foreach (Recording recording in
                (from song in songsCopy
                 join recording in db.Recordings on song.Id equals recording.SongId 
                 select recording))
            {
                recording.Song = matchingSong;
            }

            foreach (PlaylistSong playlistSong in
                (from song in songsCopy
                 join plsong in db.PlaylistSongs on song.Id equals plsong.SongId
                 select plsong))
            {
                playlistSong.Song = matchingSong;
            }

            //Now, delete any old artists with no remaining recordings
            db.Songs.RemoveRange(songsCopy);

            db.SaveChanges();
        }

        #endregion High Level Commands
        #region Delete Commands

        public void _DropTable()
        {
            var allSongs = from song in db.Songs select song;
            db.Songs.RemoveRange(allSongs);
        }

        #endregion Delete Commands
    }
}
