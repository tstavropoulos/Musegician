using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public class DeredundafierMockTree
    {
        #region Data

        ObservableCollection<DeredundafierViewModel> _viewModels =
            new ObservableCollection<DeredundafierViewModel>();

        #endregion Data
        #region Constructor

        public DeredundafierMockTree()
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

        #endregion Constructor
        #region View Properties

        public ObservableCollection<DeredundafierViewModel> ViewModels
        {
            get { return _viewModels; }
        }

        #endregion View Properties
    }
}
