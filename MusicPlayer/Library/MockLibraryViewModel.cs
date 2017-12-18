using MusicPlayer.DataStructures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MusicPlayer.Library
{
    public class MockLibraryViewModel
    {
        readonly ObservableCollection<ArtistViewModel> _classicArtistViewModels;
        readonly ObservableCollection<ArtistViewModel> _simpleArtistViewModels;
        readonly ObservableCollection<AlbumViewModel> _albumViewModels;

        public SearchChoices SearchChoice { get; set; } = SearchChoices.All;

        public MockLibraryViewModel()
        {
            ILibraryRequestHandler db = new MockDB();

            List<ArtistDTO> artistList = db.GenerateArtistList();
            List<AlbumDTO> albumList = db.GenerateAlbumList();


            _classicArtistViewModels = new ObservableCollection<ArtistViewModel>(
                (from artist in artistList
                 select new ArtistViewModel(artist, ViewMode.Classic))
                     .ToList());


            _simpleArtistViewModels = new ObservableCollection<ArtistViewModel>(
                (from artist in artistList
                 select new ArtistViewModel(artist, ViewMode.Simple))
                     .ToList());

            _albumViewModels = new ObservableCollection<AlbumViewModel>(
                (from album in albumList
                 select new AlbumViewModel(album, null, false))
                     .ToList());


            _classicArtistViewModels[1].LoadChildren(db);
            _classicArtistViewModels[1].IsExpanded = true;
            _classicArtistViewModels[1].Children[0].LoadChildren(db);
            _classicArtistViewModels[1].Children[0].IsExpanded = true;
            _classicArtistViewModels[1].Children[0].Children[1].LoadChildren(db);
            _classicArtistViewModels[1].Children[0].Children[1].IsExpanded = true;
        }

        #region ArtistViewModels

        public ObservableCollection<ArtistViewModel> ClassicArtistViewModels
        {
            get { return _classicArtistViewModels; }
        }

        public ObservableCollection<AlbumViewModel> AlbumViewModels
        {
            get { return _albumViewModels; }
        }

        public ObservableCollection<ArtistViewModel> SimpleViewModels
        {
            get { return _simpleArtistViewModels; }
        }

        #endregion // ArtistViewModels


    }

    public class MockDB : ILibraryRequestHandler
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

        List<AlbumDTO> ILibraryRequestHandler.GenerateAlbumList()
        {
            List<AlbumDTO> albumList = new List<AlbumDTO>();

            foreach (var album in albums)
            {
                albumList.Add(new AlbumDTO(
                    albumID: album.id,
                    albumTitle: album.name,
                    albumArt: LoadImage(album.art)));
            }

            return albumList;
        }

        List<SongDTO> ILibraryRequestHandler.GenerateAlbumSongList(long artistID, long albumID)
        {
            List<SongDTO> songList = new List<SongDTO>();

            int i = 0;
            foreach (var song in songs)
            {
                if (song.albumID == albumID)
                {
                    songList.Add(new SongDTO(
                        songID: song.id,
                        titlePrefix: (++i).ToString("D2") + ". ",
                        title:  song.name,
                        trackID: -1,
                        isHome: true));
                }
            }

            return songList;
        }

        List<AlbumDTO> ILibraryRequestHandler.GenerateArtistAlbumList(long artistID, string artistName)
        {
            List<AlbumDTO> albumList = new List<AlbumDTO>();

            foreach (var album in albums)
            {
                if (artistID == album.artistID)
                {
                    albumList.Add(new AlbumDTO(
                        albumID: album.id,
                        albumTitle: album.name,
                        albumArt: LoadImage(album.art)));
                }
            }

            return albumList;
        }

        List<ArtistDTO> ILibraryRequestHandler.GenerateArtistList()
        {
            List<ArtistDTO> artistList = new List<ArtistDTO>();
            foreach (var artist in artists)
            {
                artistList.Add(new ArtistDTO(artist.id, artist.name));
            }

            return artistList;
        }

        List<SongDTO> ILibraryRequestHandler.GenerateArtistSongList(long artistID, string artistName)
        {
            List<SongDTO> songList = new List<SongDTO>();
            
            foreach (var album in albums)
            {
                if (artistID == album.artistID)
                {
                    foreach (var song in songs)
                    {
                        if (song.albumID == album.id)
                        {
                            songList.Add(new SongDTO(
                                songID: song.id,
                                titlePrefix: artistName + " - ",
                                title: song.name,
                                trackID: -1,
                                isHome: true));
                        }
                    }
                }
            }

            return songList;
        }

        List<RecordingDTO> ILibraryRequestHandler.GenerateSongRecordingList(long songID, long albumID)
        {
            List<RecordingDTO> recordingList = new List<RecordingDTO>();

            foreach(var recording in recordings)
            {
                if (songID == recording.songID)
                {
                    recordingList.Add(new RecordingDTO()
                    {
                        ID = recording.id,
                        Name = GetAlbumArtistName(recording.homeAlbum) + " - " + 
                            GetAlbumName(recording.homeAlbum) + " - " + recording.title,
                        Live = recording.live,
                        IsHome = (recording.homeAlbum == albumID),
                        Weight = (recording.live ? 0.1 : 1.0)
                    });
                }
            }

            return recordingList;
        }

        void ILibraryRequestHandler.UpdateWeight(LibraryContext context, long id, double weight)
        {
            throw new NotImplementedException();
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

        private static BitmapImage LoadImage(string path)
        {
            BitmapImage image = new BitmapImage();

            var resource = File.OpenRead(Path.Combine(FileUtility.GetDataPath(), path));
            using (StreamReader sr = new StreamReader(resource))
            {
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = sr.BaseStream;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
    }
}
