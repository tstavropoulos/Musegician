using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.DataStructures
{
    public class DirectoryDTO : BaseData
    {
        public IList<DirectoryDTO> Children { get; } = new List<DirectoryDTO>();
        
        public string Name { get; set; }
        public override double Weight {
            get => double.NaN;
            set => throw new NotImplementedException();
        }

        public DirectoryDTO(string directory)
        {
            Id = -1;
            Name = directory;
        }
    }
}
