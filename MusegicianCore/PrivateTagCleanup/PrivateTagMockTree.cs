using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.PrivateTagCleanup
{
    public class PrivateTagMockTree
    {
        #region Constructor

        public PrivateTagMockTree()
        {
            ViewModels.Add(new PrivateTagViewModel(new PrivateTagDTO { Owner = "Test1/Test" }));
            ViewModels.Add(new PrivateTagViewModel(new PrivateTagDTO { Owner = "Test1/OtherTest" }));
            ViewModels.Add(new PrivateTagViewModel(new PrivateTagDTO { Owner = "Test1/OtherOtherTest" }));
            ViewModels.Add(new PrivateTagViewModel(new PrivateTagDTO { Owner = "Test2/Test2" }));

        }

        #endregion Constructor
        #region View Properties

        public ObservableCollection<PrivateTagViewModel> ViewModels { get; } =
            new ObservableCollection<PrivateTagViewModel>();

        #endregion View Properties
    }
}
