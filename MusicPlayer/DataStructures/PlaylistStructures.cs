using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.DataStructures
{
    public struct PlaylistData
    {
        public long id;
        public string title;
        public long count;
    }

    public struct PlaylistSongData
    {
        public long id;
        public long playlistID;
        public string title;
        public long songID;
        public long number;
        public double weight;
    }

    public struct PlaylistRecordingData
    {
        public long id;
        public long playlistSongID;
        public long recordingID;
        public double weight;
    }
}
