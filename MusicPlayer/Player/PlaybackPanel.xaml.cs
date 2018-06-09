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

namespace Musegician.Player
{
    /// <summary>
    /// Interaction logic for PlaybackPanel.xaml
    /// </summary>
    public partial class PlaybackPanel : UserControl, INotifyPropertyChanged
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

        public PlaybackPanel()
        {
            InitializeComponent();

            Loaded += PlaybackPanel_Loaded;
            Unloaded += PlaybackPanel_Unloaded;
        }

        private void PlaybackPanel_Loaded(object sender, RoutedEventArgs e) => MusicMan.PlayerStateChanged += PlayerStateChanged;
        private void PlaybackPanel_Unloaded(object sender, RoutedEventArgs e) => MusicMan.PlayerStateChanged -= PlayerStateChanged;

        public void OnPlayClick(object sender, RoutedEventArgs e) => MusicMan.PlayPause();
        public void OnStopClick(object sender, RoutedEventArgs e) => MusicMan.Stop();
        public void OnNextClick(object sender, RoutedEventArgs e) => MusicMan.Next();
        public void OnBackClick(object sender, RoutedEventArgs e) => MusicMan.Back();

        private void PlayerStateChanged(PlayerState newState) => State = newState;

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
