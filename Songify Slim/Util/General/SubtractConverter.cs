using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Songify_Slim.Util.General
{
    internal class SubtractConverter : IValueConverter
    {
        // Converts the ActualWidth value by subtracting a given parameter.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && double.TryParse(parameter?.ToString(), out double subtractValue))
            {
                return width - subtractValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}