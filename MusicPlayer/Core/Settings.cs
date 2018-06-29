using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician
{
    public class Settings : INotifyPropertyChanged
    {
        private double _weightParameter = 0.05;
        public double WeightParameter
        {
            get => _weightParameter;
            set
            {
                if (_weightParameter != value)
                {
                    _weightParameter = value;
                    OnPropertyChanged("WeightParameter");
                    OnPropertyChanged("LiveWeight");
                    OnPropertyChanged("StudioWeight");
                }

            }
        }

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

        public double LiveWeight => Math.Min(2.0 * WeightParameter, 1.0);
        public double StudioWeight => Math.Min(2.0 * (1.0 - WeightParameter), 1.0);

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
