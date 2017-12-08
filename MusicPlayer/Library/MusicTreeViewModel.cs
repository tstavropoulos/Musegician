using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Library
{
    public class MusicTreeViewModel
    {
        #region Data

        readonly ReadOnlyCollection<ArtistViewModel> _artistViewModels;
        readonly ICommand _searchCommand;

        IEnumerator<ArtistViewModel> _matchingArtistEnumerator;
        string _searchText = String.Empty;

        #endregion // Data

        #region Constructor

        public MusicTreeViewModel(IList<ArtistDTO> artists)
        {
            _artistViewModels = new ReadOnlyCollection<ArtistViewModel>(
                (from artist in artists
                select new ArtistViewModel(artist))
                     .ToList());

            _searchCommand = new SearchMusicTreeCommand(this);
        }

        #endregion // Constructor

        #region Properties

        #region ArtistViewModels

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ReadOnlyCollection<ArtistViewModel> ArtistViewModels
        {
            get { return _artistViewModels; }
        }

        #endregion // ArtistViewModels

        #region SearchCommand

        /// <summary>
        /// Returns the command used to execute a search in the family tree.
        /// </summary>
        public ICommand SearchCommand
        {
            get { return _searchCommand; }
        }

        private class SearchMusicTreeCommand : ICommand
        {
            readonly MusicTreeViewModel _musicTree;

            public SearchMusicTreeCommand(MusicTreeViewModel musicTree)
            {
                _musicTree = musicTree;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            event EventHandler ICommand.CanExecuteChanged
            {
                // I intentionally left these empty because
                // this command never raises the event, and
                // not using the WeakEvent pattern here can
                // cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                _musicTree.PerformSearch();
            }
        }

        #endregion // SearchCommand

        #region SearchText

        /// <summary>
        /// Gets/sets a fragment of the name to search for.
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (value == _searchText)
                    return;

                _searchText = value;

                _matchingArtistEnumerator = null;
            }
        }

        #endregion // SearchText

        #endregion // Properties

        #region Search Logic

        void PerformSearch()
        {
            if (_matchingArtistEnumerator == null || !_matchingArtistEnumerator.MoveNext())
            {
                VerifyMatchingArtistEnumerator();
            }

            var artist = _matchingArtistEnumerator.Current;

            if (artist == null)
            {
                return;
            }

            artist.IsSelected = true;
        }

        void VerifyMatchingArtistEnumerator()
        {
            var matches = FindMatchingArtists(_searchText);
            _matchingArtistEnumerator = matches.GetEnumerator();

            if (!_matchingArtistEnumerator.MoveNext())
            {
                MessageBox.Show(
                    "No matching artists were found.",
                    "Try Again",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
            }
        }

        IEnumerable<ArtistViewModel> FindMatchingArtists(string searchText)
        {
            foreach(ArtistViewModel artist in _artistViewModels)
            {
                if (artist.NameContainsText(searchText))
                {
                    yield return artist;
                }
            }
        }

        IEnumerable<SongViewModel> FindMatchingSongs(string searchText)
        {
            foreach (ArtistViewModel artist in _artistViewModels)
            {
                foreach(SongViewModel song in FindMatchingSongs(searchText, artist))
                {
                    yield return song;
                }
            }
        }

        IEnumerable<SongViewModel> FindMatchingSongs(string searchText, ArtistViewModel artist)
        {
            foreach (AlbumViewModel album in artist.Albums)
            {
                foreach (SongViewModel song in album.Songs)
                {
                    if (song.NameContainsText(searchText))
                    {
                        yield return song;
                    }
                }
            }
        }

        #endregion // Search Logic

    }
}
