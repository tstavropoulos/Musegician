using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Spatializer
{
    public class SpatializerLocationsViewModel : INotifyPropertyChanged
    {
        #region Data

        private bool _showLL = false;
        private bool _showLR = false;
        private bool _showRL = false;
        private bool _showRR = false;

        #endregion Data
        #region Constructor

        public SpatializerLocationsViewModel(float left, float top)
        {
            Left = left;
            Top = top;
        }

        #endregion Constructor
        #region Properties

        public float Left { get; }
        public float Top { get; }

        public bool ShowLL
        {
            get => _showLL;
            set
            {
                if (_showLL != value)
                {
                    _showLL = value;
                    OnPropertyChanged("ShowLL");
                }
            }
        }

        public bool ShowLR
        {
            get => _showLR;
            set
            {
                if (_showLR != value)
                {
                    _showLR = value;
                    OnPropertyChanged("ShowLR");
                }
            }
        }

        public bool ShowRL
        {
            get => _showRL;
            set
            {
                if (_showRL != value)
                {
                    _showRL = value;
                    OnPropertyChanged("ShowRL");
                }
            }
        }

        public bool ShowRR
        {
            get => _showRR;
            set
            {
                if (_showRR != value)
                {
                    _showRR = value;
                    OnPropertyChanged("ShowRR");
                }
            }
        }

        #endregion Properties
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
    }
}
