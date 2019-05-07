using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Spatializer
{
    #region EventArgs

    public class SpatializerChangedArgs : EventArgs
    {
        //public bool Enabled { get; set; }
        //public bool Isolated { get; set; }
        //public IR_Position[,] Positions { get; private set; }

        //public SpatializerChangedArgs(IR_Position[,] positions, bool enabled, bool isolated)
        //{
        //    Positions = new IR_Position[2, 2];
        //    Positions[1, 1] = positions[1, 1];
        //    Positions[1, 2] = positions[1, 2];
        //    Positions[2, 1] = positions[2, 1];
        //    Positions[2, 2] = positions[2, 2];

        //    Enabled = enabled;
        //    Isolated = isolated;
        //}
    }

    #endregion EventArgs
    public enum IR_Position
    {
        IR_0 = 0,
        IR_p5,
        IR_n5,
        IR_p10,
        IR_n10,
        IR_p15,
        IR_n15,
        IR_p20,
        IR_n20,
        IR_p25,
        IR_n25,
        IR_p30,
        IR_n30,
        IR_p35,
        IR_n35,
        IR_p40,
        IR_n40,
        IR_p45,
        IR_n45,
        IR_p55,
        IR_n55,
        IR_p65,
        IR_n65,
        IR_p80,
        IR_n80,
        MAX
    }

    public enum AudioChannel
    {
        Left = 0,
        Right,
        MAX
    }

    public class SpatializationManager : INotifyPropertyChanged
    {
        #region Data

        Dictionary<IR_Position, float[]> leftIRFs = new Dictionary<IR_Position, float[]>();
        Dictionary<IR_Position, float[]> rightIRFs = new Dictionary<IR_Position, float[]>();

        private string presetName;
        private float[] zeroIRF = null;

        #endregion Data
        #region Singleton

        private static object m_lock = new object();
        private static volatile SpatializationManager _instance = null;
        public static SpatializationManager Instance
        {
            set
            {
                lock (m_lock)
                {
                    if (_instance != null)
                    {
                        throw new Exception("Tried to set non-null MusicManager instance");
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
                            _instance = new SpatializationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion Singleton
        #region Events

        public event EventHandler<SpatializerChangedArgs> SpatializerChanged;

        #endregion Events
        #region Properties

        private bool _enableSpatializer = false;
        public bool EnableSpatializer
        {
            get => _enableSpatializer;
            set
            {
                if (_enableSpatializer != value)
                {
                    _enableSpatializer = value;
                    OnPropertyChanged("EnableSpatializer");
                    //SpatializerChanged?.Invoke(this, new SpatializerChangedArgs());
                }
            }
        }

        private bool _isolateChannels = false;
        public bool IsolateChannels
        {
            get => _isolateChannels;
            set
            {
                if (_isolateChannels != value)
                {
                    _isolateChannels = value;
                    OnPropertyChanged("IsolateChannels");
                    SpatializerChanged?.Invoke(this, new SpatializerChangedArgs());
                }
            }
        }

        public ReadOnlyCollection<SpatializerSettingDTO> Presets { get; }

        public string PresetName
        {
            get => presetName;
            private set
            {
                if (presetName != value)
                {
                    presetName = value;
                    OnPropertyChanged("PresetName");
                }
            }
        }

        public IR_Position[,] Positions { get; } = new IR_Position[2, 2];

        #endregion Properties
        #region Constructor

        private SpatializationManager()
        {
            presetName = "Offset";

            Presets = new ReadOnlyCollection<SpatializerSettingDTO>(
                new SpatializerSettingDTO[]
                {
                    new SpatializerSettingDTO()
                    {
                        name = "Offset",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_n45, IR_Position.IR_n45},
                            {IR_Position.IR_p45, IR_Position.IR_p45}
                        }
                    },
                    new SpatializerSettingDTO()
                    {
                        name = "Distant Offset",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_n80, IR_Position.IR_n80},
                            {IR_Position.IR_p80, IR_Position.IR_p80}
                        }
                    },
                    new SpatializerSettingDTO()
                    {
                        name = "Forward",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_0, IR_Position.IR_0},
                            {IR_Position.IR_0, IR_Position.IR_0}
                        }
                    },
                    new SpatializerSettingDTO()
                    {
                        name = "Left Merged",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_n45, IR_Position.IR_n45},
                            {IR_Position.IR_n45, IR_Position.IR_n45}
                        }
                    },
                    new SpatializerSettingDTO()
                    {
                        name = "Left Offset",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_n80, IR_Position.IR_n80},
                            {IR_Position.IR_0, IR_Position.IR_0}
                        }
                    },
                    new SpatializerSettingDTO()
                    {
                        name = "Right Merged",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_p45, IR_Position.IR_p45},
                            {IR_Position.IR_p45, IR_Position.IR_p45}
                        }
                    },
                    new SpatializerSettingDTO()
                    {
                        name = "Right Offset",
                        position = new IR_Position[2,2]
                        {
                            {IR_Position.IR_0, IR_Position.IR_0},
                            {IR_Position.IR_p80, IR_Position.IR_p80}
                        }
                    }
                });


            SpatializerChanged += Player.MusicManager.Instance.SpatializerUpdated;
        }

        #endregion Constructor
        #region Helper Methods

        public void SetPositions(IR_Position[,] positions)
        {
            bool changed = false;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (positions[i, j] != Positions[i, j])
                    {
                        Positions[i, j] = positions[i, j];
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                OnPropertyChanged("Positions");
                SpatializerChanged?.Invoke(this, new SpatializerChangedArgs());
            }
        }

        string GetFilename(IR_Position position) => $"Resources\\IRFiles\\{position.ToString()}.txt";

        void LoadCheck(IR_Position position)
        {
            if (leftIRFs.ContainsKey(position))
            {
                return;
            }

            List<float> leftIRF = new List<float>();
            List<float> rightIRF = new List<float>();

            string filename = GetFilename(position);

            //Read File In
            using (StreamReader reader = new StreamReader(filename))
            {
                string line;
                string[] splitLine;

                while ((line = reader.ReadLine()) != null && (line != ""))
                {
                    splitLine = line.Split(',');
                    leftIRF.Add(float.Parse(splitLine[0]));
                    rightIRF.Add(float.Parse(splitLine[1]));
                }
            }

            //Add to dictionaries
            leftIRFs.Add(position, leftIRF.ToArray());
            rightIRFs.Add(position, rightIRF.ToArray());

            if (zeroIRF == null)
            {
                zeroIRF = new float[leftIRF.Count];
            }
        }

        private void UpdatePresetName()
        {
            foreach (SpatializerSettingDTO data in Presets)
            {
                if (CompareToPreset(data))
                {
                    PresetName = data.name;
                    return;
                }
            }

            PresetName = "Custom";
        }

        private bool CompareToPreset(SpatializerSettingDTO data)
        {

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (Positions[i, j] != data.position[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion Helper Methods
        #region Public Interface

        public float[] GetIRF(AudioChannel speaker, AudioChannel channel)
        {
            LoadCheck(Positions[0,0]);
            LoadCheck(Positions[0,1]);
            LoadCheck(Positions[1,0]);
            LoadCheck(Positions[1,1]);

            if (IsolateChannels && speaker != channel)
            {
                return zeroIRF;
            }

            Dictionary<IR_Position, float[]> dict;

            switch (channel)
            {
                case AudioChannel.Left:
                    dict = leftIRFs;
                    break;

                case AudioChannel.Right:
                    dict = rightIRFs;
                    break;

                default:
                    throw new ArgumentException($"Unexpected AudioChannel: {channel}");
            }

            return dict[Positions[(int)speaker, (int)channel]];
        }

        #endregion Public Interface
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
    }
}
