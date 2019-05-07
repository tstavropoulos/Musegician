using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Musegician.DataStructures;

namespace Musegician.TagEditor
{
    public class TagMockData
    {
        #region Constructor

        public TagMockData()
        {
            TagData.Add(new TagViewable(@"C:\Test\Music\testfile.wav", MusicRecord.Filename));
            TagData.Add(new TagDataBool(false, MusicRecord.Live) { NewValue = true });
            TagData.Add(new TagDataString("Test Music File", MusicRecord.TrackTitle, ID3TagType.Title));
            TagData.Add(new TagDataString("Test Music File [Live at the Apollo]", MusicRecord.SongTitle));
            TagData.Add(new TagDataString("Classic Albu", MusicRecord.AlbumTitle, ID3TagType.Album) { NewValue = "Classic Album" });
            TagData.Add(new TagDataInt(1, MusicRecord.TrackNumber, ID3TagType.Track) { _newValue = 2 });
            TagData.Add(new TagDataInt(1, MusicRecord.DiscNumber, ID3TagType.Disc));
            TagData.Add(new TagDataInt(1999, MusicRecord.AlbumYear, ID3TagType.Year));
            TagData.Add(new TagDataInt(1999, MusicRecord.AlbumYear, ID3TagType.Year));
            TagData.Add(new TagDataString("Test Artist", MusicRecord.ArtistName, ID3TagType.Performer, 0));
        }

        #endregion Constructor
        #region View Properties

        public List<TagData> TagData { get; } = new List<TagData>();

        #endregion View Properties
    }
}
