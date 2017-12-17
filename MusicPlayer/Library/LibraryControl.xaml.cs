using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using MusicPlayer.DataStructures;
using IPlaylistTransferRequestHandler = MusicPlayer.Playlist.IPlaylistTransferRequestHandler;

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

    public partial class LibraryControl : UserControl
    {
        #region Data

        MusicTreeViewModel _musicTree;

        ILibraryRequestHandler requestHandler
        {
            get
            {
                return FileManager.Instance;
            }
        }

        IPlaylistTransferRequestHandler playlistTransferRequestHandler
        {
            get
            {
                return FileManager.Instance;
            }
        }

        #endregion // Data

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
                _musicTree = new MusicTreeViewModel(requestHandler);
            }

            DataContext = _musicTree;
        }

        public void Rebuild()
        {
            _musicTree = new MusicTreeViewModel(requestHandler);
            DataContext = _musicTree;
        }

        #endregion // Construction

        #region Context Menu Events
        
        public delegate void ContextMenuIDRequest(LibraryContext context, long id);
        public delegate void ContextMenuMultiIDRequest(IList<ValueTuple<LibraryContext, long>> items, bool deep);

        public event ContextMenuIDRequest ContextMenu_Edit;
        public event ContextMenuIDRequest ContextMenu_EditArt;

        #endregion // Context Menu Events



        private void Play(LibraryContext context, long id, bool deep)
        {
            ICollection<SongDTO> songs;

            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        songs = playlistTransferRequestHandler.GetArtistData(
                            artistID: id,
                            deep: deep);
                    }
                    break;
                case LibraryContext.Album:
                    {
                        songs = playlistTransferRequestHandler.GetAlbumData(
                            albumID: id,
                            deep: deep);
                    }
                    break;
                case LibraryContext.Song:
                    {
                        songs = playlistTransferRequestHandler.GetSongData(id);
                    }
                    break;
                case LibraryContext.Track:
                    {
                        throw new NotImplementedException();
                    }
                case LibraryContext.Recording:
                    {
                        songs = playlistTransferRequestHandler.GetSongDataFromRecordingID(id);
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    return;
            }

            Playlist.PlaylistManager.Instance.Rebuild(songs);
            Player.MusicManager.Instance.Next();
        }

        private void Add(LibraryContext context, long id, bool deep)
        {
            ICollection<SongDTO> songs;

            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        songs = playlistTransferRequestHandler.GetArtistData(
                            artistID: id,
                            deep: deep);
                    }
                    break;
                case LibraryContext.Album:
                    {
                        songs = playlistTransferRequestHandler.GetAlbumData(
                            albumID: id,
                            deep: deep);
                    }
                    break;
                case LibraryContext.Song:
                    {
                        songs = playlistTransferRequestHandler.GetSongData(id);
                    }
                    break;
                case LibraryContext.Track:
                    {
                        throw new NotImplementedException();
                    }
                case LibraryContext.Recording:
                    {
                        songs = playlistTransferRequestHandler.GetSongDataFromRecordingID(id);
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    return;
            }

            Playlist.PlaylistManager.Instance.AddBack(songs);

        }

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
                    Play(LibraryContext.Song, songModel.ID, false);
                }
                else if (treeItem.Header is RecordingViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    Play(LibraryContext.Recording, recordingModel.ID, false);
                }
            }
        }

        private void Play_Deep(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Play);

            if (id != -1)
            {
                e.Handled = true;
                Play(context, id, true);
            }
        }

        private void Play_Shallow(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Play);

            if (id != -1)
            {
                e.Handled = true;
                Play(context, id, false);
            }
        }

        private void Add_Deep(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Add);

            if (id != -1)
            {
                e.Handled = true;
                Add(context, id, true);
            }
        }

        private void Add_Shallow(object sender, System.Windows.RoutedEventArgs e)
        {
            (LibraryContext context, long id) = ExtractContextAndID(sender as MenuItem, MenuAction.Add);

            if (id != -1)
            {
                e.Handled = true;
                Add(context, id, false);
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

        #endregion // Callbacks
    }
}
