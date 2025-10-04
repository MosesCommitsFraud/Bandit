using System;
using System.Globalization;
using System.Windows.Data;

namespace Bandit.App.Converters;

public class VolumeConverter : IValueConverter
{
    // Convert from 0-1 float to 0-100 slider value
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float floatValue)
        {
            return floatValue * 100.0;
        }
        return 100.0;
    }

    // Convert from 0-100 slider value to 0-1 float
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            return (float)(doubleValue / 100.0);
        }
        return 1.0f;
    }
}


