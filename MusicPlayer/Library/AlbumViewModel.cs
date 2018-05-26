using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using System.Windows.Media.Imaging;

namespace Musegician.Library
{
    public class AlbumViewModel : LibraryViewModel
    {
        #region Constructors

        public AlbumViewModel(Album album, ArtistViewModel artist, bool lazyLoadChildren = true)
            : base(
                  data: album,
                  parent: artist,
                  lazyLoadChildren: lazyLoadChildren)
        {
        }

        #endregion Constructors
        #region Properties

        public Album _album => Data as Album;

        private BitmapImage _image = null;
        public BitmapImage AlbumArt
        {
            get
            {
                if (_image == null)
                {
                    _image = FileManager.LoadImage(_album.Image);
                }

                return _image;
            }
        }

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach (Song song in dataManager.GenerateAlbumSongList(
                artist: Parent?.Data as Artist,
                album: _album))
            {
                Children.Add(new SongViewModel(song, this));
            }
        }

        #endregion LoadChildren
    }
}
