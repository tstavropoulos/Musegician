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
        public long discNumber;
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
        public string TagName { get { return recordType.ToString(); } }
        public abstract string CurrentValue { get; }
        public TagEditor.MusicRecord recordType;
        public TagEditor.ID3TagType tagType = TagEditor.ID3TagType.NotEditable;
        public int tagTypeIndex = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Pushable
        {
            get { return tagType != TagEditor.ID3TagType.NotEditable; }
        }

        private bool _push = false;
        public bool Push
        {
            get { return TagModified && _push; }
            set
            {
                if (tagType == TagEditor.ID3TagType.NotEditable)
                {
                    return;
                }
                _push = value;
                OnPropertyChanged("Push");
            }
        }

        private bool _ApplyChanges = true;
        public bool ApplyChanges
        {
            get { return TagModified && _ApplyChanges; }
            set
            {
                if (_ApplyChanges != value)
                {
                    _ApplyChanges = value;
                    OnPropertyChanged("ApplyChanges");
                }
            }
        }

        public abstract bool TagModified { get; }

        public abstract void Reset();

    }

    public class TagDataBool : TagData
    {
        public bool _currentValue;
        public override string CurrentValue
        {
            get { return _currentValue ? "True" : "False"; }
        }


        private bool _newValue;
        public bool NewValue
        {
            get { return _newValue; }
            set
            {
                if (_newValue != value)
                {
                    _newValue = value;
                    OnPropertyChanged("NewValue");
                    OnPropertyChanged("TagModified");
                    OnPropertyChanged("ApplyChanges");
                    OnPropertyChanged("Push");
                }
            }
        }

        public override bool TagModified
        {
            get { return _currentValue != _newValue; }
        }

        public override void Reset()
        {
            NewValue = _currentValue;
        }
    }

    public class TagDataString : TagData
    {
        public string _currentValue;
        public override string CurrentValue { get { return _currentValue; } }

        private string _newValue;
        public string NewValue
        {
            get { return _newValue; }
            set
            {
                if (_newValue != value)
                {
                    _newValue = value;
                    OnPropertyChanged("NewValue");
                    OnPropertyChanged("TagModified");
                    OnPropertyChanged("ApplyChanges");
                    OnPropertyChanged("Push");
                }
            }
        }

        public override bool TagModified
        {
            get { return _currentValue != NewValue; }
        }

        public override void Reset()
        {
            NewValue = _currentValue;
        }
    }

    public class TagDataLong : TagData
    {
        public long _currentValue;
        public override string CurrentValue
        {
            get { return _currentValue.ToString(); }
        }


        public long _newValue;
        public long NewLong
        {
            get { return _newValue; }
            set
            {
                if (_newValue != value)
                {
                    _newValue = value;
                    OnPropertyChanged("NewValue");
                    OnPropertyChanged("TagModified");
                    OnPropertyChanged("ApplyChanges");
                    OnPropertyChanged("Push");
                }
            }
        }

        public string NewValue
        {
            get { return _newValue.ToString(); }
            set
            {
                long temp;
                if(value == "")
                {
                    temp = 0;
                }
                else
                {
                    temp = long.Parse(value);
                }

                if (_newValue != temp)
                {
                    _newValue = temp;
                    OnPropertyChanged("NewValue");
                    OnPropertyChanged("TagModified");
                    OnPropertyChanged("ApplyChanges");
                    OnPropertyChanged("Push");
                }
            }
        }

        public override bool TagModified
        {
            get { return _currentValue != _newValue; }
        }

        public override void Reset()
        {
            NewLong = _currentValue;
        }
    }

    public class TagViewable : TagData
    {
        public string _CurrentValue { get; set; }
        public override string CurrentValue
        {
            get { return _CurrentValue.ToString(); }
        }

        public override bool TagModified { get { return false; } }
        public override void Reset() { }

    }
}
