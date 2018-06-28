using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.PrivateTagCleanup
{
    public class PrivateTagViewModel : INotifyPropertyChanged
    {
        #region Data

        private bool _isSelected;
        private readonly PrivateTagDTO data;

        #endregion Data
        #region Constructors

        public PrivateTagViewModel(PrivateTagDTO data)
        {
            this.data = data;
        }

        #endregion Constructors
        #region Properties

        public string Name => data.Owner;

        #endregion Properties
        #region Presentation Members
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
            }
        }

        #endregion IsSelected
        #region IsChecked

        public bool IsChecked
        {
            get => data.IsChecked;
            set
            {
                if (IsChecked != value)
                {
                    data.IsChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        #endregion IsChecked
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
