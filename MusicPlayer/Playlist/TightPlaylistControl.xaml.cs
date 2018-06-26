using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Musegician.Core;
using Musegician.Database;

namespace Musegician.Playlist
{
    /// <summary>
    /// Interaction logic for PlaylistControl.xaml
    /// </summary>
    public sealed partial class TightPlaylistControl : UserControl, IPlaylistUpdateListener
    {
        PlaylistTreeViewModel _playlistTree;

        PlaylistManager PlaylistMan => PlaylistManager.Instance;

        private PlaylistSongViewModel _playingSong;
        private PlaylistSongViewModel PlayingSong
        {
            get => _playingSong;
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
            get => _playingRecording;
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

        public int ItemCount => _playlistTree.PlaylistViewModels.Count;

        public TightPlaylistControl()
        {
            InitializeComponent();

            _playlistTree = new PlaylistTreeViewModel();
        }

        /// <summary>
        /// Called by the TinyPlayer's Load/Unload events
        /// </summary>
        public void TightPlaylistControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlaylistMan.AddListener(this);
        }

        /// <summary>
        /// Called by the TinyPlayer's Load/Unload events
        /// </summary>
        public void TightPlaylistControl_Unloaded(object sender, RoutedEventArgs e)
        {
            PlaylistMan.RemoveListener(this);
        }

        private void Rebuild(IEnumerable<PlaylistSong> songs)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _playlistTree = new PlaylistTreeViewModel(songs);
                DataContext = _playlistTree;
            }
        }

        private void ClearPlaylist()
        {
            Rebuild(new List<PlaylistSong>());
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

                    PlaylistMan.PlaySong(song.PlaylistSong);
                }
                else if (treeItem.Header is PlaylistRecordingViewModel recording)
                {
                    e.Handled = true;
                    if (!recording.IsSelected)
                    {
                        return;
                    }

                    PlaylistMan.PlayRecording(recording.PlaylistSong.PlaylistSong, recording.PlaylistRecording);
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
                PlaylistMan.PlaySong(song.PlaylistSong);
            }
            else if (model is PlaylistRecordingViewModel recording)
            {
                PlaylistMan.PlayRecording(recording.PlaylistSong.PlaylistSong, recording.PlaylistRecording);
            }
            else
            {
                Console.WriteLine("Unhandled PlaylistViewModel.  Likely Error.");
            }
        }

        private void Enqueue(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.DataContext is PlaylistViewModel model)
                {
                    e.Handled = true;
                    _Enqueue(model);
                }
                else
                {
                    Console.WriteLine("Unhandled ViewModel.  Likely Error.");
                }
            }
        }

        private void _Enqueue(PlaylistViewModel model)
        {
            if (model is PlaylistSongViewModel song)
            {
                PlaylistMan.EnqueueSong(song.PlaylistSong);
            }
            else if (model is PlaylistRecordingViewModel recording)
            {
                PlaylistMan.EnqueueRecording(recording.PlaylistRecording);
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

                    List<int> sourceIndices = new List<int>(
                        from PlaylistSongViewModel model in PlaylistTree.SelectedItems
                        select _playlistTree.PlaylistViewModels.IndexOf(model));

                    PlaylistMan.RemoveIndex(sourceIndices);
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

        private void OpenLyrics(object sender, RoutedEventArgs e)
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
                    Window window = new LyricViewer.LyricViewer(recording.PlaylistRecording.Recording);
                    window.Show();
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

        #region Drag Handling
        //Some implementation details borrowed from:
        // https://stackoverflow.com/questions/3350187/wpf-c-rearrange-items-in-listbox-via-drag-and-drop

        private bool _validForDrag = false;

        private void Tree_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is MultiSelectTreeView tree)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void Tree_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is MultiSelectTreeView tree)
            {
                e.Handled = true;
            }
        }

        private void Tree_Drop(object sender, DragEventArgs e)
        {
            if (sender is MultiSelectTreeView tree)
            {
                e.Handled = true;
                int targetIndex = _playlistTree.PlaylistViewModels.Count;

                if (e.Data.GetData(typeof(PlaylistSongViewModel)) is PlaylistSongViewModel source)
                {
                    List<int> sourceIndices = new List<int>(
                        from PlaylistSongViewModel model in PlaylistTree.SelectedItems
                        select _playlistTree.PlaylistViewModels.IndexOf(model));

                    sourceIndices.Sort();

                    PlaylistMan.BatchRearrangeSongs(sourceIndices, targetIndex);
                }
                else if (e.Data.GetData(typeof(Library.LibraryDragData)) is Library.LibraryDragData dragData)
                {
                    dragData.callback(targetIndex);
                }

            }
        }

        private void Item_Drop(object sender, DragEventArgs e)
        {
            if (sender is MultiSelectTreeViewItem treeItem)
            {
                //Find the next droppable target in hierarchy
                while (!IsDraggable(treeItem) && treeItem != null)
                {
                    treeItem = FindDraggableVisualParent(treeItem);
                }

                if (treeItem == null)
                {
                    //No draggable item in hierarchy
                    e.Handled = true;
                    return;
                }

                if (e.Data.GetData(typeof(PlaylistSongViewModel)) is PlaylistSongViewModel source &&
                    treeItem.DataContext is PlaylistSongViewModel target)
                {
                    target.ShowDropLine = false;

                    List<int> sourceIndices = new List<int>(
                        from PlaylistSongViewModel model in PlaylistTree.SelectedItems
                        select _playlistTree.PlaylistViewModels.IndexOf(model));

                    sourceIndices.Sort();

                    int targetIndex = _playlistTree.PlaylistViewModels.IndexOf(target);

                    PlaylistMan.BatchRearrangeSongs(sourceIndices, targetIndex);
                    e.Handled = true;
                }
            }
        }

        private void Item_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is MultiSelectTreeViewItem treeItem)
            {
                e.Handled = true;
                //Find the next droppable target in hierarchy
                while (!IsDraggable(treeItem) && treeItem != null)
                {
                    treeItem = FindDraggableVisualParent(treeItem);
                }

                if (treeItem == null)
                {
                    //No draggable item in hierarchy
                    e.Effects = DragDropEffects.None;
                    return;
                }

                if (treeItem.Header is PlaylistViewModel model)
                {
                    e.Effects = DragDropEffects.Move;
                    model.ShowDropLine = true;
                }
            }
        }

        private void Item_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is MultiSelectTreeViewItem treeItem)
            {
                e.Handled = true;
                //Find the next droppable target in hierarchy
                while (!IsDraggable(treeItem) && treeItem != null)
                {
                    treeItem = FindDraggableVisualParent(treeItem);
                }

                if (treeItem == null)
                {
                    //No draggable item in hierarchy
                    return;
                }

                if (treeItem.Header is PlaylistViewModel model)
                {
                    model.ShowDropLine = false;
                }
            }
        }

        private void Item_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is MultiSelectTreeViewItem dragSource &&
                IsDraggable(dragSource) && dragSource.IsItemSelected)
            {
                _validForDrag = true;
            }
        }

        private void Item_MouseEnter(object sender, MouseEventArgs e)
        {
            _validForDrag = false;
        }

        private void Item_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _validForDrag &&
                sender is MultiSelectTreeViewItem dragSource &&
                IsDraggable(dragSource) && dragSource.IsItemSelected)
            {
                DragDrop.DoDragDrop(dragSource, dragSource.DataContext, DragDropEffects.Move);
            }
        }

        private bool IsDraggable(MultiSelectTreeViewItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.Header is PlaylistSongViewModel)
            {
                return true;
            }

            return false;
        }

        private MultiSelectTreeViewItem FindDraggableVisualParent(DependencyObject child)
        {
            MultiSelectTreeViewItem parentItem = FindVisualParent<MultiSelectTreeViewItem>(child);

            if (parentItem == null)
            {
                return null;
            }

            if (IsDraggable(parentItem))
            {
                return parentItem;
            }

            return FindDraggableVisualParent(parentItem);
        }

        private T FindVisualParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
            {
                return null;
            }

            if (parentObject is T parent)
            {
                return parent;
            }

            return FindVisualParent<T>(parentObject);
        }

        #endregion Drag Handling
        #region IPlaylistUpdateListener

        void IPlaylistUpdateListener.Rebuild(IEnumerable<PlaylistSong> songs)
        {
            Rebuild(songs);
        }

        void IPlaylistUpdateListener.AddBack(IEnumerable<PlaylistSong> songs)
        {
            _playlistTree.Add(songs);
        }

        void IPlaylistUpdateListener.InsertSongs(int index, IEnumerable<PlaylistSong> songs)
        {
            _playlistTree.InsertRange(index, songs);
        }

        void IPlaylistUpdateListener.RemoveIndices(IEnumerable<int> indices)
        {
            List<int> indexCopy = new List<int>(indices);

            indexCopy.Sort((a, b) => b.CompareTo(a));

            foreach (int index in indexCopy)
            {
                _playlistTree.PlaylistViewModels.RemoveAt(index);
            }
        }

        void IPlaylistUpdateListener.MarkIndex(int index)
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

        void IPlaylistUpdateListener.MarkRecording(PlaylistRecording playlistRecording)
        {
            PlayingRecording = (from recording in PlayingSong.Children
                                where recording is PlaylistRecordingViewModel recordingVM &&
                                recordingVM.PlaylistRecording == playlistRecording
                                select recording as PlaylistRecordingViewModel).FirstOrDefault();
        }

        void IPlaylistUpdateListener.UnmarkAll()
        {
            PlayingSong = null;
            PlayingRecording = null;
        }

        void IPlaylistUpdateListener.Rearrange(IEnumerable<int> sourceIndices, int targetIndex)
        {
            int preTargetMoves = 0;

            foreach (int sourceIndex in sourceIndices)
            {
                if (sourceIndex < targetIndex)
                {
                    _playlistTree.PlaylistViewModels.Move(sourceIndex - preTargetMoves, targetIndex - 1);
                    ++preTargetMoves;
                }
                else if (sourceIndex == targetIndex)
                {
                    ++targetIndex;
                }
                else // (sourceIndex > targetIndex)
                {
                    _playlistTree.PlaylistViewModels.Move(sourceIndex, targetIndex);
                    ++targetIndex;
                }
            }
        }

        #endregion IPlaylistUpdateListener
    }
}
