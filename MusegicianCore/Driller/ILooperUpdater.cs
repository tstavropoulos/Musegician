using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Driller
{
    public interface ILooperUpdater
    {
        double GetStartPosition();
        double GetEndPosition();
        void ResetBounds();
    }
}
