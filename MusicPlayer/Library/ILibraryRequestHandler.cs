﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public interface ILibraryRequestHandler
    {
        List<ArtistDTO> GenerateArtistList();
        List<AlbumDTO> GenerateArtistAlbumList(long artistID, string artistName);
        List<SongDTO> GenerateAlbumSongList(long artistID, long albumID);
        List<RecordingDTO> GenerateSongRecordingList(long songID, long albumID);


        List<AlbumDTO> GenerateAlbumList();
        List<SongDTO> GenerateArtistSongList(long artistID, string artistName);

        void UpdateWeights(LibraryContext context, IList<(long id, double weight)> values);

        event EventHandler RebuildNotifier; 
    }
}
