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

        public override string Name => _album.Year == 0 ? _album.Title : $"{_album.Title} ({_album.Year})";

        private BitmapImage _image = null;
        public BitmapImage AlbumArt
        {
            get
            {
                if (_image == null)
                {
                    _image = FileManager.LoadImage(_album.Thumbnail);
                }

                return _image;
            }
        }

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);

            if (Parent == null)
            {
                //Album View
                foreach (Recording recording in dataManager.GenerateAlbumRecordingList(_album))
                {
                    Children.Add(new SongViewModel(recording, true, this));
                }

            }
            else
            {
                Artist artist = Parent.Data as Artist;
                //Classic View
                foreach (Recording recording in dataManager.GenerateAlbumRecordingList(_album))
                {
                    Children.Add(new SongViewModel(recording, recording.Artist == artist, this));
                }

            }
        }

        #endregion LoadChildren
    }
}
