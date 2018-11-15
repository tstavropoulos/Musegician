using System;
using System.Threading;

namespace TASAgency.Math
{
    //The feature is trivially simple, but I still stole it from:
    //https://stackoverflow.com/questions/273313/randomize-a-listt
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random Rand => Local ??
            (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));
    }
}
