using System.Globalization;
using System.Windows;
using System;
using System.Windows.Data;

namespace Songify_Slim.Util
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            bool invert = parameter is string s &&
                          s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

            if (invert)
                flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}