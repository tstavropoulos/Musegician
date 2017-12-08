using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MusicPlayer.Library
{
    public class SongViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ReadOnlyCollection<RecordingViewModel> _recordings;
        readonly AlbumViewModel _album;
        readonly SongDTO _song;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public SongViewModel(SongDTO song, AlbumViewModel album)
        {
            _song = song;
            _album = album;

            _recordings = new ReadOnlyCollection<RecordingViewModel>(
                    (from recording in _song.Recordings
                     select new RecordingViewModel(recording, this))
                     .ToList());
        }

        #endregion // Constructors

        #region Song Properties

        public ReadOnlyCollection<RecordingViewModel> Recordings
        {
            get { return _recordings; }
        }

        public string Title
        {
            get { return _song.Title; }
        }

        public long ID
        {
            get { return _song.SongID; }
        }

        public long ContextualTrackID
        {
            get { return _song.TrackID; }
        }

        public bool IsExpandable
        {
            get { return _recordings.Count > 1; }
        }

        public double Weight
        {
            get { return _song.Weight; }
        }

        public bool IsDim
        {
            get { return Weight == 0.0 || _album.IsDim || !_song.IsHome; }
        }

        #endregion // Song Properties

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
                if (_isExpanded && _album != null)
                {
                    _album.IsExpanded = true;
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
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Title))
                return false;

            return Title.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText

        #region Parent

        public AlbumViewModel Album
        {
            get { return _album; }
        }

        #endregion // Parent

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
