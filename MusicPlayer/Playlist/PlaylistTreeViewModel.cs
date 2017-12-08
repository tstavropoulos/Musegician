using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Playlist
{
    class PlaylistTreeViewModel
    {
        #region Data

        readonly ObservableCollection<PlaylistSongViewModel> _playlistViewModels;

        #endregion // Data

        #region Constructor

        public PlaylistTreeViewModel(IList<SongDTO> songs)
        {
            _playlistViewModels = new ObservableCollection<PlaylistSongViewModel>(
                (from song in songs
                 select new PlaylistSongViewModel(song))
                     .ToList());
        }

        #endregion // Constructor

        public void Add(SongDTO song)
        {
            _playlistViewModels.Add(new PlaylistSongViewModel(song));
        }

        public void Add(IList<SongDTO> songs)
        {
            foreach(SongDTO song in songs)
            {
                Add(song);
            }
        }


        #region Properties

        #region PlaylistViewModels

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ObservableCollection<PlaylistSongViewModel> PlaylistViewModels
        {
            get { return _playlistViewModels; }
        }

        #endregion // ArtistViewModels

        #endregion // Properties

    }
}
