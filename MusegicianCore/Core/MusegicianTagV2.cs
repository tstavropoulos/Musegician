using System;
using System.Runtime.Serialization;

namespace Musegician.Core
{
    [Serializable]
    public sealed class MusegicianTagV2 : ISerializable
    {
        public RecordingType RecordingType { get; set; }

        public Guid ArtistGuid { get; set; }
        public long ArtistTimestamp { get; set; }

        public Guid AlbumGuid { get; set; }
        public long AlbumTimestamp { get; set; }

        public Guid SongGuid { get; set; }
        public long SongTimestamp { get; set; }

        public string SongTitle { get; set; }

        public MusegicianTagV2() { }

        //Deserialization constructor
        public MusegicianTagV2(SerializationInfo info, StreamingContext context)
        {
            RecordingType = (RecordingType)info.GetValue("RecordingType", typeof(RecordingType));

            ArtistGuid = (Guid)info.GetValue("ArtistGuid", typeof(Guid));
            ArtistTimestamp = (long)info.GetValue("ArtistTimestamp", typeof(long));

            AlbumGuid = (Guid)info.GetValue("AlbumGuid", typeof(Guid));
            AlbumTimestamp = (long)info.GetValue("AlbumTimestamp", typeof(long));

            SongGuid = (Guid)info.GetValue("SongGuid", typeof(Guid));
            SongTimestamp = (long)info.GetValue("SongTimestamp", typeof(long));

            SongTitle = (string)info.GetValue("SongTitle", typeof(string));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RecordingType", RecordingType, typeof(RecordingType));
            
            info.AddValue("ArtistGuid", ArtistGuid, typeof(Guid));
            info.AddValue("ArtistTimestamp", ArtistTimestamp, typeof(long));

            info.AddValue("AlbumGuid", AlbumGuid, typeof(Guid));
            info.AddValue("AlbumTimestamp", AlbumTimestamp, typeof(long));

            info.AddValue("SongGuid", SongGuid, typeof(Guid));
            info.AddValue("SongTimestamp", SongTimestamp, typeof(long));

            info.AddValue("SongTitle", SongTitle, typeof(string));
        }
    }
}
