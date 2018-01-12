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

namespace Musegician.Playlist
{
    /// <summary>
    /// Interaction logic for PlaylistToolBar.xaml
    /// </summary>
    public partial class PlaylistToolBar : UserControl
    {
        PlaylistManager PlaylistMan
        {
            get { return PlaylistManager.Instance; }
        }

        public event RoutedEventHandler TinyViewerPressed
        {
            add
            {
                tinyViewerButton.Click += value;
            }
            remove
            {
                tinyViewerButton.Click -= value;
            }
        }

        #region Constructor

        public PlaylistToolBar()
        {
            InitializeComponent();

            Loaded += PlaylistToolBar_Loaded;
            Unloaded += PlaylistToolBar_Unloaded;
        }

        private void PlaylistToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPlaylistPopup.Opened += PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed += PlaylistManControl.Popup_Closed;
        }

        private void PlaylistToolBar_Unloaded(object sender, RoutedEventArgs e)
        {
            LoadPlaylistPopup.Opened -= PlaylistManControl.Popup_Opened;
            LoadPlaylistPopup.Closed -= PlaylistManControl.Popup_Closed;
        }

        #endregion Constructor
        #region Callbacks

        private void Toolbar_NewPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            PlaylistMan.ClearPlaylist();
        }

        public void Toolbar_SavePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (PlaylistMan.ItemCount == 0)
            {
                MessageBox.Show("Cannot save empty playlist.", "Empty Playlist");
                SavePlaylist.IsChecked = false;
                return;
            }

            PlaylistManControl.SaveMode = true;
            LoadPlaylistPopup.IsOpen = true;
        }

        public void Toolbar_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            PlaylistManControl.SaveMode = false;
            LoadPlaylistPopup.IsOpen = true;
        }

        private void Popup_PlaylistClosed(object sender, EventArgs e)
        {
            SavePlaylist.IsChecked = false;
            LoadPlaylist.IsChecked = false;
        }

        #endregion Callbacks
    }
}
