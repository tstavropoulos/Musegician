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
using LibraryContext = MusicPlayer.Library.LibraryContext;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileManager fileMan = null;

        //public double Volume { get; set; } = 1.0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainWindow_Loaded);

            fileMan = new FileManager();
            fileMan.Initialize();

            libraryControl.Initialize(fileMan);
            libraryControl.Rebuild();
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
                fileMan.AddDirectoryToLibrary(dialog.FileName);
                libraryControl.Rebuild();
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

        public void Request_PlayRecording(long recordingID)
        {
            playbackPanel.PlaySong(fileMan.GetRecordingPlayData(recordingID));
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

        private void Library_Request_Play(LibraryContext context, long id)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                case LibraryContext.Album:
                case LibraryContext.Song:
                case LibraryContext.Recording:
                case LibraryContext.Track:
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

        private void Library_Request_Add(LibraryContext context, long id)
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
                case LibraryContext.Track:
                    {
                        throw new NotImplementedException();
                    }
                case LibraryContext.Recording:
                    {
                        playlistControl.AddBack(fileMan.GetSongDataFromRecordingID(id));
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    break;
            }
        }

        private void Library_Request_Edit(LibraryContext context, long id)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                case LibraryContext.Album:
                case LibraryContext.Song:
                case LibraryContext.Track:
                case LibraryContext.Recording:
                    //Do Nothing
                    break;
                case LibraryContext.MAX:
                default:
                    Console.WriteLine("Unexpected LibraryContext: " + context + ".  Likey error.");
                    return;
            }

            TagEditor.TagEditor tagEditor = new TagEditor.TagEditor(context, id, fileMan);
            tagEditor.Show();
        }

        private void MenuClearLibraryClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            MessageBoxResult response = MessageBox.Show(
                messageBoxText: "Are you sure you want to clear the Music Library?",
                caption: "Clear Library Confirm",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Warning);

            switch (response)
            {
                case MessageBoxResult.Yes:
                    fileMan.DropDB();
                    libraryControl.Rebuild();
                    break;
                case MessageBoxResult.No:
                    //Do nothing
                    break;
                default:
                    Console.WriteLine("Unexpected MessageBoxResult: " + response + ".  Likely Error.");
                    break;
            }
        }
    }
}
