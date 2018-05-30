using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.DataStructures
{
    public class RecordingDTO : DTO
    {
        public RecordingDTO(Recording recording, string title)
        {
            Name = title;
            ID = recording.ID;
            Weight = recording.Weight;
            IsHome = true;
            TrackID = recording.Tracks.First().ID;
            Live = recording.Live;
        }


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

        public long TrackID { get; set; } = -1;
    }
}
