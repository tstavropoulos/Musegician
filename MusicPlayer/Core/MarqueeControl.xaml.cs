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
using System.Windows.Media.Animation;

namespace Musegician.Core
{
    public enum TickerDirection
    {
        East,
        West
    }

    /// <summary>
    /// Shamelessly copied from:
    /// https://koderhack.blogspot.com/2011/05/content-ticker-control-in-wpf.html
    /// </summary>
    public partial class MarqueeControl : ContentControl
    {
        Storyboard _ContentTickerStoryboard = null;
        Canvas _ContentControl = null;
        ContentPresenter _Content = null;

        static MarqueeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MarqueeControl),
                new FrameworkPropertyMetadata(typeof(MarqueeControl)));
        }

        public MarqueeControl()
        {
            Loaded += new RoutedEventHandler(ContentTicker_Loaded);
        }

        public void Start()
        {
            if (_ContentTickerStoryboard != null &&
                !IsStarted)
            {
                UpdateAnimationDetails(_ContentControl.ActualWidth, _Content.ActualWidth);

                _ContentTickerStoryboard.Begin(_ContentControl, true);
                IsStarted = true;
            }
        }

        public void Pause()
        {
            if (IsStarted &&
                !IsPaused &&
                _ContentTickerStoryboard != null)
            {
                _ContentTickerStoryboard.Pause(_ContentControl);
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsPaused &&
                _ContentTickerStoryboard != null)
            {
                _ContentTickerStoryboard.Resume(_ContentControl);
                IsPaused = false;
            }
        }

        public void Stop()
        {
            if (_ContentTickerStoryboard != null &&
                IsStarted)
            {
                _ContentTickerStoryboard.Stop(_ContentControl);
                IsStarted = false;
            }
        }

        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }


        public double Rate
        {
            get => (double)GetValue(RateProperty);
            set => SetValue(RateProperty, value);
        }

        public static readonly DependencyProperty RateProperty =
            DependencyProperty.Register(
                "Rate",
                typeof(double),
                typeof(MarqueeControl),
                new UIPropertyMetadata(60.0));

        public TickerDirection Direction
        {
            get => (TickerDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                "Direction",
                typeof(TickerDirection),
                typeof(MarqueeControl),
                new UIPropertyMetadata(TickerDirection.West));

        void ContentTicker_Loaded(object sender, RoutedEventArgs e)
        {
            _ContentControl = GetTemplateChild("PART_ContentControl") as Canvas;
            if (_ContentControl != null)
            {
                _ContentControl.SizeChanged += new SizeChangedEventHandler(_ContentControl_SizeChanged);
            }

            _Content = GetTemplateChild("PART_Content") as ContentPresenter;
            if (_Content != null)
            {
                _Content.SizeChanged += new SizeChangedEventHandler(_Content_SizeChanged);
            }

            _ContentTickerStoryboard = GetTemplateChild("ContentTickerStoryboard") as Storyboard;

            if (_ContentControl.ActualWidth == 0 && double.IsNaN(_ContentControl.Width))
            {
                _ContentControl.Width = _Content.ActualWidth;
            }

            if (_ContentControl.ActualHeight == 0 && double.IsNaN(_ContentControl.Height))
            {
                _ContentControl.Height = _Content.ActualHeight;
            }

            VerticallyAlignContent(_ContentControl.ActualHeight);

            Start();
        }

        void _Content_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAnimationDetails(_ContentControl.ActualWidth, e.NewSize.Width);
        }

        void _ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VerticallyAlignContent(e.NewSize.Height);
            UpdateAnimationDetails(e.NewSize.Width, _Content.ActualWidth);
        }

        void VerticallyAlignContent(double height)
        {
            double contentHeight = _Content.ActualHeight;
            switch (_Content.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    Canvas.SetTop(_Content, 0);
                    break;
                case VerticalAlignment.Bottom:
                    if (height > contentHeight)
                    {
                        Canvas.SetTop(_Content, height - contentHeight);
                    }

                    break;
                case VerticalAlignment.Center:
                case VerticalAlignment.Stretch:
                    if (height > contentHeight)
                    {
                        Canvas.SetTop(_Content, (height - contentHeight) / 2);
                    }

                    break;
            }
        }

        void UpdateAnimationDetails(double holderLength, double contentLength)
        {
            if (_ContentTickerStoryboard.Children.First() is DoubleAnimation animation)
            {
                bool start = false;
                if (IsStarted)
                {
                    Stop();
                    start = true;
                }

                double from = 0, to = 0, time = 0;
                switch (Direction)
                {
                    case TickerDirection.West:
                        from = holderLength;
                        to = -1 * contentLength;
                        time = from / Rate;
                        break;
                    case TickerDirection.East:
                        from = -1 * contentLength;
                        to = holderLength;
                        time = to / Rate;
                        break;
                }

                animation.From = from;
                animation.To = to;
                TimeSpan newDuration = TimeSpan.FromSeconds(time);
                animation.Duration = new Duration(newDuration);

                if (start)
                {
                    TimeSpan? oldDuration = null;
                    if (animation.Duration.HasTimeSpan)
                    {
                        oldDuration = animation.Duration.TimeSpan;
                    }

                    double basis = (oldDuration.HasValue ? oldDuration.Value.TotalSeconds : 1.0);

                    if (basis <= 1.0)
                    {
                        basis = 1.0;
                    }

                    TimeSpan? currentTime = _ContentTickerStoryboard.GetCurrentTime(_ContentControl);
                    int? iteration = _ContentTickerStoryboard.GetCurrentIteration(_ContentControl);
                    TimeSpan? offset = TimeSpan.FromSeconds(
                        currentTime.HasValue ? (currentTime.Value.TotalSeconds % basis) : 0.0);

                    Start();

                    if (offset.HasValue &&
                        offset.Value != TimeSpan.Zero &&
                        offset.Value < newDuration)
                    {
                        _ContentTickerStoryboard.SeekAlignedToLastTick(_ContentControl, offset.Value, TimeSeekOrigin.BeginTime);
                    }
                }
            }
        }
    }
}
