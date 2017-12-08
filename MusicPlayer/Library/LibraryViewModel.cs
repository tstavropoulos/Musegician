﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Library
{
    public class LibraryViewModel : INotifyPropertyChanged
    {
        #region Data

        static readonly LibraryViewModel DummyChild = new LibraryViewModel();

        readonly ObservableCollection<LibraryViewModel> _children;
        readonly LibraryViewModel _parent;
        readonly DTO _data;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public LibraryViewModel(DTO data, LibraryViewModel parent, bool lazyLoadChildren)
        {
            _data = data;
            _parent = parent;

            _children = new ObservableCollection<LibraryViewModel>();

            if (lazyLoadChildren)
            {
                _children.Add(DummyChild);
            }
        }

        //For dummy child
        private LibraryViewModel()
        {
        }

        #endregion // Constructors

        #region Artist Properties

        public ObservableCollection<LibraryViewModel> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return Children.Count == 1 && Children[0] == DummyChild; }
        }

        public DTO Data
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

        public double Weight
        {
            get { return _data.Weight; }
        }

        public virtual bool IsDim
        {
            get { return Weight == 0.0 || (Parent != null && Parent.IsDim); }
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
                if (_isExpanded && _parent != null)
                {
                    _parent.IsExpanded = true;
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
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(this.Name))
                return false;

            return this.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion // NameContainsText

        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        public virtual void LoadChildren(ILibraryRequestHandler dataManager)
        {
            Children.Clear();
        }

        #endregion // LoadChildren

        #region Parent

        public LibraryViewModel Parent
        {
            get { return _parent; }
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
