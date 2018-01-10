using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public abstract class DeredundafierViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ObservableCollection<DeredundafierViewModel> _children;
        readonly DeredundafierViewModel _parent;
        readonly DeredundafierDTO _data;

        bool _isExpanded;
        bool _isSelected;

        #endregion Data
        #region Constructors

        public DeredundafierViewModel(DeredundafierDTO data, DeredundafierViewModel parent)
        {
            _data = data;
            _parent = parent;

            _children = new ObservableCollection<DeredundafierViewModel>();
        }

        #endregion Constructors
        #region Properties

        public ObservableCollection<DeredundafierViewModel> Children
        {
            get { return _children; }
        }

        public DeredundafierDTO Data
        {
            get { return _data; }
        }

        public string Name
        {
            get { return _data.Name; }
        }

        public long ID
        {
            get { return _data.ID; }
        }

        #endregion Properties
        #region Presentation Members
        #region IsGrayedOut

        public virtual bool IsGrayedOut
        {
            get { return true; }
        }

        #endregion IsGrayedOut
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

                //Expand Parent
                if (_isSelected && _parent != null && !_parent.IsExpanded)
                {
                    _parent.IsExpanded = true;
                }
            }
        }

        #endregion IsSelected
        #region Parent

        public DeredundafierViewModel Parent
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
