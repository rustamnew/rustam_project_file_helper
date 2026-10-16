using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
// namespace MyAvaloniaApp.Services
// {
public class FileSearcherService
{
    private readonly List<string> fileList = new();
    private readonly List<string> directoryList = new();
    // Метод принимает путь к папке, которую нужно убрать
    public string[] SearchFilesInFolder(
        string sourceFolder,
        string searchQuery,
        IProgress<int>? progress = null,
        IProgress<List<string>>? result = null/*,
            bool matchCase = false,
            bool matchWholeWord = false,
            bool useRegular = false,
            bool recursive = false*/
        )
    {
        fileList.Clear();
        directoryList.Clear();

        // int totalItems = 0;
        // int processedItems = 0;

        // Список файлов
        // List<string> fileList = new List<string>();

        // Список папок
        // List<string> directoryList = new List<string>();

        // Список ошибок при перемещении
        List<string> failedOperations = new List<string>();

        // Переменная для хранения пути к выбранной папке
        string folderPath = sourceFolder;


        DirectoryInfo subDir = new DirectoryInfo(folderPath);

        if (!Directory.Exists(folderPath))
        {
            // Console.WriteLine("Указанный путь не найден");
            progress?.Report(100);
            return new string[0];
        }

        if (subDir.GetFiles().Length == 0 && subDir.GetDirectories().Length == 0)
        {
            // Console.WriteLine("Папка пуста");
            progress?.Report(100);
            return new string[0];
        }

        ScanDirectoryAsync(folderPath, folderPath, result, CancellationToken.None).Wait();

        progress?.Report(100);

        return fileList.ToArray();
    }

    private async Task ScanDirectoryAsync(
    string rootPath,
    string folderPath,
    IProgress<List<string>>? result,
    CancellationToken cancellationToken)
    {
        foreach (string file in Directory.GetFiles(folderPath))
        {
            // fileList.Add(file);
            // result?.Report(new List<string> { Path.GetRelativePath(rootPath, file) });
            await ProcessFileAsync(file, rootPath, result, cancellationToken);
        }

        foreach (string directory in Directory.GetDirectories(folderPath))
        {
            directoryList.Add(directory);
            await ScanDirectoryAsync(rootPath, directory, result, cancellationToken);
        }
    }

    private async Task ProcessFileAsync(
    string filePath,
    string folderPath,
    IProgress<List<string>>? result,
    CancellationToken cancellationToken)
    {
        var progress = new Progress<(long read, long total, double speed)>(_ => { });

        fileList.Add(filePath);
        result?.Report(new List<string> { Path.GetRelativePath(filePath, folderPath) });
    }

}

// }