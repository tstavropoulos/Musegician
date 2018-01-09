using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;

namespace Musegician.Playlist
{
    public class PlaylistSongViewModel : PlaylistViewModel
    {
        #region Constructor

        public PlaylistSongViewModel(SongDTO song)
            : base(song, null)
        {
            foreach(RecordingDTO recording in song.Children)
            {
                Children.Add(new PlaylistRecordingViewModel(recording, this));
            }
        }

        #endregion Constructor
        #region Properties

        public SongDTO Song
        {
            get { return _data as SongDTO; }
        }

        #endregion Properties
    }

}
