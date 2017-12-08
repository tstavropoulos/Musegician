using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Library
{
    public interface ILibraryRequestHandler
    {
        List<ArtistDTO> GenerateArtistList();
        List<AlbumDTO> GenerateArtistAlbumList(long artistID, string artistName);
        List<SongDTO> GenerateAlbumSongList(long artistID, long albumID);
        List<RecordingDTO> GenerateSongRecordingList(long songID, long albumID);
    }
}
