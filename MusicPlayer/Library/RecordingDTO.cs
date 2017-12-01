using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Library
{
    public class RecordingDTO
    {
        public string Title { get; set; }
        public long RecordingID { get; set; }
        public bool Live { get; set; }

        /// <summary>
        /// Indicates whether the current recording officially belongs to the album its being visualized under.
        /// </summary>
        public bool IsHome { get; set; }
        
        private double _weight = double.NaN;
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
    }
}
