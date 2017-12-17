using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.DataStructures
{
    public class RecordingDTO : DTO
    {
        public bool Live { get; set; }

        /// <summary>
        /// Indicates whether the current recording officially belongs to the album its being visualized under.
        /// </summary>
        public bool IsHome { get; set; }

        protected override double DefaultWeight
        {
            get
            {
                if (Live)
                {
                    return Settings.Instance.LiveWeight;
                }
                return Settings.Instance.StudioWeight;
            }
        }
    }
}
