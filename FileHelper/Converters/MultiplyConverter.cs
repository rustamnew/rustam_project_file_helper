using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FileHelper.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        private static readonly Dictionary<int, string> SpeedMap = new()
        {
            { 1, "10мб/с" },
            { 2, "25мб/с" },
            { 3, "50мб/с" },
            { 4, "100мб/с" },
            { 5, "150мб/с" },
            { 6, "250мб/с" },
            { 7, "500мб/с" },
            { 8, "1024мб/с" },
            { 9, "5024мб/с" },
            { 10, "неограничено" }
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is double doubleValue)
            {
                int intValue = (int)Math.Round(doubleValue);
                if (SpeedMap.TryGetValue(intValue, out var speedText))
                {
                    return speedText;
                }
            }
            return "неизвестно";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
