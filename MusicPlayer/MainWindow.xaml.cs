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
using Microsoft.Win32;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileManager fileMan
        {
            get
            {
                return FileManager.Instance;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
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

            Playlist.PlaylistManager.Instance.ClearPlaylist();
        }

        private void Menu_LoadPlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            Playlist.PlaylistWindow window = new Playlist.PlaylistWindow(false);

            window.Show();
        }

        private void Menu_SavePlaylist(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (Playlist.PlaylistManager.Instance.ItemCount == 0)
            {
                MessageBox.Show("Cannot save empty playlist.", "Empty Playlist");
                return;
            }

            Playlist.PlaylistWindow window = new Playlist.PlaylistWindow(true);

            window.Show();
        }

        private void Quitting()
        {
            Player.MusicManager.Instance.CleanUp();
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

        private void Library_Request_Edit_Art(LibraryContext context, long id)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title = "Select Album Art to add to Library",
                InitialDirectory = "C:\\",
                Multiselect = false,
                Filter = "image files (*.bmp;*.png;*.jpg;*.jpeg)|*.bmp;*.jpg;*.jpeg;*.png"
            };

            bool? val = dialog.ShowDialog();

            if (val.HasValue && val.Value)
            {
                fileMan.SetAlbumArt(id, dialog.FileName);
                libraryControl.Rebuild();
            }
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

        private void Menu_CondensedView(object sender, RoutedEventArgs e)
        {
            Window tinyPlayer = new TinyPlayer.TinyPlayer();

            tinyPlayer.Show();
            Close();
        }
    }
}
