using Musegician.DataStructures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Musegician.Playlist
{
    /// <summary>
    /// Interaction logic for PlaylistManagementControl.xaml
    /// </summary>
    public partial class PlaylistManagementControl : UserControl
    {
        IPlaylistRequestHandler RequestHandler
        {
            get { return FileManager.Instance; }
        }

        ObservableCollection<PlaylistModelView> playlists = new ObservableCollection<PlaylistModelView>();

        private bool saveMode = false;

        private Popup myPopup;

        public bool SaveMode
        {
            get { return saveMode; }
            set
            {
                saveMode = value;

                savePanel.Visibility = saveMode ? Visibility.Visible : Visibility.Collapsed;
                savePanel.IsEnabled = saveMode;
            }
        }

        public PlaylistManagementControl()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
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
            else
            {
                playlistList.ItemsSource = playlists;
            }
        }

        public void Popup_Opened(object sender, EventArgs e)
        {
            myPopup = sender as Popup;

            playlistName.Text = PlaylistManager.Instance.PlaylistName;

            playlists.Clear();

            foreach (var (title, count) in RequestHandler.GetPlaylistInfo())
            {
                playlists.Add(new PlaylistModelView()
                {
                    Name = title,
                    SongCount = count
                });
            }
        }

        public void Popup_Closed(object sender, EventArgs e)
        {
            myPopup = null;
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
                        PlaylistManager.Instance.TryLoadPlaylist(item.Name);
                        PlaylistManager.Instance.PlaylistName = item.Name;
                        Close();
                    }
                }
            }

        }

        private void Close()
        {
            myPopup.IsOpen = false;
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
                    PlaylistManager.Instance.AppendPlaylist(item.Name);
                }
            }
        }

        private void Menu_Delete(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (playlistList.SelectedItem is PlaylistModelView item)
                {
                    PlaylistManager.Instance.DeletePlaylist(item.Name);

                    playlists.Remove(item);
                }
            }
        }

    }
}
