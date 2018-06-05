using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Playlist
{
    public interface IPlaylistUpdateListener
    {
        void AddBack(IEnumerable<PlaylistSong> songs);
        void InsertSongs(int index, IEnumerable<PlaylistSong> songs);
        void Rebuild(IEnumerable<PlaylistSong> songs);
        void RemoveIndices(IEnumerable<int> indices);
        void MarkIndex(int index);
        void MarkRecording(PlaylistRecording playlistRecording);
        void UnmarkAll();
        void Rearrange(IEnumerable<int> sourceIndices, int targetIndex);
    }
}
