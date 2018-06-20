using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.AlbumArtPicker
{
    public interface IAlbumArtPickerRequestHandler
    {
        IEnumerable<AlbumArtAlbumDTO> GetAlbumArtMatches(bool includeAll);

        void PushChanges();
    }
}
