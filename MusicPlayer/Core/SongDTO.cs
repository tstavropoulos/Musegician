using System;
using System.Collections.Generic;

namespace MusicPlayer.DataStructures
{
    public class SongDTO : DTO
    {
        public SongDTO(long songID, string title, bool isHome)
        {
            ID = songID;
            IsHome = isHome;
            Name = title;
            TrackID = -1;
        }

        public SongDTO(long songID, string title)
        {
            ID = songID;
            TrackID = -1;
            IsHome = true;
            Name = title;
        }
        
        /// <summary>
        /// The ID of the track that this context (Artist>Album>Track) represents
        /// </summary>
        public long TrackID { get; set; }

        /// <summary>
        /// Indicates whether the current Song belongs to the Artist that the Album its being visualized under.
        /// </summary>
        public bool IsHome { get; set; }
    }
}
