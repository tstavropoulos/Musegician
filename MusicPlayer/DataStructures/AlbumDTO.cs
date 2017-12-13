using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MusicPlayer.DataStructures
{
    public class AlbumDTO : DTO
    {
        public AlbumDTO(
            long albumID,
            string albumTitle,
            BitmapImage albumArt)
        {
            ID = albumID;
            Name = albumTitle;
            AlbumArt = albumArt;
        }

        public BitmapImage AlbumArt { get; set; }

    }
}
