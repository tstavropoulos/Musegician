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

            fileMan = new FileManager();
            fileMan.Initialize();

            artistWindow.Rebuild(fileMan.GenerateArtistList());
        }

        private void MenuOpenClick(object sender, RoutedEventArgs e)
        {
            fileMan.OpenDirectory("C:\\Users\\Trevor Stavropoulos\\Music");
            artistWindow.Rebuild(fileMan.GenerateArtistList());
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
