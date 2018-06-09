using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Equalizer
{
    public class EqualizerFilterData : INotifyPropertyChanged
    {
        #region Data

        private float _gain = 0f;
        private (float L, float R) _power = (0f, 0f);
        private string _name = "440";

        #endregion Data
        #region Properties

        public float Gain
        {
            get => _gain;
            set
            {
                if (_gain != value)
                {
                    _gain = value;
                    OnPropertyChanged("Gain");
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public float PowerL => _power.L;
        public float PowerR => _power.R;
        public float PowerAvg => (_power.L + _power.R) / 2f;

        public (float L, float R) Power
        {
            get => _power;
            set
            {
                if (PowerL != value.L ||
                    PowerR != value.R)
                {
                    _power = value;
                    OnPropertyChanged("PowerL");
                    OnPropertyChanged("PowerR");
                    OnPropertyChanged("Power");
                    OnPropertyChanged("PowerAvg");
                }
            }
        }
        #endregion Properties
        #region Constructor

        public EqualizerFilterData(string name) => Name = name;

        #endregion Constructor
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
