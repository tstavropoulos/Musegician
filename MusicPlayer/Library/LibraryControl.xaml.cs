using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using Musegician.Core;
using Musegician.Database;
using Musegician.DataStructures;
using IPlaylistTransferRequestHandler = Musegician.Playlist.IPlaylistTransferRequestHandler;

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
        Edit,
        Lyrics,
        Tags
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
        Directory,
        MAX
    }

    #endregion Library Enums
    #region Drag Data

    public class LibraryDragData
    {
        public delegate void AddCallback(int position);
        public AddCallback callback;
        public LibraryDragData(AddCallback callback)
        {
            this.callback = callback;
        }
    }

    #endregion Drag Data

    public partial class LibraryControl : UserControl
    {
        #region Data

        MusicTreeViewModel _musicTree;

        ILibraryRequestHandler LibraryRequestHandler => FileManager.Instance;
        IPlaylistTransferRequestHandler PlaylistTransferRequestHandler => FileManager.Instance;

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

        public delegate void ContextMenuIDRequest(Album album);
        public delegate void ContextMenuMultiIDRequest(IEnumerable<BaseData> data);

        public event ContextMenuMultiIDRequest ContextMenu_MultiPushTags;
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
                    case ViewMode.Directory:
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
                Loaded += LibraryControl_Loaded;
                Unloaded += LibraryControl_Unloaded;

                _musicTree = new MusicTreeViewModel(LibraryRequestHandler);
            }

            DataContext = _musicTree;
        }

        public void Rebuild()
        {
            ViewMode tempViewMode = _musicTree.CurrentViewMode;
            Stack<long> idStack = new Stack<long>();
            if (tempViewMode != ViewMode.MAX && CurrentTreeView.SelectedItem is LibraryViewModel model)
            {
                while (model != null)
                {
                    idStack.Push(model.Data.Id);
                    model = model.Parent;
                }
            }

            _musicTree = new MusicTreeViewModel(LibraryRequestHandler);
            DataContext = _musicTree;

            if (tempViewMode != ViewMode.MAX)
            {
                //Trigger the loading of the current view mode
                _musicTree.CurrentViewMode = tempViewMode;

                _musicTree.RestoreHierarchy(tempViewMode, idStack);
            }
        }

        #endregion Construction
        #region Class Methods

        private void Play(IEnumerable<BaseData> data, bool deep)
        {
            Playlist.PlaylistManager.Instance.ClearPlaylist();

            List<PlaylistSong> songs = new List<PlaylistSong>();

            foreach (BaseData datum in data)
            {
                if (datum is Artist artist)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetArtistData(
                        artist: artist,
                        deep: deep));
                }
                else if (datum is Album album)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetAlbumData(
                        album: album,
                        deep: deep));
                }
                else if (datum is Song song)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(song));
                }
                else if (datum is Track track)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(track.Recording));
                }
                else if (datum is Recording recording)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(recording));
                }
                else
                {
                    Console.WriteLine($"Unexpected LibraryContext: {datum}.  Likey error.");
                    return;
                }
            }

            if (data.Count() == 1)
            {
                Playlist.PlaylistManager.Instance.PlaylistName =
                    PlaylistTransferRequestHandler.GetDefaultPlaylistName(
                        data: data.First());
            }
            else
            {
                Playlist.PlaylistManager.Instance.PlaylistName = "Assorted";
            }

            Playlist.PlaylistManager.Instance.Rebuild(songs);
            Player.MusicManager.Instance.Next();
        }

        private void Add(IEnumerable<BaseData> data, bool deep, int position = -1)
        {
            List<PlaylistSong> songs = new List<PlaylistSong>();

            foreach (BaseData datum in data)
            {
                if (datum is Artist artist)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetArtistData(
                        artist: artist,
                        deep: deep));
                }
                else if (datum is Album album)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetAlbumData(
                        album: album,
                        deep: deep));
                }
                else if (datum is Song song)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(song));
                }
                else if (datum is Track track)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(track.Recording));
                }
                else if (datum is Recording recording)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(recording));
                }
                else
                {
                    Console.WriteLine($"Unexpected LibraryContext: {datum}.  Likey error.");
                    return;
                }
            }

            if (position == -1)
            {
                Playlist.PlaylistManager.Instance.AddBack(songs);
            }
            else
            {
                Playlist.PlaylistManager.Instance.InsertSongs(position, songs);
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
                    Play(new BaseData[] { songModel.Data }, false);
                }
                else if (treeItem.Header is RecordingViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    Play(new BaseData[] { recordingModel.Data }, false);
                }
            }
        }

        #endregion Mouse Callbacks
        #region Context Menu Callbacks

        private void Play_Deep(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Play);

            if (data.Count() > 0)
            {
                e.Handled = true;
                Play(data, true);
            }
        }

        private void Play_Shallow(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Play);

            if (data.Count() > 0)
            {
                e.Handled = true;
                Play(data, false);
            }
        }

        private void Add_Deep(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Add);

            if (data.Count() > 0)
            {
                e.Handled = true;
                Add(data, true);
            }
        }

        private void Add_Shallow(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Add);

            if (data.Count() > 0)
            {
                e.Handled = true;
                Add(data, false);
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Edit);

            if (data.Count() > 0)
            {
                e.Handled = true;

                ContextMenu_MultiEdit?.Invoke(data);
            }
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Edit);

            if (data.Count() > 0)
            {
                e.Handled = true;

                LibraryRequestHandler.Delete(data.Select(x => x as Recording));

                Rebuild();
            }
        }

        private void OpenLyrics(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            BaseData data = ExtractContextAndData(MenuAction.Lyrics).FirstOrDefault();

            if (data is Recording recording)
            {
                //Handle
                Window viewer = new LyricViewer.LyricViewer(recording);
                viewer.Show();
            }
            else
            {
                Console.WriteLine($"Unexpected LibraryContext: {data}.  Likey error.");
            }
        }

        private void EditArt(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Edit);

            if (data.Count() != 1)
            {
                MessageBox.Show("Must select exactly one album to edit Album Art", "Selection Error");
                return;
            }

            e.Handled = true;

            ContextMenu_EditArt?.Invoke(data.First() as Album);
        }

        private void Explore(object sender, RoutedEventArgs e)
        {
            IEnumerable<LibraryViewModel> selectedItems =
                GetSelectedItems(ViewMode.MAX).OfType<LibraryViewModel>();

            foreach (LibraryViewModel model in selectedItems)
            {
                if (model is RecordingViewModel recording)
                {
                    System.Diagnostics.Process.Start(
                        fileName: "explorer",
                        arguments: $"/select, \"{recording._recording.Filename}\"");
                }
                else if (model is DirectoryViewModel directory)
                {
                    System.Diagnostics.Process.Start(
                        fileName: "explorer",
                        arguments: directory.Path);
                }
                else
                {
                    throw new Exception("Unexpected LibraryViewModel: " + model.GetType());
                }
            }
        }

        private void PushTags(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndData(MenuAction.Tags);

            if (data.Count() > 0)
            {
                e.Handled = true;

                ContextMenu_MultiPushTags?.Invoke(data);
            }
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
                    case "Directories":
                        _musicTree.CurrentViewMode = ViewMode.Directory;
                        break;
                    default:
                        throw new Exception("Unexpected tabItem.Header: " + tabItem.Header);
                }

                //Disable ArtistSearch for Album view
                radioSearchArtist.IsEnabled =
                    (_musicTree.CurrentViewMode != ViewMode.Album &&
                    _musicTree.CurrentViewMode != ViewMode.Directory);

                //Disable albumsearch for Simple view
                radioSearchAlbum.IsEnabled =
                    (_musicTree.CurrentViewMode != ViewMode.Simple &&
                    _musicTree.CurrentViewMode != ViewMode.Directory);

                //Disable song search for Directory
                radioSearchSong.IsEnabled = _musicTree.CurrentViewMode != ViewMode.Directory;
            }
        }

        private void LibraryRequestHandler_Rebuild(object sender, EventArgs e) => Rebuild();

        private void LibraryControl_Loaded(object sender, RoutedEventArgs e) =>
            LibraryRequestHandler.RebuildNotifier += LibraryRequestHandler_Rebuild;

        private void LibraryControl_Unloaded(object sender, RoutedEventArgs e) =>
            LibraryRequestHandler.RebuildNotifier -= LibraryRequestHandler_Rebuild;

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


                foreach (LibraryViewModel model in GetSelectedItems(ViewMode.MAX))
                {
                    switch (action)
                    {
                        case KeyboardActions.WeightUp:
                            model.Weight = Math.Min(model.Weight + 0.05, 1.0);
                            break;
                        case KeyboardActions.WeightDown:
                            model.Weight = Math.Max(model.Weight - 0.05, 0.0);
                            break;
                        case KeyboardActions.Play:
                            //play thing
                            return;
                        case KeyboardActions.None:
                        case KeyboardActions.MAX:
                        default:
                            throw new Exception("Unexpected KeyboardAction: " + action);
                    }
                }

                LibraryRequestHandler.DatabaseUpdated();
            }
        }

        #endregion Keyboard Callbacks
        #region External Callbacks

        public void LookupRequest(object sender, Playlist.LookupEventArgs e)
        {
            _musicTree.PerformLookup(e);
        }

        #endregion External Callbacks
        #region Drag Callbacks

        private bool _validForDrag = false;

        private void Item_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is MultiSelectTreeViewItem dragSource &&
                IsDraggable(dragSource) && dragSource.IsItemSelected)
            {
                _validForDrag = true;
            }
        }

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            _validForDrag = false;
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _validForDrag &&
                sender is MultiSelectTreeViewItem dragSource &&
                IsDraggable(dragSource) && dragSource.IsItemSelected)
            {
                DragDrop.DoDragDrop(
                    dragSource: dragSource,
                    data: new LibraryDragData(AddSelectedItems),
                    allowedEffects: DragDropEffects.Link);
            }
        }

        private void AddSelectedItems(int position)
        {
            var data = ExtractContextAndData(MenuAction.Add);

            if (data.Count() > 0)
            {
                Add(data, false, position);
            }
        }

        private bool IsDraggable(MultiSelectTreeViewItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.Header is SongViewModel)
            {
                return true;
            }

            if (item.Header is ArtistViewModel)
            {
                return true;
            }

            if (item.Header is AlbumViewModel)
            {
                return true;
            }

            if (item.Header is RecordingViewModel)
            {
                return true;
            }

            return false;
        }

        private T FindVisualParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
            {
                return null;
            }

            if (parentObject is T parent)
            {
                return parent;
            }

            return FindVisualParent<T>(parentObject);
        }

        #endregion DragCallbacks
        #endregion Callbacks
        #region Helper Fuctions

        private IEnumerable<BaseData> ExtractContextAndData(
            MenuAction option,
            ViewMode overrideMode = ViewMode.MAX)
        {
            IEnumerable<LibraryViewModel> selectedItems =
                GetSelectedItems(overrideMode).OfType<LibraryViewModel>();

            LibraryContext context = LibraryContext.MAX;

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
                        case MenuAction.Lyrics:
                        case MenuAction.Tags:
                        case MenuAction.Edit:
                            if (song.Track != null)
                            {
                                context = LibraryContext.Track;
                            }
                            else
                            {
                                context = LibraryContext.Song;
                            }
                            break;
                        default:
                            throw new ArgumentException("Unexpected MenuAction: " + option);
                    }
                }
                else if (firstSelectedItem is RecordingViewModel recording)
                {
                    context = LibraryContext.Recording;
                }

                if (context == LibraryContext.Track)
                {
                    return selectedItems.Select(x => (x as SongViewModel).Track);
                }
                else
                {
                    return selectedItems.Select(x => x.Data);
                }
            }

            return new List<BaseData>();
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
                case ViewMode.Directory:
                    return DirectoryTreeView;
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
