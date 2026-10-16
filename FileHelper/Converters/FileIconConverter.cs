using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;

namespace FileHelper.Converters;

public class FileIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path)
        {
            if (Directory.Exists(path)) return "📂";

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".ico") return "🖼️";
            if (ext is ".mp3" or ".wav" or ".flac") return "🎵";
            if (ext is ".mp4" or ".avi" or ".mkv") return "🎬";
            if (ext is ".zip" or ".rar" or ".7z" or ".tar" or ".iso") return "📦";
            if (ext is ".exe") return "⚙️";
            if (ext is ".torrent") return "🔄";
            if (ext is ".doc" or ".docx" or ".pdf" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt") return "📄";

            return "📄"; // По умолчанию
        }
        return "❓";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}