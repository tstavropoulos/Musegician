using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Core
{
    public class TwoValueVisualizerSlider : Slider
    {
        #region Dependency Properties

        public static readonly DependencyProperty LeftValueProperty =
            DependencyProperty.RegisterAttached("LeftValue", typeof(double),
                typeof(TwoValueVisualizerSlider), new PropertyMetadata(0.0));

        public double LeftValue
        {
            get { return (double)GetValue(LeftValueProperty); }
            set { SetValue(LeftValueProperty, value); }
        }

        public static readonly DependencyProperty RightValueProperty =
            DependencyProperty.RegisterAttached("RightValue", typeof(double),
                typeof(TwoValueVisualizerSlider), new PropertyMetadata(0.0));

        public double RightValue
        {
            get { return (double)GetValue(RightValueProperty); }
            set { SetValue(RightValueProperty, value); }
        }

        #endregion Dependency Properties
        #region Constructors

        static TwoValueVisualizerSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TwoValueVisualizerSlider),
                   new FrameworkPropertyMetadata(typeof(TwoValueVisualizerSlider)));
        }

        #endregion Constructors
    }
}
