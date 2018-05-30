using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;
using Musegician.Library;
using Musegician.Database;

namespace Musegician
{
    public partial class FileManager : ILibraryRequestHandler
    {
        #region ILibraryRequestHandler

        event EventHandler ILibraryRequestHandler.RebuildNotifier
        {
            add { _rebuildNotifier += value; }
            remove { _rebuildNotifier -= value; }
        }

        IEnumerable<Artist> ILibraryRequestHandler.GenerateArtistList()
        {
            return (from artist in db.Artists
                    orderby (artist.Name.StartsWith("The ") ? artist.Name.Substring(4) : artist.Name)
                    select artist);
        }

        IEnumerable<Album> ILibraryRequestHandler.GenerateArtistAlbumList(Artist artist)
        {
            return (from recording in artist.Recordings
                    from track in recording.Tracks
                    orderby track.Album.Year ascending
                    select track.Album).Distinct();
        }

        IEnumerable<Track> ILibraryRequestHandler.GenerateAlbumTrackList(Album album)
        {
            return (from track in album.Tracks
                    orderby track.DiscNumber ascending, track.TrackNumber ascending
                    select track);
        }

        IEnumerable<Recording> ILibraryRequestHandler.GenerateSongRecordingList(Song song)
        {
            return (from recording in song.Recordings
                    orderby recording.Live
                    select recording);
        }

        IEnumerable<Album> ILibraryRequestHandler.GenerateAlbumList()
        {
            return (from album in db.Albums
                    orderby album.Title
                    select album);
        }

        IEnumerable<Song> ILibraryRequestHandler.GenerateArtistSongList(Artist artist)
        {
            return (from recording in artist.Recordings
                    orderby (recording.Song.Title.StartsWith("The ") ? recording.Song.Title.Substring(4) : recording.Song.Title)
                    select recording.Song);
        }

        void ILibraryRequestHandler.DatabaseUpdated()
        {
            db.SaveChanges();
        }

        List<DirectoryDTO> ILibraryRequestHandler.GetDirectories(string path)
        {
            return recordingCommands.GetDirectories(path);
        }

        IEnumerable<Recording> ILibraryRequestHandler.GetDirectoryRecordings(string path)
        {
            return (from recording in db.Recordings
                    where recording.Filename.StartsWith(path)
                    orderby recording.Filename ascending
                    select recording);
        }

        #endregion ILibraryRequestHandler
    }
}
