using System;
using System.Collections.Generic;

namespace MusicPlayer.DataStructures
{
    public class ArtistDTO : DTO
    {
        public ArtistDTO(
            long id,
            string name)
        {
            ID = id;
            Name = name;
        }
    }
}
