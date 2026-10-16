using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FileHelper.Converters;

public class PlusOneConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue) return intValue + 1;
        if (value is long longValue) return longValue + 1;
        // Если данные приходят строкой:
        if (value != null && int.TryParse(value.ToString(), out int parsed)) return parsed + 1;

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}