using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicPlayer
{
    public partial class ArtistTreeControl : UserControl
    {
        MusicTreeViewModel _musicTree;

        public ArtistTreeControl()
        {
            InitializeComponent();

            // Create UI-friendly wrappers around the 
            // raw data objects (i.e. the view-model).
            _musicTree = new MusicTreeViewModel(new List<ArtistDTO>());

            // Let the UI bind to the view-model.
            base.DataContext = _musicTree;
        }

        public void Rebuild(IList<ArtistDTO> artists)
        {
            _musicTree = new MusicTreeViewModel(artists);
            base.DataContext = _musicTree;
        }

        #region Callbacks

        void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _musicTree.SearchCommand.Execute(null);
            }
        }

        public delegate void PassID(int id);

        public event PassID Request_PlaySong;
        public event PassID Request_AddSong;
        public event PassID Request_EditSong;

        public event PassID Request_PlayAlbum;
        public event PassID Request_AddAlbum;
        public event PassID Request_EditAlbum;

        public event PassID Request_PlayArtist;
        public event PassID Request_AddArtist;
        public event PassID Request_EditArtist;

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (sender is TreeViewItem && ((TreeViewItem)sender).Header is SongViewModel)
            {
                SongViewModel song = (SongViewModel)((TreeViewItem)sender).Header;
                if (!song.IsSelected)
                {
                    return;
                }

                Request_PlaySong.Invoke(song.ID);
            }
        }

        private void Play(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is ArtistViewModel)
            {
                Request_PlayArtist?.Invoke(((ArtistViewModel)menuItem.DataContext).ID);
            }
            else if (menuItem.DataContext is AlbumViewModel)
            {
                Request_PlayAlbum?.Invoke(((AlbumViewModel)menuItem.DataContext).ID);
            }
            else if (menuItem.DataContext is SongViewModel)
            {
                Request_PlaySong?.Invoke(((SongViewModel)menuItem.DataContext).ID);
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void Add(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is ArtistViewModel)
            {
                Request_AddArtist?.Invoke(((ArtistViewModel)menuItem.DataContext).ID);
            }
            else if (menuItem.DataContext is AlbumViewModel)
            {
                Request_AddAlbum?.Invoke(((AlbumViewModel)menuItem.DataContext).ID);
            }
            else if (menuItem.DataContext is SongViewModel)
            {
                Request_AddSong?.Invoke(((SongViewModel)menuItem.DataContext).ID);
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void Edit(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is ArtistViewModel)
            {
                Request_EditArtist?.Invoke(((ArtistViewModel)menuItem.DataContext).ID);
            }
            else if (menuItem.DataContext is AlbumViewModel)
            {
                Request_EditAlbum?.Invoke(((AlbumViewModel)menuItem.DataContext).ID);
            }
            else if (menuItem.DataContext is SongViewModel)
            {
                Request_EditSong?.Invoke(((SongViewModel)menuItem.DataContext).ID);
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        #endregion // Callbacks

    }
}
