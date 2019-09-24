using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TASAgency.Math;

namespace Musegician.Core
{
    public static class ListExtensions
    {
        //The approach is trivially simple, but I still stole it from:
        //https://stackoverflow.com/questions/273313/randomize-a-listt
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.Rand.Next(n + 1);
                T temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }

            return list;
        }
    }
}
