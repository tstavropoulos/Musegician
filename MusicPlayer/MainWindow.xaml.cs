using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Musegician.Database;
using Microsoft.Win32;

namespace Musegician
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileManager FileMan => FileManager.Instance;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
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
                FileMan.AddDirectoryToLibrary(dialog.FileName);
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

        private void Menu_LoadPlaylist(object sender, RoutedEventArgs e) => playlistToolbar.Toolbar_LoadPlaylist(sender, e);
        private void Menu_SavePlaylist(object sender, RoutedEventArgs e) => playlistToolbar.Toolbar_SavePlaylist(sender, e);
        private void Quitting() => Player.MusicManager.Instance.CleanUp();

        private void Library_Request_Edit(IEnumerable<BaseData> data)
        {
            TagEditor.TagEditor tagEditor = new TagEditor.TagEditor(data);
            tagEditor.Show();
        }

        private void Library_Request_Edit_Art(Album data)
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
                FileMan.SetAlbumArt(data as Album, dialog.FileName);
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
                    FileMan.DropDB();
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
            Hide();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            window.Close();
        }

        private void MaximizeRestoreClick(object sender, RoutedEventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                window.WindowState = WindowState.Normal;
            }
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            Window window = ((FrameworkElement)sender).TemplatedParent as Window;
            window.WindowState = WindowState.Minimized;
        }

        private void Menu_Deredundafier(object sender, RoutedEventArgs e)
        {
            Window deredundafier = new Deredundafier.DeredundafierWindow();
            deredundafier.Show();
        }

        private void Menu_AlbumArtPicker(object sender, RoutedEventArgs e)
        {
            Window albumArtPicker = new AlbumArtPicker.AlbumArtPickerWindow();
            albumArtPicker.Show();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) => PlaylistControl.LookupRequest += libraryControl.LookupRequest;
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e) => PlaylistControl.LookupRequest -= libraryControl.LookupRequest;
        private void Menu_OpenSpatializer(object sender, RoutedEventArgs e) => SpatializerPopup.IsOpen = true;

        private void Menu_MusicDriller(object sender, RoutedEventArgs e)
        {
            Window musicDriller = new Driller.DrillerWindow();

            musicDriller.Show();
            Hide();
        }

        private void MenuPushMusegicianTags(object sender, RoutedEventArgs e)
        {
            FileMan.PushMusegicianTagsToFiles();
        }

    }
}
