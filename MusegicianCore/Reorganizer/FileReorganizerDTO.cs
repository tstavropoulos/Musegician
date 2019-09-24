using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;

namespace Musegician.Reorganizer
{
    public class FileReorganizerDTO
    {
        public bool Possible { get; set; }
        public string NewPath { get; set; }
        public string Name => $"{NewPath} ({Data.Filename})";
        public Recording Data { get; set; }
        public bool IsChecked { get; set; }
    }
}
