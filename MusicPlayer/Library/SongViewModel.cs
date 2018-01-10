using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public class SongViewModel : LibraryViewModel
    {
        #region Constructor

        public SongViewModel(SongDTO song, LibraryViewModel parent)
            : base(
                data: song,
                parent: parent,
                lazyLoadChildren: true)
        {
        }

        #endregion Constructor
        #region Properties

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

        public string SearchableName
        {
            get { return _song.SearchableName; }
        }

        #endregion Properties
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

        #endregion LoadChildren
        #region NameContainsText

        public override bool NameContainsText(string text)
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(SearchableName))
                return false;

            return SearchableName.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        #endregion NameContainsText
    }
}
