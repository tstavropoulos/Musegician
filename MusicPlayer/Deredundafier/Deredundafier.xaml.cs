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

namespace Musegician.Deredundafier
{
    /// <summary>
    /// Interaction logic for Deredundafier.xaml
    /// </summary>
    public partial class Deredundafier : UserControl
    {
        #region Data

        ObservableCollection<DeredundafierViewModel> _viewModels =
            new ObservableCollection<DeredundafierViewModel>();

        DeredundancyMode mode = DeredundancyMode.Song;

        IDeredundancyRequestHandler RequestHandler
        {
            get { return FileManager.Instance; }
        }

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

            DataContext = this;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                var parent = new DeredundafierDTO() { Name = "Test 1 - None Checked" };
                parent.Children.Add(new SelectorDTO() { IsChecked = false });
                parent.Children.Add(new SelectorDTO() { IsChecked = false });

                ViewModels.Add(new PotentialMatchViewModel(parent));
                (ViewModels.Last() as PotentialMatchViewModel).ReevaluateColor();

                parent = new DeredundafierDTO() { Name = "Test 2 - Mixed Checks" };
                var child = new SelectorDTO() { Name = "SubTest 1", IsChecked = true };
                child.Children.Add(new DeredundafierDTO());
                parent.Children.Add(child);

                child = new SelectorDTO() { Name = "SubTest 2", IsChecked = false };
                child.Children.Add(new DeredundafierDTO());
                parent.Children.Add(child);

                ViewModels.Add(new PotentialMatchViewModel(parent) { IsExpanded = true });
                (ViewModels.Last() as PotentialMatchViewModel).ReevaluateColor();

                parent = new DeredundafierDTO() { Name = "Test 3 - All Checked" };
                parent.Children.Add(new SelectorDTO() { IsChecked = true });
                parent.Children.Add(new SelectorDTO() { IsChecked = true });

                ViewModels.Add(new PotentialMatchViewModel(parent));
                (ViewModels.Last() as PotentialMatchViewModel).ReevaluateColor();
            }

        }

        #endregion Constructor
        #region View Properties

        public ObservableCollection<DeredundafierViewModel> ViewModels
        {
            get { return _viewModels; }
        }

        #endregion View Properties
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
                    _viewModels.Clear();
                }
            }
        }

        private void Deredundafier_Calculate(object sender, RoutedEventArgs e)
        {
            _viewModels.Clear();

            IList<DeredundafierDTO> newModels = null;

            switch (mode)
            {
                case DeredundancyMode.Artist:
                    {
                        newModels = RequestHandler.GetArtistTargets();
                    }
                    break;
                case DeredundancyMode.Album:
                    {
                        newModels = RequestHandler.GetAlbumTargets();
                    }
                    break;
                case DeredundancyMode.Song:
                    {
                        newModels = RequestHandler.GetSongTargets();
                    }
                    break;
                case DeredundancyMode.MAX:
                default:
                    throw new Exception("Unexpected DeredundancyMode: " + mode);
            }

            _viewModels.Clear();
            foreach (DeredundafierDTO data in newModels)
            {
                _viewModels.Add(new PotentialMatchViewModel(data));
            }

        }

        private void Deredundafier_Apply(object sender, RoutedEventArgs e)
        {

        }

        #endregion Callbacks
    }
}
