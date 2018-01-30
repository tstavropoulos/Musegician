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
        private readonly ReadOnlyCollection<SpatializerLocationsViewModel> _interfacePositions;

        #endregion Data
        #region Constructor

        public SpatializerControl()
        {
            InitializeComponent();

            _presets = new ReadOnlyCollection<SpatializerSettingViewModel>(
                (from data in SpatializationManager.Instance.Presets
                 select new SpatializerSettingViewModel(data))
                .ToArray());

            SpatializerLocationsViewModel[] locationViewModels =
                new SpatializerLocationsViewModel[(int)IR_Position.MAX];

            for (IR_Position pos = 0; pos < IR_Position.MAX; pos++)
            {
                (float left, float top) = GetOffsets(pos);
                locationViewModels[(int)pos] = new SpatializerLocationsViewModel(left, top);
            }

            _interfacePositions = new ReadOnlyCollection<SpatializerLocationsViewModel>(locationViewModels);

            DataContext = this;

            Loaded += SpatializerControl_Loaded;
            Unloaded += SpatializerControl_Unloaded;
        }

        private void SpatializerControl_Loaded(object sender, RoutedEventArgs e)
        {
            SpatializationManager.Instance.SpatializerChanged += SpatializerChanged;

            RebuildPositions();
        }

        private void SpatializerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SpatializationManager.Instance.SpatializerChanged -= SpatializerChanged;
        }

        #endregion Constructor
        #region Properties

        public ReadOnlyCollection<SpatializerSettingViewModel> Presets { get => _presets; }
        public ReadOnlyCollection<SpatializerLocationsViewModel> InterfacePositions { get => _interfacePositions; }

        #endregion Properties
        #region Callbacks

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

        private void SpatializerChanged(object sender, SpatializerChangedArgs e)
        {
            RebuildPositions();
        }

        private void RebuildPositions()
        {
            for (IR_Position position = 0; position < IR_Position.MAX; position++)
            {
                InterfacePositions[(int)position].ShowLL =
                    (SpatializationManager.Instance.Positions[0, 0] == position);

                InterfacePositions[(int)position].ShowLR =
                    (!SpatializationManager.Instance.IsolateChannels &&
                    SpatializationManager.Instance.Positions[1, 0] == position);

                InterfacePositions[(int)position].ShowRL =
                    (!SpatializationManager.Instance.IsolateChannels &&
                    SpatializationManager.Instance.Positions[0, 1] == position);

                InterfacePositions[(int)position].ShowRR =
                    (SpatializationManager.Instance.Positions[1, 1] == position);
            }
        }

        #endregion Callbacks
        #region Helper Methods

        private (float, float) GetOffsets(IR_Position position)
        {
            float offset = 8f;
            float radius = 120f;
            (float x, float y) center = (150f, 140f);

            float angle = GetAngle(position);

            float x_coord = center.x + radius * (float)Math.Sin(angle * Math.PI / 180f) - offset;
            float y_coord = center.y - radius * (float)Math.Cos(angle * Math.PI / 180f) - offset;

            return (x_coord, y_coord);
        }

        private float GetAngle(IR_Position position)
        {
            switch (position)
            {
                case IR_Position.IR_0:
                    return 0f;
                case IR_Position.IR_p5:
                    return 5f;
                case IR_Position.IR_n5:
                    return -5f;
                case IR_Position.IR_p10:
                    return 10f;
                case IR_Position.IR_n10:
                    return -10f;
                case IR_Position.IR_p15:
                    return 15f;
                case IR_Position.IR_n15:
                    return -15f;
                case IR_Position.IR_p20:
                    return 20f;
                case IR_Position.IR_n20:
                    return -20f;
                case IR_Position.IR_p25:
                    return 25f;
                case IR_Position.IR_n25:
                    return -25f;
                case IR_Position.IR_p30:
                    return 30f;
                case IR_Position.IR_n30:
                    return -30f;
                case IR_Position.IR_p35:
                    return 35f;
                case IR_Position.IR_n35:
                    return -35f;
                case IR_Position.IR_p40:
                    return 40f;
                case IR_Position.IR_n40:
                    return -40f;
                case IR_Position.IR_p45:
                    return 45f;
                case IR_Position.IR_n45:
                    return -45f;
                case IR_Position.IR_p55:
                    return 55f;
                case IR_Position.IR_n55:
                    return -55f;
                case IR_Position.IR_p65:
                    return 65f;
                case IR_Position.IR_n65:
                    return -65f;
                case IR_Position.IR_p80:
                    return 80f;
                case IR_Position.IR_n80:
                    return -80f;
                case IR_Position.MAX:
                default:
                    throw new Exception("Unexpected IR_Position" + position);
            }
        }

        #endregion Helper Methods
    }
}
