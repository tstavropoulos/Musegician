using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Musegician.AlbumArtPicker
{
    public class SelectorViewModel : AlbumArtPickerViewModel
    {
        #region Constructor

        public SelectorViewModel(AlbumArtArtDTO data, AlbumViewModel parent)
            : base(parent)
        {
            this.data = data;
        }

        #endregion Constructor
        #region Properties

        public AlbumArtArtDTO data;
        public new AlbumViewModel Parent => base.Parent as AlbumViewModel;

        public bool IsChecked
        {
            get => data.IsChecked;
            set
            {
                if (IsChecked != value)
                {
                    data.IsChecked = value;
                    OnPropertyChanged("IsChecked");
                    Parent.ReevaluateColor();
                }
            }
        }

        private BitmapImage _image = null;
        public BitmapImage AlbumArt
        {
            get
            {
                if (_image == null)
                {
                    _image = FileManager.LoadImage(data.Image);
                }

                return _image;
            }
        }

        #endregion Properties
        #region Presentation Members

        public override bool IsGrayedOut => false;
        public override string Name => data.Name;

        #endregion Presentation Members
    }
}
