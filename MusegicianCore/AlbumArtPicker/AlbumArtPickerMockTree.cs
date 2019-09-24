using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.AlbumArtPicker
{
    public class AlbumArtPickerMockTree
    {
        #region Constructor

        public AlbumArtPickerMockTree()
        {
            AlbumArtAlbumDTO parent = new AlbumArtAlbumDTO { Name = "Test 1 - Collapsed" };
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\0.jpg"), Name = @"MockDBResources\0.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\1.jpg"), Name = @"MockDBResources\1.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\2.jpg"), Name = @"MockDBResources\2.jpg" });
            ViewModels.Add(new AlbumViewModel(parent));

            parent = new AlbumArtAlbumDTO { Name = "Test 2 - Expanded" };
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\0.jpg"), Name = @"MockDBResources\0.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\1.jpg"), Name = @"MockDBResources\1.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\2.jpg"), Name = @"MockDBResources\2.jpg" });
            ViewModels.Add(new AlbumViewModel(parent));

            parent = new AlbumArtAlbumDTO { Name = "Test 3 - Collapsed" };
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\0.jpg"), Name = @"MockDBResources\0.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\1.jpg"), Name = @"MockDBResources\1.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\2.jpg"), Name = @"MockDBResources\2.jpg" });
            ViewModels.Add(new AlbumViewModel(parent));

            parent = new AlbumArtAlbumDTO {
                Name = "Test 4 - Collapsed With Image",
                Album = new Database.Album() { Image = LoadImage(@"MockDBResources\0.jpg"), Thumbnail = LoadImage(@"MockDBResources\0.jpg") }
            };
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\0.jpg"), Name = @"MockDBResources\0.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\1.jpg"), Name = @"MockDBResources\1.jpg" });
            parent.Children.Add(new AlbumArtArtDTO() { IsChecked = false, Image = LoadImage(@"MockDBResources\2.jpg"), Name = @"MockDBResources\2.jpg" });
            ViewModels.Add(new AlbumViewModel(parent));

            ViewModels[1].IsExpanded = true;

        }

        #endregion Constructor
        #region View Properties

        public ObservableCollection<AlbumArtPickerViewModel> ViewModels { get; } =
            new ObservableCollection<AlbumArtPickerViewModel>();

        #endregion View Properties
        #region Helper Methods

        private static byte[] LoadImage(string path)
        {
            string filePath = Path.Combine(FileUtility.GetDataPath(), path);
            if (File.Exists(filePath))
            {
                return File.ReadAllBytes(filePath);
            }

            return null;
        }

        #endregion Helper Methods
    }
}
