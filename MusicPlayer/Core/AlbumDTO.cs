using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MusicPlayer.DataStructures
{
    public class AlbumDTO
    {
        readonly List<SongDTO> _songs = new List<SongDTO>();
        public IList<SongDTO> Songs
        {
            get { return _songs; }
        }

        public AlbumDTO(
            long albumID,
            string albumTitle,
            List<SongDTO> songs,
            BitmapImage albumArt = null)
        {
            AlbumID = albumID;
            Title = albumTitle;
            _songs = songs;
            AlbumArt = albumArt;
        }

        public string Title { get; set; }
        public long AlbumID { get; set; }

        public BitmapImage AlbumArt { get; set; }

        private double _weight = double.NaN;
        public double Weight
        {
            get
            {
                if (double.IsNaN(_weight))
                {
                    return 1.0;
                }

                return _weight;
            }
            set { _weight = value; }
        }
    }
}
