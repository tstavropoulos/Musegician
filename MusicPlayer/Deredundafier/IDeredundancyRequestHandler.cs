using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Deredundafier;

namespace Musegician.Deredundafier
{
    public interface IDeredundancyRequestHandler
    {
        IList<DeredundafierDTO> GetArtistTargets();
        IList<DeredundafierDTO> GetAlbumTargets();
        IList<DeredundafierDTO> GetSongTargets();
    }
}
