using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using Musegician.Database;

namespace Musegician.Library
{
    public class RecordingViewModel : LibraryViewModel
    {
        #region Constructors

        public RecordingViewModel(Recording recording, SongViewModel song)
            : base(data: recording,
                    parent: song,
                    lazyLoadChildren: false)
        {
            _isHome = song.ContextualTrack?.Recording == recording;
        }

        public RecordingViewModel(Recording recording, DirectoryViewModel directory)
            : base(data: recording,
                    parent: directory,
                    lazyLoadChildren: false)
        { }

        #endregion Constructors
        #region Properties

        public Recording _recording => Data as Recording;
        public string LiveString => Live ? "🎤" : "";
        public bool Live => _recording.Live;

        private readonly bool _isHome = true;
        public override bool IsDim => base.IsDim || !_isHome;

        #endregion Properties
    }
}
