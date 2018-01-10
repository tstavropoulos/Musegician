﻿using System;
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
            throw new NotImplementedException();
        }

        #endregion IDeredundancyRequestHandler
    }
}
