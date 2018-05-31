using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using Musegician.DataStructures;

namespace Musegician.Playlist
{
    public interface IPlaylistRequestHandler
    {
        Database.Playlist GetCurrentPlaylist();

        void PushCurrentTo(string title);
        void LoadPlaylist(string title);
        void DeletePlaylist(string title);

        IEnumerable<(string title, int count)> GetPlaylistInfo();

        PlayData GetRecordingPlayData(Recording recording);
    }
}
