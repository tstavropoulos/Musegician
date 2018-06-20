using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.AlbumArtPicker
{
    public class AlbumArtPickerViewTree
    {
        #region Constructor

        public AlbumArtPickerViewTree()
        {

        }

        #endregion Constructor
        #region View Properties

        public bool IncludeAll { get; set; } = false;

        public ObservableCollection<AlbumArtPickerViewModel> ViewModels { get; } =
            new ObservableCollection<AlbumArtPickerViewModel>();

        #endregion View Properties
        #region Data Methods

        public void Clear()
        {
            ViewModels.Clear();
        }

        public void Add(AlbumArtPickerViewModel model)
        {
            ViewModels.Add(model);
        }

        #endregion Data Methods

    }
}
