using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Reorganizer
{
    public class FileReorganizerViewTree : INotifyPropertyChanged
    {
        #region Data

        private string newPath = "";

        #endregion Data
        #region Constructor

        public FileReorganizerViewTree()
        {

        }

        #endregion Constructor
        #region View Properties

        public string NewPath
        {
            get => newPath;
            set
            {
                if (newPath != value)
                {
                    newPath = value;
                    OnPropertyChanged("NewPath");
                }
            }
        }

        public ObservableCollection<FileReorganizerViewModel> ViewModels { get; } =
            new ObservableCollection<FileReorganizerViewModel>();

        #endregion View Properties
        #region Data Methods

        public void Clear()
        {
            ViewModels.Clear();
        }

        public void Add(FileReorganizerViewModel model)
        {
            ViewModels.Add(model);
        }

        #endregion Data Methods
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

    }
}
