using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Musegician.DataStructures;
using Musegician.Core;
using IPlaylistTransferRequestHandler = Musegician.Playlist.IPlaylistTransferRequestHandler;
using System.Windows;

namespace Musegician.Library
{
    #region Library Enums

    public enum LibraryContext
    {
        Artist = 0,
        Album,
        /// <summary>
        /// For a context-less song, like from the Playlist
        /// </summary>
        Song,
        /// <summary>
        /// For a context-full song, like from the Library
        /// </summary>
        Track,
        Recording,
        MAX
    }

    public enum MenuAction
    {
        Play = 0,
        Add,
        Edit
    }

    public enum SearchChoices
    {
        All = 0,
        Artist,
        Album,
        Song,
        MAX
    }

    public enum ViewMode
    {
        Classic = 0,
        Simple,
        Album,
        MAX
    }

    #endregion Library Enums

    public partial class LibraryControl : UserControl
    {
        #region Data

        MusicTreeViewModel _musicTree;

        ILibraryRequestHandler RequestHandler
        {
            get
            {
                return FileManager.Instance;
            }
        }

        IPlaylistTransferRequestHandler PlaylistTransferRequestHandler
        {
            get
            {
                return FileManager.Instance;
            }
        }

        #endregion Data
        #region Inner Enumerations

        private enum KeyboardActions
        {
            None = 0,
            WeightUp,
            WeightDown,
            Play,
            MAX
        }

        #endregion Inner Enumerations
        #region Context Menu Events

        public delegate void ContextMenuIDRequest(LibraryContext context, long id);
        public delegate void ContextMenuMultiIDRequest(LibraryContext context, IList<long> ids);

        public event ContextMenuMultiIDRequest ContextMenu_MultiEdit;
        public event ContextMenuIDRequest ContextMenu_EditArt;

        #endregion Context Menu Events
        #region Properties

        private MultiSelectTreeView CurrentTreeView
        {
            get
            {
                switch (_musicTree.CurrentViewMode)
                {
                    case ViewMode.Classic:
                    case ViewMode.Simple:
                    case ViewMode.Album:
                        return GetTreeView(_musicTree.CurrentViewMode);
                    default:
                        throw new Exception("Unexpected ViewMode: " + _musicTree.CurrentViewMode);
                }
            }
        }

        #endregion Properties
        #region Construction

        public LibraryControl()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                _musicTree = new MusicTreeViewModel();
            }
            else
            {
                _musicTree = new MusicTreeViewModel(RequestHandler);
            }

            DataContext = _musicTree;
        }

        public void Rebuild()
        {
            ViewMode tempViewMode = _musicTree.CurrentViewMode;

            _musicTree = new MusicTreeViewModel(RequestHandler);
            DataContext = _musicTree;

            if (tempViewMode != ViewMode.MAX)
            {
                //Trigger the loading of the current view mode
                _musicTree.CurrentViewMode = tempViewMode;
            }
        }

        #endregion Construction
        #region Class Methods

        private void Play(LibraryContext context, IList<long> ids, bool deep)
        {
            List<SongDTO> songs = new List<SongDTO>();

            foreach (long id in ids)
            {
                switch (context)
                {
                    case LibraryContext.Artist:
                        {
                            songs.AddRange(
                                PlaylistTransferRequestHandler.GetArtistData(
                                    artistID: id,
                                    deep: deep));
                        }
                        break;
                    case LibraryContext.Album:
                        {
                            songs.AddRange(
                                PlaylistTransferRequestHandler.GetAlbumData(
                                albumID: id,
                                deep: deep));
                        }
                        break;
                    case LibraryContext.Song:
                        {
                            songs.AddRange(
                                PlaylistTransferRequestHandler.GetSongData(id));
                        }
                        break;
                    case LibraryContext.Track:
                        {
                            throw new NotImplementedException();
                        }
                    case LibraryContext.Recording:
                        {
                            songs.AddRange(
                                PlaylistTransferRequestHandler.GetSongDataFromRecordingID(id));
                        }
                        break;
                    case LibraryContext.MAX:
                    default:
                        Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                        return;
                }
            }

            if (ids.Count == 1)
            {
                Playlist.PlaylistManager.Instance.PlaylistName =
                    PlaylistTransferRequestHandler.GetDefaultPlaylistName(
                        context: context,
                        id: ids[0]);
            }
            else
            {
                Playlist.PlaylistManager.Instance.PlaylistName = "Assorted";
            }

            Playlist.PlaylistManager.Instance.Rebuild(songs);
            Player.MusicManager.Instance.Next();
        }

        private void Add(LibraryContext context, IEnumerable<long> ids, bool deep)
        {
            ICollection<SongDTO> songs;

            foreach (long id in ids)
            {
                switch (context)
                {
                    case LibraryContext.Artist:
                        {
                            songs = PlaylistTransferRequestHandler.GetArtistData(
                                artistID: id,
                                deep: deep);
                        }
                        break;
                    case LibraryContext.Album:
                        {
                            songs = PlaylistTransferRequestHandler.GetAlbumData(
                                albumID: id,
                                deep: deep);
                        }
                        break;
                    case LibraryContext.Song:
                        {
                            songs = PlaylistTransferRequestHandler.GetSongData(id);
                        }
                        break;
                    case LibraryContext.Track:
                        {
                            throw new NotImplementedException();
                        }
                    case LibraryContext.Recording:
                        {
                            songs = PlaylistTransferRequestHandler.GetSongDataFromRecordingID(id);
                        }
                        break;
                    case LibraryContext.MAX:
                    default:
                        Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                        return;
                }

                Playlist.PlaylistManager.Instance.AddBack(songs);
            }
        }

        #endregion Class Methods
        #region Callbacks
        #region Callbacks Search Execution

        void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _musicTree.SearchCommand.Execute(null);
            }
        }

        #endregion Callbacks Search Execution
        #region Mouse Callbacks

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is MultiSelectTreeViewItem treeItem)
            {
                if (treeItem.Header is SongViewModel songModel)
                {
                    if (!songModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    Play(LibraryContext.Song, new long[] { songModel.ID }, false);
                }
                else if (treeItem.Header is RecordingViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    Play(LibraryContext.Recording, new long[] { recordingModel.ID }, false);
                }
            }
        }

        #endregion Mouse Callbacks
        #region Context Menu Callbacks

        private void Play_Deep(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Play);

            if (ids.Count() > 0)
            {
                e.Handled = true;
                Play(context, ids, true);
            }
        }

        private void Play_Shallow(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Play);

            if (ids.Count() > 0)
            {
                e.Handled = true;
                Play(context, ids, false);
            }
        }

        private void Add_Deep(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Add);

            if (ids.Count() > 0)
            {
                e.Handled = true;
                Add(context, ids, true);
            }
        }

        private void Add_Shallow(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Add);

            if (ids.Count() > 0)
            {
                e.Handled = true;
                Add(context, ids, false);
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Edit);

            if (ids.Count() > 0)
            {
                e.Handled = true;

                ContextMenu_MultiEdit?.Invoke(context, ids);
            }
        }

        private void EditArt(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Edit);

            if (ids.Count() != 1)
            {
                MessageBox.Show("Must select exactly one album to edit Album Art", "Selection Error");
                return;
            }

            e.Handled = true;

            ContextMenu_EditArt?.Invoke(context, ids[0]);
        }

        #endregion Context Menu Callbacks
        #region View Callbacks

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MultiSelectTreeViewItem treeItem)
            {
                if (treeItem.Header is LibraryViewModel libraryModel)
                {
                    if (!libraryModel.HasDummyChild)
                    {
                        return;
                    }

                    libraryModel.LoadChildren(FileManager.Instance);
                }
            }
        }

        private void LibraryView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((e.Source is TabControl libraryView) && (libraryView.SelectedItem is TabItem tabItem))
            {
                e.Handled = true;

                switch (tabItem.Header)
                {
                    case "Classic":
                        _musicTree.CurrentViewMode = ViewMode.Classic;
                        break;
                    case "Simple":
                        _musicTree.CurrentViewMode = ViewMode.Simple;
                        break;
                    case "Album":
                        _musicTree.CurrentViewMode = ViewMode.Album;
                        break;
                    default:
                        throw new Exception("Unexpected tabItem.Header: " + tabItem.Header);
                }

                //Disable ArtistSearch for Album view
                radioSearchArtist.IsEnabled = (_musicTree.CurrentViewMode != ViewMode.Album);

                //Disable albumsearch for Simple view
                radioSearchAlbum.IsEnabled = (_musicTree.CurrentViewMode != ViewMode.Simple);
            }
        }

        #endregion View Callbacks
        #region Keyboard Callbacks

        private void Tree_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is MultiSelectTreeView)
            {
                KeyboardActions action = TranslateKey(e.Key);

                if (action == KeyboardActions.None)
                {
                    //Do nothing
                    return;
                }

                e.Handled = true;

                (LibraryContext context, List<(long id, double weight)> values) =
                    ExtractContextIDAndWeights();

                for (int i = 0; i < values.Count; i++)
                {
                    (long id, double weight) = values[i];

                    if (id == -1)
                    {
                        throw new Exception("Unexpected id = -1!");
                    }

                    switch (action)
                    {
                        case KeyboardActions.WeightUp:
                            weight = Math.Min(weight + 0.05, 1.0);
                            break;
                        case KeyboardActions.WeightDown:
                            weight = Math.Max(weight - 0.05, 0.0);
                            break;
                        case KeyboardActions.Play:
                            //Play thing

                            return;
                        case KeyboardActions.None:
                        case KeyboardActions.MAX:
                        default:
                            throw new Exception("Unexpected KeyboardAction: " + action);
                    }

                    values[i] = (id, weight);
                }

                RequestHandler.UpdateWeights(context, values);
                UpdateWeights(values);
            }
        }

        #endregion Keyboard Callbacks
        #endregion Callbacks
        #region Helper Fuctions

        private (LibraryContext, List<long>) ExtractContextAndIDs(
            MenuAction option,
            ViewMode overrideMode = ViewMode.MAX)
        {
            IEnumerable<LibraryViewModel> selectedItems =
                GetSelectedItems(overrideMode).OfType<LibraryViewModel>();

            LibraryContext context = LibraryContext.MAX;
            List<long> ids = new List<long>();

            if (selectedItems.Count() > 0)
            {
                LibraryViewModel firstSelectedItem = selectedItems.First();

                if (firstSelectedItem is ArtistViewModel artist)
                {
                    context = LibraryContext.Artist;
                }
                else if (firstSelectedItem is AlbumViewModel album)
                {
                    context = LibraryContext.Album;
                }
                else if (firstSelectedItem is SongViewModel song)
                {
                    switch (option)
                    {
                        case MenuAction.Play:
                        case MenuAction.Add:
                            context = LibraryContext.Song;
                            break;
                        case MenuAction.Edit:
                            context = LibraryContext.Track;
                            break;
                        default:
                            throw new ArgumentException("Unexpected MenuAction: " + option);
                    }
                }
                else if (firstSelectedItem is RecordingViewModel recording)
                {
                    context = LibraryContext.Recording;
                }

                foreach (LibraryViewModel model in selectedItems)
                {
                    //Only on Edit, we return the ContextualTrackID
                    if (option == MenuAction.Edit && model is SongViewModel song)
                    {
                        ids.Add((song as SongViewModel).ContextualTrackID);
                    }
                    else
                    {
                        ids.Add(model.ID);
                    }
                }
            }

            return (context, ids);
        }

        private (LibraryContext, List<(long, double)>) ExtractContextIDAndWeights(
            ViewMode overrideMode = ViewMode.MAX)
        {
            IList selectedItems = GetSelectedItems(overrideMode);

            LibraryContext context = LibraryContext.MAX;

            List<(long, double)> weightsList = new List<(long, double)>();

            if (selectedItems.Count > 0)
            {
                object firstSelectedItem = selectedItems[0];

                if (firstSelectedItem is ArtistViewModel artist)
                {
                    context = LibraryContext.Artist;
                }
                else if (firstSelectedItem is AlbumViewModel album)
                {
                    context = LibraryContext.Album;
                }
                else if (firstSelectedItem is SongViewModel song)
                {
                    context = LibraryContext.Song;
                }
                else if (firstSelectedItem is RecordingViewModel recording)
                {
                    context = LibraryContext.Track;
                }

                foreach (LibraryViewModel model in selectedItems)
                {
                    if (model is RecordingViewModel recording)
                    {
                        weightsList.Add(
                            ((recording.Parent as SongViewModel).ContextualTrackID, model.Weight));
                    }
                    else
                    {
                        weightsList.Add((model.ID, model.Weight));
                    }
                }
            }

            return (context, weightsList);
        }

        private void UpdateWeights(
            IList<(long id, double weight)> values,
            ViewMode overrideMode = ViewMode.MAX)
        {
            IEnumerable<LibraryViewModel> selectedItems =
                GetSelectedItems(overrideMode).OfType<LibraryViewModel>();

            int i = 0;
            foreach (LibraryViewModel model in selectedItems)
            {
                //Lets find out if this is sufficient...
                if (model.ID != values[i].id)
                {
                    throw new Exception("selectedItem doesn't match up!");
                }

                model.Weight = values[i].weight;

                i++;
            }
        }

        /// <summary>
        /// Returns an IList of the currently selected items
        /// ViewMode.MAX means the CurrentTreeView.SelectedItems
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private IList GetSelectedItems(ViewMode mode)
        {
            return GetTreeView(mode).SelectedItems;
        }

        /// <summary>
        /// Returns the requested TreeView,
        /// ViewMode.MAX means the CurrentTreeView
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private MultiSelectTreeView GetTreeView(ViewMode mode)
        {
            switch (mode)
            {
                case ViewMode.Classic:
                    return ClassicTreeView;
                case ViewMode.Simple:
                    return SimpleTreeView;
                case ViewMode.Album:
                    return AlbumTreeView;
                case ViewMode.MAX:
                    return CurrentTreeView;
                default:
                    throw new Exception("Unexpected ViewMode: " + mode);
            }
        }

        private KeyboardActions TranslateKey(Key key)
        {
            switch (key)
            {
                case Key.Return:
                    return KeyboardActions.Play;
                case Key.Add:
                case Key.OemPlus:
                    return KeyboardActions.WeightUp;
                case Key.Subtract:
                case Key.OemMinus:
                    return KeyboardActions.WeightDown;
                default:
                    return KeyboardActions.None;
            }
        }

        #endregion Helper Fuctions
    }
}
