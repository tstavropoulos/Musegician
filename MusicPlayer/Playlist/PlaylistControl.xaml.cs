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
        public event PassID Request_PlaySong;

        private int _currentIndex = -1;
        public int CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                UnmarkIndex(_currentIndex);
                _currentIndex = value;
                MarkIndex(_currentIndex);
            }
        }

        public int ItemCount
        {
            get { return _playlistTree.PlaylistViewModels.Count; }
        }

        public PlaylistControl()
        {
            InitializeComponent();

            _playlistTree = new PlaylistTreeViewModel(new List<PlaylistItemDTO>());
            base.DataContext = _playlistTree;
        }

        public void Rebuild(IList<PlaylistItemDTO> songs)
        {
            _playlistTree = new PlaylistTreeViewModel(songs);
            base.DataContext = _playlistTree;
        }

        public void AddBack(DataStructures.PlaylistData song)
        {
            _playlistTree.Add(new PlaylistItemDTO()
            {
                SongID = song.songID,
                Title = String.Format("{0} - {1}", song.artistName, song.songTitle)
            });
        }

        public void AddBack(IList<DataStructures.PlaylistData> songs)
        {
            foreach(DataStructures.PlaylistData song in songs)
            {
                _playlistTree.Add(new PlaylistItemDTO()
                {
                    SongID = song.songID,
                    Title = String.Format("{0} - {1}", song.artistName, song.songTitle)
                });
            }
        }

        public void PlayPrevious()
        {
            if (CurrentIndex > 0)
            {
                --CurrentIndex;
                Request_PlaySong?.Invoke(_playlistTree.PlaylistViewModels[CurrentIndex].ID);
            }
        }

        public void PlayNext()
        {
            if (ItemCount > CurrentIndex + 1)
            {
                ++CurrentIndex;
                Request_PlaySong?.Invoke(_playlistTree.PlaylistViewModels[CurrentIndex].ID);
            }
        }

        public void PlayBack()
        {
            if (ItemCount > 0)
            {
                CurrentIndex = ItemCount - 1;
                Request_PlaySong?.Invoke(_playlistTree.PlaylistViewModels[CurrentIndex].ID);
            }
        }

        public void PlayIndex(int index)
        {
            if (ItemCount > index)
            {
                CurrentIndex = index;
                Request_PlaySong?.Invoke(_playlistTree.PlaylistViewModels[CurrentIndex].ID);
            }
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

                CurrentIndex = _playlistTree.PlaylistViewModels.IndexOf(song);
                Request_PlaySong.Invoke(song.ID);
            }
        }

        private void Play(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is PlaylistItemViewModel song)
            {
                CurrentIndex = _playlistTree.PlaylistViewModels.IndexOf(song);
                Request_PlaySong.Invoke(song.ID);
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {

        }

        private void UnmarkIndex(int index)
        {
            if (index >= 0 && index < ItemCount)
            {
                _playlistTree.PlaylistViewModels[index].Playing = false;
            }
        }

        private void MarkIndex(int index)
        {
            if (index >= 0 && index < ItemCount)
            {
                _playlistTree.PlaylistViewModels[index].Playing = true;
            }
        }
    }
}
