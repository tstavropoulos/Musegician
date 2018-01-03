using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Equalizer
{
    public class EqualizerManager : INotifyPropertyChanged
    {
        #region Data

        private float[] eqGain;

        #endregion Data
        #region Singleton

        private static object m_lock = new object();
        private static volatile EqualizerManager _instance = null;
        public static EqualizerManager Instance
        {
            set
            {
                lock (m_lock)
                {
                    if (_instance != null)
                    {
                        throw new Exception("Tried to set non-null EqualizerManager instance");
                    }

                    _instance = value;
                }
            }
            get
            {
                if (_instance == null)
                {
                    lock (m_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new EqualizerManager();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion Singleton
        #region Constructor

        private EqualizerManager()
        {
            eqGain = new float[10];
        }

        #endregion Constructor
        #region Properties
        #region Properties Gain

        public float EqCh0
        {
            get { return eqGain[0]; }
            set
            {
                if (eqGain[0] != value)
                {
                    eqGain[0] = value;
                    OnPropertyChanged("EqCh0");
                }
            }
        }

        public float EqCh1
        {
            get { return eqGain[1]; }
            set
            {
                if (eqGain[1] != value)
                {
                    eqGain[1] = value;
                    OnPropertyChanged("EqCh1");
                }
            }
        }

        public float EqCh2
        {
            get { return eqGain[2]; }
            set
            {
                if (eqGain[2] != value)
                {
                    eqGain[2] = value;
                    OnPropertyChanged("EqCh2");
                }
            }
        }

        public float EqCh3
        {
            get { return eqGain[3]; }
            set
            {
                if (eqGain[3] != value)
                {
                    eqGain[3] = value;
                    OnPropertyChanged("EqCh3");
                }
            }
        }

        public float EqCh4
        {
            get { return eqGain[4]; }
            set
            {
                if (eqGain[4] != value)
                {
                    eqGain[4] = value;
                    OnPropertyChanged("EqCh4");
                }
            }
        }

        public float EqCh5
        {
            get { return eqGain[5]; }
            set
            {
                if (eqGain[5] != value)
                {
                    eqGain[5] = value;
                    OnPropertyChanged("EqCh5");
                }
            }
        }

        public float EqCh6
        {
            get { return eqGain[6]; }
            set
            {
                if (eqGain[6] != value)
                {
                    eqGain[6] = value;
                    OnPropertyChanged("EqCh6");
                }
            }
        }

        public float EqCh7
        {
            get { return eqGain[7]; }
            set
            {
                if (eqGain[7] != value)
                {
                    eqGain[7] = value;
                    OnPropertyChanged("EqCh7");
                }
            }
        }

        public float EqCh8
        {
            get { return eqGain[8]; }
            set
            {
                if (eqGain[8] != value)
                {
                    eqGain[8] = value;
                    OnPropertyChanged("EqCh8");
                }
            }
        }

        public float EqCh9
        {
            get { return eqGain[9]; }
            set
            {
                if (eqGain[9] != value)
                {
                    eqGain[9] = value;
                    OnPropertyChanged("EqCh9");
                }
            }
        }

        #endregion Properties Gain
        #region Properties Labels

        public string EqCh0Name { get { return "32"; } }
        public string EqCh1Name { get { return "64"; } }
        public string EqCh2Name { get { return "125"; } }
        public string EqCh3Name { get { return "250"; } }
        public string EqCh4Name { get { return "500"; } }
        public string EqCh5Name { get { return "1k"; } }
        public string EqCh6Name { get { return "2k"; } }
        public string EqCh7Name { get { return "4k"; } }
        public string EqCh8Name { get { return "8k"; } }
        public string EqCh9Name { get { return "16k"; } }

        #endregion Properties Labels
        #endregion Properties
        #region Data Access

        public float GetGain(int channel)
        {
            if (channel < 0 || channel > eqGain.Length)
            {
                throw new ArgumentException("Invalid requested Channel: " + channel);
            }

            return eqGain[channel];
        }

        #endregion Data Access
        #region Convenience Data Methods

        public void Reset()
        {
            eqGain = new float[10];
            OnPropertyChanged("EqCh0");
            OnPropertyChanged("EqCh1");
            OnPropertyChanged("EqCh2");
            OnPropertyChanged("EqCh3");
            OnPropertyChanged("EqCh4");
            OnPropertyChanged("EqCh5");
            OnPropertyChanged("EqCh6");
            OnPropertyChanged("EqCh7");
            OnPropertyChanged("EqCh8");
            OnPropertyChanged("EqCh9");
        }

        #endregion Convenience Data Methods
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
