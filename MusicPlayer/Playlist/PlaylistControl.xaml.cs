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

        PlaylistManager PlaylistMan
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
        private PlaylistSongViewModel PlayingSong
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
        private PlaylistRecordingViewModel PlayingRecording
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

            PlaylistMan.addBack += AddBack;
            PlaylistMan.rebuild += Rebuild;
            PlaylistMan.RemoveAt += RemoveAt;

            PlaylistMan.MarkIndex += MarkIndex;
            PlaylistMan.MarkRecordingIndex += MarkRecordingIndex;
            PlaylistMan.UnmarkAll += UnmarkAll;

            _playlistTree = new PlaylistTreeViewModel();
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

                    PlaylistMan.PlayIndex(_playlistTree.PlaylistViewModels.IndexOf(song));
                }
                else if (treeItem.Header is PlaylistRecordingViewModel recording)
                {
                    args.Handled = true;
                    if (!recording.IsSelected)
                    {
                        return;
                    }

                    int songIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                    int recordingIndex = _playlistTree.PlaylistViewModels[songIndex].Children.IndexOf(recording);

                    PlaylistMan.PlayRecording(songIndex, recordingIndex);
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

                PlaylistMan.PlayIndex(index);
            }
            else if (menuItem.DataContext is PlaylistRecordingViewModel recording)
            {
                e.Handled = true;
                int songIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                int recordingIndex = _playlistTree.PlaylistViewModels[songIndex].Children.IndexOf(recording);

                PlaylistMan.PlayRecording(songIndex, recordingIndex);
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

                PlaylistMan.RemoveIndex(indexToRemove);
            }
            else if (menuItem.DataContext is PlaylistRecordingData recording)
            {
                e.Handled = true;
                MessageBox.Show("Not Yet Implemented.");
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
            else if (menuItem.DataContext is PlaylistRecordingData recording)
            {
                e.Handled = true;
                MessageBox.Show("Not Yet Implemented.");
            }
            else
            {
                Console.WriteLine("Unhandled ViewModel.  Likely Error.");
            }
        }

        private void Find(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            if (menuItem.DataContext is PlaylistSongViewModel song)
            {
                e.Handled = true;
                MessageBox.Show("Not Yet Implemented.");
            }
            else if (menuItem.DataContext is PlaylistRecordingData recording)
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
            PlayingSong = null;
            PlayingRecording = null;
        }

        private void MarkIndex(int index)
        {
            if (index >= 0 && index < ItemCount)
            {
                PlayingSong = _playlistTree.PlaylistViewModels[index];
            }
            else
            {
                PlayingSong = null;
            }
        }

        private void MarkRecordingIndex(int index)
        {
            if (index >= 0 && index < PlayingSong.Children.Count)
            {
                PlayingRecording = PlayingSong.Children[index] as PlaylistRecordingViewModel;
            }
            else
            {
                PlayingRecording = null;
            }
        }

        private void RemoveAt(int index)
        {
            _playlistTree.PlaylistViewModels.RemoveAt(index);
        }

        private void Toolbar_NewPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            PlaylistMan.ClearPlaylist();
        }

        private void Toolbar_SavePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (PlaylistMan.ItemCount == 0)
            {
                MessageBox.Show("Cannot save empty playlist.", "Empty Playlist");
                return;
            }

            PlaylistWindow window = new PlaylistWindow(true);

            window.Show();
        }

        private void Toolbar_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            PlaylistWindow window = new PlaylistWindow(false);

            window.Show();
        }

        private enum KeyboardActions
        {
            None = 0,
            WeightUp,
            WeightDown,
            Play,
            MAX
        }

        private KeyboardActions TranslateKey(Key key)
        {
            switch (key)
            {
                case Key.Return:
                    return KeyboardActions.Play;
                case Key.Add:
                case Key.OemPlus:
                    return KeyboardActions.WeightUp;
                case Key.Subtract:
                case Key.OemMinus:
                    return KeyboardActions.WeightDown;
                default:
                    return KeyboardActions.None;
            }
        }

        public enum PlaylistContext
        {
            Song = 0,
            Recording,
            MAX
        }

        private void Tree_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TreeView tree)
            {
                e.Handled = true;

                KeyboardActions action = TranslateKey(e.Key);

                if (action == KeyboardActions.None)
                {
                    //Do nothing
                    return;
                }

                (PlaylistContext context, List<(long id, double weight)> values) =
                    ExtractContextIDAndWeights();
                
                for (int i = 0; i < values.Count; i++)
                {
                    (long id, double weight) = values[i];

                    if (id == -1)
                    {
                        throw new Exception("Unexpected id = -1!");
                    }

                    switch (action)
                    {
                        case KeyboardActions.WeightUp:
                            weight = Math.Min(weight + 0.05, 1.0);
                            break;
                        case KeyboardActions.WeightDown:
                            weight = Math.Max(weight - 0.05, 0.0);
                            break;
                        case KeyboardActions.Play:
                            //Play thing

                            return;
                        case KeyboardActions.None:
                        case KeyboardActions.MAX:
                        default:
                            throw new Exception("Unexpected KeyboardAction: " + action);
                    }

                    values[i] = (id, weight);
                }
                
                UpdateWeights(values);
            }
        }

        private (PlaylistContext context, List<(long id, double weight)> values) ExtractContextIDAndWeights()
        {
            IEnumerable<PlaylistViewModel> selectedItems = PlaylistTree.SelectedItems.OfType<PlaylistViewModel>();
            
            PlaylistContext context = PlaylistContext.MAX;
            List<(long id, double weight)> weightList = new List<(long id, double weight)>();


            if(selectedItems.Count() > 0)
            {
                PlaylistViewModel firstSelectedItem = selectedItems.First();

                if (firstSelectedItem is PlaylistSongViewModel song)
                {
                    context = PlaylistContext.Song;
                }
                else if (firstSelectedItem is PlaylistRecordingViewModel recording)
                {
                    context = PlaylistContext.Recording;
                }

                foreach (PlaylistViewModel model in selectedItems)
                {
                    weightList.Add((model.ID, model.Weight));
                }
            }

            return (context, weightList);
        }

        private void UpdateWeights(IList<(long id, double weight)> values)
        {
            int i = 0;
            foreach (PlaylistViewModel model in PlaylistTree.SelectedItems)
            {
                //Lets find out if this is sufficient...
                if (model.ID != values[i].id)
                {
                    throw new Exception("selectedItem doesn't match up!");
                }

                model.Weight = values[i].weight;

                i++;
            }
        }
    }
}
