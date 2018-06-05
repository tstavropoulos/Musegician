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
    public class PlaylistRecordingViewModel : PlaylistViewModel
    {
        #region Constructor

        public PlaylistRecordingViewModel(PlaylistRecording recording, PlaylistSongViewModel song)
            : base(recording, song)
        {
        }

        #endregion Constructor
        #region Properties

        public bool Live => PlaylistRecording.Recording.Live;
        public string LiveString => Live ? "🎤" : "";

        public PlaylistRecording PlaylistRecording => _data as PlaylistRecording;
        public PlaylistSongViewModel PlaylistSong => Parent as PlaylistSongViewModel;
        
        public override string Title
        {
            get => PlaylistRecording.Title;
            set
            {
                if (Title != value)
                {
                    PlaylistRecording.Title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        #endregion Properties
    }

}
