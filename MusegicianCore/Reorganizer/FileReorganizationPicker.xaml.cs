using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Musegician.Reorganizer
{
    /// <summary>
    /// Interaction logic for FileReorganizationPicker.xaml
    /// </summary>
    public partial class FileReorganizationPicker : UserControl
    {
        #region Data

        IFileReorganizerRequestHandler RequestHandler => FileManager.Instance;

        FileReorganizerViewTree _viewTree;

        #endregion Data
        #region Constructor

        public FileReorganizationPicker()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _viewTree = new FileReorganizerViewTree();
                DataContext = _viewTree;
            }
        }

        #endregion Constructor
        #region Callbacks

        private void FileReorganizer_FindMatches(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            IEnumerable<FileReorganizerDTO> newModels =
                RequestHandler.GetReorganizerTargets(_viewTree.NewPath);

            _viewTree.Clear();
            foreach (FileReorganizerDTO data in newModels)
            {
                _viewTree.Add(new FileReorganizerViewModel(data));
            }
        }

        private void FileReorganizer_Apply(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            IEnumerable<FileReorganizerDTO> reorgData = _viewTree.ViewModels
                .Select(x => x.Data)
                .Where(data => data.IsChecked);

            if (reorgData.Count() == 0)
            {
                return;
            }

            RequestHandler.ApplyReorganization(reorgData);

            _viewTree.Clear();
        }

        private void FileReorganizer_ChooseRoot(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select new Music Root Directory",
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.MyComputer,
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _viewTree.NewPath = dialog.SelectedPath;
                FindButton.IsEnabled = true;
                MoveButton.IsEnabled = true;
            }
        }

        private void FileReorganizer_SelectAll(object sender, RoutedEventArgs e)
        {
            foreach (FileReorganizerViewModel viewModel in _viewTree.ViewModels)
            {
                if (viewModel.Data.Possible)
                {
                    viewModel.IsChecked = true;
                }
            }
        }

        private void FileReorganizer_ClearAll(object sender, RoutedEventArgs e)
        {
            foreach (FileReorganizerViewModel viewModel in _viewTree.ViewModels)
            {
                viewModel.IsChecked = false;
            }
        }

        #endregion Callbacks

    }
}
