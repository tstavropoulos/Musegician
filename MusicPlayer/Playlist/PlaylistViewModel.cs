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
    public abstract class PlaylistViewModel : INotifyPropertyChanged
    {
        #region Data

        protected readonly ObservableCollection<PlaylistViewModel> _children;
        protected readonly DTO _data;
        protected readonly PlaylistViewModel _parent;

        bool _isExpanded;
        bool _isSelected;

        #endregion Data
        #region Constructor

        public PlaylistViewModel(DTO data, PlaylistViewModel parent)
        {
            _data = data;
            _parent = parent;

            _children = new ObservableCollection<PlaylistViewModel>();
        }

        #endregion Constructor
        #region Properties

        public ObservableCollection<PlaylistViewModel> Children
        {
            get { return _children; }
        }

        public string Title
        {
            get { return _data.Name; }
            set
            {
                _data.Name = value;
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
            }
        }

        public long ID
        {
            get { return _data.ID; }
            set
            {
                _data.ID = value;
                OnPropertyChanged("ID");
            }
        }

        public double Weight
        {
            get { return _data.Weight; }
            set
            {
                bool dimUpdate = false;

                if ((Weight == 0.0) != (value == 0.0))
                {
                    dimUpdate = true;
                }

                _data.Weight = value;
                OnPropertyChanged("Weight");

                if (dimUpdate)
                {
                    OnPropertyChanged("IsDim");
                }
            }
        }

        public bool IsDim
        {
            get { return Weight == 0.0; }
        }

        #endregion Properties
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

                if (_isExpanded && _parent != null)
                {
                    _parent.IsExpanded = true;
                }
            }
        }

        #endregion IsExpanded
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

        #endregion IsSelected
        #region Parent

        public PlaylistViewModel Parent
        {
            get { return _parent; }
        }

        #endregion Parent
        #endregion Presentation Members
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
