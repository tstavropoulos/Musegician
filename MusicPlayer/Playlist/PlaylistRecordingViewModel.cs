using System;
using Musegician.Core;
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

        public RecordingType RecordingType => PlaylistRecording.Recording.RecordingType;
        public string TypeLabel => RecordingType.ToLabel();

        public override double DefaultWeight => Settings.Instance.GetDefaultWeight(RecordingType);

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
