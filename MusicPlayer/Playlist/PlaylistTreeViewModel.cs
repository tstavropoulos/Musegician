using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Playlist
{
    class PlaylistTreeViewModel
    {
        #region Constructors

        public PlaylistTreeViewModel()
        {
            PlaylistViewModels = new ObservableCollection<PlaylistSongViewModel>();
        }

        public PlaylistTreeViewModel(IEnumerable<PlaylistSong> songs)
        {
            PlaylistViewModels = new ObservableCollection<PlaylistSongViewModel>(
                (from song in songs
                 select new PlaylistSongViewModel(song))
                     .ToList());
        }

        #endregion Constructors
        #region Data Methods

        public void Add(IEnumerable<PlaylistSong> songs)
        {
            foreach (PlaylistSong song in songs)
            {
                PlaylistViewModels.Add(new PlaylistSongViewModel(song));
            }
        }

        public void InsertRange(int index, IEnumerable<PlaylistSong> songs)
        {
            foreach (PlaylistSong song in songs)
            {
                PlaylistViewModels.Insert(index, new PlaylistSongViewModel(song));
                ++index;
            }
        }

        #endregion Data Methods
        #region Properties

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ObservableCollection<PlaylistSongViewModel> PlaylistViewModels { get; }

        #endregion Properties
    }
}
