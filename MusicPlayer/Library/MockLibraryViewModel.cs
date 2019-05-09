using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Musegician.DataStructures;
using Musegician.Database;

namespace Musegician.Library
{
    public sealed class MockLibraryViewModel
    {
        #region Data

        public SearchChoices SearchChoice { get; set; } = SearchChoices.All;

        #endregion Data
        #region Constructor

        public MockLibraryViewModel()
        {
            ILibraryRequestHandler db = new MockDB();

            IEnumerable<Artist> artistList = db.GenerateArtistList();
            IEnumerable<Album> albumList = db.GenerateAlbumList();
            IEnumerable<DirectoryDTO> directoryList = db.GetDirectories("");

            ClassicArtistViewModels = new ObservableCollection<ArtistViewModel>(
                (from artist in artistList
                 select new ArtistViewModel(artist, ViewMode.Classic))
                     .ToList());

            SimpleViewModels = new ObservableCollection<ArtistViewModel>(
                (from artist in artistList
                 select new ArtistViewModel(artist, ViewMode.Simple))
                     .ToList());

            AlbumViewModels = new ObservableCollection<AlbumViewModel>(
                (from album in albumList
                 select new AlbumViewModel(album, null, false))
                     .ToList());

            DirectoryViewModels = new ObservableCollection<DirectoryViewModel>(
                (from data in directoryList
                 select new DirectoryViewModel(data, null, false))
                     .ToList());

            ClassicArtistViewModels[0].LoadChildren(db);
            ClassicArtistViewModels[0].IsExpanded = true;

            ClassicArtistViewModels[1].LoadChildren(db);
            ClassicArtistViewModels[1].IsExpanded = true;
            ClassicArtistViewModels[1].IsSelected = true;
            ClassicArtistViewModels[1].Children[0].LoadChildren(db);
            ClassicArtistViewModels[1].Children[0].IsExpanded = true;
            ClassicArtistViewModels[1].Children[0].Children[1].LoadChildren(db);
            ClassicArtistViewModels[1].Children[0].Children[1].IsExpanded = true;
        }

        #endregion Constructor
        #region ArtistViewModels

        public ObservableCollection<ArtistViewModel> ClassicArtistViewModels { get; }
        public ObservableCollection<AlbumViewModel> AlbumViewModels { get; }
        public ObservableCollection<ArtistViewModel> SimpleViewModels { get; }
        public ObservableCollection<DirectoryViewModel> DirectoryViewModels { get; }

        #endregion ArtistViewModels
    }

    #region MockDB

    public sealed class MockDB : ILibraryRequestHandler
    {
        #region Data

        private List<Artist> Artists { get; } = new List<Artist>();
        private List<Album> Albums { get; } = new List<Album>();
        private List<Song> Songs { get; } = new List<Song>();
        private List<Recording> Recordings { get; } = new List<Recording>();

        #endregion Data
        #region Constructor

        public MockDB()
        {
            int artistID = 0;
            int albumID = 0;
            int songID = 0;
            int recordingID = 0;

            //Aerosmith
            {
                Artist aerosmith = new Artist()
                {
                    Id = artistID++,
                    Name = "Aerosmith",
                    Weight = -1.0
                };
                Artists.Add(aerosmith);

                Album permanentVacation = new Album()
                {
                    Id = albumID++,
                    Title = "Permanent Vacation",
                    Image = LoadImage(@"MockDBResources\0.jpg"),
                    Weight = -1.0,
                    Year = 1987
                };
                Albums.Add(permanentVacation);

                AddSimple("Heart's Done Time", 1, aerosmith, permanentVacation, songID++, recordingID++);
                AddSimple("Magic Touch", 2, aerosmith, permanentVacation, songID++, recordingID++);
                AddSimple("Rag Doll", 3, aerosmith, permanentVacation, songID++, recordingID++);

                Album toysInTheAttic = new Album()
                {
                    Id = albumID++,
                    Title = "Toys In The Attic",
                    Image = LoadImage(@"MockDBResources\1.jpg"),
                    Weight = -1.0,
                    Year = 1975
                };
                Albums.Add(toysInTheAttic);

                AddSimple("Toys In The Attic", 1, aerosmith, toysInTheAttic, songID++, recordingID++);
                AddSimple("Uncle Salty", 2, aerosmith, toysInTheAttic, songID++, recordingID++);
                AddSimple("Adam's Apple", 3, aerosmith, toysInTheAttic, songID++, recordingID++);
            }

            //Billy Joel
            {
                Artist billyJoel = new Artist()
                {
                    Id = artistID++,
                    Name = "Billy Joel",
                    Weight = -1.0
                };
                Artists.Add(billyJoel);

                Album stormFront = new Album()
                {
                    Id = albumID++,
                    Title = "Storm Front",
                    Image = LoadImage(@"MockDBResources\2.jpg"),
                    Weight = -1.0,
                    Year = 1989
                };
                Albums.Add(stormFront);

                AddSimple("Storm Front", 1, billyJoel, stormFront, songID++, recordingID++);
                Song fireSong = AddSimple("We Didn't Start The Fire", 2, billyJoel, stormFront, songID++, recordingID++);
                AddSimple("Leningrad", 3, billyJoel, stormFront, songID++, recordingID++);
                AddSimple("State of Grace", 4, billyJoel, stormFront, songID++, recordingID++);

                Album songsInTheAttic = new Album()
                {
                    Id = albumID++,
                    Title = "Songs In The Attic",
                    Image = LoadImage(@"MockDBResources\3.jpg"),
                    Weight = -1.0,
                    Year = 1981
                };
                Albums.Add(stormFront);
                AddExisting("We Didn't Start The Fire (Live)", 1, fireSong, billyJoel, songsInTheAttic, recordingID++, true);
                AddSimple("A Matter Of Trust", 2, billyJoel, songsInTheAttic, songID++, recordingID++, true);
            }

            //Steely Dan
            {
                Artist steelyDan = new Artist()
                {
                    Id = artistID++,
                    Name = "Steely Dan",
                    Weight = -1.0
                };
                Artists.Add(steelyDan);

                Album twoAgainstNature = new Album()
                {
                    Id = albumID++,
                    Title = "Two Against Nature",
                    Image = LoadImage(@"MockDBResources\4.jpg"),
                    Weight = -1.0,
                    Year = 2000
                };
                Albums.Add(twoAgainstNature);

                AddSimple("Gaslighting Abbie", 1, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("What A Shame", 2, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("Two Against Nature", 3, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("Janie Runaway", 4, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("Almost Gothic", 5, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("Jack of Speed", 6, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("Cousin Dupree", 7, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("Negative Girl", 8, steelyDan, twoAgainstNature, songID++, recordingID++);
                AddSimple("West of Hollywood", 9, steelyDan, twoAgainstNature, songID++, recordingID++);

            }


        }

        #endregion Constructor
        #region ILibraryRequestHandler

        IEnumerable<Album> ILibraryRequestHandler.GenerateAlbumList()
        {
            return (from album in Albums
                    orderby album.Title
                    select album);
        }

        IEnumerable<Recording> ILibraryRequestHandler.GenerateAlbumRecordingList(Album album)
        {
            return (from recording in album.Recordings
                    orderby recording.DiscNumber ascending, recording.TrackNumber ascending
                    select recording);
        }

        IEnumerable<Album> ILibraryRequestHandler.GenerateArtistAlbumList(Artist artist)
        {
            return (from recording in artist.Recordings
                    orderby recording.Album.Year ascending
                    select recording.Album).Distinct();
        }

        IEnumerable<Artist> ILibraryRequestHandler.GenerateArtistList()
        {
            return (from artist in Artists
                    orderby (artist.Name.StartsWith("The ") ? artist.Name.Substring(4) : artist.Name)
                    select artist);
        }

        IEnumerable<Song> ILibraryRequestHandler.GenerateArtistSongList(Artist artist)
        {
            return (from recording in artist.Recordings
                    orderby (recording.Song.Title.StartsWith("The ") ? recording.Song.Title.Substring(4) : recording.Song.Title)
                    select recording.Song);
        }

        IEnumerable<Recording> ILibraryRequestHandler.GenerateSongRecordingList(Song song)
        {
            return (from recording in song.Recordings
                    orderby recording.Live
                    select recording);
        }

        IEnumerable<DirectoryDTO> ILibraryRequestHandler.GetDirectories(string path)
        {
            if (path == "")
            {
                return new List<DirectoryDTO>()
                {
                    new DirectoryDTO("C:")
                };
            }
            else if (path == "C:")
            {
                return new List<DirectoryDTO>()
                {
                    new DirectoryDTO("TestDir")
                };
            }

            return new List<DirectoryDTO>();
        }

        IEnumerable<Recording> ILibraryRequestHandler.GetDirectoryRecordings(string path)
        {
            if (path == "C:\\TestDir")
            {
                return new List<Recording>()
                {
                    new Recording()
                    {
                        Id = 1,
                        Live = false
                    }
                };
            }

            return new List<Recording>();
        }

        #endregion ILibraryRequestHandler
        #region Playlist Methods

        public List<PlaylistSong> GenerateSongList()
        {
            List<PlaylistSong> songList = new List<PlaylistSong>();

            foreach (var song in Songs)
            {
                string artistName = "Various";

                var artistList = song.Recordings.Select(x => x.Artist).Distinct();
                if (artistList.Count() == 1)
                {
                    artistName = artistList.First().Name;
                }

                PlaylistSong newPlaylistSong = new PlaylistSong(
                    song: song,
                    title: $"{artistName} - {song.Title}")
                {
                    Weight = song.Weight
                };

                foreach (Recording recording in song.Recordings)
                {
                    newPlaylistSong.PlaylistRecordings.Add(
                        new PlaylistRecording(
                            recording,
                            $"{recording.Artist.Name} - {recording.Album.Title} - {recording.Title}")
                        {
                            Weight = recording.Weight
                        });
                }

                songList.Add(newPlaylistSong);
            }

            return songList;
        }

        #endregion Playlist Methods
        #region Helper Methods

        private static byte[] LoadImage(string path)
        {
            string filePath = Path.Combine(FileUtility.GetDataPath(), path);
            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }

            return null;
        }

        private Song AddSimple(
            string title,
            int trackNum,
            Artist artist,
            Album album,
            int songID,
            int recordingID,
            bool live = false)
        {
            Song simpleSong = new Song()
            {
                Id = songID,
                Title = title,
                Weight = -1.0
            };

            Songs.Add(simpleSong);

            Recording simpleRecording = new Recording()
            {
                Id = recordingID,
                Filename = "",
                Title = title,
                Live = live,
                Artist = artist,
                Song = simpleSong,
                Album = album,
                TrackNumber = trackNum,
                DiscNumber = 1,
                Weight = -1.0
            };
            simpleSong.Recordings.Add(simpleRecording);
            artist.Recordings.Add(simpleRecording);

            Recordings.Add(simpleRecording);

            album.Recordings.Add(simpleRecording);

            return simpleSong;
        }


        private void AddExisting(
            string title,
            int trackNum,
            Song song,
            Artist artist,
            Album album,
            int recordingID,
            bool live = false)
        {
            Recording simpleRecording = new Recording()
            {
                Id = recordingID,
                Filename = "",
                Live = live,
                Title = title,
                Artist = artist,
                Song = song,
                Album = album,
                TrackNumber = trackNum,
                DiscNumber = 1,
                Weight = -1.0
            };
            song.Recordings.Add(simpleRecording);
            artist.Recordings.Add(simpleRecording);

            Recordings.Add(simpleRecording);

            album.Recordings.Add(simpleRecording);
        }


        event EventHandler ILibraryRequestHandler.RebuildNotifier
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }

        void ILibraryRequestHandler.DatabaseUpdated() => throw new NotImplementedException();
        void ILibraryRequestHandler.Delete(IEnumerable<Recording> recordings) => throw new NotImplementedException();

        #endregion Helper Methods
    }

    #endregion MockDB
}
