using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using Musegician.Core;

using PlaylistManager = Musegician.Playlist.PlaylistManager;
using PlayerState = Musegician.Player.PlayerState;

namespace Musegician.Driller
{
    /// <summary>
    /// Interaction logic for DrillerWindow.xaml
    /// </summary>
    public partial class DrillerWindow : Window
    {
        PlaylistManager PlaylistMan => PlaylistManager.Instance;

        public DrillerWindow()
        {
            InitializeComponent();

            Loaded += DrillerWindow_Loaded;
            Loaded += TightPlaylist.TightPlaylistControl_Loaded;

            Unloaded += DrillerWindow_Unloaded;
            Unloaded += TightPlaylist.TightPlaylistControl_Unloaded;
        }

        private void DrillerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Player.MusicManager.Instance.PlayerStateChanged += PlayerStateChanged;

            LoadPlaylistPopup.Opened += PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed += PlaylistManControl.Popup_Closed;
        }

        private void DrillerWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Player.MusicManager.Instance.PlayerStateChanged -= PlayerStateChanged;
            Player.MusicManager.Instance.Speed = 1f;

            LoadPlaylistPopup.Opened -= PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed -= PlaylistManControl.Popup_Closed;
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Toolbar_RestoreWindow(object sender, RoutedEventArgs e) => Close();

        private void PlayerStateChanged(PlayerState newState)
        {
            switch (newState)
            {
                case PlayerState.Playing:
                case PlayerState.Paused:
                    {
                        if (Template.FindName("Marquee", this) is MarqueeControl marquee)
                        {
                            if (!marquee.IsStarted)
                            {
                                marquee.Start();
                            }
                        }
                    }
                    break;
                case PlayerState.NotLoaded:
                case PlayerState.Stopped:
                    {
                        if (Template.FindName("Marquee", this) is MarqueeControl marquee)
                        {
                            if (marquee.IsStarted)
                            {
                                marquee.Stop();
                            }
                        }
                    }
                    break;
                default:
                    Console.WriteLine("Unrecognized PlayerState: " + newState);
                    break;
            }
        }

        #region Callbacks

        private void DrillerWindow_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.MainWindow.Show();
        }

        private void Toolbar_SavePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (PlaylistMan.ItemCount == 0)
            {
                MessageBox.Show("Cannot save empty playlist.", "Empty Playlist");
                SavePlaylistButton.IsChecked = false;
                return;
            }

            PlaylistManControl.SaveMode = true;
            LoadPlaylistPopup.IsOpen = true;
        }

        private void Toolbar_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            PlaylistManControl.SaveMode = false;
            LoadPlaylistPopup.IsOpen = true;
        }

        private void Popup_PlaylistClosed(object sender, EventArgs e)
        {
            SavePlaylistButton.IsChecked = false;
            LoadPlaylistButton.IsChecked = false;
        }

        #endregion Callbacks
    }
}
