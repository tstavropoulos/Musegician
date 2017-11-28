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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileManager fileMan = null;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);

            fileMan = new FileManager();
            fileMan.Initialize();
            
            artistWindow.Rebuild(fileMan.GenerateArtistList());
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MenuOpenClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            CommonOpenFileDialog dialog = new CommonOpenFileDialog()
            {
                Title = "Select Directory to add to Library",
                IsFolderPicker = true,
                InitialDirectory = "C:\\",

                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = "C:\\",
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                fileMan.OpenDirectory(dialog.FileName);
                artistWindow.Rebuild(fileMan.GenerateArtistList());
            }

        }

        private void MenuQuitClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Quitting();
            Application.Current.Shutdown();
        }

        private void Menu_ClearPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            playlistControl.ClearPlaylist();
        }

        private void Menu_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            MessageBox.Show("Not Yet Implemented.");
        }

        private void Menu_SavePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            MessageBox.Show("Not Yet Implemented.");
        }

        private void Menu_RepeatPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Menu_ShufflePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (playlistControl.Shuffle)
            {
                playlistControl.PrepareShuffleList();
            }

        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Quitting();
        }

        private void Quitting()
        {
            playbackPanel.CleanUp();
        }

        public void Request_PlaySong(int songID)
        {
            playbackPanel.PlaySong(fileMan.GetPlayData(songID));
        }

        private void PlaybackPanel_BackClicked()
        {
            playlistControl.PlayPrevious();
        }

        private void PlaybackPanel_NextClicked()
        {
            playlistControl.PlayNext();
        }

        private void PlaybackPanel_PlaybackFinished()
        {
            playlistControl.PlayNext();
        }

        public void Library_Request_PlaySong(int songID)
        {
            Library_Request_AddSong(songID);
            playlistControl.PlayBack();
        }

        private void Library_Request_PlayAlbum(int albumID)
        {
            int firstNewSong = playlistControl.ItemCount;
            Library_Request_AddAlbum(albumID);
            playlistControl.PlayIndex(firstNewSong);
        }

        private void Library_Request_PlayArtist(int artistID)
        {
            int firstNewSong = playlistControl.ItemCount;
            Library_Request_AddArtist(artistID);
            playlistControl.PlayIndex(firstNewSong);
        }

        private void Library_Request_AddSong(int songID)
        {
            playlistControl.AddBack(fileMan.GetSongData(songID));
        }

        private void Library_Request_AddAlbum(int albumID)
        {
            playlistControl.AddBack(fileMan.GetAlbumData(albumID));
        }

        private void Library_Request_AddArtist(int artistID)
        {
            playlistControl.AddBack(fileMan.GetArtistData(artistID));
        }
    }
}
