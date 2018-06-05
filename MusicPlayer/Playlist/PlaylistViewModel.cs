using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Playlist
{
    public abstract class PlaylistViewModel : INotifyPropertyChanged
    {
        #region Data
        
        protected readonly BaseData _data;

        bool _isExpanded;
        bool _isSelected;
        bool _showDropLine;

        #endregion Data
        #region Constructor

        public PlaylistViewModel(BaseData data, PlaylistViewModel parent)
        {
            _data = data;
            Parent = parent;
        }

        #endregion Constructor
        #region Properties

        public ObservableCollection<PlaylistViewModel> Children { get; } = new ObservableCollection<PlaylistViewModel>();

        public abstract string Title { get; set; }

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

        public bool IsDim => (Weight == 0.0);

        #endregion Properties
        #region Presentation Members
        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem associated with this object is expanded.
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

                if (_isExpanded && Parent != null)
                {
                    Parent.IsExpanded = true;
                }
            }
        }

        #endregion IsExpanded
        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem associated with this object is selected.
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
        #region ShowDropLine

        /// <summary>
        /// Gets/sets whether the TreeViewItem associated with this object should render a dragdrop line.
        /// </summary>
        public bool ShowDropLine
        {
            get { return _showDropLine; }
            set
            {
                if (value != _showDropLine)
                {
                    _showDropLine = value;
                    OnPropertyChanged("ShowDropLine");
                }
            }
        }

        #endregion ShowDropLine
        #region Parent

        public PlaylistViewModel Parent { get; }

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
