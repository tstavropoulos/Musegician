using System;
using System.Collections;
using System.Collections.Generic;

using LoadingUpdater = Musegician.LoadingDialog.LoadingDialog.LoadingUpdater;

namespace Musegician.PrivateTagCleanup
{
    public interface IPrivateTagCleanupRequestHandler
    {
        IEnumerable<string> GetAllPrivateTagOwners(LoadingUpdater updater);

        void CullPrivateTagsByOwner(LoadingUpdater updater, IEnumerable<string> tagOwners);
    }
}
