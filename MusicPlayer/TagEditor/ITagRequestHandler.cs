using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagData = MusicPlayer.DataStructures.TagData;
using LibraryContext = MusicPlayer.Library.LibraryContext;

namespace MusicPlayer.TagEditor
{
    public interface ITagRequestHandler
    {
        IEnumerable<TagData> GetTagData(LibraryContext context, long id);
        IEnumerable<TagData> GetTagData(LibraryContext context, IEnumerable<long> ids);
        IEnumerable<string> GetAffectedFiles(LibraryContext context, long id);
        IEnumerable<string> GetAffectedFiles(LibraryContext context, IEnumerable<long> id);

        void UpdateRecord(LibraryContext context, IEnumerable<long> ids, MusicRecord record, string newValue);
        void UpdateRecord(LibraryContext context, IEnumerable<long> ids, MusicRecord record, long newValue);
        void UpdateRecord(LibraryContext context, IEnumerable<long> ids, MusicRecord record, bool newValue);
        void UpdateRecord(LibraryContext context, IEnumerable<long> ids, MusicRecord record, double newValue);
    }
}
