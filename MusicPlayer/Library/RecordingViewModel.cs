using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public class RecordingViewModel : LibraryViewModel
    {
        #region Constructors

        public RecordingViewModel(RecordingDTO recording, SongViewModel song)
            : base(data: recording,
                    parent: song,
                    lazyLoadChildren: false)
        { }

        public RecordingViewModel(RecordingDTO recording, DirectoryViewModel directory)
            : base(data: recording,
                    parent: directory,
                    lazyLoadChildren: false)
        { }

        #endregion Constructors
        #region Properties

        public RecordingDTO _recording
        {
            get { return Data as RecordingDTO; }
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

        public bool Live
        {
            get { return _recording.Live; }
        }

        public override bool IsDim
        {
            get { return base.IsDim || !_recording.IsHome; }
        }

        public long TrackID
        {
            get { return _recording.TrackID; }
        }

        #endregion Properties
    }
}
