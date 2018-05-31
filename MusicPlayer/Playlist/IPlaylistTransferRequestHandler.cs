using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician.Playlist
{
    public interface IPlaylistTransferRequestHandler
    {
        IEnumerable<PlaylistSong> GetAlbumData(
            Album album,
            bool deep = true);

        IEnumerable<PlaylistSong> GetArtistData(
            Artist artist,
            bool deep = true);

        IEnumerable<PlaylistSong> GetSongData(
            Song song,
            Artist exclusiveArtist = null,
            Recording exclusiveRecording = null);

        IEnumerable<PlaylistSong> GetSongData(
            Recording recording);

        string GetDefaultPlaylistName(
            LibraryContext context,
            BaseData data);
    }
}
