using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FileHelper.Converters;

public class ConfirmedFilesColorConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not int foundFiles || values[1] is not int confirmedFiles)
            return Brushes.Black;

        if (foundFiles > 0 && foundFiles > confirmedFiles)
            return Brushes.Red;

        if (confirmedFiles > 0 && confirmedFiles == foundFiles)
        {
            return Brushes.Green;
        }

        return Brushes.Black;
    }
}