using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Equalizer
{
    #region EventArgs

    public class EqualizerChangedArgs : EventArgs
    {
        public int Index { get; set; }

        public EqualizerChangedArgs(int index)
        {
            Index = index;
        }
    }

    public class MeterUpdateArgs : EventArgs
    {
        public (float L, float R) Power { get; set; }
        public int Index { get; set; }
    }

    #endregion EventArgs

    public class EqualizerManager : INotifyPropertyChanged
    {
        #region Data

        private string presetName;
        private ReadOnlyCollection<EqualizerSettingDTO> presets;
        private ReadOnlyCollection<EqualizerFilterData> eqFilterData;
        private bool blockUpdate = false;

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
        #region Events

        public event EventHandler<EqualizerChangedArgs> EqualizerChanged;

        #endregion Events
        #region Constructor

        private EqualizerManager()
        {
            presetName = "Flat";

            presets = new ReadOnlyCollection<EqualizerSettingDTO>(
                new EqualizerSettingDTO[]
                {
                    new EqualizerSettingDTO()
                    {
                        name = "Flat",
                        gain = new float[10]
                    },
                    new EqualizerSettingDTO()
                    {
                        name = "Scooped",
                        gain = new float[10]
                        {
                            4.0f, 3.8f, 3.5f, 2.0f, 0.0f, 0.0f, 2.0f, 3.5f, 3.8f, 4.0f
                        }
                    },
                    new EqualizerSettingDTO()
                    {
                        name = "Scooped (Bass Boost)",
                        gain = new float[10]
                        {
                            4.0f, 3.8f, 3.5f, 2.0f, 0.0f, 0.0f, 1.0f, 1.6f, 1.9f, 2.0f
                        }
                    },
                    new EqualizerSettingDTO()
                    {
                        name = "Custom",
                        gain = new float[10]
                    }
                });

            List<string> channelNames = new List<string>()
            {
                "32",
                "64",
                "125",
                "250",
                "500",
                "1k",
                "2k",
                "4k",
                "8k",
                "16k"
            };

            eqFilterData = new ReadOnlyCollection<EqualizerFilterData>(
                (from name in channelNames
                 select new EqualizerFilterData(name)).ToArray());

            for (int i = 0; i < eqFilterData.Count; i++)
            {
                int index = i;
                eqFilterData[i].PropertyChanged += (s, e) =>
                {
                    if (!blockUpdate && e.PropertyName == "Gain")
                    {
                        BroadcastEqUpdate(index);
                    }
                };
            }

            Player.MusicManager.Instance.MeterUpdate += (s, e) =>
            {
                eqFilterData[e.Index].Power = e.Power;
            };

            EqualizerChanged += Player.MusicManager.Instance.EqualizerUpdated;

        }

        #endregion Constructor
        #region Properties
        #region Properties FilterData

        public ReadOnlyCollection<EqualizerFilterData> EqualizerFilterData { get { return eqFilterData; } }

        #endregion Properties FilterData
        #region Properties Presets

        public string PresetName
        {
            get { return presetName; }
            private set
            {
                if (presetName != value)
                {
                    presetName = value;
                    OnPropertyChanged("PresetName");
                }
            }
        }

        public ReadOnlyCollection<EqualizerSettingDTO> Presets { get { return presets; } }

        #endregion Properties Presets
        #endregion Properties
        #region Data Access

        public float GetGain(int index)
        {
            if (index < 0 || index > eqFilterData.Count)
            {
                throw new ArgumentException("Invalid requested Channel: " + index);
            }

            return eqFilterData[index].Gain;
        }

        public void SetGain(float[] gain)
        {
            if (gain == null)
            {
                throw new ArgumentException("Invalid submitted gain array: Null");
            }

            if (gain.Length != eqFilterData.Count)
            {
                throw new ArgumentException("Invalid submitted gain array length: " + gain.Length);
            }

            blockUpdate = true;

            for (int i = 0; i < gain.Length; i++)
            {
                if (eqFilterData[i].Gain != gain[i])
                {
                    eqFilterData[i].Gain = gain[i];
                }
            }

            blockUpdate = false;

            BroadcastEqUpdate();
        }

        #endregion Data Access
        #region Convenience Data Methods

        public void Reset()
        {
            blockUpdate = true;

            for (int i = 0; i < eqFilterData.Count; i++)
            {
                eqFilterData[i].Gain = 0f;
            }

            blockUpdate = false;

            BroadcastEqUpdate();
        }

        #endregion Convenience Data Methods
        #region Helper Methods

        private void UpdatePresetName()
        {
            foreach (EqualizerSettingDTO data in Presets)
            {
                if (CompareToPreset(data))
                {
                    PresetName = data.name;
                    return;
                }
            }

            PresetName = "Custom";
        }

        private bool CompareToPreset(EqualizerSettingDTO data)
        {
            for (int i = 0; i < eqFilterData.Count; i++)
            {
                if (data.gain[i] != eqFilterData[i].Gain)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateGain(int index, float value)
        {
            if (index < 0 || index >= eqFilterData.Count)
            {
                throw new ArgumentException("Unexpected Gain Index: " + index);
            }

            if (eqFilterData[index].Gain != value)
            {
                eqFilterData[index].Gain = value;
            }
        }

        private void BroadcastEqUpdate(int index)
        {
            if (index < 0 || index >= eqFilterData.Count)
            {
                throw new ArgumentException("Unexpected Gain Index: " + index);
            }

            EqualizerChanged?.Invoke(this, new EqualizerChangedArgs(index));
            UpdatePresetName();
        }

        private void BroadcastEqUpdate()
        {
            EqualizerChanged?.Invoke(this, new EqualizerChangedArgs(-1));
            UpdatePresetName();
        }

        #endregion Helper Methods
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
