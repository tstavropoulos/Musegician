﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;
using Musegician.Library;

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

        List<ArtistDTO> ILibraryRequestHandler.GenerateArtistList()
        {
            return artistCommands.GeneratArtistList();
        }

        List<AlbumDTO> ILibraryRequestHandler.GenerateArtistAlbumList(long artistID, string artistName)
        {
            return albumCommands.GenerateArtistAlbumList(artistID, artistName);
        }

        List<SongDTO> ILibraryRequestHandler.GenerateAlbumSongList(long artistID, long albumID)
        {
            return songCommands.GenerateAlbumSongList(artistID, albumID);
        }

        List<RecordingDTO> ILibraryRequestHandler.GenerateSongRecordingList(long songID, long albumID)
        {
            return recordingCommands.GenerateSongRecordingList(songID, albumID);
        }

        List<AlbumDTO> ILibraryRequestHandler.GenerateAlbumList()
        {
            return albumCommands.GenerateAlbumList();
        }

        List<SongDTO> ILibraryRequestHandler.GenerateArtistSongList(long artistID, string artistName)
        {
            return songCommands.GenerateArtistSongList(artistID, artistName);
        }

        void ILibraryRequestHandler.UpdateWeights(
            LibraryContext context,
            IList<(long id, double weight)> values)
        {
            switch (context)
            {
                case LibraryContext.Artist:
                    artistCommands.UpdateWeights(values);
                    break;
                case LibraryContext.Album:
                    albumCommands.UpdateWeights(values);
                    break;
                case LibraryContext.Song:
                    songCommands.UpdateWeights(values);
                    break;
                case LibraryContext.Track:
                    trackCommands.UpdateWeights(values);
                    break;
                case LibraryContext.Recording:
                case LibraryContext.MAX:
                default:
                    throw new Exception("Unexpected LibraryContext: " + context);
            }
        }

        List<DirectoryDTO> ILibraryRequestHandler.GetDirectories(string path)
        {
            return recordingCommands.GetDirectories(path);
        }

        List<RecordingDTO> ILibraryRequestHandler.GetDirectoryRecordings(string path)
        {
            return recordingCommands.GetDirectoryRecordings(path);
        }

        string ILibraryRequestHandler.GetRecordingFilepath(long recordingID)
        {
            return recordingCommands.GetData(recordingID).filename;
        }

        #endregion ILibraryRequestHandler
    }
}
