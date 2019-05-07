using System;
using System.IO;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;
using Musegician.AlbumArtPicker;

namespace Musegician
{
    public partial class FileManager : IAlbumArtPickerRequestHandler
    {
        IEnumerable<AlbumArtAlbumDTO> IAlbumArtPickerRequestHandler.GetAlbumArtMatches(bool includeAll)
        {
            List<AlbumArtAlbumDTO> albums = new List<AlbumArtAlbumDTO>();

            IEnumerable<Album> albumSearch = null;
            string[] filters = new string[] { "jpg", "jpeg", "png", "tiff", "bmp" };

            if (includeAll)
            {
                albumSearch = db.Albums;
            }
            else
            {
                albumSearch = db.Albums.Where(x => x.Image == null);
            }

            foreach (Album album in albumSearch)
            {
                Recording firstRecording = album.Tracks.OrderBy(x => x.TrackNumber).First().Recording;

                string directory = Path.GetDirectoryName(firstRecording.Filename);

                List<string> imagesFound = new List<string>();

                foreach (string filter in filters)
                {
                    imagesFound.AddRange(Directory.GetFiles(directory, $"*.{filter}"));
                }

                if (imagesFound.Count == 0)
                {
                    continue;
                }

                string artistNames = string.Join(", ", album.Tracks.Select(x => x.Recording.Artist).Distinct().Select(x=>x.Name));

                AlbumArtAlbumDTO albumDTO = new AlbumArtAlbumDTO()
                {
                    Name = $"{artistNames} - {album.Title}",
                    Album = album
                };
                albums.Add(albumDTO);

                foreach (string imagepath in imagesFound)
                {
                    albumDTO.Children.Add(new AlbumArtArtDTO()
                    {
                        IsChecked = false,
                        Name = imagepath,
                        Image = File.ReadAllBytes(imagepath)
                    });
                }
            }

            return albums;
        }

        void IAlbumArtPickerRequestHandler.PushChanges()
        {
            db.SaveChanges();
        }
    }
}
