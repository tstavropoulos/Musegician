using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Playlist
{
    class PlaylistRecordingViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly RecordingDTO _recording;
        readonly PlaylistSongViewModel _song;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructor

        public PlaylistRecordingViewModel(RecordingDTO recording, PlaylistSongViewModel song)
        {
            _recording = recording;
            _song = song;
        }

        #endregion // Constructor

        #region PlaylistItem Properties

        public string Title
        {
            get { return _recording.Title; }
            set
            {
                _recording.Title = value;
                OnPropertyChanged("Title");
            }

        }

        private bool _playing = false;

        public bool Playing
        {
            get { return _playing; }
            set
            {
                _playing = value;
                OnPropertyChanged("Playing");
                OnPropertyChanged("PlayingString");
            }
        }

        public string PlayingString
        {
            get
            {
                if (_playing)
                {
                    return "★";
                }
                return " ";
            }
        }

        public long ID
        {
            get { return _recording.RecordingID; }
            set
            {
                _recording.RecordingID = value;
                OnPropertyChanged("ID");
            }
        }

        public RecordingDTO Recording
        {
            get { return _recording; }
        }

        #endregion

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

                if(_isExpanded && _song != null)
                {
                    _song.IsExpanded = true;
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

        #region Parent

        public PlaylistSongViewModel Song
        {
            get { return _song; }
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
