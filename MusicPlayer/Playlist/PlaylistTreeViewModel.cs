using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Playlist
{
    class PlaylistTreeViewModel
    {
        #region Data

        readonly ObservableCollection<PlaylistItemViewModel> _playlistViewModels;

        #endregion // Data

        #region Constructor

        public PlaylistTreeViewModel(IList<PlaylistItemDTO> songs)
        {
            _playlistViewModels = new ObservableCollection<PlaylistItemViewModel>(
                (from song in songs
                 select new PlaylistItemViewModel(song))
                     .ToList());
        }

        #endregion // Constructor

        public void Add(PlaylistItemDTO song)
        {
            _playlistViewModels.Add(new PlaylistItemViewModel(song));
        }


        #region Properties

        #region PlaylistViewModels

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ObservableCollection<PlaylistItemViewModel> PlaylistViewModels
        {
            get { return _playlistViewModels; }
        }

        #endregion // ArtistViewModels

        #endregion // Properties

    }
}
