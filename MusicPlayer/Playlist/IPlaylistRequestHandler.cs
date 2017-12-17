using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Playlist
{
    public interface IPlaylistRequestHandler
    {
        void SavePlaylist(string title, ICollection<SongDTO> songs);

        long FindPlaylist(string title);

        List<SongDTO> LoadPlaylist(long playlistID);

        List<PlaylistData> GetPlaylistInfo();
    }
}
