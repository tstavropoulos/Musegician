using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

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
            get { return _recording.Name; }
            set
            {
                _recording.Name = value;
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
                if (Playing)
                {
                    return "🔊";
                }
                return " ";
            }
        }

        public long ID
        {
            get { return _recording.ID; }
            set
            {
                _recording.ID = value;
                OnPropertyChanged("ID");
            }
        }

        public bool Live
        {
            get { return _recording.Live; }
        }

        public string LiveString
        {
            get
            {
                if (Live)
                {
                    return "🎤";
                }
                return "";
            }
        }

        public double Weight
        {
            get { return _recording.Weight; }
        }

        public RecordingDTO Recording
        {
            get { return _recording; }
        }

        public bool IsDim
        {
            get { return Weight == 0.0 || _song.IsDim; }
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
