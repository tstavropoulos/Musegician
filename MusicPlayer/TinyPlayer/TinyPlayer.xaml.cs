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

namespace MusicPlayer.TinyPlayer
{
    /// <summary>
    /// Interaction logic for TinyPlayer.xaml
    /// </summary>
    public partial class TinyPlayer : Window
    {
        public TinyPlayer()
        {
            InitializeComponent();

            Player.MusicManager.Instance.RecordingStarted += Instance_RecordingStarted;
        }

        private void Instance_RecordingStarted(long id)
        {
            albumArt.Source = FileManager.Instance.GetAlbumArtForRecording(id);
        }

        private void Toolbar_RestoreWindow(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();
            Close();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        private void Toolbar_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            Playlist.PlaylistWindow window = new Playlist.PlaylistWindow(false);

            window.Show();
        }
    }
}
