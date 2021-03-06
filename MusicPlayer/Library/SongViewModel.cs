﻿using System;
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

        /// <summary>
        /// SimpleView Constructor
        /// </summary>
        public SongViewModel(Song song, LibraryViewModel parent)
            : base(
                data: song,
                parent: parent,
                lazyLoadChildren: true)
        {
            Recording = null;
        }

        /// <summary>
        /// ClassicView & AlbumView Constructor
        /// </summary>
        public SongViewModel(Recording recording, bool isHome, LibraryViewModel parent)
            : base(
                data: recording.Song,
                parent: parent,
                lazyLoadChildren: true)
        {
            _isHome = isHome;
            Recording = recording;
        }

        #endregion Constructor
        #region Properties

        public Song _song => Data as Song;
        public string SearchableName => _song.Title;
        public Recording Recording { get; }
        public override string Name =>
            Recording == null ? _song.Title : $"{Recording.TrackNumber}. {Recording.Title}";

        private readonly bool _isHome = true;

        public override bool IsDim => base.IsDim || !_isHome;

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);

            if (Recording == null)
            {
                //SimpleView
                Artist artist = Parent.Data as Artist;
                foreach (Recording recording in dataManager.GenerateSongRecordingList(_song))
                {
                    Children.Add(new RecordingViewModel(recording, artist == recording.Artist, this));
                }

            }
            else
            {
                //ClassicView & AlbumView
                foreach (Recording recording in dataManager.GenerateSongRecordingList(_song))
                {
                    Children.Add(new RecordingViewModel(recording, Recording == recording, this));
                }

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
