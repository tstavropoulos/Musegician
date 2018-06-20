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
            Library.MockDB db = new Library.MockDB();

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
}
