using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Deredundafier
{
    public interface IDeredundancyRequestHandler
    {
        IEnumerable<DeredundafierDTO> GetArtistTargets();
        IEnumerable<DeredundafierDTO> GetAlbumTargets();
        IEnumerable<DeredundafierDTO> GetSongTargets();

        void MergeArtists(IEnumerable<BaseData> data);
        void MergeAlbums(IEnumerable<BaseData> data);
        void MergeSongs(IEnumerable<BaseData> data);

        void PushChanges();
    }
}
