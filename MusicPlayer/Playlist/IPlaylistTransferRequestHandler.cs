using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using Musegician.DataStructures;
using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician.Playlist
{
    public interface IPlaylistTransferRequestHandler
    {
        IEnumerable<SongDTO> GetAlbumData(
            Album album,
            bool deep = true);

        IEnumerable<SongDTO> GetArtistData(
            Artist artist,
            bool deep = true);

        IEnumerable<SongDTO> GetSongData(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null);

        IEnumerable<SongDTO> GetSongData(
            Recording recording);

        string GetDefaultPlaylistName(
            LibraryContext context,
            BaseData data);
    }
}
