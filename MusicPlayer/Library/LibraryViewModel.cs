using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Musegician.Database;

namespace Musegician.Library
{
    public class LibraryViewModel : INotifyPropertyChanged
    {
        #region Data

        static readonly LibraryViewModel DummyChild = new LibraryViewModel();

        bool _isExpanded;
        bool _isSelected;

        #endregion Data
        #region Constructors

        public LibraryViewModel(BaseData data, LibraryViewModel parent, bool lazyLoadChildren)
        {
            Data = data;
            Parent = parent;

            if (lazyLoadChildren)
            {
                Children.Add(DummyChild);
            }
        }

        //For dummy child
        private LibraryViewModel()
        {
            Parent = null;
            Data = new Artist();
        }

        #endregion Constructors
        #region Properties

        public ObservableCollection<LibraryViewModel> Children { get; }
            = new ObservableCollection<LibraryViewModel>();

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild => Children.Count == 1 && Children[0] == DummyChild;

        public BaseData Data { get; }

        public virtual string Name => "";

        protected virtual double WeightValue
        {
            get => Data.Weight;
            set => Data.Weight = value;
        }

        public double Weight
        {
            get => (WeightValue != -1.0) ? WeightValue : Data.DefaultWeight;
            set
            {
                bool dimUpdate = false;

                if ((Weight == 0.0) != (value == 0.0))
                {
                    dimUpdate = true;
                }

                WeightValue = value;
                OnPropertyChanged("Weight");

                if (dimUpdate)
                {
                    OnPropertyChanged("IsDim");
                }
            }
        }

        public virtual bool IsDim => Weight == 0.0;

        #endregion Properties
        #region Presentation Members
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
        #region NameContainsText

        public virtual bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(Name))
            {
                return false;
            }

            return Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion NameContainsText
        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        public virtual void LoadChildren(ILibraryRequestHandler dataManager)
        {
            Children.Clear();
        }

        #endregion LoadChildren
        #region Parent

        public LibraryViewModel Parent { get; }

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
