using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FileHelper.Converters
{
    public class FileNameFromPathConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is string path)
            {
                return System.IO.Path.GetFileName(path);
            }
            return "неизвестно";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
