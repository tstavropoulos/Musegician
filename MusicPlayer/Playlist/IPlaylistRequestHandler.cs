using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;

namespace Musegician.Playlist
{
    public interface IPlaylistRequestHandler
    {
        void SavePlaylist(string title, ICollection<SongDTO> songs);

        void DeletePlaylist(long playlistID);

        long FindPlaylist(string title);

        List<SongDTO> LoadPlaylist(long playlistID);

        List<PlaylistData> GetPlaylistInfo();
    }
}
