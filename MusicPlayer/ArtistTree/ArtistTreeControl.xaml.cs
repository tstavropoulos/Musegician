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

        #endregion // Callbacks

        private void PlayArtist(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void AddArtist(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void EditArtist(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void EditSong(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void PlaySong(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void AddSong(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void EditAlbum(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void PlayAlbum(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void AddAlbum(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
