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

            Loaded += PlaylistControl_Loaded;
            Unloaded += PlaylistControl_Unloaded;

            _playlistTree = new PlaylistTreeViewModel();
        }

        private void PlaylistControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlaylistMan.addBack += AddBack;
            PlaylistMan.rebuild += Rebuild;
            PlaylistMan.RemoveAt += RemoveAt;

            PlaylistMan.MarkIndex += MarkIndex;
            PlaylistMan.MarkRecordingIndex += MarkRecordingIndex;
            PlaylistMan.UnmarkAll += UnmarkAll;

            LoadPlaylistPopup.Opened += PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed += PlaylistManControl.Popup_Closed;
        }

        private void PlaylistControl_Unloaded(object sender, RoutedEventArgs e)
        {
            PlaylistMan.addBack -= AddBack;
            PlaylistMan.rebuild -= Rebuild;
            PlaylistMan.RemoveAt -= RemoveAt;

            PlaylistMan.MarkIndex -= MarkIndex;
            PlaylistMan.MarkRecordingIndex -= MarkRecordingIndex;
            PlaylistMan.UnmarkAll -= UnmarkAll;

            LoadPlaylistPopup.Opened -= PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed -= PlaylistManControl.Popup_Closed;
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

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeItem)
            {
                if (treeItem.Header is PlaylistSongViewModel song)
                {
                    e.Handled = true;
                    if (!song.IsSelected)
                    {
                        return;
                    }

                    PlaylistMan.PlayIndex(_playlistTree.PlaylistViewModels.IndexOf(song));
                }
                else if (treeItem.Header is PlaylistRecordingViewModel recording)
                {
                    e.Handled = true;
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
            if (sender is MenuItem menuItem)
            {
                if (menuItem.DataContext is PlaylistViewModel model)
                {
                    e.Handled = true;
                    _Play(model);
                }
                else
                {
                    Console.WriteLine("Unhandled ViewModel.  Likely Error.");
                }
            }
        }

        private void _Play(PlaylistViewModel model)
        {
            if (model is PlaylistSongViewModel song)
            {
                int index = _playlistTree.PlaylistViewModels.IndexOf(song);

                PlaylistMan.PlayIndex(index);
            }
            else if (model is PlaylistRecordingViewModel recording)
            {
                int songIndex = _playlistTree.PlaylistViewModels.IndexOf(recording.Song);
                int recordingIndex = _playlistTree.PlaylistViewModels[songIndex].Children.IndexOf(recording);

                PlaylistMan.PlayRecording(songIndex, recordingIndex);
            }
            else
            {
                Console.WriteLine("Unhandled PlaylistViewModel.  Likely Error.");
            }
        }

        private void Remove(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.DataContext is PlaylistSongViewModel song)
                {
                    e.Handled = true;
                    int indexToRemove = _playlistTree.PlaylistViewModels.IndexOf(song);

                    PlaylistMan.RemoveIndex(indexToRemove);
                }
                else if (menuItem.DataContext is PlaylistRecordingViewModel recording)
                {
                    e.Handled = true;
                    MessageBox.Show("Not Yet Implemented.");
                }
                else
                {
                    Console.WriteLine("Unhandled ViewModel.  Likely Error.");
                }
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.DataContext is PlaylistSongViewModel song)
                {
                    e.Handled = true;
                    MessageBox.Show("Not Yet Implemented.");
                }
                else if (menuItem.DataContext is PlaylistRecordingViewModel recording)
                {
                    e.Handled = true;
                    MessageBox.Show("Not Yet Implemented.");
                }
                else
                {
                    Console.WriteLine("Unhandled ViewModel.  Likely Error.");
                }
            }
        }

        private void Find(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.DataContext is PlaylistSongViewModel song)
                {
                    e.Handled = true;
                    MessageBox.Show("Not Yet Implemented.");
                }
                else if (menuItem.DataContext is PlaylistRecordingViewModel recording)
                {
                    e.Handled = true;
                    MessageBox.Show("Not Yet Implemented.");
                }
                else
                {
                    Console.WriteLine("Unhandled ViewModel.  Likely Error.");
                }
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

        public void Toolbar_SavePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (PlaylistMan.ItemCount == 0)
            {
                MessageBox.Show("Cannot save empty playlist.", "Empty Playlist");
                SavePlaylist.IsChecked = false;
                return;
            }

            PlaylistManControl.SaveMode = true;
            LoadPlaylistPopup.IsOpen = true;
        }

        public void Toolbar_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            PlaylistManControl.SaveMode = false;
            LoadPlaylistPopup.IsOpen = true;
        }

        private void Popup_PlaylistClosed(object sender, EventArgs e)
        {
            SavePlaylist.IsChecked = false;
            LoadPlaylist.IsChecked = false;
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

                KeyboardActions action = TranslateKey(e.Key);

                switch (action)
                {
                    case KeyboardActions.None:
                        //Do nothing
                        return;
                    case KeyboardActions.WeightUp:
                    case KeyboardActions.WeightDown:
                        {
                            e.Handled = true;
                            UpdateWeights(action);
                        }
                        break;
                    case KeyboardActions.Play:
                        {
                            e.Handled = true;
                            if (PlaylistTree.SelectedItem is PlaylistViewModel model)
                            {
                                _Play(model);
                            }
                        }
                        break;
                    case KeyboardActions.MAX:
                    default:
                        throw new Exception("Unexpected KeyboardAction: " + action);
                }
            }
        }

        private void UpdateWeights(KeyboardActions action)
        {
            IEnumerable<PlaylistViewModel> selectedItems =
                PlaylistTree.SelectedItems.OfType<PlaylistViewModel>();

            foreach (PlaylistViewModel model in selectedItems)
            {
                switch (action)
                {
                    case KeyboardActions.WeightUp:
                        model.Weight = MathExt.Clamp(model.Weight + 0.05, 0.0, 1.0);
                        break;
                    case KeyboardActions.WeightDown:
                        model.Weight = MathExt.Clamp(model.Weight - 0.05, 0.0, 1.0);
                        break;
                    default:
                        throw new Exception("Unexpected KeyboardAction: " + action);
                }
            }
        }

        private (PlaylistContext context, List<(long id, double weight)> values) ExtractContextIDAndWeights()
        {
            IEnumerable<PlaylistViewModel> selectedItems = PlaylistTree.SelectedItems.OfType<PlaylistViewModel>();

            PlaylistContext context = PlaylistContext.MAX;
            List<(long id, double weight)> weightList = new List<(long id, double weight)>();


            if (selectedItems.Count() > 0)
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

    }
}
