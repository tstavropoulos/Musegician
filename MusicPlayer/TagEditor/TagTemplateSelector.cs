using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.TagEditor
{
    public class TagTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate LongTemplate { get; set; }

        public override DataTemplate SelectTemplate(object obj, DependencyObject c)
        {
            if (obj is DataStructures.TagDataBool)
            {
                return BoolTemplate;
            }
            else if (obj is DataStructures.TagDataString)
            {
                return StringTemplate;
            }
            else if (obj is DataStructures.TagDataLong)
            {
                return LongTemplate;
            }

            Console.WriteLine("Unidentified DataTempate: " + obj.ToString());
            return StringTemplate;
        }
    }
}
