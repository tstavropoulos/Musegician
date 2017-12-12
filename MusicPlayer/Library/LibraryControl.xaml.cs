using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Library
{
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

    public partial class LibraryControl : UserControl
    {
        #region Data

        MusicTreeViewModel _musicTree;
        ILibraryRequestHandler dataManager;

        #endregion // Data

        #region Construction

        public LibraryControl()
        {
            InitializeComponent();

            _musicTree = new MusicTreeViewModel();

            // Bind view-model to UI.
            DataContext = _musicTree;
        }

        public void Initialize(ILibraryRequestHandler dataManager)
        {
            this.dataManager = dataManager;
        }

        public void Rebuild()
        {
            _musicTree = new MusicTreeViewModel(dataManager);
            DataContext = _musicTree;
        }

        #endregion // Construction

        #region Context Menu Events

        public delegate void ContextMenuIDRequest(LibraryContext context, long id);
        public delegate void ContextMenuMultiIDRequest(IList<ValueTuple<LibraryContext, long>> items);

        public event ContextMenuIDRequest ContextMenu_Play;
        public event ContextMenuIDRequest ContextMenu_Add;
        public event ContextMenuIDRequest ContextMenu_Edit;
        public event ContextMenuIDRequest ContextMenu_EditArt;

        #endregion // Context Menu Events

        #region Callbacks

        #region Callbacks Search Example

        void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _musicTree.SearchCommand.Execute(null);
            }
        }

        #endregion // Callbacks Search Example

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeItem)
            {
                if (treeItem.Header is SongViewModel songModel)
                {
                    if (!songModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    ContextMenu_Play?.Invoke(LibraryContext.Song, songModel.ID);
                }
                else if (treeItem.Header is RecordingViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    ContextMenu_Play?.Invoke(LibraryContext.Recording, recordingModel.ID);
                }
            }
        }

        private void Play(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Play);

            if (id != -1)
            {
                e.Handled = true;
                ContextMenu_Play?.Invoke(context, id);
            }
        }

        private void Add(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Add);

            if (id != -1)
            {
                e.Handled = true;
                ContextMenu_Add?.Invoke(context, id);
            }
        }

        private void Edit(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Edit);

            if (id != -1)
            {
                e.Handled = true;

                ContextMenu_Edit?.Invoke(context, id);
            }
        }

        private void EditArt(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Edit);

            if (id != -1)
            {
                e.Handled = true;

                ContextMenu_EditArt?.Invoke(context, id);
            }
        }

        private (LibraryContext, long) ExtractContextAndID(MenuItem menuItem, MenuAction option)
        {
            LibraryContext context = LibraryContext.MAX;
            long id = -1;

            if (menuItem is null)
            {
                Console.WriteLine("Null menuItem.  Likely Error.");
            }
            else if (menuItem.DataContext is ArtistViewModel artistModel)
            {
                context = LibraryContext.Artist;
                id = artistModel.ID;
            }
            else if (menuItem.DataContext is AlbumViewModel albumModel)
            {
                context = LibraryContext.Album;
                id = albumModel.ID;
            }
            else if (menuItem.DataContext is SongViewModel songModel)
            {
                switch (option)
                {
                    case MenuAction.Play:
                    case MenuAction.Add:
                        context = LibraryContext.Song;
                        id = songModel.ID;
                        break;
                    case MenuAction.Edit:
                        context = LibraryContext.Track;
                        id = songModel.ContextualTrackID;
                        break;
                    default:
                        Console.WriteLine("Unhandled MenuAction.  Likely Error: " + option);
                        context = LibraryContext.Song;
                        id = songModel.ID;
                        break;
                }

            }
            else if (menuItem.DataContext is RecordingViewModel recordingModel)
            {
                context = LibraryContext.Recording;
                id = recordingModel.ID;
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }

            return (context, id);
        }

        private void TreeViewItem_Expanded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem treeItem)
            {
                if (treeItem.Header is LibraryViewModel libraryModel)
                {
                    if (!libraryModel.HasDummyChild)
                    {
                        return;
                    }

                    libraryModel.LoadChildren(dataManager);
                }
            }
        }

        #endregion // Callbacks
    }
}
