using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    public class AlbumDTO
    {
        readonly List<SongDTO> _songs = new List<SongDTO>();
        public IList<SongDTO> Songs
        {
            get { return _songs; }
        }

        public AlbumDTO(int id, string title, List<SongDTO> songs)
        {
            AlbumID = id;
            Title = title;
            _songs = songs;
        }

        public string Title { get; set; }
        public int AlbumID { get; set; }
    }
}
