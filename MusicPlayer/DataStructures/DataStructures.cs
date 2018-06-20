using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.DataStructures
{
    //public struct ArtistData
    //{
    //    public long artistID;
    //    public string artistName;
    //}

    //public struct AlbumData
    //{
    //    public long albumID;
    //    public string albumTitle;
    //    public long albumYear;
    //}

    //public struct ArtData
    //{
    //    public long albumArtID;
    //    public long albumID;
    //    public byte[] image;
    //}

    //public struct SongData
    //{
    //    public long songID;
    //    public string songTitle;
    //}

    //public struct TrackData
    //{
    //    public long trackID;
    //    public long albumID;
    //    public long recordingID;
    //    public string trackTitle;
    //    public long trackNumber;
    //    public long discNumber;
    //    public double weight;
    //}

    //public struct RecordingData
    //{
    //    public static RecordingData Invalid
    //    {
    //        get
    //        {
    //            return new RecordingData()
    //            {
    //                recordingID = -1,
    //                artistID = -1,
    //                songID = -1,
    //                filename = "",
    //                live = false,
    //                valid = false
    //            };
    //        }
    //    }

    //    public bool RecordFound()
    //    {
    //        return recordingID != -1;
    //    }

    //    public long recordingID;
    //    public long artistID;
    //    public long songID;
    //    public string filename;
    //    public bool live;
    //    public bool valid;
    //}

    public struct PlayData
    {
        public string songTitle;
        public string artistName;
        public Recording recording;
    }

    public abstract class TagData : INotifyPropertyChanged
    {
        public string TagName => recordType.ToString();
        public abstract string CurrentValue { get; }
        public TagEditor.MusicRecord recordType;
        public TagEditor.ID3TagType tagType = TagEditor.ID3TagType.NotEditable;
        public int tagTypeIndex = -1;

        public bool Pushable => tagType != TagEditor.ID3TagType.NotEditable;

        private bool _push = false;
        public bool Push
        {
            get => TagModified && _push;
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
            get => TagModified && _ApplyChanges;
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

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
    }

    public class TagDataBool : TagData
    {
        public bool _currentValue;
        public override string CurrentValue => _currentValue ? "True" : "False";


        private bool _newValue;
        public bool NewValue
        {
            get => _newValue;
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

        public override bool TagModified => _currentValue != _newValue;

        public override void Reset() => NewValue = _currentValue;
    }

    public class TagDataString : TagData
    {
        public string _currentValue;
        public override string CurrentValue => _currentValue;

        private string _newValue;
        public string NewValue
        {
            get => _newValue;
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

        public override bool TagModified => _currentValue != NewValue;

        public override void Reset() => NewValue = _currentValue;
    }

    public class TagDataInt : TagData
    {
        public int _currentValue;
        public override string CurrentValue => _currentValue.ToString();


        public int _newValue;
        public int NewInt
        {
            get => _newValue;
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
            get => _newValue.ToString();
            set
            {
                int temp;
                if (value == "")
                {
                    temp = 0;
                }
                else
                {
                    temp = int.Parse(value);
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

        public override bool TagModified => _currentValue != _newValue;

        public override void Reset() => NewInt = _currentValue;
    }

    public class TagViewable : TagData
    {
        public string _CurrentValue { get; set; }
        public override string CurrentValue => _CurrentValue.ToString();

        public override bool TagModified => false;
        public override void Reset() { }

    }
}
