using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using TagData = Musegician.DataStructures.TagData;

namespace Musegician.TagEditor
{
    public interface ITagRequestHandler
    {
        IEnumerable<TagData> GetTagData(BaseData data);
        IEnumerable<TagData> GetTagData(IEnumerable<BaseData> data);
        IEnumerable<string> GetAffectedFiles(BaseData data);
        IEnumerable<string> GetAffectedFiles(IEnumerable<BaseData> data);

        void UpdateRecord(IEnumerable<BaseData> data, MusicRecord record, string newValue);
        void UpdateRecord(IEnumerable<BaseData> data, MusicRecord record, int newValue);
        void UpdateRecord(IEnumerable<BaseData> data, MusicRecord record, bool newValue);
        void UpdateRecord(IEnumerable<BaseData> data, MusicRecord record, double newValue);

        void PushChanges();
    }
}
