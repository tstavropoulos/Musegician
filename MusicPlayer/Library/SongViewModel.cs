using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Library
{
    public class SongViewModel : LibraryViewModel
    {
        #region Constructors

        public SongViewModel(SongDTO song, LibraryViewModel parent)
            : base (
                  data: song,
                  parent: parent,
                  lazyLoadChildren: true)
        {
        }

        #endregion // Constructors

        #region Song Properties

        public SongDTO _song
        {
            get { return Data as SongDTO; }
        }

        public override bool IsDim
        {
            get { return base.IsDim || !_song.IsHome; }
        }

        public long ContextualTrackID
        {
            get { return _song.TrackID; }
        }

        #endregion // Song Properties

        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach (RecordingDTO recordingData in dataManager.GenerateSongRecordingList(ID, Parent.ID))
            {
                Data.Children.Add(recordingData);
                Children.Add(new RecordingViewModel(recordingData, this));
            }
        }

        #endregion // LoadChildren
    }
}
