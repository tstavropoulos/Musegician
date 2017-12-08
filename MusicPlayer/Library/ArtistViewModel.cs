using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Library
{
    public class ArtistViewModel : LibraryViewModel
    {
        #region Constructors

        public ArtistViewModel(ArtistDTO artist) : 
            base(
                data: artist,
                parent: null,
                lazyLoadChildren: true)
        {
        }

        #endregion // Constructors

        #region Properties

        public ArtistDTO _artist
        {
            get { return Data as ArtistDTO; }
        }

        #endregion // Properties


        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach(AlbumDTO albumData in dataManager.GenerateArtistAlbumList(ID, Name))
            {
                _artist.Children.Add(albumData);
                Children.Add(new AlbumViewModel(albumData, this));
            }
        }

        #endregion // LoadChildren
    }
}
