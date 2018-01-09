using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Musegician.DataStructures;
using System.ComponentModel;

namespace Musegician.Library
{
    public class MusicTreeViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ObservableCollection<LibraryViewModel> _classicArtistViewModels =
            new ObservableCollection<LibraryViewModel>();

        readonly ObservableCollection<LibraryViewModel> _simpleArtistViewModels =
            new ObservableCollection<LibraryViewModel>();

        readonly ObservableCollection<LibraryViewModel> _albumViewModels =
            new ObservableCollection<LibraryViewModel>();

        readonly ICommand _searchCommand = new SearchMusicTreeCommand(null);

        IEnumerator<LibraryViewModel> _matchingRecordEnumerator;
        string _searchText = String.Empty;

        bool classicLoaded = false;
        bool simpleLoaded = false;
        bool albumLoaded = false;

        #endregion Data
        #region Engine References

        ILibraryRequestHandler requestHandler;

        #endregion Engine References
        #region Constructor

        public MusicTreeViewModel() { }

        public MusicTreeViewModel(ILibraryRequestHandler requestHandler)
            : this()
        {
            this.requestHandler = requestHandler;

            _searchCommand = new SearchMusicTreeCommand(this);
        }

        #endregion Constructor
        #region Helper Functions

        void CheckIsLoaded(ViewMode mode)
        {
            //Shortciruit for designer
            if (requestHandler == null)
            {
                return;
            }

            switch (mode)
            {
                case ViewMode.Classic:
                    if (!classicLoaded)
                    {
                        classicLoaded = true;
                        _classicArtistViewModels.Clear();

                        foreach (ArtistDTO artist in requestHandler.GenerateArtistList())
                        {
                            _classicArtistViewModels.Add(new ArtistViewModel(artist, ViewMode.Classic));
                        }
                    }
                    break;
                case ViewMode.Simple:
                    if (!simpleLoaded)
                    {
                        simpleLoaded = true;
                        _simpleArtistViewModels.Clear();

                        foreach (ArtistDTO artist in requestHandler.GenerateArtistList())
                        {
                            _simpleArtistViewModels.Add(new ArtistViewModel(artist, ViewMode.Simple));
                        }

                    }
                    break;
                case ViewMode.Album:
                    if (!albumLoaded)
                    {
                        albumLoaded = true;
                        _albumViewModels.Clear();

                        foreach (AlbumDTO album in requestHandler.GenerateAlbumList())
                        {
                            _albumViewModels.Add(new AlbumViewModel(album, null));
                        }

                    }
                    break;
                case ViewMode.MAX:
                default:
                    throw new Exception("Unexpected ViewMode: " + mode);
            }
        }

        #endregion Helper Functions
        #region Properties
        #region ArtistViewModels

        /// <summary>
        /// Returns a read-only collection containing the first person 
        /// in the family tree, to which the TreeView can bind.
        /// </summary>
        public ObservableCollection<LibraryViewModel> ClassicArtistViewModels
        {
            get { return _classicArtistViewModels; }
        }

        public ObservableCollection<LibraryViewModel> AlbumViewModels
        {
            get { return _albumViewModels; }
        }

        public ObservableCollection<LibraryViewModel> SimpleViewModels
        {
            get { return _simpleArtistViewModels; }
        }

        #endregion ArtistViewModels

        #region SearchCommand

        private SearchChoices _searchChoice = SearchChoices.All;
        public SearchChoices SearchChoice
        {
            get
            {
                return _searchChoice;
            }
            set
            {
                if (_searchChoice != value)
                {
                    _searchChoice = value;
                    //Clear the ongoing search
                    _matchingRecordEnumerator = null;

                    OnPropertyChanged("SearchChoice");
                }
            }
        }

        private ViewMode _currentViewMode = ViewMode.MAX;
        public ViewMode CurrentViewMode
        {
            get { return _currentViewMode; }
            set
            {
                if (_currentViewMode != value)
                {
                    _currentViewMode = value;

                    //Clear the ongoing search
                    _matchingRecordEnumerator = null;

                    CheckIsLoaded(_currentViewMode);

                    switch (_currentViewMode)
                    {
                        case ViewMode.Classic:
                            {
                            }
                            break;
                        case ViewMode.Simple:
                            //Album search is invalid in Simple Mode
                            if (SearchChoice == SearchChoices.Album)
                            {
                                SearchChoice = SearchChoices.All;
                            }
                            break;
                        case ViewMode.Album:
                            //Artist search is invalid in Album mode
                            if (SearchChoice == SearchChoices.Artist)
                            {
                                SearchChoice = SearchChoices.All;
                            }
                            break;
                        case ViewMode.MAX:
                        default:
                            throw new Exception("Unexpected ViewMode: " + _currentViewMode);
                    }
                }
            }
        }

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
                // I intentionally left these empty because this command never raises the event, and
                // not using the WeakEvent pattern here can cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                _musicTree.PerformSearch();
            }
        }

        #endregion SearchCommand
        #region SearchText

        /// <summary>
        /// Gets/sets a fragment of the name to search for.
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;

                    //Clear ongoing search
                    _matchingRecordEnumerator = null;
                }
            }
        }

        #endregion SearchText
        #endregion Properties
        #region Search Logic

        void PerformSearch()
        {
            if (_matchingRecordEnumerator == null || !_matchingRecordEnumerator.MoveNext())
            {
                VerifyMatches();
            }

            LibraryViewModel model = _matchingRecordEnumerator.Current;

            if (model == null)
            {
                return;
            }

            model.IsSelected = true;
        }

        void VerifyMatches()
        {
            var matches = FindMatches(_searchText);
            _matchingRecordEnumerator = matches.GetEnumerator();

            if (!_matchingRecordEnumerator.MoveNext())
            {
                MessageBox.Show(
                    "No matching records were found.",
                    "Try Again",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
            }
        }

        ICollection<LibraryViewModel> ActiveModelCollection
        {
            get
            {
                switch (CurrentViewMode)
                {
                    case ViewMode.Classic:
                        return ClassicArtistViewModels;
                    case ViewMode.Simple:
                        return SimpleViewModels;
                    case ViewMode.Album:
                        return AlbumViewModels;
                    case ViewMode.MAX:
                    default:
                        throw new Exception("Unexpected ViewMode: " + CurrentViewMode);
                }
            }
        }

        IEnumerable<LibraryViewModel> FindMatches(string searchText)
        {
            foreach (LibraryViewModel match in FindMatches(searchText, ActiveModelCollection))
            {
                yield return match;
            }
        }

        IEnumerable<LibraryViewModel> FindMatches(string searchText, ICollection<LibraryViewModel> models)
        {
            bool initialized = false;

            bool searchTier = false;
            bool searchChildren = false;

            foreach (LibraryViewModel model in models)
            {
                if (!initialized)
                {
                    initialized = true;
                    searchTier = SearchTypeMatch(model);
                    searchChildren = SearchChildren(model);
                }

                if (searchTier && model.NameContainsText(searchText))
                {
                    yield return model;
                }

                if (searchChildren)
                {
                    //Can't lazy-load children if we need to search through them
                    if (model.HasDummyChild)
                    {
                        model.LoadChildren(requestHandler);
                    }

                    foreach (LibraryViewModel match in FindMatches(searchText, model.Children))
                    {
                        yield return match;
                    }
                }
            }
        }

        bool SearchTypeMatch(LibraryViewModel model)
        {
            if (SearchChoice == SearchChoices.All)
            {
                return true;
            }

            if (model is ArtistViewModel)
            {
                return SearchChoice == SearchChoices.Artist;
            }

            if (model is AlbumViewModel)
            {
                return SearchChoice == SearchChoices.Album;
            }

            if (model is SongViewModel)
            {
                return SearchChoice == SearchChoices.Song;
            }

            if (model is RecordingViewModel)
            {
                throw new Exception("Unexpected: RecordingViewModel.");
            }

            throw new Exception("Unsupported LibraryViewModel: " + model.GetType().ToString());
        }

        bool SearchChildren(LibraryViewModel model)
        {
            if (model is SongViewModel)
            {
                //Nothing requires searching past songs
                return false;
            }

            if (SearchChoice == SearchChoices.All)
            {
                //Always search deeper if All was selected (and we're not at Song yet, handled above)
                return true;
            }

            //Otherwise, any tier that matches is the final tier
            return !SearchTypeMatch(model);
        }

        #endregion Search Logic
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

    }
}
