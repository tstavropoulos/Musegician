using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Musegician.Database;

namespace Musegician.AlbumArtPicker
{
    public class AlbumArtAlbumDTO
    {
        public IList<AlbumArtArtDTO> Children { get; } = new List<AlbumArtArtDTO>();

        public string Name { get; set; }
        public Album Album { get; set; }
    }

    public class AlbumArtArtDTO
    {
        public bool IsChecked { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
    }
}
