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
using Musegician.DataStructures;
using IPlaylistTransferRequestHandler = Musegician.Playlist.IPlaylistTransferRequestHandler;

namespace Musegician.Library
{
    public partial class TightLibraryControl : UserControl
    {
        #region Data

        MusicTreeViewModel _musicTree;

        ILibraryRequestHandler LibraryRequestHandler
        {
            get { return FileManager.Instance; }
        }

        IPlaylistTransferRequestHandler PlaylistTransferRequestHandler
        {
            get { return FileManager.Instance; }
        }

        #endregion Data
        #region Context Menu Events

        public delegate void ContextMenuIDRequest(LibraryContext context, long id);
        public delegate void ContextMenuMultiIDRequest(LibraryContext context, IList<long> ids);

        #endregion Context Menu Events
        #region Constructor

        public TightLibraryControl()
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
                _musicTree.CurrentViewMode = ViewMode.Classic;
            }

            DataContext = _musicTree;
        }

        public void Rebuild()
        {
            _musicTree = new MusicTreeViewModel(LibraryRequestHandler);
            DataContext = _musicTree;
            
            //Trigger the loading of the current view mode
            _musicTree.CurrentViewMode = ViewMode.Classic;
        }

        #endregion Constructor
        #region Class Methods

        private void Add(LibraryContext context, IEnumerable<long> ids, bool deep, int position = -1)
        {
            List<SongDTO> songs = new List<SongDTO>();

            foreach (long id in ids)
            {
                switch (context)
                {
                    case LibraryContext.Artist:
                        {
                            songs.AddRange(PlaylistTransferRequestHandler.GetArtistData(
                                artistID: id,
                                deep: deep));
                        }
                        break;
                    case LibraryContext.Album:
                        {
                            songs.AddRange(PlaylistTransferRequestHandler.GetAlbumData(
                                albumID: id,
                                deep: deep));
                        }
                        break;
                    case LibraryContext.Song:
                        {
                            songs.AddRange(PlaylistTransferRequestHandler.GetSongData(id));
                        }
                        break;
                    case LibraryContext.Track:
                        {
                            throw new NotImplementedException();
                        }
                    case LibraryContext.Recording:
                        {
                            songs.AddRange(PlaylistTransferRequestHandler.GetSongDataFromRecordingID(id));
                        }
                        break;
                    case LibraryContext.MAX:
                    default:
                        Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
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
                    Add(LibraryContext.Song, new long[] { songModel.ID }, false);
                }
                else if (treeItem.Header is RecordingViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    Add(LibraryContext.Recording, new long[] { recordingModel.ID }, false);
                }
            }
        }

        #endregion Mouse Callbacks
        #region Context Menu Callbacks

        private void Add_Shallow(object sender, RoutedEventArgs e)
        {
            (LibraryContext context, List<long> ids) = ExtractContextAndIDs(MenuAction.Add);

            if (ids.Count() > 0)
            {
                e.Handled = true;
                Add(context, ids, false);
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

        private void LibraryRequestHandler_Rebuild(object sender, EventArgs e)
        {
            Rebuild();
        }

        private void LibraryControl_Loaded(object sender, RoutedEventArgs e)
        {
            LibraryRequestHandler.RebuildNotifier += LibraryRequestHandler_Rebuild;
        }

        private void LibraryControl_Unloaded(object sender, RoutedEventArgs e)
        {
            LibraryRequestHandler.RebuildNotifier -= LibraryRequestHandler_Rebuild;
        }

        #endregion View Callbacks
        #endregion Callbacks
        #region Helper Fuctions

        private (LibraryContext, List<long>) ExtractContextAndIDs(MenuAction option)
        {
            IEnumerable<LibraryViewModel> selectedItems =
               ClassicTreeView.SelectedItems.OfType<LibraryViewModel>();

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
                        ids.Add(song.ContextualTrackID);
                    }
                    else
                    {
                        ids.Add(model.ID);
                    }
                }
            }

            return (context, ids);
        }

        #endregion Helper Fuctions
    }
}
