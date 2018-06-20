using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

using MusicManager = Musegician.Player.MusicManager;
using PlayerState = Musegician.Player.PlayerState;

namespace Musegician.Driller
{
    /// <summary>
    /// Interaction logic for LooperPlaybackPanel.xaml
    /// </summary>
    public partial class LooperPlaybackPanel : UserControl, INotifyPropertyChanged, ILooperUpdater
    {
        private MusicManager MusicMan => MusicManager.Instance;

        private PlayerState _state;
        public PlayerState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        public LooperPlaybackPanel()
        {
            InitializeComponent();

            Loaded += LooperPlaybackPanel_Loaded;
            Unloaded += LooperPlaybackPanel_Unloaded;
        }

        private void LooperPlaybackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            MusicMan.PlayerStateChanged += PlayerStateChanged;
            MusicMan.SetLooperUpdater(this);
        }

        private void LooperPlaybackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            MusicMan.PlayerStateChanged -= PlayerStateChanged;
            MusicMan.RemoveLooperUpdater(this);
        }

        public void OnPlayClick(object sender, RoutedEventArgs e) => MusicMan.PlayPause();
        public void OnStopClick(object sender, RoutedEventArgs e) => MusicMan.Stop();
        public void OnNextClick(object sender, RoutedEventArgs e) => MusicMan.Next();
        public void OnBackClick(object sender, RoutedEventArgs e) => MusicMan.Back();

        private void OnLoopbackClick(object sender, RoutedEventArgs e) => MusicMan.Restart();
        private void OnSetStartClick(object sender, RoutedEventArgs e) => progressSlider.LowerValue = progressSlider.Value;
        private void OnSetStopClick(object sender, RoutedEventArgs e) => progressSlider.UpperValue = progressSlider.Value;

        private void PlayerStateChanged(PlayerState newState) => State = newState;

        private void progressSlider_BoundsChanged(object sender, BoundsChangedEventArgs e)
        {
            MusicMan.StartPosition = e.LowerBound;
            MusicMan.EndPosition = e.UpperBound;
        }

        private void progressSlider_BoundsExceeded(object sender, BoundsExceededEventArgs e) { }

        #region ILooperUpdater

        double ILooperUpdater.GetStartPosition() => progressSlider.LowerValue;
        double ILooperUpdater.GetEndPosition() => progressSlider.UpperValue;

        void ILooperUpdater.ResetBounds()
        {
            progressSlider.LowerValue = progressSlider.Minimum;
            progressSlider.UpperValue = progressSlider.Maximum;
        }

        #endregion ILooperUpdater
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
    }
}
