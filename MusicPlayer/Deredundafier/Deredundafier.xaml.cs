using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Musegician.Database;

namespace Musegician.Deredundafier
{
    /// <summary>
    /// Interaction logic for Deredundafier.xaml
    /// </summary>
    public partial class Deredundafier : UserControl
    {
        #region Data

        DeredundancyMode mode = DeredundancyMode.Song;
        IDeredundancyRequestHandler RequestHandler => FileManager.Instance;
        Playlist.IPlaylistTransferRequestHandler PlaylistTransferRequestHandler => FileManager.Instance;

        DeredundafierViewTree _viewTree;

        #endregion Data
        #region Enum

        private enum DeredundancyMode
        {
            Artist = 0,
            Album,
            Song,
            MAX
        }

        #endregion Enum
        #region Constructor

        public Deredundafier()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _viewTree = new DeredundafierViewTree();
                DataContext = _viewTree;
            }
        }

        #endregion Constructor
        #region Callbacks

        private void Deredundafier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((e.Source is TabControl deredundafierView) && (deredundafierView.SelectedItem is TabItem tabItem))
            {
                e.Handled = true;

                DeredundancyMode newMode = DeredundancyMode.MAX;

                switch (tabItem.Header)
                {
                    case "Artist":
                        newMode = DeredundancyMode.Artist;
                        break;
                    case "Album":
                        newMode = DeredundancyMode.Album;
                        break;
                    case "Song":
                        newMode = DeredundancyMode.Song;
                        break;
                    default:
                        throw new Exception("Unexpected tabItem.Header: " + tabItem.Header);
                }

                if (mode != newMode)
                {
                    mode = newMode;
                    if (_viewTree != null)
                    {
                        _viewTree.Clear();
                    }
                }
            }
        }

        private void Deredundafier_Calculate(object sender, RoutedEventArgs e)
        {
            IEnumerable<DeredundafierDTO> newModels = null;

            switch (mode)
            {
                case DeredundancyMode.Artist:
                    {
                        newModels = RequestHandler.GetArtistTargets(_viewTree.DeepSearch);
                    }
                    break;
                case DeredundancyMode.Album:
                    {
                        newModels = RequestHandler.GetAlbumTargets(_viewTree.DeepSearch);
                    }
                    break;
                case DeredundancyMode.Song:
                    {
                        newModels = RequestHandler.GetSongTargets(_viewTree.DeepSearch);
                    }
                    break;
                case DeredundancyMode.MAX:
                default:
                    throw new Exception("Unexpected DeredundancyMode: " + mode);
            }

            _viewTree.Clear();
            foreach (DeredundafierDTO data in newModels)
            {
                _viewTree.Add(new PotentialMatchViewModel(data));
            }

        }

        private void Deredundafier_Apply(object sender, RoutedEventArgs e)
        {
            bool changes = false;

            foreach (PotentialMatchViewModel model in _viewTree.ViewModels)
            {
                if (!model.ChildrenSelected.HasValue || model.ChildrenSelected.Value)
                {
                    List<BaseData> data = new List<BaseData>();

                    foreach (SelectorViewModel selector in model.Children)
                    {
                        if (selector.IsChecked)
                        {
                            data.Add(selector.Data.Data);
                        }
                    }

                    if (data.Count < 2)
                    {
                        Console.WriteLine("Not enough records to merge");
                        continue;
                    }

                    changes = true;

                    switch (mode)
                    {
                        case DeredundancyMode.Artist:
                            RequestHandler.MergeArtists(data);
                            break;
                        case DeredundancyMode.Album:
                            RequestHandler.MergeAlbums(data);
                            break;
                        case DeredundancyMode.Song:
                            RequestHandler.MergeSongs(data);
                            break;
                        case DeredundancyMode.MAX:
                        default:
                            throw new Exception("Unexpected DeredundancyMode: " + mode);
                    }

                }
            }

            if (changes)
            {
                _viewTree.Clear();
                RequestHandler.PushChanges();
            }
        }

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeItem)
            {
                if (treeItem.Header is PassiveViewModel recordingModel)
                {
                    if (!recordingModel.IsSelected)
                    {
                        return;
                    }

                    e.Handled = true;

                    Playlist.PlaylistManager.Instance.PlaylistName = "";
                    Playlist.PlaylistManager.Instance.Rebuild(
                        PlaylistTransferRequestHandler.GetSongData(recordingModel.Data.Data as Recording));
                    Player.MusicManager.Instance.Next();
                }
            }
        }

        #endregion Callbacks

    }
}
