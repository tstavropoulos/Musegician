using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using Musegician.DataStructures;

using IPlaylistRequestHandler = Musegician.Playlist.IPlaylistRequestHandler;

namespace Musegician
{
    public class PlaylistTuple
    {
        public string title;
        public int count;
    }

    public partial class FileManager : IPlaylistRequestHandler
    {
        #region IPlaylistRequestHandler

        Database.Playlist _currentPlaylist = null;

        private Database.Playlist GetCurrentPlaylist()
        {
            if (_currentPlaylist != null)
            {
                return _currentPlaylist;
            }

            _currentPlaylist =
                (from playlist in db.Playlists
                 where playlist.Title == "Default"
                 select playlist).FirstOrDefault();

            if (_currentPlaylist == null)
            {
                _currentPlaylist = new Database.Playlist()
                {
                    Title = "Default"
                };
                db.Playlists.Add(_currentPlaylist);
                db.SaveChanges();
            }

            return _currentPlaylist;
        }

        private Database.Playlist GetPlaylistClone(string title)
        {
            return (from playlist in db.Playlists
                        .Include(p => p.PlaylistSongs.Select(s => s.PlaylistRecordings))
                        .AsNoTracking()
                    where playlist.Title == title
                    select playlist).FirstOrDefault();
        }

        Database.Playlist IPlaylistRequestHandler.GetCurrentPlaylist() => GetCurrentPlaylist();

        //void IPlaylistRequestHandler.SavePlaylistAs(string title)
        //{
        //    //Let's just do the easy thing - kill the whole record and start over
        //    db.Playlists.RemoveRange(from playlist in db.Playlists
        //                             where playlist.Title == title
        //                             select playlist);

        //    Database.Playlist savePlaylist = GetPlaylistClone("Default");
        //    savePlaylist.Title = title;

        //    db.Playlists.Add(savePlaylist);
        //    db.SaveChanges();
        //}

        void IPlaylistRequestHandler.SavePlaylistAs(string title, IEnumerable<PlaylistSong> songs)
        {
            //Let's just do the easy thing - kill the whole record and start over
            db.Playlists.RemoveRange(from playlist in db.Playlists
                                     where playlist.Title == title
                                     select playlist);

            Database.Playlist savePlaylist = new Database.Playlist()
            {
                Title = title
            };

            int index = 0;
            foreach (PlaylistSong song in songs)
            {
                PlaylistSong songCopy = new PlaylistSong()
                {
                    Number = index++,
                    Song = song.Song,
                    Weight = song.Weight,
                    Title = song.Title,
                    Playlist = savePlaylist
                };
                db.PlaylistSongs.Add(songCopy);

                foreach (PlaylistRecording recording in song.PlaylistRecordings)
                {
                    PlaylistRecording recordingCopy = new PlaylistRecording()
                    {
                        Title = recording.Title,
                        Recording = recording.Recording,
                        Weight = recording.Weight,
                        PlaylistSong = songCopy
                    };

                    db.PlaylistRecordings.Add(recordingCopy);
                }
                
            }

            db.Playlists.Add(savePlaylist);
            db.SaveChanges();
        }

        IEnumerable<PlaylistTuple> IPlaylistRequestHandler.GetPlaylistInfo()
        {
            return db.Playlists
                .Select(x => new PlaylistTuple {
                    title = x.Title,
                    count = x.PlaylistSongs.Count()});



            //IEnumerable<ValueTuple<string, int>> query =
            //    (from playlist in db.Playlists
            //     where playlist.Title != "Default"
            //     select ValueTuple<string, int>(playlist.Title, playlist.PlaylistSongs.Count()));
            //return query;
        }

        //void IPlaylistRequestHandler.LoadPlaylist(string title)
        //{
        //    Database.Playlist loadingPlaylist = GetPlaylistClone(title);

        //    if (loadingPlaylist == null)
        //    {
        //        Console.WriteLine($"Tried to load Playlist \"{title}\"... Not found!");
        //        return;
        //    }

        //    if (_currentPlaylist == null)
        //    {
        //        GetCurrentPlaylist();
        //    }

        //    _currentPlaylist.PlaylistSongs.Clear();

        //    foreach (PlaylistSong song in loadingPlaylist.PlaylistSongs)
        //    {
        //        _currentPlaylist.PlaylistSongs.Add(song);
        //    }

        //    db.SaveChanges();
        //}

        void IPlaylistRequestHandler.ClearPlaylist()
        {
            if (_currentPlaylist == null)
            {
                GetCurrentPlaylist();
            }

            db.PlaylistSongs.RemoveRange(from plsong in _currentPlaylist.PlaylistSongs
                                         select plsong);
        }

        IEnumerable<PlaylistSong> IPlaylistRequestHandler.LoadPlaylist(string title)
        {
            Database.Playlist loadingPlaylist = GetPlaylistClone(title);

            if (loadingPlaylist == null)
            {
                Console.WriteLine($"Tried to load Playlist \"{title}\"... Not found!");
                return new List<PlaylistSong>();
            }

            if (_currentPlaylist == null)
            {
                GetCurrentPlaylist();
            }
            
            foreach (PlaylistSong song in loadingPlaylist.PlaylistSongs)
            {
                song.Playlist = _currentPlaylist;
            }
            db.SaveChanges();


            return _currentPlaylist.PlaylistSongs;
        }

        void IPlaylistRequestHandler.DeletePlaylist(string title)
        {
            Database.Playlist playlist =
                (from pl in db.Playlists
                 where pl.Title == title
                 select pl).FirstOrDefault();

            if (playlist == null)
            {
                return;
            }

            db.Playlists.Remove(playlist);
            db.SaveChanges();
        }

        PlayData IPlaylistRequestHandler.GetRecordingPlayData(Recording recording)
        {
            return new PlayData()
            {
                artistName = recording.Artist.Name,
                songTitle = recording.Tracks.First().Title,
                recording = recording
            };
        }

        #endregion IPlaylistRequestHandler
    }
}
