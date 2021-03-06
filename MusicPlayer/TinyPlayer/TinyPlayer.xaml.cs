﻿using System;
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
using Musegician.Database;

namespace Musegician.TinyPlayer
{
    /// <summary>
    /// Interaction logic for TinyPlayer.xaml
    /// </summary>
    public partial class TinyPlayer : Window
    {
        public TinyPlayer()
        {
            InitializeComponent();

            Loaded += TinyPlayer_Loaded;
            Loaded += TightPlaylist.TightPlaylistControl_Loaded;

            Unloaded += TinyPlayer_Unloaded;
            Unloaded += TightPlaylist.TightPlaylistControl_Unloaded;
        }

        private void TinyPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            Player.MusicManager.Instance.RecordingStarted += RecordingStarted;
            Player.MusicManager.Instance.PlayerStateChanged += PlayerStateChanged;

            LoadPlaylistPopup.Opened += PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed += PlaylistManControl.Popup_Closed;
        }

        private void TinyPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            Player.MusicManager.Instance.RecordingStarted -= RecordingStarted;
            Player.MusicManager.Instance.PlayerStateChanged -= PlayerStateChanged;

            LoadPlaylistPopup.Opened -= PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed -= PlaylistManControl.Popup_Closed;
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

        private void RecordingStarted(Recording recording) => albumArt.Source = FileManager.Instance.GetAlbumArtForRecording(recording);

        private void Toolbar_RestoreWindow(object sender, RoutedEventArgs e) => Close();
        private void Window_Deactivated(object sender, EventArgs e) => Topmost = true;
        private void TinyPlayer_Closing(object sender, CancelEventArgs e) => Application.Current.MainWindow.Show();

        private void CloseClick(object sender, RoutedEventArgs e) => Close();
        private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    }
}
