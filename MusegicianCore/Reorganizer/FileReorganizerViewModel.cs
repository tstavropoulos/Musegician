using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Reorganizer
{
    public class FileReorganizerViewModel : INotifyPropertyChanged
    {
        #region Constructors

        public FileReorganizerViewModel(FileReorganizerDTO data)
        {
            Data = data;
        }

        #endregion Constructors
        #region Properties

        public FileReorganizerDTO Data { get; }

        public string Name => Data.Name;

        #endregion Properties
        #region IsChecked
        public bool IsChecked
        {
            get => Data.IsChecked;
            set
            {
                if (IsChecked != value)
                {
                    Data.IsChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }
        #endregion IsChecked
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
