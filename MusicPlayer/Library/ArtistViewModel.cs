using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Musegician.Database;

namespace Musegician.Library
{
    public class ArtistViewModel : LibraryViewModel
    {
        #region Data

        readonly ViewMode mode;

        #endregion Data
        #region Constructors

        public ArtistViewModel(Artist artist, ViewMode mode, bool lazyLoadChildren = true) : 
            base(
                data: artist,
                parent: null,
                lazyLoadChildren: lazyLoadChildren)
        {
            this.mode = mode;
        }

        #endregion Constructors
        #region Properties

        public Artist _artist => Data as Artist;

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            switch (mode)
            {
                case ViewMode.Classic:
                    {
                        foreach (Album album in dataManager.GenerateArtistAlbumList(_artist))
                        {
                            Children.Add(new AlbumViewModel(album, this));
                        }
                    }
                    break;
                case ViewMode.Simple:
                    {
                        foreach (Song song in dataManager.GenerateArtistSongList(_artist))
                        {
                            Children.Add(new SongViewModel(song, this));
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
