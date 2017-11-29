using System;
using System.Collections.Generic;
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
using System.Text.RegularExpressions;
using LibraryContext = MusicPlayer.Library.LibraryContext;

namespace MusicPlayer.TagEditor
{
    public enum MusicTag
    {
        SongTitle = 0,
        ArtistName,
        AlbumTitle,
        AlbumYear,
        TrackNumber,
        Live,
        MAX
    }


    /// <summary>
    /// Interaction logic for TagEditor.xaml
    /// </summary>
    public partial class TagEditor : Window
    {
        private FileManager fileManager;
        private long id;
        private LibraryContext context;

        List<DataStructures.TagData> tags;

        public List<DataStructures.TagData> Tags
        {
            get { return tags; }
        }
        
        public TagEditor(LibraryContext context, long id, FileManager fileManager)
        {
            InitializeComponent();

            this.context = context;
            this.id = id;
            this.fileManager = fileManager;

            tags = fileManager.GetTagData(context, id);

            tagView.ItemsSource = Tags;
        }

        private void Click_Apply(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Click_Reset(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
