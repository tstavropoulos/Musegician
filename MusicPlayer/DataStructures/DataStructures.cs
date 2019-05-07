using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

using MusicRecord = Musegician.TagEditor.MusicRecord;
using ID3TagType = Musegician.TagEditor.ID3TagType;

namespace Musegician.DataStructures
{
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
