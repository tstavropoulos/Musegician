using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Playlist
{
    public interface IPlaylistTransferRequestHandler
    {
        List<SongDTO> GetAlbumData(
            long albumID,
            long exclusiveArtistID = -1,
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
    }
}
