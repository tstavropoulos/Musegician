using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.DataStructures
{
    public struct Artist
    {
        public int artistID;
        public string artistName;
    }

    public struct Album_Simple
    {
        public int albumID;
        public int artistID;
        public string albumName;
        public string albumYear;
    }

    public struct SongData
    {
        public int songID;
        public int artistID;
        public string fileName;
        public string songName;
        public bool live;
    }

    public struct Track_Simple
    {
        public int trackID;
        public int songID;
        public int albumID;
        public int songNumber;
    }

    public struct PlayData
    {
        public string fileName;
        public string songName;
        public string artistName;
    }
}
