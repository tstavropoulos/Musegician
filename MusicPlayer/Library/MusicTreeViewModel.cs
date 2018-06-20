using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Musegician.DataStructures;
using Musegician.Database;
using System.ComponentModel;

namespace Musegician.Library
{
    public class MusicTreeViewModel : INotifyPropertyChanged
    {
        #region Data

        IEnumerator<LibraryViewModel> _matchingRecordEnumerator;
        string _searchText = String.Empty;

        bool classicLoaded = false;
        bool simpleLoaded = false;
        bool albumLoaded = false;
        bool directoryLoaded = false;

        Playlist.LookupEventArgs lastLookupArgs = null;

        #endregion Data
        #region Engine References

        private readonly ILibraryRequestHandler requestHandler;

        #endregion Engine References
        #region Constructor

        public MusicTreeViewModel() { }

        public MusicTreeViewModel(ILibraryRequestHandler requestHandler)
            : this()
        {
            this.requestHandler = requestHandler;

            SearchCommand = new SearchMusicTreeCommand(this);
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
                        ClassicArtistViewModels.Clear();

                        foreach (Artist artist in requestHandler.GenerateArtistList())
                        {
                            ClassicArtistViewModels.Add(new ArtistViewModel(artist, ViewMode.Classic));
                        }
                    }
                    break;
                case ViewMode.Simple:
                    if (!simpleLoaded)
                    {
                        simpleLoaded = true;
                        SimpleViewModels.Clear();

                        foreach (Artist artist in requestHandler.GenerateArtistList())
                        {
                            SimpleViewModels.Add(new ArtistViewModel(artist, ViewMode.Simple));
                        }

                    }
                    break;
                case ViewMode.Album:
                    if (!albumLoaded)
                    {
                        albumLoaded = true;
                        AlbumViewModels.Clear();

                        foreach (Album album in requestHandler.GenerateAlbumList())
                        {
                            AlbumViewModels.Add(new AlbumViewModel(album, null));
                        }

                    }
                    break;
                case ViewMode.Directory:
                    if (!directoryLoaded)
                    {
                        directoryLoaded = true;
                        DirectoryViewModels.Clear();

                        foreach (DirectoryDTO directory in requestHandler.GetDirectories(""))
                        {
                            DirectoryViewModels.Add(new DirectoryViewModel(directory, null));
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
        public ObservableCollection<LibraryViewModel> ClassicArtistViewModels { get; } = new ObservableCollection<LibraryViewModel>();
        public ObservableCollection<LibraryViewModel> AlbumViewModels { get; } = new ObservableCollection<LibraryViewModel>();
        public ObservableCollection<LibraryViewModel> SimpleViewModels { get; } = new ObservableCollection<LibraryViewModel>();
        public ObservableCollection<LibraryViewModel> DirectoryViewModels { get; } = new ObservableCollection<LibraryViewModel>();

        #endregion ArtistViewModels
        #region SearchCommand

        private SearchChoices _searchChoice = SearchChoices.All;
        public SearchChoices SearchChoice
        {
            get => _searchChoice;
            set
            {
                if (_searchChoice != value)
                {
                    _searchChoice = value;
                    //Clear the ongoing search
                    _matchingRecordEnumerator = null;
                    lastLookupArgs = null;

                    OnPropertyChanged("SearchChoice");
                }
            }
        }

        private ViewMode _currentViewMode = ViewMode.MAX;
        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
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
                        case ViewMode.Directory:
                            SearchChoice = SearchChoices.All;
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
        public ICommand SearchCommand { get; } = new SearchMusicTreeCommand(null);

        private class SearchMusicTreeCommand : ICommand
        {
            readonly MusicTreeViewModel _musicTree;

            public SearchMusicTreeCommand(MusicTreeViewModel musicTree)
            {
                _musicTree = musicTree;
            }

            public bool CanExecute(object parameter) => true;

            event EventHandler ICommand.CanExecuteChanged
            {
                // I intentionally left these empty because this command never raises the event, and
                // not using the WeakEvent pattern here can cause memory leaks.  WeakEvent pattern is
                // not simple to implement, so why bother.
                add { }
                remove { }
            }

            public void Execute(object parameter) => _musicTree.PerformSearch();
        }

        #endregion SearchCommand
        #region SearchText

        /// <summary>
        /// Gets/sets a fragment of the name to search for.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;

                    //Clear ongoing search
                    _matchingRecordEnumerator = null;
                    lastLookupArgs = null;
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
            IEnumerable<LibraryViewModel> matches = null;
            if (lastLookupArgs != null)
            {
                matches = FindIDMatches(lastLookupArgs);
            }
            else
            {
                matches = FindTextMatches(_searchText);
            }

            _matchingRecordEnumerator = matches.GetEnumerator();

            if (!_matchingRecordEnumerator.MoveNext())
            {
                MessageBox.Show(
                    messageBoxText: "No matching records were found.",
                    caption: "Try Again",
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Information);
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
                    case ViewMode.Directory:
                        return DirectoryViewModels;
                    case ViewMode.MAX:
                    default:
                        throw new Exception("Unexpected ViewMode: " + CurrentViewMode);
                }
            }
        }

        IEnumerable<LibraryViewModel> FindTextMatches(string searchText)
        {
            foreach (LibraryViewModel match in FindTextMatches(searchText, ActiveModelCollection))
            {
                yield return match;
            }
        }

        IEnumerable<LibraryViewModel> FindTextMatches(string searchText, ICollection<LibraryViewModel> models)
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

                    foreach (LibraryViewModel match in FindTextMatches(searchText, model.Children))
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

            if (model is DirectoryViewModel)
            {
                throw new Exception("Unexpected: DirectoryViewModel.");
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
        #region Search ID Logic

        public void PerformLookup(Playlist.LookupEventArgs e)
        {
            SearchText = "{Playlist Item Search}";
            _matchingRecordEnumerator = null;
            lastLookupArgs = e;
            PerformSearch();
        }

        IEnumerable<LibraryViewModel> FindIDMatches(Playlist.LookupEventArgs e)
        {
            foreach (LibraryViewModel match in FindIDMatches(e, ActiveModelCollection))
            {
                yield return match;
            }
        }

        IEnumerable<LibraryViewModel> FindIDMatches(
            Playlist.LookupEventArgs e,
            ICollection<LibraryViewModel> models)
        {
            LibraryContext context = LibraryContext.MAX;

            if (e.data is Song)
            {
                context = LibraryContext.Song;
            }
            else if (e.data is Recording)
            {
                context = LibraryContext.Recording;
            }

            foreach (LibraryViewModel model in models)
            {
                switch (context)
                {
                    case LibraryContext.Song:
                        if (model is SongViewModel song && song.Data == e.data)
                        {
                            yield return song;
                        }
                        break;
                    case LibraryContext.Recording:
                        if (model is RecordingViewModel recording && recording.Data == e.data)
                        {
                            yield return recording;
                        }
                        break;
                    case LibraryContext.Artist:
                    case LibraryContext.Album:
                    case LibraryContext.Track:
                    case LibraryContext.MAX:
                    default:
                        throw new ArgumentException("Unexpected LibraryContext: " + context);
                }

                //Can't lazy-load children if we need to search through them
                if (model.HasDummyChild)
                {
                    model.LoadChildren(requestHandler);
                }

                foreach (LibraryViewModel match in FindIDMatches(e, model.Children))
                {
                    yield return match;
                }
            }
        }

        #endregion
        #region Selection

        public void RestoreHierarchy(ViewMode mode, Stack<long> ids)
        {
            if (ids.Count == 0)
            {
                //Nothing to restore
                return;
            }

            ObservableCollection<LibraryViewModel> models = null;

            switch (mode)
            {
                case ViewMode.Classic:
                    models = ClassicArtistViewModels;
                    break;
                case ViewMode.Simple:
                    models = SimpleViewModels;
                    break;
                case ViewMode.Album:
                    models = AlbumViewModels;
                    break;
                case ViewMode.Directory:
                    models = DirectoryViewModels;
                    break;
                case ViewMode.MAX:
                default:
                    Console.WriteLine($"Invalid ViewMode: {mode}");
                    return;
            }

            LibraryViewModel model = null;
            do
            {
                //Travel down hierarchy selecting, expanding, and loading
                long nextId = ids.Pop();
                
                LibraryViewModel nextmodel = models.Where(x => x.Data.Id == nextId).FirstOrDefault();
                if (nextmodel != null)
                {
                    //Expand parent
                    if (model != null)
                    {
                        model.IsExpanded = true;
                    }
                    //Move pointer to child
                    model = nextmodel;
                }
                else
                {
                    //Abort when the next isn't found
                    break;
                }

                //Load the children so they can be searched
                model.LoadChildren(requestHandler);
                //Update list pointer to children
                models = model.Children;
            }
            while (model != null && ids.Count > 0);

            //Select the last model we were able to match
            if (model != null)
            {
                model.IsSelected = true;
            }
        }

        #endregion Selection
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
    }
}
