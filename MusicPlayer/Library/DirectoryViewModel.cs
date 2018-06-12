using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using Musegician.DataStructures;

namespace Musegician.Library
{
    public class DirectoryViewModel : LibraryViewModel
    {
        #region Constructors

        public DirectoryViewModel(
            DirectoryDTO directory,
            DirectoryViewModel parent,
            bool lazyLoadChildren = true)
            : base(
                  data: directory,
                  parent: parent,
                  lazyLoadChildren: lazyLoadChildren)
        {
        }

        #endregion Constructors
        #region Properties

        public DirectoryDTO _directory => Data as DirectoryDTO;

        public DirectoryViewModel Up => Parent as DirectoryViewModel;

        public string Path
        {
            get { return System.IO.Path.Combine(
                Up?.Path ?? "",
                _directory.Name + System.IO.Path.DirectorySeparatorChar); }
        }

        public override string Name => _directory.Name;

        #endregion Properties
        #region LoadChildren

        public override void LoadChildren(ILibraryRequestHandler dataManager)
        {
            base.LoadChildren(dataManager);
            foreach (DirectoryDTO data in dataManager.GetDirectories(Path))
            {
                Children.Add(new DirectoryViewModel(data, this));
            }

            foreach (Recording data in dataManager.GetDirectoryRecordings(Path))
            {
                Children.Add(new RecordingViewModel(data, this));
            }
        }

        #endregion LoadChildren
    }
}
