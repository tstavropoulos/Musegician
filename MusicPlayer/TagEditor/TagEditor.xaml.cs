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
using Musegician.DataStructures;
using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician.TagEditor
{
    public enum MusicRecord
    {
        SongTitle = 0,
        ArtistName,
        AlbumTitle,
        AlbumYear,
        TrackNumber,
        Live,
        DiscNumber,
        TrackTitle,
        Filename,
        ArtistWeight,
        AlbumWeight,
        SongWeight,
        TrackWeight,
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
        Disc,
        MAX
    }


    /// <summary>
    /// Interaction logic for TagEditor.xaml
    /// </summary>
    public partial class TagEditor : Window
    {
        private IList<long> ids;
        private LibraryContext context;

        IEnumerable<TagData> tags;

        public IEnumerable<TagData> Tags
        {
            get { return tags; }
        }

        private ITagRequestHandler RequestHandler
        {
            get { return FileManager.Instance; }
        }

        public TagEditor(LibraryContext context, long id)
        {
            InitializeComponent();

            this.context = context;
            ids = new List<long>(new long[] { id });

            tags = RequestHandler.GetTagData(context, id);

            tagView.ItemsSource = Tags;
        }

        public TagEditor(LibraryContext context, IList<long> ids)
        {
            InitializeComponent();

            this.context = context;
            this.ids = ids;

            tags = RequestHandler.GetTagData(context, ids[0]);

            tagView.ItemsSource = Tags;
        }

        private void Click_Apply(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool ID3Updates = false;
            bool rebuild = false;

            foreach (TagData tag in tags)
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
                        case MusicRecord.DiscNumber:
                            rebuild = true;
                            UpdateRecord(tag.recordType, tag);
                            break;
                        case MusicRecord.Filename:
                        case MusicRecord.MAX:
                        default:
                            throw new Exception("Unexpected MusicRecord value: " + tag.recordType);
                    }
                }

                if (tag.Push && tag.Pushable)
                {
                    ID3Updates = true;
                }
            }

            if (ID3Updates)
            {
                foreach (long id in ids)
                {
                    IEnumerable<string> paths = RequestHandler.GetAffectedFiles(context, id);

                    foreach (string path in paths)
                    {
                        TagLib.File file = null;

                        try
                        {
                            file = TagLib.File.Create(path);
                            file.Mode = TagLib.File.AccessMode.Write;
                        }
                        catch (TagLib.UnsupportedFormatException)
                        {
                            Console.WriteLine("UNSUPPORTED FILE: " + path);
                            Console.WriteLine(String.Empty);
                            Console.WriteLine("---------------------------------------");
                            Console.WriteLine(String.Empty);
                            continue;
                        }
                        catch (System.IO.IOException)
                        {
                            file.Mode = TagLib.File.AccessMode.Closed;
                            file.Dispose();
                            Console.WriteLine("FILE IN USE: " + path);
                            Console.WriteLine("SKIPPING");
                            Console.WriteLine(String.Empty);
                            Console.WriteLine("---------------------------------------");
                            Console.WriteLine(String.Empty);
                            continue;
                        }

                        UpdateTags(file, tags);

                        file.Save();
                    }
                }
            }

            Close();

            if (rebuild)
            {
                RequestHandler.PushChanges();
            }
        }

        private void Click_Reset(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            foreach (TagData tag in tags)
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

        private void UpdateRecord(MusicRecord record, TagData tag)
        {
            switch (record)
            {
                case MusicRecord.SongTitle:
                case MusicRecord.ArtistName:
                case MusicRecord.AlbumTitle:
                case MusicRecord.TrackTitle:
                case MusicRecord.Filename:
                    {
                        if (tag is TagDataString data)
                        {
                            RequestHandler.UpdateRecord(context, ids, record, data.NewValue);
                        }
                    }
                    break;
                case MusicRecord.AlbumYear:
                case MusicRecord.TrackNumber:
                case MusicRecord.DiscNumber:
                    {
                        if (tag is TagDataLong data)
                        {
                            RequestHandler.UpdateRecord(context, ids, record, data.NewLong);
                        }
                    }
                    break;
                case MusicRecord.Live:
                    {
                        if (tag is TagDataBool data)
                        {
                            RequestHandler.UpdateRecord(context, ids, record, data.NewValue);
                        }
                    }
                    break;
                case MusicRecord.MAX:
                default:
                    throw new Exception("Invalid MusicRecord for Updating: " + record);
            }
        }

        private void UpdateTags(TagLib.File file, IEnumerable<TagData> tags)
        {
            foreach (TagData tag in tags)
            {
                if (tag.Push && tag.Pushable)
                {
                    switch (tag.tagType)
                    {
                        case ID3TagType.Title:
                            {
                                if (tag is TagDataString data)
                                {
                                    file.Tag.Title = data.NewValue;
                                }
                            }
                            break;
                        case ID3TagType.Performer:
                            {
                                if (tag is TagDataString data)
                                {
                                    file.Tag.Performers = new string[] { data.NewValue };
                                }
                            }
                            break;
                        case ID3TagType.AlbumArtist:
                            {
                                if (tag is TagDataString data)
                                {
                                    file.Tag.AlbumArtists = new string[] { data.NewValue };
                                }
                            }
                            break;
                        case ID3TagType.Album:
                            {
                                if (tag is TagDataString data)
                                {
                                    file.Tag.Album = data.NewValue;
                                }
                            }
                            break;
                        case ID3TagType.Year:
                            {
                                if (tag is TagDataLong data)
                                {
                                    file.Tag.Year = (uint)data.NewLong;
                                }
                            }
                            break;
                        case ID3TagType.Track:
                            {
                                if (tag is TagDataLong data)
                                {
                                    file.Tag.Track = (uint)data.NewLong;
                                }
                            }
                            break;
                        case ID3TagType.Disc:
                            {
                                if (tag is TagDataLong data)
                                {
                                    file.Tag.Disc = (uint)data.NewLong;
                                }
                            }
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
        }
    }
}
