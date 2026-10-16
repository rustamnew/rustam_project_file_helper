using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileHelper.Models;

namespace MyAvaloniaApp.Services;

public class FileSorterSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FileHelper",
        "sorter_data.txt");

    public List<FileSortCategory> LoadCategories()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var categories = File.ReadAllLines(SettingsPath)
                    .Select(line => line.Split(';', 3))
                    .Where(parts => parts.Length >= 3)
                    .Select(parts => new FileSortCategory
                    {
                        Name = parts[0],
                        Extensions = parts[1],
                        IsEnabled = parts[2] != "0"
                    })
                    .ToList();
                if (categories is { Count: > 0 })
                    return categories;
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return CreateDefaultCategories();
    }

    public void SaveCategories(IReadOnlyCollection<FileSortCategory> categories)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reservedNames = new HashSet<string>(["Другое", "Папки"], StringComparer.OrdinalIgnoreCase);
        var invalidFileNameCharacters = Path.GetInvalidFileNameChars();

        foreach (var category in categories)
        {
            category.HasNameError = false;
            category.HasExtensionsError = false;
        }

        foreach (var category in categories)
        {
            var name = category.Name.Trim();
            if (name.IndexOfAny([';', '\t', '\r', '\n']) >= 0)
            {
                category.HasNameError = true;
                throw new InvalidOperationException("Укажите допустимое имя для каждой категории.");
            }

            if (!category.IsEnabled)
                continue;

            if (string.IsNullOrWhiteSpace(name) || name is "." or ".." ||
                name.IndexOfAny(invalidFileNameCharacters) >= 0 ||
                name.IndexOfAny(['/', '\\', ':', '*', '?', '"', '<', '>', '|']) >= 0 ||
                name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
            {
                category.HasNameError = true;
                throw new InvalidOperationException("Укажите допустимое имя для каждой категории.");
            }

            if (reservedNames.Contains(name) || !names.Add(name))
            {
                category.HasNameError = true;
                throw new InvalidOperationException($"Имя категории «{name}» зарезервировано или уже используется.");
            }

            var categoryExtensions = GetExtensions(category);
            if (categoryExtensions.Length == 0)
            {
                category.HasExtensionsError = true;
                throw new InvalidOperationException($"Добавьте хотя бы одно расширение в категорию «{name}».");
            }

            foreach (var extension in categoryExtensions)
            {
                if (!extensions.Add(extension))
                {
                    category.HasExtensionsError = true;
                    throw new InvalidOperationException($"Расширение «{extension}» используется в нескольких категориях.");
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var lines = categories.Select(category =>
            $"{category.Name.Trim()};{string.Join(", ", GetExtensions(category))};{(category.IsEnabled ? "1" : "0")}");
        File.WriteAllLines(SettingsPath, lines);
    }

    public static string[] GetExtensions(FileSortCategory category) => category.Extensions
        .Split([',', ';', ' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(extension => extension.TrimStart('.').ToLowerInvariant())
        .Where(extension => extension.Length > 0 && extension.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or '+'))
        .Select(extension => "." + extension)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static List<FileSortCategory> CreateDefaultCategories() =>
    [
        new() { Name = "Изображения", Extensions = ".jpg, .jpeg, .png, .gif, .ico" },
        new() { Name = "Аудио", Extensions = ".mp3, .wav, .flac" },
        new() { Name = "Видео", Extensions = ".mp4, .avi, .mkv" },
        new() { Name = "Документы", Extensions = ".doc, .docx, .pdf, .xls, .xlsx, .ppt, .pptx, .txt" },
        new() { Name = "Исполняемые файлы", Extensions = ".exe" },
        new() { Name = "Торренты", Extensions = ".torrent" },
        new() { Name = "Архивы", Extensions = ".iso, .zip, .rar, .7z, .tar" }
    ];
}