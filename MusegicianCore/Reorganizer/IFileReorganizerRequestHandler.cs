using System;
using System.Collections.Generic;

namespace Musegician.Reorganizer
{
    public interface IFileReorganizerRequestHandler
    {
        IEnumerable<FileReorganizerDTO> GetReorganizerTargets(string newMusicPath);
        void ApplyReorganization(IEnumerable<FileReorganizerDTO> reorganizationTargets);
    }
}
