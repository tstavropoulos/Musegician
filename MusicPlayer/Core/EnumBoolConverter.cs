using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Musegician.Core
{
    /// <summary>
    /// Example Usage:
    /// enum RadioOptions {Option1, Option2}
    /// <RadioButton IsChecked="{Binding SelectedOption, Converter={StaticResource EnumBooleanConverter}, ConverterParameter={x:Static local:RadioOptions.Option1}}"/>
    /// <RadioButton IsChecked = "{Binding SelectedOption, Converter={StaticResource EnumBooleanConverter}, ConverterParameter={x:Static local:RadioOptions.Option2}}" />
    /// </summary>
    public class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? parameter : Binding.DoNothing;
        }
    }
}
