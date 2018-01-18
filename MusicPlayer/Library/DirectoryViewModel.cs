using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public class DirectoryViewModel : LibraryViewModel
    {
        #region Constructors

        public DirectoryViewModel(
            DirectoryDTO album,
            DirectoryViewModel parent,
            bool lazyLoadChildren = true)
            : base(
                  data: album,
                  parent: parent,
                  lazyLoadChildren: lazyLoadChildren)
        {
        }

        #endregion Constructors
        #region Properties

        public DirectoryDTO _directory
        {
            get { return Data as DirectoryDTO; }
        }

        public DirectoryViewModel Up
        {
            get { return Parent as DirectoryViewModel; }
        }

        public string Path
        {
            get { return System.IO.Path.Combine(
                Up?.Path ?? "",
                _directory.Name + System.IO.Path.DirectorySeparatorChar); }
        }

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach (DirectoryDTO data in dataManager.GetDirectories(Path))
            {
                Data.Children.Add(data);
                Children.Add(new DirectoryViewModel(data, this));
            }

            foreach (RecordingDTO data in dataManager.GetDirectoryRecordings(Path))
            {
                Data.Children.Add(data);
                Children.Add(new RecordingViewModel(data, this));
            }
        }

        #endregion LoadChildren
    }
}
