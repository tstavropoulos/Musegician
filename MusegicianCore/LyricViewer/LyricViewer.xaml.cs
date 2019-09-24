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
using Musegician.Database;

namespace Musegician.LyricViewer
{
    /// <summary>
    /// Interaction logic for TagEditor.xaml
    /// </summary>
    public partial class LyricViewer : Window
    {
        private Recording Recording { get; }
        private ILyricsRequestHandler Handler => FileManager.Instance;

        public LyricViewer(Recording recording)
        {
            InitializeComponent();

            Recording = recording;

            lyricBox.Text = Handler.GetRecordingLyrics(Recording);
        }

        private void Click_Apply(object sender, RoutedEventArgs e)
        {
            Handler.UpdateRecordingLyrics(Recording, lyricBox.Text);
        }

        private void Click_Reset(object sender, RoutedEventArgs e)
        {
            lyricBox.Text = Handler.GetRecordingLyrics(Recording);
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
