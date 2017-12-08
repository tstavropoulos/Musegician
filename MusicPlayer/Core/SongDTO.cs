using System;
using System.Collections.Generic;

namespace MusicPlayer.DataStructures
{
    public class SongDTO
    {
        readonly List<RecordingDTO> _recordings = new List<RecordingDTO>();
        public IList<RecordingDTO> Recordings
        {
            get { return _recordings; }
        }

        public SongDTO(long songID, long trackID, string title, bool isHome, List<RecordingDTO> recordings)
        {
            SongID = songID;
            TrackID = trackID;
            IsHome = isHome;
            Title = title;
            _recordings = recordings;
        }

        public SongDTO(long songID, string title, List<RecordingDTO> recordings)
        {
            SongID = songID;
            TrackID = -1;
            IsHome = true;
            Title = title;
            _recordings = recordings;
        }

        public string Title { get; set; }
        public long SongID { get; set; }
        /// <summary>
        /// The ID of the track that this context (Artist>Album>Track) represents
        /// </summary>
        public long TrackID { get; set; }

        /// <summary>
        /// Indicates whether the current Song belongs to the Artist that the Album its being visualized under.
        /// </summary>
        public bool IsHome { get; set; }

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
