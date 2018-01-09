using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musegician
{
    public static class MathExt
    {
        public static double Clamp(double value, double lowerBound, double upperBound)
        {
            return Math.Max(Math.Min(value, upperBound), lowerBound);
        }

        public static float Clamp(float value, float lowerBound, float upperBound)
        {
            return Math.Max(Math.Min(value, upperBound), lowerBound);
        }

        public static int Clamp(int value, int lowerBound, int upperBound)
        {
            return Math.Max(Math.Min(value, upperBound), lowerBound);
        }

    }
}
