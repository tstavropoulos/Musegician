using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Deredundafier
{
    public class PassiveViewModel : DeredundafierViewModel
    {
        #region Constructor

        public PassiveViewModel(DeredundafierDTO data, DeredundafierViewModel parent)
            : base(data, parent)
        {
            foreach (DeredundafierDTO child in data.Children)
            {
                Children.Add(new PassiveViewModel(child, this));
            }
        }

        #endregion Constructor
    }
}
