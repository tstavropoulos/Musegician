using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.DataStructures
{
    public struct ArtistData
    {
        public int artistID;
        public string artistName;
    }

    public struct AlbumData
    {
        public int albumID;
        public int artistID;
        public string albumTitle;
        public string albumYear;
    }

    public struct SongData
    {
        public int songID;
        public int artistID;
        public string fileName;
        public string songTitle;
        public bool live;
        public bool valid;
    }

    public struct TrackData
    {
        public int trackID;
        public int songID;
        public int albumID;
        public int trackNumber;
    }

    public struct PlayData
    {
        public string fileName;
        public string songTitle;
        public string artistName;
    }

    public struct PlaylistData
    {
        public int songID;
        public string songTitle;
        public string artistName;
    }
}
