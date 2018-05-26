using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using Musegician.Database;

namespace Musegician.Library
{
    public class SongViewModel : LibraryViewModel
    {
        #region Constructor

        public SongViewModel(Song song, LibraryViewModel parent)
            : base(
                data: song,
                parent: parent,
                lazyLoadChildren: true)
        {
            ContextualTrack = null;
        }

        public SongViewModel(Track track, LibraryViewModel parent)
            : base(
                data: track.Recording.Song,
                parent: parent,
                lazyLoadChildren: true)
        {
            Artist recordingArtist = track.Recording.Artist;
            Artist contextArtist = parent?.Parent?.Data as Artist ?? recordingArtist;
            _isHome = (recordingArtist == contextArtist);
            ContextualTrack = track;
        }

        #endregion Constructor
        #region Properties

        public Song _song => Data as Song;
        public string SearchableName => _song.Title;
        public Track ContextualTrack { get; }

        private bool _isHome = true;

        public override bool IsDim => base.IsDim || !_isHome;

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach (Recording recording in dataManager.GenerateSongRecordingList(_song))
            {
                Children.Add(new RecordingViewModel(recording, this));
            }
        }

        #endregion LoadChildren
        #region NameContainsText

        public override bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(SearchableName))
            {
                return false;
            }

            return SearchableName.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion NameContainsText
    }
}
