using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    public class ArtistViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ReadOnlyCollection<SongViewModel> _songs;
        readonly ArtistDTO _artist;

        bool _isExpanded;
        bool _isSelected;

        #endregion // Data

        #region Constructors

        public ArtistViewModel(ArtistDTO artist)
        {
            _artist = artist;

            _songs = new ReadOnlyCollection<SongViewModel>(
                    (from song in artist.Songs
                     select new SongViewModel(song, this))
                     .ToList());
        }

        #endregion // Constructors

        #region Artist Properties

        public ReadOnlyCollection<SongViewModel> Songs
        {
            get { return _songs; }
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
