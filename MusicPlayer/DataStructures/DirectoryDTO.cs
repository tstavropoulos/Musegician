using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.DataStructures
{
    public class DirectoryDTO : DTO
    {
        public DirectoryDTO(string directory)
        {
            ID = -1;
            Name = directory;
        }
    }
}
