using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Musegician.Database;
using Musegician.Reorganizer;

namespace Musegician
{
    public partial class FileManager : IFileReorganizerRequestHandler
    {
        IEnumerable<FileReorganizerDTO> IFileReorganizerRequestHandler.GetReorganizerTargets(string newMusicRoot)
        {
            if (newMusicRoot is null || !Directory.Exists(newMusicRoot))
            {
                throw new ArgumentException($"Path \"{newMusicRoot ?? ""}\" poorly formed or does not exist.");
            }

            //Regularize the path, potentially standardizing slashes, and other unknown representations.
            newMusicRoot = Path.GetFullPath(newMusicRoot);

            //LOGIC:
            //    {Root}/Artist/[YYYY] Album/##. Artist - RecordingTitle.ext

            List<FileReorganizerDTO> reorgTargets = new List<FileReorganizerDTO>();
            foreach (Recording recording in db.Recordings)
            {
                Track track = recording.Tracks.First();
                Album album = track.Album;

                //
                // Filename
                //

                string extension = Path.GetExtension(recording.Filename);
                string currentFileName = Path.GetFileNameWithoutExtension(recording.Filename);

                string properFileName;
                if (track.TrackNumber == 0)
                {
                    properFileName = $"{recording.Artist.Name} - {track.Title}";
                }
                else
                {
                    properFileName = $"{track.TrackNumber:D2}. {recording.Artist.Name} - {track.Title}";
                }

                properFileName = SanitizeFileName(properFileName);


                //
                // Album Directory
                //

                string directory = Path.GetDirectoryName(recording.Filename);

                string currentAlbumDirectory = Path.GetFileName(directory);

                string properAlbumDirectory;
                if (album.Year == 0)
                {
                    properAlbumDirectory = album.Title;
                }
                else
                {
                    properAlbumDirectory = $"[{album.Year}] {album.Title}";
                }

                properAlbumDirectory = SanitizeFileName(properAlbumDirectory);


                //
                // Artist Directory
                //

                directory = Path.GetDirectoryName(directory);

                string currentArtistDirectory = Path.GetFileName(directory);
                string properArtistDirectory = SanitizeFileName(recording.Artist.Name);

                //
                // Root Directory
                //

                string currentRoot = Path.GetDirectoryName(directory);


                if (properFileName != currentFileName ||
                    properAlbumDirectory != currentAlbumDirectory ||
                    properArtistDirectory != currentArtistDirectory ||
                    newMusicRoot != currentRoot)
                {
                    string newPath = Path.Combine(
                        newMusicRoot,
                        properArtistDirectory,
                        properAlbumDirectory,
                        $"{properFileName}{extension}");

                    reorgTargets.Add(new FileReorganizerDTO()
                    {
                        NewPath = newPath,
                        Data = recording,
                        Possible = !File.Exists(newPath)
                    });

                    if (reorgTargets.Count > 1000)
                    {
                        break;
                    }

                    continue;
                }
            }

            return reorgTargets;
        }


        void IFileReorganizerRequestHandler.ApplyReorganization(
            IEnumerable<FileReorganizerDTO> reorganizationTargets)
        {
            foreach (FileReorganizerDTO file in reorganizationTargets)
            {
                if (!file.IsChecked)
                {
                    continue;
                }

                if (File.Exists(file.NewPath))
                {
                    Console.WriteLine($"File \"{file.NewPath}\" already exists");
                    continue;
                }

                if (!Directory.Exists(Path.GetDirectoryName(file.NewPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file.NewPath));
                }

                try
                {
                    File.Move(file.Data.Filename, file.NewPath);
                }
                catch (Exception)
                {
                    Console.WriteLine($"File Move \"{file.Data.Filename}\" to \"{file.NewPath}\" failed");
                    continue;
                }

                file.Data.Filename = file.NewPath;
            }

            db.SaveChanges();
        }

        private static void RemoveEmptyDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                RemoveEmptyDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        private string SanitizeFileName(string filename) =>
            string.Join(
                "_",
                filename.Split(
                    Path.GetInvalidFileNameChars(),
                    StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.')
            .Trim();
    }
}
