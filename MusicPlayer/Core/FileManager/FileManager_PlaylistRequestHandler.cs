using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;

using IPlaylistRequestHandler = Musegician.Playlist.IPlaylistRequestHandler;

namespace Musegician
{
    public partial class FileManager : IPlaylistRequestHandler
    {
        #region IPlaylistRequestHandler

        void IPlaylistRequestHandler.SavePlaylist(string title, ICollection<SongDTO> songs)
        {
            playlistCommands.SavePlaylist(
                title: title,
                songs: songs);
        }

        List<SongDTO> IPlaylistRequestHandler.LoadPlaylist(long playlistID)
        {
            return playlistCommands.LoadPlaylist(playlistID);
        }

        List<PlaylistData> IPlaylistRequestHandler.GetPlaylistInfo()
        {
            return playlistCommands.GetPlaylistInfo();
        }

        long IPlaylistRequestHandler.FindPlaylist(string title)
        {
            return playlistCommands.FindPlaylist(title);
        }

        void IPlaylistRequestHandler.DeletePlaylist(long playlistID)
        {
            playlistCommands.DeletePlaylist(
                playlistID: playlistID);
        }

        PlayData IPlaylistRequestHandler.GetRecordingPlayData(long recordingID)
        {
            return recordingCommands.GetRecordingPlayData(recordingID);
        }

        #endregion IPlaylistRequestHandler
    }
}
