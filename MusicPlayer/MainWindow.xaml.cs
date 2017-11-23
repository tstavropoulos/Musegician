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
            Quitting();
            Application.Current.Shutdown();
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Quitting();
        }

        private void Quitting()
        {
            playerPanel.CleanUp();
        }

        public void SongDoubleClicked(int songID)
        {
            playerPanel.PlaySong(fileMan.GetPlayData(songID));
        }
    }
}
