using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Musegician.Core
{
    public class TwoValueVisualizerSlider : Slider
    {
        #region Dependency Properties

        public static readonly DependencyProperty LeftValueProperty =
            DependencyProperty.RegisterAttached("LeftValue", typeof(float),
                typeof(TwoValueVisualizerSlider), new PropertyMetadata(0f));

        public float LeftValue
        {
            get => (float)GetValue(LeftValueProperty);
            set => SetValue(LeftValueProperty, value);
        }

        public static readonly DependencyProperty RightValueProperty =
            DependencyProperty.RegisterAttached("RightValue", typeof(float),
                typeof(TwoValueVisualizerSlider), new PropertyMetadata(0f));

        public float RightValue
        {
            get => (float)GetValue(RightValueProperty);
            set => SetValue(RightValueProperty, value);
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
