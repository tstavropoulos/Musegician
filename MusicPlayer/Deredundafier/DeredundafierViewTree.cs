using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public class DeredundafierViewTree
    {
        #region Data

        ObservableCollection<DeredundafierViewModel> _viewModels =
            new ObservableCollection<DeredundafierViewModel>();

        #endregion Data
        #region Constructor

        public DeredundafierViewTree()
        {

        }

        #endregion Constructor
        #region View Properties

        public ObservableCollection<DeredundafierViewModel> ViewModels
        {
            get { return _viewModels; }
        }

        #endregion View Properties
        #region Data Methods

        public void Clear()
        {
            ViewModels.Clear();
        }

        public void Add(DeredundafierViewModel model)
        {
            ViewModels.Add(model);
        }

        #endregion Data Methods

    }
}
