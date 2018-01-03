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

namespace MusicPlayer.Equalizer
{
    /// <summary>
    /// Interaction logic for EqualizerSlider.xaml
    /// </summary>
    public partial class EqualizerSlider : UserControl
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
            set { SetValue(ValueProperty, value); }
        }

        #endregion Value
        #endregion DependencyProperties
        #region Constructor

        public EqualizerSlider()
        {
            InitializeComponent();

            DataContext = this;
        }

        #endregion Constructor
    }
}
