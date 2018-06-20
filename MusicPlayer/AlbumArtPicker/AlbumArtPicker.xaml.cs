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

namespace Musegician.AlbumArtPicker
{
    /// <summary>
    /// Interaction logic for Deredundafier.xaml
    /// </summary>
    public partial class AlbumArtPicker : UserControl
    {
        #region Data

        IAlbumArtPickerRequestHandler RequestHandler => FileManager.Instance;

        AlbumArtPickerViewTree _viewTree;

        #endregion Data
        #region Constructor

        public AlbumArtPicker()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _viewTree = new AlbumArtPickerViewTree();
                DataContext = _viewTree;
            }
        }

        #endregion Constructor
        #region Callbacks

        private void ArtPicker_FindMatches(object sender, RoutedEventArgs e)
        {
            IEnumerable<AlbumArtAlbumDTO> newModels =
                RequestHandler.GetAlbumArtMatches(_viewTree.IncludeAll);

            _viewTree.Clear();
            foreach (AlbumArtAlbumDTO data in newModels)
            {
                _viewTree.Add(new AlbumViewModel(data));
            }
        }

        private void ArtPicker_Apply(object sender, RoutedEventArgs e)
        {
            bool changes = false;

            foreach (AlbumViewModel model in _viewTree.ViewModels)
            {
                if (model.ChildrenSelected)
                {
                    foreach (SelectorViewModel selector in model.Children)
                    {
                        if (selector.IsChecked)
                        {
                            model.data.Album.Image = selector.data.Image;
                            changes = true;
                            break;
                        }
                    }

                }
            }

            if (changes)
            {
                _viewTree.Clear();
                RequestHandler.PushChanges();
            }
        }

        #endregion Callbacks

    }
}
