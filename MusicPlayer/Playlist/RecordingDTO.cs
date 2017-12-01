using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Playlist
{
    public class RecordingDTO
    {
        public string Title { get; set; }
        public long RecordingID { get; set; }
        public bool Live { get; set; }
        /// <summary>
        /// Weight of NaN means use the global value for Live-ness
        /// </summary>
        public double Weight
        {
            get
            {
                if (double.IsNaN(_weight))
                {
                    if (Live)
                    {
                        return Settings.LiveWeight;
                    }
                    return Settings.StudioWeight;
                }

                return _weight;
            }
            set { _weight = value; }
        }
        private double _weight = double.NaN;
    }
}
