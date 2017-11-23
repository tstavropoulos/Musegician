using System;
using System.Collections.Generic;

namespace MusicPlayer
{
    public class ArtistDTO
    {
        readonly List<SongDTO> _songs = new List<SongDTO>();
        public IList<SongDTO> Songs
        {
            get { return _songs; }
        }

        public ArtistDTO(int id, string name, List<SongDTO> songs)
        {
            ArtistID = id;
            Name = name;
            _songs = songs;
        }

        public string Name { get; set; }
        public int ArtistID { get; set; }
    }
}
