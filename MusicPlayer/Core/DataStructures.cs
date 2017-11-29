using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.DataStructures
{
    public struct ArtistData
    {
        public long artistID;
        public string artistName;
    }

    public struct AlbumData
    {
        public long albumID;
        public long artistID;
        public string albumTitle;
        public long albumYear;
    }

    public struct SongData
    {
        public long songID;
        public long artistID;
        public string fileName;
        public string songTitle;
        public bool live;
        public bool valid;
    }

    public struct TrackData
    {
        public long trackID;
        public long songID;
        public long albumID;
        public long trackNumber;
    }

    public struct PlayData
    {
        public string fileName;
        public string songTitle;
        public string artistName;
    }

    public struct PlaylistData
    {
        public long songID;
        public string songTitle;
        public string artistName;
    }

    public abstract class TagData
    {
        public string TagName { get { return TagType.ToString(); } }
        public abstract string CurrentValue { get; }
        public TagEditor.MusicTag TagType;
    }

    public class TagDataBool : TagData
    {
        public override string CurrentValue { get { return _CurrentValue ? "True" : "False"; } }
        public bool _CurrentValue { get; set; }
        public bool NewValue { get; set; }
    }

    public class TagDataString : TagData
    {
        public override string CurrentValue { get { return _CurrentValue; } }
        public string _CurrentValue { get; set; }
        public string NewValue { get; set; }
    }

    public class TagDataLong : TagData
    {
        public override string CurrentValue { get { return _CurrentValue.ToString(); } }
        public long _CurrentValue { get; set; }
        public long NewValue { get; set; }
    }
}
