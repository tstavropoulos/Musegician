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

        readonly List<PlaylistItemViewModel> _playlistViewModels;
        
        #endregion // Data

        #region Constructor

        public PlaylistTreeViewModel(IList<SongDTO> songs)
        {
            _playlistViewModels = new List<PlaylistItemViewModel>(
                (from song in songs
                 select new PlaylistItemViewModel(song))
                     .ToList());
        }

        #endregion // Constructor

        public void Add(SongDTO song)
        {
            _playlistViewModels.Add(new PlaylistItemViewModel(song));
        }


        #region Properties

        #region ArtistViewModels

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public List<PlaylistItemViewModel> PlaylistItemModels
        {
            get { return _playlistViewModels; }
        }

        #endregion // ArtistViewModels

        #endregion // Properties
        
    }
}
