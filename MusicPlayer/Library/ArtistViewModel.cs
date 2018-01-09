using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public class ArtistViewModel : LibraryViewModel
    {
        #region Data

        readonly ViewMode mode;

        #endregion Data
        #region Constructors

        public ArtistViewModel(ArtistDTO artist, ViewMode mode, bool lazyLoadChildren = true) : 
            base(
                data: artist,
                parent: null,
                lazyLoadChildren: lazyLoadChildren)
        {
            this.mode = mode;
        }

        #endregion Constructors
        #region Properties

        public ArtistDTO _artist
        {
            get { return Data as ArtistDTO; }
        }

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            switch (mode)
            {
                case ViewMode.Classic:
                    {
                        foreach (AlbumDTO albumData in dataManager.GenerateArtistAlbumList(ID, Name))
                        {
                            _artist.Children.Add(albumData);
                            Children.Add(new AlbumViewModel(albumData, this));
                        }
                    }
                    break;
                case ViewMode.Simple:
                    {
                        foreach (SongDTO songData in dataManager.GenerateArtistSongList(ID, Name))
                        {
                            _artist.Children.Add(songData);
                            Children.Add(new SongViewModel(songData, this));
                        }
                    }
                    break;
                case ViewMode.Album:
                case ViewMode.MAX:
                default:
                    throw new Exception("Inappropriate view mode for an ArtistViewModel: " + mode);
            }
        }

        #endregion LoadChildren
    }
}
