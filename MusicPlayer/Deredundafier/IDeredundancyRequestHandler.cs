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
        IEnumerable<DeredundafierDTO> GetArtistTargets(bool deep);
        IEnumerable<DeredundafierDTO> GetAlbumTargets(bool deep);
        IEnumerable<DeredundafierDTO> GetSongTargets(bool deep);

        void MergeArtists(IEnumerable<BaseData> data);
        void MergeAlbums(IEnumerable<BaseData> data);
        void MergeSongs(IEnumerable<BaseData> data);

        void PushChanges();
    }
}
