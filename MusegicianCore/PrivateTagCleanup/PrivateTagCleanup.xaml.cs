using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Musegician.PrivateTagCleanup
{
    /// <summary>
    /// Interaction logic for Deredundafier.xaml
    /// </summary>
    public partial class PrivateTagCleanup : UserControl
    {
        #region Data

        private IPrivateTagCleanupRequestHandler RequestHandler => FileManager.Instance;

        private PrivateTagViewTree _viewTree;

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

        private void PrivateTag_CullSelected(object sender, RoutedEventArgs e)
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
                LoadingDialog.LoadingDialog.ArgBuilder(RequestHandler.CullPrivateTagsByOwner, owners);
            }
        }

        #endregion Callbacks

    }
}
