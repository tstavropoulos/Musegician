using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Deredundafier
{
    public class DeredundafierDTO
    {
        readonly List<DeredundafierDTO> _children = new List<DeredundafierDTO>();
        public IList<DeredundafierDTO> Children
        {
            get { return _children; }
        }

        public string Name { get; set; }
        public BaseData Data { get; set; }
    }


    public class SelectorDTO : DeredundafierDTO
    {
        public bool IsChecked { get; set; }
    }
}
