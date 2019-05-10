using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Musegician.Core
{
    public class Settings : INotifyPropertyChanged
    {
        private bool _createMusegicianTags = true;
        public bool CreateMusegicianTags
        {
            get => _createMusegicianTags;
            set
            {
                if (_createMusegicianTags != value)
                {
                    _createMusegicianTags = value;
                    OnPropertyChanged("CreateMusegicianTags");
                }
            }
        }

        private double _liveWeight = 0.05;
        public double LiveWeight
        {
            get => _liveWeight;
            set
            {
                if (_liveWeight != value)
                {
                    _liveWeight = value;
                    OnPropertyChanged("LiveWeight");
                }
            }
        }

        private double _standardWeight = 1.0;
        public double StandardWeight
        {
            get => _standardWeight;
            set
            {
                if (_standardWeight != value)
                {
                    _standardWeight = value;
                    OnPropertyChanged("StandardWeight");
                }
            }
        }

        private double _acousticWeight = 1.0;
        public double AcousticWeight
        {
            get => _acousticWeight;
            set
            {
                if (_acousticWeight != value)
                {
                    _acousticWeight = value;
                    OnPropertyChanged("AcousticWeight");
                }
            }
        }

        private double _alternateWeight = 1.0;
        public double AlternateWeight
        {
            get => _alternateWeight;
            set
            {
                if (_alternateWeight != value)
                {
                    _alternateWeight = value;
                    OnPropertyChanged("AlternateWeight");
                }
            }
        }

        private int _fontSize = 14;
        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged("FontSize");
                }
            }
        }

        private static readonly object _lock = new object();
        private static volatile Settings _instance;

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Settings();
                        }
                    }
                }
                return _instance;
            }
        }

        public double GetDefaultWeight(RecordingType recordingType)
        {
            switch (recordingType)
            {
                case RecordingType.Standard: return StandardWeight;
                case RecordingType.Alternate: return AlternateWeight;
                case RecordingType.Acoustic: return AcousticWeight;
                case RecordingType.Live: return LiveWeight;

                default:
                    throw new ArgumentException($"Unexpected RecordingType: {recordingType}");
            }
        }

        public static long BarUpdatePeriod => 16L;

        private Settings() { }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
