using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Playlist
{
    public class PlaylistSongViewModel : PlaylistViewModel
    {
        #region Constructor

        public PlaylistSongViewModel(PlaylistSong song)
            : base(song, null)
        {
            foreach(PlaylistRecording recording in song.PlaylistRecordings)
            {
                Children.Add(new PlaylistRecordingViewModel(recording, this));
            }
        }

        #endregion Constructor
        #region Properties

        public PlaylistSong PlaylistSong => _data as PlaylistSong;

        public override string Title
        {
            get => PlaylistSong.Title;
            set
            {
                if (Title != value)
                {
                    PlaylistSong.Title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        #endregion Properties
    }

}
