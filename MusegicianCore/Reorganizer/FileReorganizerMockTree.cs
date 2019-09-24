using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Musegician.Reorganizer
{
    public class FileReorganizerMockTree
    {
        #region Constructor

        public FileReorganizerMockTree()
        {
            ViewModels.Add(new FileReorganizerViewModel(new FileReorganizerDTO
            {
                NewPath = "C:/Music/TestArtist/TestAlbum/TestArtist - TestA.wav",
                Data = new Database.Recording() { Filename = "C:/Music/TestA.wav" },
                Possible = true,
                IsChecked = false
            }));
            ViewModels.Add(new FileReorganizerViewModel(new FileReorganizerDTO
            {
                NewPath = "C:/Music/TestArtist/TestAlbum/TestArtist - TestB.wav",
                Data = new Database.Recording() { Filename = "C:/Music/TestB.wav" },
                Possible = true,
                IsChecked = true
            }));
            ViewModels.Add(new FileReorganizerViewModel(new FileReorganizerDTO
            {
                NewPath = "C:/Music/TestArtistB/TestAlbum/TestArtistB - TestC.wav",
                Data = new Database.Recording() { Filename = "C:/Music/TestC.wav" },
                Possible = false,
                IsChecked = true
            }));
            ViewModels.Add(new FileReorganizerViewModel(new FileReorganizerDTO
            {
                NewPath = "C:/Music/TestArtist/TestAlbumB/TestArtist - TestD.wav",
                Data = new Database.Recording() { Filename = "C:/Music/TestD.wav" },
                Possible = false,
                IsChecked = false
            }));
        }

        #endregion Constructor
        #region View Properties

        public ObservableCollection<FileReorganizerViewModel> ViewModels { get; } =
            new ObservableCollection<FileReorganizerViewModel>();

        #endregion View Properties
    }
}
