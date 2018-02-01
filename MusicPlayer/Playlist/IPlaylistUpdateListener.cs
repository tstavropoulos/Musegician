using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;

namespace Musegician.Playlist
{
    public interface IPlaylistUpdateListener
    {
        void AddBack(ICollection<SongDTO> songs);
        void InsertSongs(int index, ICollection<SongDTO> songs);
        void Rebuild(ICollection<SongDTO> songs);
        void RemoveIndices(ICollection<int> indices);
        void MarkIndex(int index);
        void MarkRecordingIndex(int index);
        void UnmarkAll();
        void Rearrange(IEnumerable<int> sourceIndices, int targetIndex);
    }
}
