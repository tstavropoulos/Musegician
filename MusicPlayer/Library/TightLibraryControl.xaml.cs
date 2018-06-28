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
using IPlaylistTransferRequestHandler = Musegician.Playlist.IPlaylistTransferRequestHandler;

namespace Musegician.Library
{
    public partial class TightLibraryControl : UserControl
    {
        #region Data

        MusicTreeViewModel _musicTree;

        ILibraryRequestHandler LibraryRequestHandler => FileManager.Instance;
        IPlaylistTransferRequestHandler PlaylistTransferRequestHandler => FileManager.Instance;

        #endregion Data
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

        private void Add(IEnumerable<BaseData> data, bool deep, int position = -1)
        {
            List<PlaylistSong> songs = new List<PlaylistSong>();
            
            foreach (BaseData datum in data)
            {
                if (datum is Artist artist)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetArtistData(artist, deep));
                }
                else if(datum is Album album)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetAlbumData(album, deep));
                }
                else if (datum is Song song)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(song));
                }
                else if(datum is Track track)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(track.Recording));
                }
                else if(datum is Recording recording)
                {
                    songs.AddRange(PlaylistTransferRequestHandler.GetSongData(recording));
                }
                else
                {
                    Console.WriteLine($"Unexpected LibraryContext: {datum}.  Likey error.");

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
                    Add(new BaseData[] { songModel.Data }, false);
                }
                else if (treeItem.Header is RecordingViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;
                    Add(new BaseData[] { recordingModel.Data }, false);
                }
            }
        }

        #endregion Mouse Callbacks
        #region Context Menu Callbacks

        private void Add_Shallow(object sender, RoutedEventArgs e)
        {
            var data = ExtractContextAndIDs(MenuAction.Add);

            if (data.Count() > 0)
            {
                e.Handled = true;
                Add(data, false);
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

        private IEnumerable<BaseData> ExtractContextAndIDs(MenuAction option)
        {
            IEnumerable<LibraryViewModel> selectedItems =
               ClassicTreeView.SelectedItems.OfType<LibraryViewModel>();

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
                
                if(context == LibraryContext.Track)
                {
                    return selectedItems.Select(x => (x as SongViewModel).Track);
                }
                else
                {
                    return selectedItems.Select(x => x.Data);
                }
            }

            return null;
        }

        #endregion Helper Fuctions
    }
}
