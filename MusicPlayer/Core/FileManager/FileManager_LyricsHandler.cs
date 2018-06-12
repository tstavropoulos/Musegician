using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;
using Musegician.DataStructures;
using ILyricsRequestHandler = Musegician.LyricViewer.ILyricsRequestHandler;

namespace Musegician
{
    public partial class FileManager : ILyricsRequestHandler
    {
        #region ILyricsRequestHandler

        string ILyricsRequestHandler.GetRecordingLyrics(Recording recording)
        {
            using (TagLib.File file = TagLib.File.Create(recording.Filename))
            {
                return file.Tag.Lyrics;
            }
        }

        void ILyricsRequestHandler.UpdateRecordingLyrics(Recording recording, string lyrics)
        {
            try
            {
                using (TagLib.File file = TagLib.File.Create(recording.Filename))
                {
                    file.Tag.Lyrics = lyrics;
                    file.Save();
                }
            }
            catch (System.IO.IOException e)
            {
                System.Windows.MessageBox.Show(
                    messageBoxText: $"Error: Unable to save lyrics.\n\n{e.Message}",
                    caption: "IOException");
            }
        }

        #endregion ILyricsRequestHandler
    }
}
