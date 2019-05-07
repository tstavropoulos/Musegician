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

namespace Musegician.Equalizer
{
    /// <summary>
    /// Interaction logic for EqualizerControl.xaml
    /// </summary>
    public partial class EqualizerControl : UserControl, INotifyPropertyChanged
    {
        #region Constructor

        public EqualizerControl()
        {
            InitializeComponent();

            Presets = new ReadOnlyCollection<EqualizerSettingViewModel>(
                (from data in EqualizerManager.Instance.Presets
                 select new EqualizerSettingViewModel(data))
                .ToArray());

            DataContext = this;
        }

        #endregion Constructor
        #region Properties

        public ReadOnlyCollection<EqualizerSettingViewModel> Presets { get; }

        #endregion Properties
        #region Callbacks

        private void Button_Reset(object sender, RoutedEventArgs e)
        {
            EqualizerManager.Instance.Reset();
        }

        private void EqPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.AddedItems is null))
            {
                if (e.AddedItems.Count == 0)
                {
                    //Callback from updating just name

                    //Do nothing
                }
                else if (e.AddedItems.Count == 1 && e.AddedItems[0] is EqualizerSettingViewModel viewModel)
                {
                    //Callback from new selection
                    if (viewModel.Name != "Custom")
                    {
                        EqualizerManager.Instance.SetGain(viewModel.Gain);
                    }
                }
                else
                {
                    throw new ArgumentException($"Unexpected callback argument: {e.AddedItems}");
                }
            }
            else
            {
                throw new ArgumentException($"Unexpected callback argument: {e.AddedItems}");
            }
        }

        #endregion Callbacks
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
    }
}
