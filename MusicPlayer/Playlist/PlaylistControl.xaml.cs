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
        Random random = new Random();

        public delegate void PassID(long id);
        public event PassID Request_PlayRecording;

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

        private List<int> shuffleList = new List<int>();

        public static readonly DependencyProperty _shuffle = DependencyProperty.Register(
            "Shuffle",
            typeof(bool),
            typeof(PlaylistControl),
            new UIPropertyMetadata(false));

        public bool Shuffle
        {
            get { return (bool)GetValue(_shuffle); }
            set { SetValue(_shuffle, value); }
        }

        public static readonly DependencyProperty _repeat = DependencyProperty.Register(
            "Repeat",
            typeof(bool),
            typeof(PlaylistControl),
            new UIPropertyMetadata(false));

        public bool Repeat
        {
            get { return (bool)GetValue(_repeat); }
            set { SetValue(_repeat, value); }
        }

        public PlaylistControl()
        {
            InitializeComponent();

            _playlistTree = new PlaylistTreeViewModel(new List<SongDTO>());
            base.DataContext = _playlistTree;
            PrepareShuffleList();
        }

        public void Rebuild(IList<SongDTO> songs)
        {
            _currentIndex = -1;
            _playlistTree = new PlaylistTreeViewModel(songs);
            base.DataContext = _playlistTree;
            PrepareShuffleList();
        }

        public void AddBack(IList<SongDTO> songs)
        {
            int originalItemCount = ItemCount;

            _playlistTree.Add(songs);

            for (int i = originalItemCount; i < ItemCount; i++)
            {
                shuffleList.Add(i);
            }
        }

        public void ClearPlaylist()
        {
            Rebuild(new List<SongDTO>());
        }

        public void PlayPrevious()
        {
            if (CurrentIndex > 0)
            {
                --CurrentIndex;
                Request_PlayRecording?.Invoke(
                        SelectRecording(_playlistTree.PlaylistViewModels[CurrentIndex].Song));
            }
        }

        public void PlayNext()
        {
            if (ItemCount == 0)
            {
                return;
            }

            if (Shuffle)
            {
                if (shuffleList.Count == 0 && Repeat)
                {
                    PrepareShuffleList();
                }

                if (shuffleList.Count > 0)
                {
                    int nextIndex = random.Next(0, shuffleList.Count);
                    CurrentIndex = shuffleList[nextIndex];
                    shuffleList.RemoveAt(nextIndex);
                    Request_PlayRecording?.Invoke(
                        SelectRecording(_playlistTree.PlaylistViewModels[CurrentIndex].Song));
                }
            }
            else
            {
                if (ItemCount > CurrentIndex + 1)
                {
                    ++CurrentIndex;
                    Request_PlayRecording?.Invoke(
                        SelectRecording(_playlistTree.PlaylistViewModels[CurrentIndex].Song));
                }
            }
        }

        public void PlayIndex(int index)
        {
            if (ItemCount > index)
            {
                CurrentIndex = index;
                Request_PlayRecording?.Invoke(
                        SelectRecording(_playlistTree.PlaylistViewModels[CurrentIndex].Song));
            }
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

                    CurrentIndex = _playlistTree.PlaylistViewModels.IndexOf(song);
                    Request_PlayRecording?.Invoke(
                            SelectRecording(_playlistTree.PlaylistViewModels[CurrentIndex].Song));
                }
                else if (treeItem.Header is PlaylistRecordingViewModel recording)
                {
                    args.Handled = true;
                    if (!recording.IsSelected)
                    {
                        return;
                    }

                    CurrentIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                    Request_PlayRecording?.Invoke(recording.ID);
                }
            }
        }

        private void Play(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is PlaylistSongViewModel song)
            {
                e.Handled = true;
                CurrentIndex = _playlistTree.PlaylistViewModels.IndexOf(song);
                Request_PlayRecording?.Invoke(
                        SelectRecording(_playlistTree.PlaylistViewModels[CurrentIndex].Song));
            }
            else if (menuItem.DataContext is PlaylistRecordingViewModel recording)
            {
                e.Handled = true;
                CurrentIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                Request_PlayRecording?.Invoke(recording.ID);
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

                _playlistTree.PlaylistViewModels.RemoveAt(indexToRemove);

                if (indexToRemove < CurrentIndex)
                {
                    --_currentIndex;
                }

                for (int i = 0; i < shuffleList.Count; i++)
                {
                    if (indexToRemove < shuffleList[i])
                    {
                        --shuffleList[i];
                    }
                }
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

        public void PrepareShuffleList()
        {
            shuffleList.Clear();

            for (int i = 0; i < ItemCount; i++)
            {
                shuffleList.Add(i);
            }
        }

        private double GetWeight(RecordingDTO recording)
        {
            if (double.IsNaN(recording.Weight))
            {
                if (recording.Live)
                {
                    return Settings.LiveWeight;
                }
                else
                {
                    return Settings.StudioWeight;
                }
            }

            return recording.Weight;
        }

        private long SelectRecording(SongDTO song)
        {
            if (song.Recordings.Count == 0)
            {
                return -1;
            }

            if (song.Recordings.Count == 1)
            {
                return song.Recordings[0].RecordingID;
            }


            double cumulativeWeight = 0.0;

            foreach (RecordingDTO recording in song.Recordings)
            {
                cumulativeWeight += GetWeight(recording);
            }

            cumulativeWeight *= random.NextDouble();

            foreach (RecordingDTO recording in song.Recordings)
            {
                cumulativeWeight -= GetWeight(recording);
                if (cumulativeWeight <= 0.0)
                {
                    return recording.RecordingID;
                }
            }

            Console.WriteLine("Failed to pick a recording before running out.  Method is broken.");
            return song.Recordings[0].RecordingID;
        }
    }
}
