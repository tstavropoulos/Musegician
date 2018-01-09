using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;

namespace Musegician.Playlist
{
    class PlaylistTreeViewModel
    {
        #region Data

        readonly ObservableCollection<PlaylistSongViewModel> _playlistViewModels;

        #endregion Data
        #region Constructors

        public PlaylistTreeViewModel()
        {
            _playlistViewModels = new ObservableCollection<PlaylistSongViewModel>();
        }

        public PlaylistTreeViewModel(ICollection<SongDTO> songs)
        {
            _playlistViewModels = new ObservableCollection<PlaylistSongViewModel>(
                (from song in songs
                 select new PlaylistSongViewModel(song))
                     .ToList());
        }

        #endregion Constructors
        #region Data Methods

        public void Add(SongDTO song)
        {
            _playlistViewModels.Add(new PlaylistSongViewModel(song));
        }

        public void Add(ICollection<SongDTO> songs)
        {
            foreach(SongDTO song in songs)
            {
                Add(song);
            }
        }

        #endregion Data Methods
        #region Properties

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ObservableCollection<PlaylistSongViewModel> PlaylistViewModels
        {
            get { return _playlistViewModels; }
        }

        #endregion Properties
    }
}
