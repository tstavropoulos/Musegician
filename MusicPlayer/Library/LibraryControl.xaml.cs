using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicPlayer.Library
{
    public enum LibraryContext
    {
        Artist = 0,
        Album,
        Song,
        Recording,
        MAX
    }

    public partial class LibraryControl : UserControl
    {
        #region Data
        MusicTreeViewModel _musicTree;
        #endregion // Data

        #region Construction
        public LibraryControl()
        {
            InitializeComponent();

            _musicTree = new MusicTreeViewModel(new List<ArtistDTO>());

            // Bind view-mdoel to UI.
            base.DataContext = _musicTree;
        }

        public void Rebuild(IList<ArtistDTO> artists)
        {
            _musicTree = new MusicTreeViewModel(artists);
            base.DataContext = _musicTree;
        }
        #endregion // Construction

        #region Context Menu Events
        public delegate void ContextMenuIDRequest(LibraryContext context, long id);
        public delegate void ContextMenuMultiIDRequest(IList<Tuple<LibraryContext, long>> items);

        public event ContextMenuIDRequest ContextMenu_Play;
        public event ContextMenuIDRequest ContextMenu_Add;
        public event ContextMenuIDRequest ContextMenu_Edit;
        #endregion // Context Menu Events

        #region Callbacks

        #region Callbacks Defunct Search Example
        void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _musicTree.SearchCommand.Execute(null);
            }
        }
        #endregion // Callbacks Defunct Search Example

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender is TreeViewItem treeItem)
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
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem);

            if (id != -1)
            {
                e.Handled = true;
                ContextMenu_Play?.Invoke(context, id);
            }
        }

        private void Add(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem);

            if (id != -1)
            {
                e.Handled = true;
                ContextMenu_Add?.Invoke(context, id);
            }
        }

        private void Edit(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem);

            if (id != -1)
            {
                e.Handled = true;
                ContextMenu_Edit?.Invoke(context, id);
            }
        }

        private (LibraryContext, long) ExtractContextAndID(MenuItem menuItem)
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
                context = LibraryContext.Song;
                id = songModel.ID;
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


        #endregion // Callbacks

    }
}
