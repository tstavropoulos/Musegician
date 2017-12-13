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
using MusicPlayer.DataStructures;

namespace MusicPlayer.Playlist
{
    /// <summary>
    /// Interaction logic for PlaylistControl.xaml
    /// </summary>
    public partial class PlaylistControl : UserControl
    {
        PlaylistTreeViewModel _playlistTree;

        PlaylistManager playlistMan
        {
            get { return PlaylistManager.Instance; }
        }

        public event RoutedEventHandler TinyViewerPressed
        {
            add
            {
                tinyViewerButton.Click += value;
            }
            remove
            {
                tinyViewerButton.Click -= value;
            }
        }


        private PlaylistSongViewModel _playingSong;
        private PlaylistSongViewModel playingSong
        {
            get { return _playingSong; }
            set
            {
                if (_playingSong == value)
                {
                    return;
                }

                if (_playingSong != null)
                {
                    _playingSong.Playing = false;
                }
                _playingSong = value;
                if (_playingSong != null)
                {
                    _playingSong.Playing = true;
                }
            }
        }

        private PlaylistRecordingViewModel _playingRecording;
        private PlaylistRecordingViewModel playingRecording
        {
            get { return _playingRecording; }
            set
            {
                if (_playingRecording == value)
                {
                    return;
                }

                if (_playingRecording != null)
                {
                    _playingRecording.Playing = false;
                }
                _playingRecording = value;
                if (_playingRecording != null)
                {
                    _playingRecording.Playing = true;
                }
            }
        }

        public int ItemCount
        {
            get { return _playlistTree.PlaylistViewModels.Count; }
        }

        public PlaylistControl()
        {
            InitializeComponent();

            playlistMan.addBack += AddBack;
            playlistMan.rebuild += Rebuild;
            playlistMan.RemoveAt += RemoveAt;

            playlistMan.MarkIndex += MarkIndex;
            playlistMan.MarkRecordingIndex += MarkRecordingIndex;
            playlistMan.UnmarkAll += UnmarkAll;
        }

        private void Rebuild(ICollection<SongDTO> songs)
        {
            _playlistTree = new PlaylistTreeViewModel(songs);
            base.DataContext = _playlistTree;
        }

        private void AddBack(ICollection<SongDTO> songs)
        {
            _playlistTree.Add(songs);
        }

        private void ClearPlaylist()
        {
            Rebuild(new List<SongDTO>());
        }

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (sender is TreeViewItem treeItem)
            {
                if (treeItem.Header is PlaylistSongViewModel song)
                {
                    args.Handled = true;
                    if (!song.IsSelected)
                    {
                        return;
                    }

                    playlistMan.PlayIndex(_playlistTree.PlaylistViewModels.IndexOf(song));
                }
                else if (treeItem.Header is PlaylistRecordingViewModel recording)
                {
                    args.Handled = true;
                    if (!recording.IsSelected)
                    {
                        return;
                    }

                    int songIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                    int recordingIndex = _playlistTree.PlaylistViewModels[songIndex].Recordings.IndexOf(recording);

                    playlistMan.PlayRecording(songIndex, recordingIndex);
                }
            }
        }

        private void Play(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is PlaylistSongViewModel song)
            {
                e.Handled = true;
                int index = _playlistTree.PlaylistViewModels.IndexOf(song);

                playlistMan.PlayIndex(index);
            }
            else if (menuItem.DataContext is PlaylistRecordingViewModel recording)
            {
                e.Handled = true;
                int songIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                int recordingIndex = _playlistTree.PlaylistViewModels[songIndex].Recordings.IndexOf(recording);

                playlistMan.PlayRecording(songIndex, recordingIndex);
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void Remove(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is PlaylistSongViewModel song)
            {
                e.Handled = true;
                int indexToRemove = _playlistTree.PlaylistViewModels.IndexOf(song);

                playlistMan.RemoveIndex(indexToRemove);
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is PlaylistSongViewModel song)
            {
                e.Handled = true;
                MessageBox.Show("Not Yet Implemented.");
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void UnmarkAll()
        {
            playingSong = null;
            playingRecording = null;
        }

        private void MarkIndex(int index)
        {
            if (index >= 0 && index < ItemCount)
            {
                playingSong = _playlistTree.PlaylistViewModels[index];
            }
            else
            {
                playingSong = null;
            }
        }

        private void MarkRecordingIndex(int index)
        {
            if (index >= 0 && index < playingSong.Recordings.Count)
            {
                playingRecording = playingSong.Recordings[index];
            }
            else
            {
                playingRecording = null;
            }
        }

        private void RemoveAt(int index)
        {
            _playlistTree.PlaylistViewModels.RemoveAt(index);
        }
    }
}
