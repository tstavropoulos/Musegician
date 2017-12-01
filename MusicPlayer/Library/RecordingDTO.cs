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
    }
}
