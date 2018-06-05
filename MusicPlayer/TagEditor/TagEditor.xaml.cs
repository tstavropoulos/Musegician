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
using Musegician.DataStructures;

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
        private IEnumerable<BaseData> Data { get; }
        public IEnumerable<TagData> Tags { get; }

        private ITagRequestHandler RequestHandler => FileManager.Instance;

        public TagEditor(BaseData data)
        {
            InitializeComponent();
            
            Data = new List<BaseData>() { data };

            Tags = RequestHandler.GetTagData(data);

            tagView.ItemsSource = Tags;
        }

        public TagEditor(IEnumerable<BaseData> data)
        {
            InitializeComponent();

            Data = data;
            Tags = RequestHandler.GetTagData(Data.First());

            tagView.ItemsSource = Tags;
        }

        private void Click_Apply(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool ID3Updates = false;
            bool rebuild = false;

            foreach (TagData tag in Tags)
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
                foreach (BaseData data in Data)
                {
                    IEnumerable<string> paths = RequestHandler.GetAffectedFiles(data);

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

                        UpdateTags(file, Tags);

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

            foreach (TagData tag in Tags)
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
                            RequestHandler.UpdateRecord(Data, record, data.NewValue);
                        }
                    }
                    break;
                case MusicRecord.AlbumYear:
                case MusicRecord.TrackNumber:
                case MusicRecord.DiscNumber:
                    {
                        if (tag is TagDataInt data)
                        {
                            RequestHandler.UpdateRecord(Data, record, data.NewInt);
                        }
                    }
                    break;
                case MusicRecord.Live:
                    {
                        if (tag is TagDataBool data)
                        {
                            RequestHandler.UpdateRecord(Data, record, data.NewValue);
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
                                if (tag is TagDataInt data)
                                {
                                    file.Tag.Year = (uint)data.NewInt;
                                }
                            }
                            break;
                        case ID3TagType.Track:
                            {
                                if (tag is TagDataInt data)
                                {
                                    file.Tag.Track = (uint)data.NewInt;
                                }
                            }
                            break;
                        case ID3TagType.Disc:
                            {
                                if (tag is TagDataInt data)
                                {
                                    file.Tag.Disc = (uint)data.NewInt;
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
