using System;

namespace Musegician.Core
{
    public static class Epoch
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary> Get current Epoch time </summary>
        public static long Time => (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
    }
}
