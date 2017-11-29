using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.TagEditor
{
    public class TagEditorSelector : DataTemplateSelector
    {
        public DataTemplate BoolEditorTemplate { get; set; }
        public DataTemplate StringEditorTemplate { get; set; }
        public DataTemplate LongEditorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object obj, DependencyObject c)
        {
            if (obj is DataStructures.TagDataBool)
            {
                return BoolEditorTemplate;
            }
            else if (obj is DataStructures.TagDataString)
            {
                return StringEditorTemplate;
            }
            else if (obj is DataStructures.TagDataLong)
            {
                return LongEditorTemplate;
            }

            Console.WriteLine("Unidentified DataTempate: " + obj.ToString());
            return StringEditorTemplate;
        }
    }
}
