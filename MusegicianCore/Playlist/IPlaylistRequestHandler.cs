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

        void ClearPlaylist();
        void SavePlaylistAs(string title, IEnumerable<PlaylistSong> songs);
        IEnumerable<PlaylistSong> LoadPlaylist(string title);
        void DeletePlaylist(string title);

        void Delete(PlaylistSong playlistSong);
        void Delete(PlaylistRecording playlistRecording);

        IEnumerable<PlaylistTuple> GetPlaylistInfo();

        PlayData GetRecordingPlayData(Recording recording);

        void NotifyDBChanged();
        IEnumerable<PlaylistSong> GetDefaultSongList();
    }
}
