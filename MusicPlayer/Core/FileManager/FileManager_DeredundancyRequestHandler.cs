using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using Musegician.Deredundafier;

namespace Musegician
{
    public partial class FileManager : IDeredundancyRequestHandler
    {
        #region IDeredundancyRequestHandler

        IEnumerable<DeredundafierDTO> IDeredundancyRequestHandler.GetArtistTargets() =>
            artistCommands.GetDeredundancyTargets();

        IEnumerable<DeredundafierDTO> IDeredundancyRequestHandler.GetAlbumTargets() =>
            albumCommands.GetDeredundancyTargets();

        IEnumerable<DeredundafierDTO> IDeredundancyRequestHandler.GetSongTargets() =>
            songCommands.GetDeredundancyTargets();

        void IDeredundancyRequestHandler.MergeArtists(IEnumerable<BaseData> data) =>
            artistCommands.Merge(data);

        void IDeredundancyRequestHandler.MergeAlbums(IEnumerable<BaseData> data) =>
            albumCommands.Merge(data);

        void IDeredundancyRequestHandler.MergeSongs(IEnumerable<BaseData> data) =>
            songCommands.Merge(data);

        void IDeredundancyRequestHandler.PushChanges() => 
            _rebuildNotifier?.Invoke(this, EventArgs.Empty);

        #endregion IDeredundancyRequestHandler
    }
}
