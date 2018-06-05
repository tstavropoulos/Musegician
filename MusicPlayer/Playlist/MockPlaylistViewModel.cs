using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Musegician.Database;

namespace Musegician.Playlist
{
    public class MockPlaylistViewModel
    {
        #region Constructor

        public MockPlaylistViewModel()
        {
            MockDB db = new MockDB();

            List<PlaylistSong> songlist = db.GenerateSongList();

            PlaylistViewModels = new ObservableCollection<PlaylistSongViewModel>(
                (from song in songlist
                 select new PlaylistSongViewModel(song))
                     .ToList());

            PlaylistViewModels[7].IsExpanded = true;
            PlaylistViewModels[7].Playing = true;
            PlaylistViewModels[7].Children[0].IsSelected = true;
            PlaylistViewModels[7].Children[0].Playing = true;
            PlaylistViewModels[10].IsExpanded = true;
        }

        #endregion Constructor
        #region ViewModels

        public ObservableCollection<PlaylistSongViewModel> PlaylistViewModels { get; }

        #endregion ViewModels
    }

    #region MockDB

    public class MockDB
    {
        #region Data

        private List<Artist> Artists { get; } = new List<Artist>();
        private List<Album> Albums { get; } = new List<Album>();
        private List<Song> Songs { get; } = new List<Song>();
        private List<Recording> Recordings { get; } = new List<Recording>();
        private List<Track> Tracks { get; } = new List<Track>();

        private int artistID = 0;
        private int albumID = 0;
        private int songID = 0;
        private int trackID = 0;
        private int recordingID = 0;

        #endregion Data
        #region Constructor

        public MockDB()
        {
            //Aerosmith
            {
                Artist aerosmith = new Artist()
                {
                    Id = artistID++,
                    Name = "Aerosmith",
                    Weight = 1.0
                };
                Artists.Add(aerosmith);

                Album permanentVacation = new Album()
                {
                    Id = albumID++,
                    Title = "Permanent Vacation",
                    Image = LoadImage(@"MockDBResources\0.jpg"),
                    Weight = 1.0,
                    Year = 1987
                };
                Albums.Add(permanentVacation);

                AddSimple("Heart's Done Time", 1, aerosmith, permanentVacation);
                AddSimple("Magic Touch", 2, aerosmith, permanentVacation);
                AddSimple("Rag Doll", 3, aerosmith, permanentVacation);

                Album toysInTheAttic = new Album()
                {
                    Id = albumID++,
                    Title = "Toys In The Attic",
                    Image = LoadImage(@"MockDBResources\1.jpg"),
                    Weight = 1.0,
                    Year = 1975
                };
                Albums.Add(toysInTheAttic);

                AddSimple("Toys In The Attic", 1, aerosmith, toysInTheAttic);
                AddSimple("Uncle Salty", 2, aerosmith, toysInTheAttic);
                AddSimple("Adam's Apple", 3, aerosmith, toysInTheAttic);
            }

            //Billy Joel
            {
                Artist billyJoel = new Artist()
                {
                    Id = artistID++,
                    Name = "Billy Joel",
                    Weight = 1.0
                };
                Artists.Add(billyJoel);

                Album stormFront = new Album()
                {
                    Id = albumID++,
                    Title = "Storm Front",
                    Image = LoadImage(@"MockDBResources\2.jpg"),
                    Weight = 1.0,
                    Year = 1989
                };
                Albums.Add(stormFront);

                AddSimple("Storm Front", 1, billyJoel, stormFront);
                Song fireSong = AddSimple("We Didn't Start The Fire", 2, billyJoel, stormFront);
                AddSimple("Leningrad", 3, billyJoel, stormFront);
                AddSimple("State of Grace", 4, billyJoel, stormFront);

                Album songsInTheAttic = new Album()
                {
                    Id = albumID++,
                    Title = "Songs In The Attic",
                    Image = LoadImage(@"MockDBResources\3.jpg"),
                    Weight = 1.0,
                    Year = 1981
                };
                Albums.Add(stormFront);
                AddExisting("We Didn't Start The Fire (Live)", 1, fireSong, billyJoel, songsInTheAttic, true);
                AddSimple("A Matter Of Trust", 2, billyJoel, songsInTheAttic, true);
            }

            //Steely Dan
            {
                Artist steelyDan = new Artist()
                {
                    Id = artistID++,
                    Name = "Steely Dan",
                    Weight = 1.0
                };
                Artists.Add(steelyDan);

                Album twoAgainstNature = new Album()
                {
                    Id = albumID++,
                    Title = "Two Against Nature",
                    Image = LoadImage(@"MockDBResources\4.jpg"),
                    Weight = 1.0,
                    Year = 2000
                };
                Albums.Add(twoAgainstNature);

                AddSimple("Gaslighting Abbie", 1, steelyDan, twoAgainstNature);
                AddSimple("What A Shame", 2, steelyDan, twoAgainstNature);
                AddSimple("Two Against Nature", 3, steelyDan, twoAgainstNature);
                AddSimple("Janie Runaway", 4, steelyDan, twoAgainstNature);
                AddSimple("Almost Gothic", 5, steelyDan, twoAgainstNature);
                AddSimple("Jack of Speed", 6, steelyDan, twoAgainstNature);
                AddSimple("Cousin Dupree", 7, steelyDan, twoAgainstNature);
                AddSimple("Negative Girl", 8, steelyDan, twoAgainstNature);
                AddSimple("West of Hollywood", 9, steelyDan, twoAgainstNature);

            }
        }

        #endregion Constructor
        #region Public Data Interface

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
                    title: $"{artistName} - {song.Title}");

                foreach (Recording recording in song.Recordings)
                {
                    newPlaylistSong.PlaylistRecordings.Add(
                        new PlaylistRecording(
                            recording,
                            $"{recording.Artist.Name} - {recording.Tracks.First().Album.Title} - {recording.Tracks.First().Title}"));
                }

                songList.Add(newPlaylistSong);
            }

            return songList;
        }

        #endregion Public Data Interface
        #region Helper Methods

        private Song AddSimple(
            string title,
            int trackNum,
            Artist artist,
            Album album,
            bool live = false)
        {
            Song simpleSong = new Song()
            {
                Id = songID,
                Title = title,
                Weight = 1.0
            };

            Songs.Add(simpleSong);

            Recording simpleRecording = new Recording()
            {
                Id = recordingID++,
                Filename = "",
                Live = live,
                Artist = artist,
                Song = simpleSong
            };
            Recordings.Add(simpleRecording);

            Track simpleTrack = new Track()
            {
                Id = trackID,
                Title = title,
                TrackNumber = trackNum,
                DiscNumber = 1,
                Album = album,
                Recording = simpleRecording,
                Weight = 1.0
            };
            Tracks.Add(simpleTrack);

            return simpleSong;
        }


        private void AddExisting(
            string title,
            int trackNum,
            Song song,
            Artist artist,
            Album album,
            bool live = false)
        {
            Recording simpleRecording = new Recording()
            {
                Id = recordingID++,
                Filename = "",
                Live = live,
                Artist = artist,
                Song = song
            };
            Recordings.Add(simpleRecording);

            Track simpleTrack = new Track()
            {
                Id = trackID,
                Title = title,
                TrackNumber = trackNum,
                DiscNumber = 1,
                Album = album,
                Recording = simpleRecording,
                Weight = 1.0
            };
            Tracks.Add(simpleTrack);
        }

        private static byte[] LoadImage(string path)
        {
            string filePath = Path.Combine(FileUtility.GetDataPath(), path);
            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }

            return null;
        }

        #endregion Helper Methods
    }

    #endregion MockDB
}
