
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace UbiDisplays.Interface.Controls
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>From here: http://stackoverflow.com/questions/2787725/how-to-display-different-enum-icons-using-xaml-only</remarks>
    [ContentProperty("Items")]
    public class EnumToObjectConverter : IValueConverter
    {
        public ResourceDictionary Items { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string key = Enum.GetName(value.GetType(), value);
            return Items[key];
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("This converter only works for one way binding");
        }
    }
}
