using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    public class AlbumViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ReadOnlyCollection<SongViewModel> _songs;
        readonly AlbumDTO _album;
        readonly ArtistViewModel _artist;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public AlbumViewModel(AlbumDTO album, ArtistViewModel artist)
        {
            _album = album;
            _artist = artist;

            _songs = new ReadOnlyCollection<SongViewModel>(
                    (from song in _album.Songs
                     select new SongViewModel(song, this))
                     .ToList());
        }

        #endregion // Constructors

        #region Artist Properties

        public ReadOnlyCollection<SongViewModel> Songs
        {
            get { return _songs; }
        }

        public string Title
        {
            get { return _album.Title; }
        }

        public int ID
        {
            get { return _album.AlbumID; }
        }

        #endregion // Artist Properties

        #region Presentation Members

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _artist != null)
                {
                    _artist.IsExpanded = true;
                }
            }
        }

        #endregion // IsExpanded

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion // IsSelected

        #region NameContainsText

        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Title))
                return false;

            return this.Title.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText
        
        #endregion // Presentation Members        

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}
