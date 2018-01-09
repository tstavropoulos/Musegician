using System;
using System.Collections.Generic;

namespace Musegician.DataStructures
{
    public class SongDTO : DTO
    {
        public SongDTO(long songID, string titlePrefix, string title, long trackID, bool isHome)
        {
            ID = songID;
            IsHome = isHome;
            Name = titlePrefix + title;
            SearchableName = title;
            TrackID = trackID;
        }

        public SongDTO(long songID, string title)
        {
            ID = songID;
            TrackID = -1;
            IsHome = true;
            Name = title;
            SearchableName = title;
        }
        
        /// <summary>
        /// The ID of the track that this context (Artist>Album>Track) represents
        /// </summary>
        public long TrackID { get; set; }

        /// <summary>
        /// Indicates whether the current Song belongs to the Artist that the Album its being visualized under.
        /// </summary>
        public bool IsHome { get; set; }

        public string SearchableName { get; set; }
    }
}
