using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Musegician.Spatializer
{
    /// <summary>
    /// Interaction logic for SpatializerControl.xaml
    /// </summary>
    public partial class SpatializerControl : UserControl
    {
        #region Data

        private readonly ReadOnlyCollection<SpatializerSettingViewModel> _presets;

        #endregion Data
        #region Constructor

        public SpatializerControl()
        {
            InitializeComponent();

            _presets = new ReadOnlyCollection<SpatializerSettingViewModel>(
                (from data in SpatializationManager.Instance.Presets
                 select new SpatializerSettingViewModel(data))
                .ToArray());

            DataContext = this;
        }

        #endregion Constructor
        #region Properties

        public ReadOnlyCollection<SpatializerSettingViewModel> Presets { get { return _presets; } }

        #endregion Properties

        private void SpatializationPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.AddedItems is null))
            {
                if (e.AddedItems.Count == 0)
                {
                    //Callback from updating just name

                    //Do nothing
                }
                else if (e.AddedItems.Count == 1 && e.AddedItems[0] is SpatializerSettingViewModel viewModel)
                {
                    //Callback from new selection
                    if (viewModel.Name != "Custom")
                    {
                        SpatializationManager.Instance.SetPositions(viewModel.Position);
                    }
                }
                else
                {
                    throw new ArgumentException("Unexpected callback argument: " + e.AddedItems);
                }
            }
            else
            {
                throw new ArgumentException("Unexpected callback argument: " + e.AddedItems);
            }
        }
    }
}
