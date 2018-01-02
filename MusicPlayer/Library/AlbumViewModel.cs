using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;
using System.Windows.Media.Imaging;

namespace MusicPlayer.Library
{
    public class AlbumViewModel : LibraryViewModel
    {
        #region Constructors

        public AlbumViewModel(AlbumDTO album, ArtistViewModel artist, bool lazyLoadChildren = true)
            : base(
                  data: album,
                  parent: artist,
                  lazyLoadChildren: lazyLoadChildren)
        {
        }

        #endregion Constructors
        #region Properties

        public AlbumDTO _album
        {
            get { return Data as AlbumDTO; }
        }

        public BitmapImage AlbumArt
        {
            get { return _album.AlbumArt; }
        }

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach (SongDTO songData in dataManager.GenerateAlbumSongList(
                artistID: (Parent != null) ? Parent.ID : -1,
                albumID: ID))
            {
                Data.Children.Add(songData);
                Children.Add(new SongViewModel(songData, this));
            }
        }

        #endregion LoadChildren
    }
}
