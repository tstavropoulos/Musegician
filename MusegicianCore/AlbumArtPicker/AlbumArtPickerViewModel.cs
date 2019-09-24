using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.AlbumArtPicker
{
    public abstract class AlbumArtPickerViewModel : INotifyPropertyChanged
    {
        #region Data

        bool _isExpanded;
        bool _isSelected;

        #endregion Data
        #region Constructors

        public AlbumArtPickerViewModel(AlbumArtPickerViewModel parent)
        {
            Parent = parent;
        }

        #endregion Constructors
        #region Properties

        public ObservableCollection<AlbumArtPickerViewModel> Children { get; } =
            new ObservableCollection<AlbumArtPickerViewModel>();

        public abstract string Name { get; }

        #endregion Properties
        #region Presentation Members
        #region IsGrayedOut

        public virtual bool IsGrayedOut => true;

        #endregion IsGrayedOut
        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                {
                    Parent.IsExpanded = true;
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
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }

                //Expand Parent
                if (_isSelected && Parent != null && !Parent.IsExpanded)
                {
                    Parent.IsExpanded = true;
                }
            }
        }

        #endregion IsSelected
        #region Parent

        public AlbumArtPickerViewModel Parent { get; }

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
