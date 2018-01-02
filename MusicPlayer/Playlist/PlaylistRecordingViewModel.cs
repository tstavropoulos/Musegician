using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Playlist
{
    public class PlaylistRecordingViewModel : PlaylistViewModel
    {
        #region Constructor

        public PlaylistRecordingViewModel(RecordingDTO recording, PlaylistSongViewModel song)
            : base(recording, song)
        {
        }

        #endregion Constructor
        #region Properties

        public bool Live
        {
            get { return Recording.Live; }
        }

        public string LiveString
        {
            get
            {
                if (Live)
                {
                    return "🎤";
                }
                return "";
            }
        }

        public RecordingDTO Recording
        {
            get { return _data as RecordingDTO; }
        }

        public PlaylistSongViewModel Song
        {
            get { return _parent as PlaylistSongViewModel; }
        }

        #endregion Properties
    }

}
