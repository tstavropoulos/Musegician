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
            Loaded += TightPlaylist.PlaylistControl_Loaded;

            Unloaded += TinyPlayer_Unloaded;
            Unloaded += TightPlaylist.PlaylistControl_Unloaded;
        }

        private void TinyPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            Player.MusicManager.Instance.RecordingStarted += RecordingStarted;

            LoadPlaylistPopup.Opened += PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed += PlaylistManControl.Popup_Closed;
        }

        private void TinyPlayer_Unloaded(object sender, RoutedEventArgs e)
        {
            Player.MusicManager.Instance.RecordingStarted -= RecordingStarted;

            LoadPlaylistPopup.Opened -= PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed -= PlaylistManControl.Popup_Closed;
        }

        private void RecordingStarted(long id)
        {
            albumArt.Source = FileManager.Instance.GetAlbumArtForRecording(id);
        }

        private void Toolbar_RestoreWindow(object sender, RoutedEventArgs e)
        {
            //Application.Current.MainWindow.Show();
            Close();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        private void TinyPlayer_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.MainWindow.Show();
        }

        //protected override void OnMouseDown(MouseButtonEventArgs e)
        //{
        //    base.OnMouseDown(e);

        //    if (PlaylistOptionsPopup.IsOpen)
        //    {
        //        PlaylistOptionsPopup.IsOpen = false;
        //    }
        //}

        //private void PlaylistOptionsPopup_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    //Block the window from closing the popup if we clicked inside.
        //    e.Handled = true;
        //}
    }
}
