using System;
using System.Collections.Generic;
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

namespace Musegician.Driller
{
    public class BoundsChangedEventArgs : EventArgs
    {
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }

    public class BoundsExceededEventArgs : EventArgs
    {
        public double Value { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }

    /// <summary>
    /// Interaction logic for LoopSlider.xaml.
    /// Modeled after:
    /// http://www.blackwasp.co.uk/WPFPathGeometry.aspx
    /// </summary>
    public partial class LoopSlider : UserControl
    {
        #region Properties

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(double), typeof(LoopSlider), new UIPropertyMetadata(0.0));

        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
            "LowerValue", typeof(double), typeof(LoopSlider),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
            "UpperValue", typeof(double), typeof(LoopSlider),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(LoopSlider),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(LoopSlider), new UIPropertyMetadata(1.0));

        #endregion Properties
        #region Events
        #region Events BoundChanged

        private EventHandler<BoundsChangedEventArgs> _boundsChanged;

        public event EventHandler<BoundsChangedEventArgs> BoundsChanged
        {
            add { _boundsChanged += value; }
            remove { _boundsChanged -= value; }
        }

        #endregion Events BoundsChanged
        #region Events BoundsExceeded

        private EventHandler<BoundsExceededEventArgs> _boundsExceeded;

        public event EventHandler<BoundsExceededEventArgs> BoundsExceeded
        {
            add { _boundsExceeded += value; }
            remove { _boundsExceeded -= value; }
        }

        #endregion Events BoundsExceeded
        #endregion Events

        public LoopSlider()
        {
            InitializeComponent();

            Loaded += LoopSlider_Loaded;
        }

        private void LoopSlider_Loaded(object sender, RoutedEventArgs e)
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;
            //ValueSlider.ValueChanged += ValueSlider_ValueChanged;
        }

        private void ValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue >= LowerValue && e.NewValue <= UpperValue)
            {
                //Do nothing
            }
            else
            {
                _boundsExceeded.Invoke(this, new BoundsExceededEventArgs()
                {
                    Value = e.NewValue,
                    LowerBound = LowerValue,
                    UpperBound = UpperValue
                });
            }
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LowerValue = Math.Min(UpperValue, LowerValue);

            _boundsChanged?.Invoke(this, new BoundsChangedEventArgs()
            {
                LowerBound = LowerValue,
                UpperBound = UpperValue
            });
        }

        private void LowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpperValue = Math.Max(LowerValue, UpperValue);

            _boundsChanged?.Invoke(this, new BoundsChangedEventArgs()
            {
                LowerBound = LowerValue,
                UpperBound = UpperValue
            });
        }
    }
}
