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
using System.ComponentModel;

using MusicManager = Musegician.Player.MusicManager;
using PlayerState = Musegician.Player.PlayerState;

namespace Musegician.Driller
{
    /// <summary>
    /// Interaction logic for LooperPlaybackPanel.xaml
    /// </summary>
    public partial class LooperPlaybackPanel : UserControl, INotifyPropertyChanged
    {
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

        public LooperPlaybackPanel()
        {
            InitializeComponent();

            Loaded += LooperPlaybackPanel_Loaded;
            Unloaded += LooperPlaybackPanel_Unloaded;
        }

        private void LooperPlaybackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            MusicMan.PlayerStateChanged += PlayerStateChanged;
        }

        private void LooperPlaybackPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            MusicMan.PlayerStateChanged -= PlayerStateChanged;
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

        private void OnLoopbackClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Temp OnLoopbackClick behavior");
            MusicMan.Back();
        }

        private void OnSetStartClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnSetStopClick(object sender, RoutedEventArgs e)
        {

        }

        private void PlayerStateChanged(PlayerState newState)
        {
            State = newState;
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
