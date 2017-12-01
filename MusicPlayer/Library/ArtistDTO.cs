using System;
using System.Collections.Generic;

namespace MusicPlayer.Library
{
    public class ArtistDTO
    {
        readonly List<AlbumDTO> _albums = new List<AlbumDTO>();
        public IList<AlbumDTO> Albums
        {
            get { return _albums; }
        }

        public ArtistDTO(long id, string name, List<AlbumDTO> albums)
        {
            ArtistID = id;
            Name = name;
            _albums = albums;
        }

        public string Name { get; set; }
        public long ArtistID { get; set; }

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
