using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Library;
using Musegician.Database;
using Musegician.DataStructures;

namespace Musegician
{
    public partial class FileManager : ILibraryRequestHandler
    {
        #region ILibraryRequestHandler

        event EventHandler ILibraryRequestHandler.RebuildNotifier
        {
            add => _rebuildNotifier += value;
            remove => _rebuildNotifier -= value;
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
                    orderby recording.Album.Year ascending
                    select recording.Album).Distinct();
        }

        IEnumerable<Recording> ILibraryRequestHandler.GenerateAlbumRecordingList(Album album)
        {
            return (from recording in album.Recordings
                    orderby recording.DiscNumber ascending, recording.TrackNumber ascending
                    select recording);
        }

        IEnumerable<Recording> ILibraryRequestHandler.GenerateSongRecordingList(Song song)
        {
            return (from recording in song.Recordings
                    orderby recording.RecordingType
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
                    select recording.Song).Distinct();
        }

        void ILibraryRequestHandler.DatabaseUpdated()
        {
            db.SaveChanges();
        }

        IEnumerable<DirectoryDTO> ILibraryRequestHandler.GetDirectories(string path)
        {
            List<DirectoryDTO> directoryList = new List<DirectoryDTO>();
            HashSet<string> directorySet = new HashSet<string>();

            var readTracks =
                (from recording in db.Recordings
                 where recording.Filename.StartsWith(path)
                 orderby recording.Filename ascending
                 select recording.Filename).Distinct();

            string currentChunk = "";
            int directoryChunkDepth = -1;
            foreach (string recordingPath in readTracks)
            {
                string relativepath;

                if (path == "")
                {
                    relativepath = recordingPath;
                }
                else
                {
                    relativepath = recordingPath.Substring(path.Length);
                }

                if (relativepath.Contains(System.IO.Path.DirectorySeparatorChar))
                {
                    if (directoryChunkDepth < 0)
                    {
                        //Initialization
                        directoryChunkDepth = _CountDirectories(relativepath);
                        currentChunk = _GrabPathChunk(relativepath, directoryChunkDepth);
                    }

                    string relativeDirChunk = _GrabPathChunk(relativepath, directoryChunkDepth);

                    while (directoryChunkDepth > 1)
                    {
                        //Possible Directory Collapse Condition
                        if (currentChunk == relativeDirChunk)
                        {
                            //Matches the deep comparison, keep going
                            break;
                        }

                        //Otherwise I need to decrement directoryChunkDepth
                        --directoryChunkDepth;
                        //Update my chunks
                        currentChunk = _GrabPathChunk(currentChunk, directoryChunkDepth);
                        relativeDirChunk = _GrabPathChunk(relativepath, directoryChunkDepth);
                        //And add this old chunk if we've hit one (otherwise it's been skipped)
                        if (directoryChunkDepth == 1)
                        {
                            if (directorySet.Add(currentChunk))
                            {
                                directoryList.Add(new DirectoryDTO(currentChunk));
                            }
                        }
                    }

                    if (directoryChunkDepth == 1)
                    {
                        //Branching - we will just add every subdir
                        if (directorySet.Add(relativeDirChunk))
                        {
                            directoryList.Add(new DirectoryDTO(relativeDirChunk));
                        }
                    }
                }
            }

            if (directoryChunkDepth > 1)
            {
                //Stash my one, multi-directory chunk
                directoryList.Add(new DirectoryDTO(currentChunk));
            }

            return directoryList;
        }

        IEnumerable<Recording> ILibraryRequestHandler.GetDirectoryRecordings(string path)
        {
            return (from recording in db.Recordings
                    where recording.Filename.StartsWith(path) && !recording.Filename.Substring(path.Length + 1).Contains("\\")
                    orderby recording.Filename ascending
                    select recording);
        }

        void ILibraryRequestHandler.Delete(IEnumerable<Recording> recordings)
        {
            db.PlaylistRecordings.RemoveRange(
                (from recording in recordings
                 join plRec in db.PlaylistRecordings on recording.Id equals plRec.RecordingId
                 select plRec).Distinct());

            db.Recordings.RemoveRange(recordings);
            db.SaveChanges();

            CleanChildlessRecords();
        }

        #endregion ILibraryRequestHandler

        private string _GrabPathChunk(string path, int directories)
        {
            int index = 0;
            for (int i = 0; i < directories; i++)
            {
                index = path.IndexOf(System.IO.Path.DirectorySeparatorChar, index + 1);
                if (index == -1)
                {
                    return "";
                }
            }

            return path.Substring(0, index);
        }

        private int _CountDirectories(string path)
        {
            int index = 0;
            int count = -1;
            while (index > -1)
            {
                index = path.IndexOf(System.IO.Path.DirectorySeparatorChar, index + 1);
                ++count;
            }

            return count;

        }
    }
}
