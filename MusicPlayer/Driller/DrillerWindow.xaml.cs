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
using System.Windows.Shapes;
using Musegician.Core;

namespace Musegician.Driller
{
    /// <summary>
    /// Interaction logic for DrillerWindow.xaml
    /// </summary>
    public partial class DrillerWindow : Window
    {
        public DrillerWindow()
        {
            InitializeComponent();

            Loaded += DrillerWindow_Loaded;
            Unloaded += DrillerWindow_Unloaded;
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

            LoadPlaylistPopup.Opened -= PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed -= PlaylistManControl.Popup_Closed;
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Toolbar_RestoreWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PlayerStateChanged(Player.PlayerState newState)
        {
            switch (newState)
            {
                case Player.PlayerState.Playing:
                case Player.PlayerState.Paused:
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
                case Player.PlayerState.NotLoaded:
                case Player.PlayerState.Stopped:
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
    }
}
