﻿using System;
using System.Collections.Generic;

namespace MusicPlayer.Library
{
    public class SongDTO
    {
        readonly List<RecordingDTO> _recordings = new List<RecordingDTO>();
        public IList<RecordingDTO> Recordings
        {
            get { return _recordings; }
        }

        public SongDTO(long id, string title, List<RecordingDTO> recordings)
        {
            SongID = id;
            Title = title;
            _recordings = recordings;
        }

        public string Title { get; set; }
        public long SongID { get; set; }
    }
}
