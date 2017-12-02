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
    public enum MusicRecord
    {
        SongTitle = 0,
        ArtistName,
        AlbumTitle,
        AlbumYear,
        TrackNumber,
        Live,
        TrackTitle,
        Filename,
        MAX
    }

    public enum ID3TagType
    {
        NotEditable = 0,
        Title,
        Performer,
        AlbumArtist,
        Album,
        Year,
        Track,
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
            List<string> paths = null;

            foreach (DataStructures.TagData tag in tags)
            {
                if (tag.ApplyChanges)
                {
                    switch (tag.recordType)
                    {
                        case MusicRecord.SongTitle:
                        case MusicRecord.ArtistName:
                        case MusicRecord.AlbumTitle:
                        case MusicRecord.AlbumYear:
                        case MusicRecord.TrackNumber:
                        case MusicRecord.Live:
                        case MusicRecord.TrackTitle:
                            UpdateRecord(tag.recordType, tag);
                            break;
                        case MusicRecord.Filename:
                        case MusicRecord.MAX:
                        default:
                            {
                                throw new Exception("Unexpected MusicRecord value: " + tag.recordType);
                            }
                    }
                }

                if (tag.Push && tag.Pushable)
                {
                    if (paths == null)
                    {
                        paths = fileManager.GetAffectedFiles(context, id);
                    }


                    switch (tag.tagType)
                    {
                        case ID3TagType.Title:
                        case ID3TagType.Performer:
                        case ID3TagType.AlbumArtist:
                        case ID3TagType.Album:
                        case ID3TagType.Year:
                        case ID3TagType.Track:
                            UpdateTag(tag.tagType, tag, paths);
                            break;
                        case ID3TagType.NotEditable:
                        case ID3TagType.MAX:
                        default:
                            {
                                throw new Exception("Unexpected ID3TagTypes value: " + tag.tagType);
                            }
                    }
                }
            }
            Close();
        }

        private void Click_Reset(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            foreach (DataStructures.TagData tag in tags)
            {
                tag.Reset();
            }
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

        private void UpdateRecord(MusicRecord record, DataStructures.TagData tag)
        {
            throw new NotImplementedException();
        }

        private void UpdateTag(ID3TagType record, DataStructures.TagData tag, List<string> paths)
        {
            TagLib.File file = null;

            foreach (string path in paths)
            {
                try
                {
                    file = TagLib.File.Create(path);
                }
                catch (TagLib.UnsupportedFormatException)
                {
                    Console.WriteLine("UNSUPPORTED FILE: " + path);
                    Console.WriteLine(String.Empty);
                    Console.WriteLine("---------------------------------------");
                    Console.WriteLine(String.Empty);
                    return;
                }


                switch (record)
                {
                    case ID3TagType.Title:
                        break;
                    case ID3TagType.Performer:
                        break;
                    case ID3TagType.AlbumArtist:
                        break;
                    case ID3TagType.Album:
                        break;
                    case ID3TagType.Year:
                        break;
                    case ID3TagType.Track:
                        break;
                    case ID3TagType.NotEditable:
                    case ID3TagType.MAX:
                    default:
                        {
                            throw new Exception("Unexpected ID3TagTypes value: " + tag.tagType);
                        }
                }

                throw new NotImplementedException();
            }
        }
    }
}
