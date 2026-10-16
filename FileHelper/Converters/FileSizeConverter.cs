using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FileHelper.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        private const long KilobyteSize = 1024;
        private const long MegabyteSize = KilobyteSize * 1024;
        private const long GigabyteSize = MegabyteSize * 1024;
        private const long TerabyteSize = GigabyteSize * 1024;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is long longValue)
            {
                return FormatSize(longValue);
            }
            if (value is int intValue)
            {
                return FormatSize(intValue);
            }
            if (value is double doubleValue)
            {
                return FormatSize((long)doubleValue);
            }
            return "неизвестно";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < KilobyteSize)
            {
                return $"{bytes} байт";
            }
            if (bytes < MegabyteSize)
            {
                double kb = (double)bytes / KilobyteSize;
                return $"{kb:F1} Кбайт";
            }
            if (bytes < GigabyteSize)
            {
                double mb = (double)bytes / MegabyteSize;
                return $"{mb:F1} Мбайт";
            }
            if (bytes < TerabyteSize)
            {
                double gb = (double)bytes / GigabyteSize;
                return $"{gb:F1} Гбайт";
            }

            double tb = (double)bytes / TerabyteSize;
            return $"{tb:F1} Тбайт";
        }
    }
}
