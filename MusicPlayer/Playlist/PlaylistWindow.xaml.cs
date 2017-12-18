using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using PlaylistData = MusicPlayer.DataStructures.PlaylistData;

namespace MusicPlayer.Playlist
{

    /// <summary>
    /// Interaction logic for PlaylistWindow.xaml
    /// </summary>
    public partial class PlaylistWindow : Window
    {
        IPlaylistRequestHandler playlistRequestHandler
        {
            get { return FileManager.Instance; }
        }

        ObservableCollection<PlaylistModelView> playlists = new ObservableCollection<PlaylistModelView>();

        private bool saveMode = false;

        public PlaylistWindow()
        {
            //Fake View
            playlists.Add(new PlaylistModelView()
            {
                Name = "My First Playlist",
                SongCount = 10
            });

            playlists.Add(new PlaylistModelView()
            {
                Name = "Another Playlist",
                SongCount = 12
            });

            playlists.Add(new PlaylistModelView()
            {
                Name = "Silly Playlist",
                SongCount = 1
            });

            playlistList.ItemsSource = playlists;
        }

        public PlaylistWindow(bool save)
        {
            InitializeComponent();

            saveMode = save;
            playlistName.Text = PlaylistManager.Instance.PlaylistName;

            savePanel.Visibility = save ? Visibility.Visible : Visibility.Collapsed;
            savePanel.IsEnabled = save;

            ICollection<PlaylistData> playlistData = playlistRequestHandler.GetPlaylistInfo();

            foreach (PlaylistData data in playlistData)
            {
                playlists.Add(new PlaylistModelView()
                {
                    Name = data.title,
                    SongCount = data.count,
                    ID = data.id
                });
            }

            playlistList.ItemsSource = playlists;
        }

        private void PlaylistList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView)
            {
                if (listView.SelectedItem is PlaylistModelView item)
                {
                    e.Handled = true;

                    if (saveMode)
                    {
                        //Overwrite with name
                        SaveAndClose(item.Name);
                    }
                    else
                    {
                        //Load
                        PlaylistManager.Instance.LoadPlaylist(item.ID);
                        PlaylistManager.Instance.PlaylistName = item.Name;
                        Close();
                    }
                }
            }

        }

        private void SaveAndClose(string saveName)
        {
            if (saveName == "")
            {
                MessageBox.Show("You must name the playlist.", "Title Empty");
                return;
            }

            PlaylistManager.Instance.SavePlaylistAs(saveName);
            PlaylistManager.Instance.PlaylistName = saveName;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            SaveAndClose(playlistName.Text);
        }

        private void PlaylistName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                e.Handled = true;

                SaveAndClose(playlistName.Text);
            }

        }

        private void Menu_Append(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (playlistList.SelectedItem is PlaylistModelView item)
                {
                    PlaylistManager.Instance.AppendPlaylist(item.ID);
                }
            }
        }

        private void Menu_Delete(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (playlistList.SelectedItem is PlaylistModelView item)
                {
                    PlaylistManager.Instance.DeletePlaylist(item.ID);

                    playlists.Remove(item);
                }
            }
        }
    }
}
