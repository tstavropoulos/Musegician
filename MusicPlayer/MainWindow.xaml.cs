using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Musegician.Database;
using Microsoft.Win32;
using System.Threading.Tasks;

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

        private void Quitting() => Player.MusicManager.Instance.CleanUp();

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) => PlaylistControl.LookupRequest += libraryControl.LookupRequest;
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e) => PlaylistControl.LookupRequest -= libraryControl.LookupRequest;

        private void Library_Request_Edit(IEnumerable<BaseData> data) => new TagEditor.TagEditor(data).Show();

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

        private async void Library_Request_PushTags(IEnumerable<BaseData> data) =>
            await LoadingDialog.LoadingDialog.ArgBuilder(FileMan.PushMusegicianTagsToFile, data);

        #region Menu Callbacks

        private void Menu_ClearPlaylist(object sender, RoutedEventArgs e) => Playlist.PlaylistManager.Instance.ClearPlaylist();
        private void Menu_LoadPlaylist(object sender, RoutedEventArgs e) => playlistToolbar.Toolbar_LoadPlaylist(sender, e);
        private void Menu_SavePlaylist(object sender, RoutedEventArgs e) => playlistToolbar.Toolbar_SavePlaylist(sender, e);

        private void Menu_CondensedView(object sender, RoutedEventArgs e) => SwapToWindow(new TinyPlayer.TinyPlayer());
        private void Menu_MusicDriller(object sender, RoutedEventArgs e) => SwapToWindow(new Driller.DrillerWindow());

        private void Menu_Deredundafier(object sender, RoutedEventArgs e) => new Deredundafier.DeredundafierWindow().Show();
        private void Menu_AlbumArtPicker(object sender, RoutedEventArgs e) => new AlbumArtPicker.AlbumArtPickerWindow().Show();
        private void Menu_PrivateTagCleanup(object sender, RoutedEventArgs e) => new PrivateTagCleanup.PrivateTagCleanupWindow().Show();

        private void Menu_OpenSpatializer(object sender, RoutedEventArgs e) => SpatializerPopup.IsOpen = true;
        private void Menu_CleanupChildlessDBRecords(object sender, RoutedEventArgs e) => FileMan.CleanChildlessRecords();

        private async void Menu_PushID3Tags(object sender, RoutedEventArgs e) =>
            await LoadingDialog.LoadingDialog.ArgBuilder<IEnumerable<BaseData>>(FileMan.PushID3TagsToFile);
        private async void Menu_PushMusegicianTags(object sender, RoutedEventArgs e) =>
            await LoadingDialog.LoadingDialog.ArgBuilder<IEnumerable<BaseData>>(FileMan.PushMusegicianTagsToFile);
        private async void Menu_PushAlbumArt(object sender, RoutedEventArgs e) =>
            await LoadingDialog.LoadingDialog.ArgBuilder<IEnumerable<BaseData>>(FileMan.PushMusegicianAlbumArtToFile);

        private void Menu_QuitClick(object sender, RoutedEventArgs e)
        {
            Quitting();
            Application.Current.Shutdown();
        }

        private async void Menu_OpenClick(object sender, RoutedEventArgs e)
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
                await LoadingDialog.LoadingDialog.ArgBuilder(FileMan.AddDirectoryToLibrary, dialog.FileName);
                libraryControl.Rebuild();
            }
        }


        private async void Menu_CleanupMissingFiles(object sender, RoutedEventArgs e)
        {
            await LoadingDialog.LoadingDialog.VoidBuilder(FileMan.CleanupMissingFiles);
            //Rebuild library to reflect deleted records
            libraryControl.Rebuild();
        }

        private void Menu_ClearLibraryClick(object sender, RoutedEventArgs e)
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

        #endregion Menu Callbacks
        #region Window Chrome Callbacks

        private void CloseClick(object sender, RoutedEventArgs e) => GetWindow(sender).Close();

        private void MaximizeRestoreClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
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

        private void MinimizeClick(object sender, RoutedEventArgs e) => GetWindow(sender).WindowState = WindowState.Minimized;

        #endregion Window Chrome Callbacks
        #region Helper Methods

        private void SwapToWindow(Window window)
        {
            window.Show();
            Hide();
        }

        private Window GetWindow(object sender) => ((sender as FrameworkElement).TemplatedParent as Window);

        #endregion Helper Methods
    }
}
