using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicPlayer.Playlist
{
    /// <summary>
    /// Interaction logic for PlaylistControl.xaml
    /// </summary>
    public partial class PlaylistControl : UserControl
    {
        PlaylistTreeViewModel _playlistTree;

        public delegate void PassID(int id);
        public event PassID SongDoubleClicked;

        public PlaylistControl()
        {
            InitializeComponent();

            _playlistTree = new PlaylistTreeViewModel(new List<SongDTO>());
        }

        public void Rebuild(IList<SongDTO> songs)
        {
            _playlistTree = new PlaylistTreeViewModel(songs);
            base.DataContext = _playlistTree;
        }

        public void Add(SongDTO song)
        {
            _playlistTree.Add(song);
        }


        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (sender is TreeViewItem && ((TreeViewItem)sender).Header is PlaylistItemViewModel)
            {
                PlaylistItemViewModel song = (PlaylistItemViewModel)((TreeViewItem)sender).Header;
                if (!song.IsSelected)
                {
                    return;
                }

                SongDoubleClicked.Invoke(song.ID);
            }
        }

        private void Play(object sender, RoutedEventArgs e)
        {

        }

        private void Edit(object sender, RoutedEventArgs e)
        {

        }
    }
}
