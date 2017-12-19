using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Core
{

    /// <summary>
    /// Some documentation available here:
    /// https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
    /// </summary>
    public static class DoubleComparisonExtension
    {
        public static bool AlmostEqualRelative(
            this double A,
            double B,
            double maxRelDiff = double.Epsilon)
        {
            // Calculate the difference.
            double diff = Math.Abs(A - B);
            A = Math.Abs(A);
            B = Math.Abs(B);
            // Find the largest
            double largest = (B > A) ? B : A;

            if (diff <= largest * maxRelDiff)
            {
                return true;
            }
            return false;
        }

        public static bool AlmostEqualRelative(
            this float A,
            float B,
            float maxRelDiff = float.Epsilon)
        {
            // Calculate the difference.
            float diff = Math.Abs(A - B);
            A = Math.Abs(A);
            B = Math.Abs(B);

            // Find the largest
            float largest = (B > A) ? B : A;

            if (diff <= largest * maxRelDiff)
            {
                return true;
            }
            return false;
        }
    }
}
