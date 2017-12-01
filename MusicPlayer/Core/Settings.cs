using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    public class Settings
    {
        public static double WeightParameter = 0.05;

        public static double LiveWeight { get { return Math.Min(2.0 * WeightParameter, 1.0); } }
        public static double StudioWeight { get { return Math.Min(2.0 * (1.0 - WeightParameter), 1.0); } }
    }
}
