using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Playlist
{
    public class SongDTO
    {
        readonly List<RecordingDTO> _recordings = new List<RecordingDTO>();
        public IList<RecordingDTO> Recordings
        {
            get { return _recordings; }
        }
        
        public SongDTO(long id, string title, List<RecordingDTO> recordings)
        {
            SongID = id;
            Title = title;
            _recordings = recordings;
        }

        public string Title { get; set; }
        public long SongID { get; set; }

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
