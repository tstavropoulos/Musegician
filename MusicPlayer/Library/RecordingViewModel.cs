using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MusicPlayer.Library
{
    public class RecordingViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly SongViewModel _song;
        readonly RecordingDTO _recording;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public RecordingViewModel(RecordingDTO recording, SongViewModel song)
        {
            _recording = recording;
            _song = song;
        }

        #endregion // Constructors

        #region Recording Properties

        public string Title
        {
            get { return _recording.Title; }
        }

        public long ID
        {
            get { return _recording.RecordingID; }
        }

        public bool Live
        {
            get { return _recording.Live; }
        }

        public bool IsHome
        {
            get { return _recording.IsHome; }
        }

        public double Weight
        {
            get { return _recording.Weight; }
        }

        #endregion // Recording Properties

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
                if (_isExpanded && _song != null)
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

        #region NameContainsText

        public bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Title))
                return false;

            return Title.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText

        #region Parent

        public SongViewModel Song
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
