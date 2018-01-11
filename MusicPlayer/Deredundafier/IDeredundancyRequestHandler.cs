using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public interface IDeredundancyRequestHandler
    {
        IList<DeredundafierDTO> GetArtistTargets();
        IList<DeredundafierDTO> GetAlbumTargets();
        IList<DeredundafierDTO> GetSongTargets();

        void MergeArtists(IEnumerable<long> ids);
        void MergeAlbums(IEnumerable<long> ids);
        void MergeSongs(IEnumerable<long> ids);

        void PushChanges();
    }
}
