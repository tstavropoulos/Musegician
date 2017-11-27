using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace MusicPlayer
{
    public class ArtistViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ReadOnlyCollection<AlbumViewModel> _albums;
        readonly ArtistDTO _artist;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public ArtistViewModel(ArtistDTO artist)
        {
            _artist = artist;

            _albums = new ReadOnlyCollection<AlbumViewModel>(
                    (from album in artist.Albums
                     select new AlbumViewModel(album, this))
                     .ToList());
        }

        #endregion // Constructors

        #region Artist Properties

        public ReadOnlyCollection<AlbumViewModel> Albums
        {
            get { return _albums; }
        }

        public string Name
        {
            get { return _artist.Name; }
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
                    this.OnPropertyChanged("IsExpanded");
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
                    this.OnPropertyChanged("IsSelected");
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

        #endregion // Presentation Members      

        #region Callbacks

        public void PlayArtist(object sender, RoutedEventArgs e)
        {
            
        }

        #endregion //Callbacks

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}
