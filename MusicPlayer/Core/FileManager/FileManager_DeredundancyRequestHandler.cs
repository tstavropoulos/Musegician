using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Deredundafier;

namespace Musegician
{
    public partial class FileManager : IDeredundancyRequestHandler
    {
        #region IDeredundancyRequestHandler

        IList<DeredundafierDTO> IDeredundancyRequestHandler.GetArtistTargets()
        {
            return artistCommands.GetDeredundancyTargets();
        }

        IList<DeredundafierDTO> IDeredundancyRequestHandler.GetAlbumTargets()
        {
            return albumCommands.GetDeredundancyTargets();
        }

        IList<DeredundafierDTO> IDeredundancyRequestHandler.GetSongTargets()
        {
            return songCommands.GetDeredundancyTargets();
        }

        void IDeredundancyRequestHandler.MergeArtists(IEnumerable<long> ids)
        {
            artistCommands.Merge(ids);
        }

        void IDeredundancyRequestHandler.MergeAlbums(IEnumerable<long> ids)
        {
            albumCommands.Merge(ids);
        }

        void IDeredundancyRequestHandler.MergeSongs(IEnumerable<long> ids)
        {
            songCommands.Merge(ids);
        }

        void IDeredundancyRequestHandler.PushChanges()
        {
            _rebuildNotifier?.Invoke(this, EventArgs.Empty);
        }

        #endregion IDeredundancyRequestHandler
    }
}
