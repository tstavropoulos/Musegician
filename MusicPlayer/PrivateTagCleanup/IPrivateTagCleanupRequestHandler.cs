using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.PrivateTagCleanup
{
    public interface IPrivateTagCleanupRequestHandler
    {
        IEnumerable<string> GetAllPrivateTagOwners(
            IProgress<string> textSetter,
            IProgress<int> limitSetter,
            IProgress<int> progressSetter);

        void CullPrivateTagsByOwner(
            IProgress<string> textSetter,
            IProgress<int> limitSetter,
            IProgress<int> progressSetter,
            IEnumerable<string> tagOwners);
    }
}
