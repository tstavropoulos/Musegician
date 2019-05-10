using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Musegician.Database;

using MusicRecord = Musegician.TagEditor.MusicRecord;
using ID3TagType = Musegician.TagEditor.ID3TagType;

namespace Musegician.DataStructures
{
    public readonly struct PlayData
    {
        public readonly string songTitle;
        public readonly string artistName;
        public readonly Recording recording;

        public PlayData(string songTitle, string artistName, Recording recording)
        {
            this.songTitle = songTitle;
            this.artistName = artistName;
            this.recording = recording;
        }
    }

    public abstract class TagData : INotifyPropertyChanged
    {
        public string TagName => recordType.ToString();
        public abstract string CurrentValue { get; }
        public MusicRecord recordType;
        public ID3TagType tagType = ID3TagType.NotEditable;
        public int tagTypeIndex = -1;

        public bool Pushable => tagType != ID3TagType.NotEditable;

        private bool _push = true;
        public bool Push
        {
            get => TagModified && _push && Pushable;
            set
            {
                if (tagType == ID3TagType.NotEditable)
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
        private readonly bool _currentValue;
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

        public TagDataBool() { }

        public TagDataBool(
            bool currentValue,
            MusicRecord recordType,
            ID3TagType tagType = ID3TagType.NotEditable)
        {
            _currentValue = currentValue;
            _newValue = currentValue;
            this.recordType = recordType;
            this.tagType = tagType;
        }

        public override void Reset() => NewValue = _currentValue;
    }

    public class TagDataString : TagData
    {
        private readonly string _currentValue;
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

        //public TagDataString() { }

        public TagDataString(
            string currentValue,
            MusicRecord recordType,
            ID3TagType tagType = ID3TagType.NotEditable,
            int tagTypeIndex = -1)
        {
            _currentValue = currentValue;
            _newValue = currentValue;
            this.recordType = recordType;
            this.tagType = tagType;
            this.tagTypeIndex = tagTypeIndex;
        }

        public override void Reset() => NewValue = _currentValue;
    }

    public class TagDataInt : TagData
    {
        private readonly int _currentValue;
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

        public TagDataInt(
            int currentValue,
            MusicRecord recordType,
            ID3TagType tagType = ID3TagType.NotEditable)
        {
            _currentValue = currentValue;
            _newValue = currentValue;
            this.recordType = recordType;
            this.tagType = tagType;
        }

        public override void Reset() => NewInt = _currentValue;
    }

    public class TagDataEnum : TagData
    {
        private readonly int _currentValue;
        public override string CurrentValue => EnumValues[_currentValue];

        public string[] EnumValues { get; }

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
            get => EnumValues[_newValue];
            set
            {
                int temp = _newValue;
                if (value == "")
                {
                    temp = 0;
                }
                else
                {
                    for (int i = 0; i < EnumValues.Length; i++)
                    {
                        if (EnumValues[i] == value)
                        {
                            temp = i;
                            break;
                        }
                    }
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

        public TagDataEnum(
            int currentValue,
            MusicRecord recordType,
            IEnumerable<string> enumValues,
            ID3TagType tagType = ID3TagType.NotEditable)
        {
            _currentValue = currentValue;
            _newValue = currentValue;
            EnumValues = enumValues.ToArray();
            this.recordType = recordType;
            this.tagType = tagType;
        }

        public override void Reset() => NewInt = _currentValue;
    }

    public class TagViewable : TagData
    {
        public override string CurrentValue { get; }

        public override bool TagModified => false;

        public TagViewable(
            string currentValue,
            MusicRecord recordType)
        {
            CurrentValue = currentValue;
            this.recordType = recordType;
        }

        public override void Reset() { }

    }
}
