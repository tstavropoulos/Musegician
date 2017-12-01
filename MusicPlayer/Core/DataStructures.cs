using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public string albumArtFilename;
    }

    public struct SongData
    {
        public long songID;
        public long artistID;
        public string songTitle;
    }

    public struct TrackData
    {
        public long trackID;
        public long songID;
        public long albumID;
        public long recordingID;
        public string trackTitle;
        public long trackNumber;
        public double weight;
    }

    public struct RecordingData
    {
        public long recordingID;
        public string filename;
        public bool live;
        public bool valid;
    }

    public struct PlayData
    {
        public string filename;
        public string songTitle;
        public string artistName;
    }

    public abstract class TagData : INotifyPropertyChanged
    {
        public string TagName { get { return TagType.ToString(); } }
        public abstract string CurrentValue { get; }
        public TagEditor.MusicTag TagType;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TagDataBool : TagData
    {
        public override string CurrentValue { get { return _currentValue ? "True" : "False"; } }

        public bool _currentValue;

        private bool _newValue;
        public bool NewValue
        {
            get { return _newValue; }
            set { _newValue = value; OnPropertyChanged("NewValue"); }
        }
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
        public string NewValue
        {
            get { return _NewValue.ToString(); }
            set
            {
                _NewValue = long.Parse(value);
            }
        }
        public long _NewValue { get; set; }
    }
}
