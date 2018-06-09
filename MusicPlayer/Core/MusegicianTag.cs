using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Core
{
    [Serializable]
    public class MusegicianTag : ISerializable
    {
        public bool Live { get; set; }
        public Guid ArtistGuid { get; set; }
        public Guid AlbumGuid { get; set; }
        public Guid SongGuid { get; set; }

        public MusegicianTag() { }

        //Deserialization constructor
        public MusegicianTag(SerializationInfo info, StreamingContext context)
        {
            Live = (bool)info.GetValue("Live", typeof(bool));
            ArtistGuid = (Guid)info.GetValue("ArtistGuid", typeof(Guid));
            AlbumGuid = (Guid)info.GetValue("AlbumGuid", typeof(Guid));
            SongGuid = (Guid)info.GetValue("SongGuid", typeof(Guid));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Live", Live, typeof(bool));
            info.AddValue("ArtistGuid", ArtistGuid, typeof(Guid));
            info.AddValue("AlbumGuid", AlbumGuid, typeof(Guid));
            info.AddValue("SongGuid", SongGuid, typeof(Guid));
        }
    }
}
