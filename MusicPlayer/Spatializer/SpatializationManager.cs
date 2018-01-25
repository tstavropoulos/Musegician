using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Spatializer
{
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
        #region Properties

        private bool _enableSpatializer = false;
        public bool EnableSpatializer
        {
            get { return _enableSpatializer; }
            set
            {
                if (_enableSpatializer != value)
                {
                    _enableSpatializer = value;
                    OnPropertyChanged("EnableSpatializer");
                }
            }
        }

        #endregion Properties
        #region Constructor

        private SpatializationManager()
        {
        }

        #endregion Constructor
        #region Helper Methods

        string GetFilename(IR_Position position)
        {
            return $"Resources\\IRFiles\\{position.ToString()}.txt";

        }

        void Load(IR_Position position)
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
        }

        #endregion Helper Methods
        #region Public Interface

        public float[] GetIRF(AudioChannel speaker, AudioChannel channel)
        {
            Load(PositionL);
            Load(PositionR);
            
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
                    throw new ArgumentException("Unexpected AudioChannel: " + channel);
            }

            switch (speaker)
            {
                case AudioChannel.Left:
                    return dict[PositionL];
                case AudioChannel.Right:
                    return dict[PositionR];
                case AudioChannel.MAX:
                default:
                    throw new ArgumentException("Unexpected AudioChannel: " + channel);
            }
        }

        public IR_Position PositionL { get; set; } = IR_Position.IR_n80;
        public IR_Position PositionR { get; set; } = IR_Position.IR_p80;

        #endregion Public Interface
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
