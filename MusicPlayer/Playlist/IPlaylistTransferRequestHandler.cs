using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;
using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician.Playlist
{
    public interface IPlaylistTransferRequestHandler
    {
        List<SongDTO> GetAlbumData(
            long albumID,
            bool deep = true);

        List<SongDTO> GetArtistData(
            long artistID,
            bool deep = true);

        List<SongDTO> GetSongData(
            long songID,
            long exclusiveArtistID = -1,
            long exclusiveRecordingID = -1);

        List<SongDTO> GetSongDataFromRecordingID(
            long recordingID);

        string GetDefaultPlaylistName(
            LibraryContext context,
            long id);
    }
}
