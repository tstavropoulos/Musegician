using MusicPlayer.DataStructures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MusicPlayer.Playlist
{
    public class MockPlaylistViewModel
    {
        readonly ObservableCollection<PlaylistSongViewModel> _playlistViewModels;

        public MockPlaylistViewModel()
        {
            MockDB db = new MockDB();

            List<SongDTO> songlist = db.GenerateSongList();

            _playlistViewModels = new ObservableCollection<PlaylistSongViewModel>(
                (from song in songlist
                 select new PlaylistSongViewModel(song))
                     .ToList());

            _playlistViewModels[7].IsExpanded = true;
            _playlistViewModels[7].Playing = true;
            _playlistViewModels[7].Children[0].IsSelected = true;
            _playlistViewModels[7].Children[0].Playing = true;
            _playlistViewModels[10].IsExpanded = true;
        }

        #region ViewModels

        public ObservableCollection<PlaylistSongViewModel> PlaylistViewModels
        {
            get { return _playlistViewModels; }
        }

        #endregion // ViewModels
    }

    public class MockDB
    {
        (long id, string name)[] artists;
        (long id, long artistID, string name, string art)[] albums;
        (long id, long albumID, string name)[] songs;
        (long id, long songID, long homeAlbum, bool live, string title)[] recordings;

        public MockDB()
        {
            //ID, Name
            artists = new(long, string)[]
            {
                (0, "Aerosmith"),
                (1, "Billy Joel"),
                (2, "Steely Dan")
            };

            //ID, ArtistID, Name, ArtFileName
            albums = new(long, long, string, string)[]
            {
                (0, 0, "Permanent Vacation", @"MockDBResources\0.jpg"),
                (1, 0, "Toys In The Attic",  @"MockDBResources\1.jpg"),
                (2, 1, "Storm Front",        @"MockDBResources\2.jpg"),
                (3, 1, "Songs In The Attic", @"MockDBResources\3.jpg"),
                (4, 2, "Two Against Nature", @"MockDBResources\4.jpg")
            };

            //ID, AlbumID, Name
            songs = new(long, long, string)[]
            {
                ( 0, 0, "Heart's Done Time"),
                ( 1, 0, "Magic Touch"),
                ( 2, 0, "Rag Doll"),
                ( 3, 1, "Toys In The Attic"),
                ( 4, 1, "Uncle Salty"),
                ( 5, 1, "Adam's Apple"),
                ( 6, 2, "Storm Front"),
                ( 7, 2, "We Didn't Start The Fire"),
                ( 8, 2, "Leningrad"),
                ( 9, 2, "State of Grace"),
                ( 7, 3, "We Didn't Start The Fire"),
                (10, 3, "A Matter Of Trust"),
                (11, 4, "Gaslighting Abbie"),
                (12, 4, "What A Shame"),
                (13, 4, "Two Against Nature"),
                (14, 4, "Janie Runaway"),
                (15, 4, "Almost Gothic"),
                (16, 4, "Jack of Speed"),
                (17, 4, "Cousin Dupree"),
                (18, 4, "Negative Girl"),
                (19, 4, "West of Hollywood")
            };

            //ID, SongID, homeAlbum, live, title
            recordings = new(long, long, long, bool, string)[]
            {
                ( 0,  0, 0, false, "Heart's Done Time"),
                ( 1,  1, 0, false, "Magic Touch"),
                ( 2,  2, 0, false, "Rag Doll"),
                ( 3,  3, 1, false, "Toys In The Attic"),
                ( 4,  4, 1, false, "Uncle Salty"),
                ( 5,  5, 1, false, "Adam's Apple"),
                ( 6,  6, 2, false, "Storm Front"),
                ( 7,  7, 2, false, "We Didn't Start The Fire"),
                ( 8,  8, 2, false, "Leningrad"),
                ( 9,  9, 2, false, "State of Grace"),
                (10,  7, 3,  true, "We Didn't Start The Fire"),
                (11, 10, 3,  true, "A Matter Of Trust"),
                (12, 11, 4, false, "Gaslighting Abbie"),
                (13, 12, 4, false, "What A Shame"),
                (14, 13, 4, false, "Two Against Nature"),
                (15, 14, 4, false, "Janie Runaway"),
                (16, 15, 4, false, "Almost Gothic"),
                (17, 16, 4, false, "Jack of Speed"),
                (18, 17, 4, false, "Cousin Dupree"),
                (19, 18, 4, false, "Negative Girl"),
                (20, 19, 4, false, "West of Hollywood")
            };
        }

        public List<SongDTO> GenerateSongList()
        {
            List<SongDTO> songList = new List<SongDTO>();

            foreach (var song in songs)
            {
                string title = string.Format(
                    "{0} - {1}",
                    GetArtistName(GetAlbumArtist(song.albumID)),
                    song.name);

                SongDTO newSong = new SongDTO(
                    songID: song.id,
                    title: title);

                foreach(RecordingDTO recording in GenerateSongRecordingList(song.id))
                {
                    newSong.Children.Add(recording);
                }

                songList.Add(newSong);
            }

            return songList;
        }

        List<RecordingDTO> GenerateSongRecordingList(long songID)
        {
            List<RecordingDTO> recordingList = new List<RecordingDTO>();

            foreach (var recording in recordings)
            {
                if (songID == recording.songID)
                {
                    recordingList.Add(new RecordingDTO()
                    {
                        ID = recording.id,
                        Name = GetAlbumArtistName(recording.homeAlbum) + " - " +
                            GetAlbumName(recording.homeAlbum) + " - " + recording.title,
                        Live = recording.live,
                        IsHome = true,
                        Weight = (recording.live ? 0.1 : 1.0)
                    });
                }
            }

            return recordingList;
        }
        

        private string GetAlbumName(long albumID)
        {
            foreach(var album in albums)
            {
                if(album.id == albumID)
                {
                    return album.name;
                }
            }

            return "";
        }

        private string GetAlbumArtistName(long albumID)
        {
            return GetArtistName(GetAlbumArtist(albumID));
        }

        private long GetAlbumArtist(long albumID)
        {
            foreach (var album in albums)
            {
                if (album.id == albumID)
                {
                    return album.artistID;
                }
            }

            return -1;
        }

        private long GetSongAlbum(long songID)
        {
            foreach(var song in songs)
            {
                if(song.id == songID)
                {
                    return song.albumID;
                }
            }

            return -1;
        }

        private string GetArtistName(long artistID)
        {
            foreach (var artist in artists)
            {
                if (artist.id == artistID)
                {
                    return artist.name;
                }
            }

            return "";
        }
    }
}
