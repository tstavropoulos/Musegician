using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicPlayer.Equalizer
{
    /// <summary>
    /// Interaction logic for EqualizerSlider.xaml
    /// </summary>
    public partial class EqualizerSlider : UserControl, INotifyPropertyChanged
    {
        #region DependencyProperties
        #region Label

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string),
              typeof(EqualizerSlider), new PropertyMetadata(""));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        #endregion Label
        #region Value

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(float),
              typeof(EqualizerSlider), new PropertyMetadata(0f));

        public float Value
        {
            get { return (float)GetValue(ValueProperty); }
            set
            {
                SetValue(ValueProperty, value);
                OnPropertyChanged("ValueString");
            }
        }

        public string ValueString
        {
            get
            {
                return Value.ToString("+0.00;-0.00;0.00");
            }
        }

        #endregion Value
        #region Power

        public static readonly DependencyProperty PowerLProperty =
            DependencyProperty.Register("PowerL", typeof(float),
              typeof(EqualizerSlider), new PropertyMetadata(0f));

        public float PowerL
        {
            get { return (float)GetValue(PowerLProperty); }
            set { SetValue(PowerLProperty, value); }
        }

        public static readonly DependencyProperty PowerRProperty =
            DependencyProperty.Register("PowerR", typeof(float),
              typeof(EqualizerSlider), new PropertyMetadata(0f));

        public float PowerR
        {
            get { return (float)GetValue(PowerRProperty); }
            set { SetValue(PowerRProperty, value); }
        }

        #endregion Power
        #endregion DependencyProperties
        #region Constructor

        public EqualizerSlider()
        {
            InitializeComponent();

            //DataContext = this;
        }

        #endregion Constructor
        #region Callbacks

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                float oldValue = RoundOff(e.OldValue);
                float newValue = RoundOff(e.NewValue);

                if (oldValue == newValue)
                {
                    e.Handled = true;
                    return;
                }

                Value = newValue;
            }
        }

        #endregion Callbacks
        #region Helper Methods

        private static float RoundOff(double value)
        {
            return (float)(Math.Round(value * 4.0, MidpointRounding.ToEven)) / 4f;
        }

        #endregion
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }

    public class FloatPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float val = System.Convert.ToSingle(value);

            if (val == 0.0)
            {
                return new Point(0, 0);
            }

            return new Point(1 / val, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
