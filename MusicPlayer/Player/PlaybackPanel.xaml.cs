using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;

namespace MusicPlayer.Player
{
    /// <summary>
    /// Interaction logic for PlaybackPanel.xaml
    /// </summary>
    public partial class PlaybackPanel : UserControl, INotifyPropertyChanged
    {
        const string playString = "▶";
        const string pauseString = "⏸";

        private MusicManager MusicMan
        {
            get { return MusicManager.Instance; }
        }

        private PlayerState _state;
        public PlayerState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged("State");
                }
            }
        }

        public PlaybackPanel()
        {
            InitializeComponent();

            MusicMan.PlayerStateChanged += PlayerStateChanged;
            MusicMan.tickUpdate += TickUpdate;
        }

        public void OnPlayClick(object sender, RoutedEventArgs e)
        {
            MusicMan.PlayPause();
        }

        public void OnStopClick(object sender, RoutedEventArgs e)
        {
            MusicMan.Stop();
        }

        public void OnNextClick(object sender, RoutedEventArgs e)
        {
            MusicMan.Next();
        }

        public void OnBackClick(object sender, RoutedEventArgs e)
        {
            MusicMan.Back();
        }

        private void Slider_Drag(object s, DragDeltaEventArgs e)
        {
            MusicMan.DragRequest((long)progressSlider.Value);
        }

        private void TickUpdate(long position)
        {
            progressSlider.Value = position;
        }

        private void PlayerStateChanged(PlayerState newState)
        {
            State = newState;

            //switch (newState)
            //{
            //    case PlayerState.NotLoaded:
            //        playButton.Content = playString;
            //        playButton.Foreground = new SolidColorBrush(Colors.Black);
            //        stopButton.Foreground = new SolidColorBrush(Colors.Black);
            //        break;
            //    case PlayerState.Stopped:
            //        playButton.Content = playString;
            //        playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
            //        stopButton.Foreground = new SolidColorBrush(Colors.Black);
            //        break;
            //    case PlayerState.Playing:
            //        stopButton.Foreground = new SolidColorBrush(Colors.Red);
            //        playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
            //        playButton.Content = pauseString;
            //        break;
            //    case PlayerState.Paused:
            //        stopButton.Foreground = new SolidColorBrush(Colors.Red);
            //        playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
            //        playButton.Content = playString;
            //        break;
            //    case PlayerState.MAX:
            //    default:
            //        Console.WriteLine("Unexpeted PlayerState: " + newState);
            //        return;
            //}
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
