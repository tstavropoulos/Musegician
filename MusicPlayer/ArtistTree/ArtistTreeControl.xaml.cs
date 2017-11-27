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
        public event PassID SongDoubleClicked;

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (sender is TreeViewItem && ((TreeViewItem)sender).Header is SongViewModel)
            {
                SongViewModel song = (SongViewModel)((TreeViewItem)sender).Header;
                if (!song.IsSelected)
                {
                    return;
                }

                SongDoubleClicked.Invoke(song.ID);
            }
        }

        private void Play(object sender, System.Windows.RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is ArtistViewModel)
            {

            }
            else if (menuItem.DataContext is AlbumViewModel)
            {

            }
            else if (menuItem.DataContext is SongViewModel)
            {

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

            }
            else if (menuItem.DataContext is AlbumViewModel)
            {

            }
            else if (menuItem.DataContext is SongViewModel)
            {

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

            }
            else if (menuItem.DataContext is AlbumViewModel)
            {

            }
            else if (menuItem.DataContext is SongViewModel)
            {

            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        #endregion // Callbacks

    }
}
