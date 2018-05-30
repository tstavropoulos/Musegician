using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Musegician.Database;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public interface ILibraryRequestHandler
    {
        IEnumerable<Artist> GenerateArtistList();
        IEnumerable<Album> GenerateArtistAlbumList(Artist artist);
        IEnumerable<Track> GenerateAlbumTrackList(Album album);
        IEnumerable<Recording> GenerateSongRecordingList(Song song);

        IEnumerable<Album> GenerateAlbumList();
        IEnumerable<Song> GenerateArtistSongList(Artist artist);

        List<DirectoryDTO> GetDirectories(string path);
        IEnumerable<Recording> GetDirectoryRecordings(string path);

        void DatabaseUpdated();

        event EventHandler RebuildNotifier; 
    }
}
