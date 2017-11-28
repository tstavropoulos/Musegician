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
using LibraryContext = MusicPlayer.Library.LibraryControl.LibraryContext;

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

            libraryControl.Rebuild(fileMan.GenerateArtistList());
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
                libraryControl.Rebuild(fileMan.GenerateArtistList());
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

        private void Library_Request_Play(LibraryContext context, int id)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                case LibraryContext.Album:
                case LibraryContext.Song:
                    {
                        int firstNewSong = playlistControl.ItemCount;
                        Library_Request_Add(context, id);
                        playlistControl.PlayIndex(firstNewSong);
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    break;
            }
        }

        private void Library_Request_Add(LibraryContext context, int id)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        playlistControl.AddBack(fileMan.GetArtistData(id));
                    }
                    break;
                case LibraryContext.Album:
                    {
                        playlistControl.AddBack(fileMan.GetAlbumData(id));
                    }
                    break;
                case LibraryContext.Song:
                    {
                        playlistControl.AddBack(fileMan.GetSongData(id));
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    break;
            }
        }

        private void Library_Request_Edit(LibraryContext context, int id)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        Editors.ArtistEditor artistEditor = new Editors.ArtistEditor();
                        artistEditor.Show();
                    }
                    break;
                case LibraryContext.Album:
                    {
                        Editors.ArtistEditor artistEditor = new Editors.ArtistEditor();
                        artistEditor.Show();
                    }
                    break;
                case LibraryContext.Song:
                    {
                        Editors.ArtistEditor artistEditor = new Editors.ArtistEditor();
                        artistEditor.Show();
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    break;
            }
        }
    }
}
