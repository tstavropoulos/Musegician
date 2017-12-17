using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    public class Settings : INotifyPropertyChanged
    {
        private double _weightParameter = 0.05;
        public double WeightParameter
        {
            get { return _weightParameter; }
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

        public double LiveWeight { get { return Math.Min(2.0 * WeightParameter, 1.0); } }
        public double StudioWeight { get { return Math.Min(2.0 * (1.0 - WeightParameter), 1.0); } }

        private static object m_lock = new object();
        private static volatile Settings _instance;
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (m_lock)
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

        private Settings() { }

        private int _fontSize = 14;
        public int FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged("FontSize");
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged
    }
}
