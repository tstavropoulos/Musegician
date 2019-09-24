using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.PrivateTagCleanup
{
    public class PrivateTagViewTree
    {
        #region Constructor

        public PrivateTagViewTree()
        {

        }

        #endregion Constructor
        #region View Properties

        public ObservableCollection<PrivateTagViewModel> ViewModels { get; } =
            new ObservableCollection<PrivateTagViewModel>();

        #endregion View Properties
        #region Data Methods

        public void Clear()
        {
            ViewModels.Clear();
        }

        public void Add(PrivateTagViewModel model)
        {
            ViewModels.Add(model);
        }

        #endregion Data Methods

    }
}
