using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Musegician.AlbumArtPicker
{
    public class AlbumViewModel : AlbumArtPickerViewModel
    {
        #region Data

        bool _checked = false;

        public AlbumArtAlbumDTO data;

        #endregion Data
        #region Constructor

        public AlbumViewModel(AlbumArtAlbumDTO data)
            : base(null)
        {
            this.data = data;
            foreach (AlbumArtArtDTO child in data.Children)
            {
                Children.Add(new SelectorViewModel(child, this));
            }
        }

        #endregion Constructor
        #region Presentation Members

        public bool ChildrenSelected
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    OnPropertyChanged("ChildrenSelected");
                }
            }
        }

        public override bool IsGrayedOut => false;
        public override string Name => data.Name;
        
        private BitmapImage _image = null;
        public BitmapImage AlbumArt
        {
            get
            {
                if (_image == null)
                {
                    _image = FileManager.LoadImage(data?.Album?.Image);
                }

                return _image;
            }
        }

        #endregion Presentation Members
        #region Data Methods

        private void UpdateChildrenCheckState()
        {
            foreach (SelectorViewModel selector in Children)
            {
                selector.IsChecked = _checked;
            }
        }

        private bool DetermineCheckState()
        {
            foreach (SelectorViewModel selector in Children)
            {
                if (selector.IsChecked)
                {
                    return true;
                }
            }
            
            return false;
        }

        #endregion Data Methods
        #region Child Methods

        public void ReevaluateColor()
        {
            ChildrenSelected = DetermineCheckState();
        }

        #endregion Child Methods
    }
}
