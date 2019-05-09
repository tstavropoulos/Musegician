using System;
using System.Linq;
using Musegician.Database;

namespace Musegician.Library
{
    public class RecordingViewModel : LibraryViewModel
    {
        #region Constructors

        /// <summary>
        /// SimpleView Constructor
        /// </summary>
        public RecordingViewModel(Recording recording, bool isHome, SongViewModel song)
            : base(data: recording,
                   parent: song,
                   lazyLoadChildren: false)
        {
            _isHome = isHome;
            _name = $"{recording.Artist.Name} - {recording.Album.Title} - {recording.Title}";
        }

        public RecordingViewModel(Recording recording, DirectoryViewModel directory)
            : base(data: recording,
                   parent: directory,
                   lazyLoadChildren: false)
        {
            _isHome = true;
            _name = $"{recording.Artist.Name} - {recording.Album.Title} - {recording.Title}";
        }

        #endregion Constructors
        #region Properties

        public Recording _recording => Data as Recording;
        public string LiveString => Live ? "🎤" : "";
        public bool Live => _recording.Live;

        private readonly string _name;
        public override string Name => _name;

        private readonly bool _isHome = true;
        public override bool IsDim => base.IsDim || !_isHome;

        protected override double WeightValue
        {
            get => _recording.Weight;
            set => _recording.Weight = value;
        }

        #endregion Properties
    }
}
