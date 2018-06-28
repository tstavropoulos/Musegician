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

namespace Musegician.PrivateTagCleanup
{
    /// <summary>
    /// Interaction logic for Deredundafier.xaml
    /// </summary>
    public partial class PrivateTagCleanup : UserControl
    {
        #region Data

        IPrivateTagCleanupRequestHandler RequestHandler => FileManager.Instance;

        PrivateTagViewTree _viewTree;

        #endregion Data
        #region Constructor

        public PrivateTagCleanup()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _viewTree = new PrivateTagViewTree();
                DataContext = _viewTree;
            }
        }

        #endregion Constructor
        #region Callbacks

        private async void PrivateTag_FindMatches(object sender, RoutedEventArgs e)
        {
            IEnumerable<string> privateTags = 
                await LoadingDialog.LoadingDialog.ReturnBuilder(RequestHandler.GetAllPrivateTagOwners);

            _viewTree.Clear();
            foreach (string owner in privateTags)
            {
                _viewTree.Add(new PrivateTagViewModel(new PrivateTagDTO() { Owner = owner }));
            }
        }

        private async void PrivateTag_CullSelected(object sender, RoutedEventArgs e)
        {
            List<string> owners = new List<string>();

            foreach (PrivateTagViewModel model in _viewTree.ViewModels)
            {
                if (model.IsChecked)
                {
                    owners.Add(model.Name);
                }
            }

            if (owners.Count > 0)
            {
                _viewTree.Clear();
                await LoadingDialog.LoadingDialog.ArgBuilder(RequestHandler.CullPrivateTagsByOwner, owners);
            }
        }

        #endregion Callbacks

    }
}
