using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.LyricViewer
{
    public interface ILyricsRequestHandler
    {
        string GetRecordingLyrics(Recording recording);
        void UpdateRecordingLyrics(Recording recording, string lyrics);
    }
}
