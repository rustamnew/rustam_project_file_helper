using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
namespace FileHelper.ViewModels;

public partial class FileResult : ObservableObject
{
    [ObservableProperty]
    private string relativePath = string.Empty;

    [ObservableProperty]
    private string path = string.Empty;

    [ObservableProperty]
    private string hash = string.Empty;

    [ObservableProperty]
    private long sizeInBytes;
}

public class FolderHashResults
{
    public string FolderPath { get; set; } = string.Empty;
    public ObservableCollection<FileResult> Results { get; } = new();
}

/*public class FinalPathHashResult
{
    public string path { get; set; } = string.Empty;
    public string hash = string.Empty;
}*/

public class DirectoryContent
{
    public string DirectoryPath { get; set; } = string.Empty;
    public ObservableCollection<FileResult> Files { get; } = new();
}


internal class ScanProgress
{
    public int ProcessedFiles { get; set; } = 0;
    public int TotalFiles { get; set; } = 0;
    public int ProcessedBytes { get; set; } = 0;
    public int TotalBytes { get; set; } = 0;
}

public partial class FileHasherViewModel : ViewModelBase
{

    // public event PropertyChangedEventHandler? PropertyChanged;

    [ObservableProperty]
    public partial string Title { get; set; } = "Создание хешей";

    // [ObservableProperty]
    // public partial string SelectedFolder { get; set; } = string.Empty;

    // [ObservableProperty]
    // public partial bool? FoundFolder { get; set; } = null;

    [ObservableProperty]
    public partial bool IsReadyForHashing { get; set; } = false;

    [ObservableProperty]
    public partial bool IsHashing { get; set; } = false;

    [ObservableProperty]
    public partial int LoadingProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LogMessage { get; set; } = string.Empty;

    // [ObservableProperty]
    // public partial bool SeparateMediaFiles { get; set; } = false;

    [ObservableProperty]
    public partial bool SaveLogs { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowSpeedLimit { get; set; } = false;

    [ObservableProperty]
    public partial bool ExcludeHashCheckFiles { get; set; } = true;

    [ObservableProperty]
    public partial bool SaveHashCheckFilesInFolders { get; set; } = false;

    [ObservableProperty]
    public partial bool SaveHashCheckFilesNearFiles { get; set; } = false;



    // [ObservableProperty]
    // public partial bool CompareWithLocation { get; set; } = false;

    public ObservableCollection<string> ComparisonFolders { get; } = new();

    public ObservableCollection<string> ComparisonFiles { get; } = new();

    public ObservableCollection<FolderHashResults> ComparisonFoldersResults { get; } = new();

    public ObservableCollection<FileResult> ComparisonFilesResults { get; } = new();

    [ObservableProperty]
    public partial bool ResultReady { get; set; } = false;

    [ObservableProperty]
    public partial string ResultFinal { get; set; } = "";

    [ObservableProperty]
    public partial string ResultLocation { get; set; } = AppContext.BaseDirectory;


    [RelayCommand]
    public async Task AddFolderCommand()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
                return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выбрать папку",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string folderPath = folders[0].Path.LocalPath;
                if (!ComparisonFolders.Contains(folderPath) && Directory.Exists(folderPath))
                {
                    ComparisonFolders.Add(folderPath);
                    StartScanCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanStartScan));

                }

            }
        }
        catch (Exception ex)
        {
            Message = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task AddFilesCommand()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выбрать файлы",
                AllowMultiple = true
            });

            if (files.Count > 0)
            {

                foreach (var file in files)
                {
                    string localPath = file.Path.LocalPath;

                    // Ограничение Windows, не даёт выбирать папки и файлы (?)
                    /*if (!ComparisonFolders.Contains(localPath) && Directory.Exists(localPath))
                    {
                        ComparisonFolders.Add(localPath);
                        StartScanCommand.NotifyCanExecuteChanged();
                        OnPropertyChanged(nameof(CanStartScan));
                    }*/
                    if (!ComparisonFolders.Contains(localPath) && File.Exists(localPath))
                    {
                        ComparisonFiles.Add(localPath);
                        StartScanCommand.NotifyCanExecuteChanged();
                        OnPropertyChanged(nameof(CanStartScan));
                    }
                }


            }
        }
        catch (Exception ex)
        {
            Message = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RemoveComparisonFolder(string folderPath)
    {
        ComparisonFolders.Remove(folderPath);
    }
    [RelayCommand]
    public void RemoveComparisonFile(string filePath)
    {
        ComparisonFiles.Remove(filePath);
    }


    [RelayCommand]
    public async Task SaveResultAs()
    {
        string contentToSave = ResultFinal;

        var dateTimeNow = DateTime.Now;
        string filename = $"hash_check_{dateTimeNow:yyyyMMddHHmmss}";


        string extension = "txt";


        try
        {

            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel == null)
                return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить файл хеша",
                DefaultExtension = extension,
                SuggestedFileName = filename,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text Document")
                    {
                        Patterns = new[] { "*.txt" }
                    }
                }
            });

            if (file != null)
            {
                // Open a stream for writing and write the content
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(contentToSave);
            }




            /*var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выбрать папку",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                string folderPath = folders[0].Path.LocalPath;
                if (!ComparisonFolders.Contains(folderPath) && Directory.Exists(folderPath))
                {
                    ComparisonFolders.Add(folderPath);
                    StartScanCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanStartScan));

                }

            }*/


        }
        catch (Exception ex)
        {
            Message = $"Ошибка: {ex.Message}";
        }
    }


    // [ObservableProperty]
    // public partial int DiskReadSpeedSliderValueInt { get; set; } = 1;
    // [ObservableProperty]
    // public partial string DiskReadSpeedSliderValueTitle { get; set; } = "10мб/с";

    public ObservableCollection<FileResult> HashResults { get; } = new();

    public bool CanStartScan => ComparisonFolders.Count > 0 || ComparisonFiles.Count > 0;


    partial void OnIsReadyForHashingChanged(bool value)
    {
        StartScanCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanStartScan));
    }

    /// <summary>
    /// Validates if a folder path exists
    /// </summary>
    private bool ValidateFolderPath(string? folderPath)
    {
        var text = folderPath?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(text) && Directory.Exists(text);
    }

    /// <summary>
    /// Sets and validates the SelectedFolder
    /// </summary>
    public void ValidateAndSetFolders(string? selectedFolder, string? comparisonFolder = null)
    {
        /*if (!string.IsNullOrWhiteSpace(selectedFolder))
        {
            SelectedFolder = selectedFolder;
        }*/

        if (!string.IsNullOrWhiteSpace(comparisonFolder) && ValidateFolderPath(comparisonFolder))
        {
            if (!ComparisonFolders.Contains(comparisonFolder))
            {
                ComparisonFolders.Add(comparisonFolder);

                StartScanCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanStartScan));
            }
        }
    }



    [RelayCommand(CanExecute = nameof(CanStartScan))]
    public async Task StartScan()
    {
        ResultReady = false;
        ResultFinal = string.Empty;
        IsHashing = true;
        LoadingProgress = 0;
        LogMessage = string.Empty;
        Message = string.Empty;
        ComparisonFoldersResults.Clear();
        ComparisonFilesResults.Clear();

        var cancelToken = CancellationToken.None;
        var limitMbPerSec = 0;
        StorageType storageType = StorageType.Ssd;

        /*if (ShowSpeedLimit)
        {
            limitMbPerSec = DiskReadSpeedSliderValueInt switch
            {
                1 => 10,
                2 => 25,
                3 => 50,
                4 => 100,
                5 => 150,
                6 => 250,
                7 => 500,
                8 => 1024,
                9 => 5024,
                _ => int.MaxValue
            };
        }*/

        var dateTimeNow = DateTime.Now;
        string hashResultsFileName = $"hash_check_{dateTimeNow:yyyyMMddHHmmss}.txt";

        var progress = new Progress<int>(percent => LoadingProgress = percent);
        var logger = new Progress<string>(msg => LogMessage = msg + "\n" + LogMessage);
        var scanProgress = new ScanProgress();

        var tasks = new List<Task>();

        int totalFilesToProcess = 0;

        // Добавляем все папки для сравнения
        foreach (var comparisonFolder in ComparisonFolders)
        {
            var folderResult = new FolderHashResults { FolderPath = comparisonFolder };
            ComparisonFoldersResults.Add(folderResult);
            tasks.Add(ScanDirectoryAsync(comparisonFolder, comparisonFolder, folderResult.Results, scanProgress, cancelToken, limitMbPerSec, storageType, progress, logger, ExcludeHashCheckFiles));
            totalFilesToProcess += CountFilesRecursive(comparisonFolder);
        }
        // Добавляем все файлы для сравнения
        foreach (var comparisonFile in ComparisonFiles)
        {

            var fileResult = new FileResult { RelativePath = comparisonFile };
            ComparisonFilesResults.Add(fileResult);
            totalFilesToProcess++;

            string directoryPath = Path.GetDirectoryName(comparisonFile) ?? ".";
            tasks.Add(ScanFileAsync(directoryPath, comparisonFile, fileResult, scanProgress, cancelToken, limitMbPerSec, storageType, progress, logger));
        }

        scanProgress.TotalFiles = totalFilesToProcess;
        scanProgress.ProcessedFiles = 0;


        // Ждём выполнения всех задач параллельно
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Message = $"Ошибка сканирования: {ex.Message}";
            LogMessage = $"Ошибка сканирования: {ex}\n" + LogMessage;
            IsHashing = false;
            return;
        }

        // Запись файла с результатами

        try
        {
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resultBuilder = new StringBuilder();
            string resultsFileNamePath = Path.Combine(ResultLocation, hashResultsFileName);

            foreach (var folderResult in ComparisonFoldersResults)
            {
                foreach (var result in folderResult.Results)
                {
                    if (!addedPaths.Add(result.Path))
                        continue;

                    AppendResult(resultBuilder, result);
                }
            }
            foreach (var fileResult in ComparisonFilesResults)
            {
                if (!addedPaths.Add(fileResult.Path))
                    continue;

                AppendResult(resultBuilder, fileResult);
            }
            ResultFinal = resultBuilder.ToString();

            // Сохранение истории логов
            if (SaveLogs)
            {
                try
                {
                    string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string historyFolder = Path.Combine(applicationDataFolder, "FileHelper");
                    Directory.CreateDirectory(historyFolder);

                    string historyPath = Path.Combine(historyFolder, "hash_log.txt");
                    var historyLines = new List<string>
                    {
                        $"{dateTimeNow:yyyy-MM-dd HH:mm:ss}"
                    };

                    foreach (var folderResult in ComparisonFoldersResults)
                    {
                        historyLines.Add($"[Folder]{folderResult.FolderPath}");
                        historyLines.Add($"[FolderFiles]:");

                        foreach (var result in folderResult.Results)
                        {
                            historyLines.Add(result.RelativePath);
                            historyLines.Add(result.Hash);
                        }
                    }

                    if (ComparisonFilesResults.Count > 0)
                    {
                        historyLines.Add("[Files]");

                        foreach (var result in ComparisonFilesResults)
                        {
                            historyLines.Add(result.Path);
                            historyLines.Add(result.Hash);
                        }
                    }

                    string historyEntry = string.Join(Environment.NewLine, historyLines) +
                        Environment.NewLine + Environment.NewLine;
                    await File.AppendAllTextAsync(historyPath, historyEntry);


                }
                catch (Exception ex)
                {
                    LogMessage = $"Ошибка при записи истории хеширования: {ex.Message}\n" + LogMessage;
                }
            }



            using (var writer = new StreamWriter(resultsFileNamePath))
            {
                await writer.WriteLineAsync(ResultFinal);
            }

            if (SaveHashCheckFilesInFolders)
            {
                foreach (var folderResult in ComparisonFoldersResults)
                {
                    try
                    {
                        var folderResultsPath = Path.Combine(folderResult.FolderPath, hashResultsFileName);
                        var folderResultContent = string.Join(
                            Environment.NewLine + Environment.NewLine,
                            folderResult.Results.Select(result =>
                                $"Хэш SHA256 {result.Path}{Environment.NewLine}{result.Hash}"));

                        await File.WriteAllTextAsync(folderResultsPath, folderResultContent + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        LogMessage = $"Ошибка при сохранении файла хешей в папке {folderResult.FolderPath}: {ex.Message}\n" + LogMessage;
                    }
                }
            }
            if (SaveHashCheckFilesNearFiles)
            {
                var listDirectories = new List<DirectoryContent>();

                foreach (var fileResult in ComparisonFilesResults)
                {
                    try
                    {
                        var fileDirectory = Path.GetDirectoryName(fileResult.Path);
                        if (string.IsNullOrEmpty(fileDirectory))
                        {
                            fileDirectory = ".";
                        }

                        // 1. Нормализуем путь директории файла для надежного сравнения
                        string normalizedFileDir = Path.GetFullPath(fileDirectory)
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                        // 2. Проверяем, есть ли эта папка уже в результатах сканирования целых папок
                        bool isFolderAlreadyProcessed = ComparisonFoldersResults.Any(fr =>
                        {
                            string normalizedFolder = Path.GetFullPath(fr.FolderPath)
                                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                            return string.Equals(normalizedFolder, normalizedFileDir, StringComparison.OrdinalIgnoreCase);
                        });

                        if (isFolderAlreadyProcessed)
                        {
                            // Пропускаем файл, так как его папка уже была добавлена для хеширования целиком.
                            // Хеш-файл для этой папки будет создан в блоке SaveHashCheckFilesInFolders.
                            continue;
                        }

                        // 3. Если папка не была обработана целиком, ищем или создаем запись для нее
                        var existingDir = listDirectories.FirstOrDefault(dc =>
                            string.Equals(
                                Path.GetFullPath(dc.DirectoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                normalizedFileDir,
                                StringComparison.OrdinalIgnoreCase));

                        if (existingDir == null)
                        {
                            existingDir = new DirectoryContent { DirectoryPath = fileDirectory }; // Сохраняем оригинальный путь для записи
                            listDirectories.Add(existingDir);
                        }

                        existingDir.Files.Add(fileResult);
                    }
                    catch (Exception ex)
                    {
                        LogMessage = $"Ошибка при обработке файла для сохранения хеша рядом с файлом {fileResult.Path}: {ex.Message}\n" + LogMessage;
                    }
                }

                // 4. Сохраняем файлы хешей для тех директорий, которые не были обработаны целиком
                foreach (var directoryContent in listDirectories)
                {
                    var fileResultsPath = Path.Combine(directoryContent.DirectoryPath, hashResultsFileName);
                    try
                    {
                        // Собираем содержимое всех файлов в этой директории в одну строку
                        var fileResultContent = string.Join(
                            Environment.NewLine + Environment.NewLine,
                            directoryContent.Files.Select(file => $"Хэш SHA256 {file.Path}{Environment.NewLine}{file.Hash}"));

                        // Записываем всё за один раз (быстрее и надежнее, чем AppendAllText в цикле)
                        await File.WriteAllTextAsync(fileResultsPath, fileResultContent + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        LogMessage = $"Ошибка при сохранении файла хешей рядом с файлами в папке {directoryContent.DirectoryPath}: {ex.Message}\n" + LogMessage;
                    }
                }
            }

            OpenFileInEditor(resultsFileNamePath);

            ResultReady = true;


            // Открываем первый файл результатов
            /*if (ComparisonFoldersResults.Count > 0)
            {
                string firstResultsFile = Path.Combine(ComparisonFoldersResults[0].FolderPath, hashResultsFileName);
                OpenFileInEditor(firstResultsFile);
            }*/


        }
        catch (Exception ex)
        {
            LogMessage = $"Ошибка при записи файла результатов: {ex.Message}\n" + LogMessage;
        }

        // int totalFiles = ComparisonFoldersResults.Sum(x => x.Results.Count);
        Message = totalFilesToProcess > 0
            ? $"Проверка завершена! Обработано файлов: {totalFilesToProcess}"
            : "Проверка завершена! Файлы не найдены.";
        IsHashing = false;
    }

    private static void AppendResult(StringBuilder builder, FileResult result)
    {
        builder.Append("Хэш SHA256 ")
            .AppendLine(result.Path)
            .AppendLine(result.Hash)
            .AppendLine();
    }

    private static async Task ScanDirectoryAsync(
            string rootPath,
            string folderPath,
            ObservableCollection<FileResult> results,
            ScanProgress scanProgress,
            CancellationToken cancellationToken,
            int limitMbPerSec,
            StorageType storageType,
            IProgress<int>? progress,
            IProgress<string>? logger,
            bool excludeHashCheckFiles = false)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(folderPath))
            {
                if (excludeHashCheckFiles && Path.GetFileName(file).StartsWith("hash_check_"))
                    continue;

                await ProcessFileAsync(file, rootPath, results, scanProgress, cancellationToken, limitMbPerSec, storageType, progress, logger, excludeHashCheckFiles);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            logger?.Report($"Не удалось прочитать файлы в папке {folderPath}: {ex.Message}");
        }

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(folderPath))
            {
                await ScanDirectoryAsync(rootPath, directory, results, scanProgress, cancellationToken, limitMbPerSec, storageType, progress, logger, excludeHashCheckFiles);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            logger?.Report($"Не удалось прочитать вложенные папки в {folderPath}: {ex.Message}");
        }
    }

    private static async Task ScanFileAsync(
            string rootPath,
            string filePath,
            FileResult result,
            ScanProgress scanProgress,
            CancellationToken cancellationToken,
            int limitMbPerSec,
            StorageType storageType,
            IProgress<int>? progress,
            IProgress<string>? logger,
            bool excludeHashCheckFiles = false)
    {

        if (excludeHashCheckFiles && Path.GetFileName(filePath).StartsWith("hash_check_"))
        {
            return;
        }
        else
        {
            await Process2FileAsync2(filePath, rootPath, result, scanProgress, cancellationToken, limitMbPerSec, storageType, progress, logger, excludeHashCheckFiles);
        }


    }

    private static int CountFilesRecursive(string folderPath)
    {
        int count = 0;
        try
        {
            foreach (string _ in Directory.EnumerateFiles(folderPath))
                count++;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Debug.WriteLine($"Не удалось посчитать файлы в {folderPath}: {ex.Message}");
        }

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(folderPath))
            {
                count += CountFilesRecursive(directory);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Debug.WriteLine($"Не удалось получить вложенные папки в {folderPath}: {ex.Message}");
        }

        return count;
    }



    private static async Task ProcessFileAsync(
        string filePath,
        string rootPath,
        ObservableCollection<FileResult> results,
        ScanProgress scanProgress,
        CancellationToken cancellationToken,
        // int limitMbPerSec = 50,
        int limitMbPerSec = 0,
        StorageType storageType = StorageType.Ssd,
        IProgress<int>? progress = null,
        IProgress<string>? logger = null,
        bool excludeHashCheckFiles = false)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            long bytes = fileInfo.Length;

            var fileProgress = new Progress<(long read, long total, double speed)>((info) =>
            {
                double percentage = info.total > 0 ? info.read * 100.0 / info.total : 100.0;
                logger?.Report($"Файл: {Path.GetFileName(filePath)} - {percentage:F1}% ({info.speed:F2} мб/с)");
                // logger?.Report($"Файл: {Path.GetFileName(filePath)} - {(info.total / 1024.0 / 1024.0):F2} МБ");
            });

            string relativePath = Path.GetRelativePath(rootPath, filePath);

            var result = new FileResult
            {
                RelativePath = relativePath,
                Path = filePath,
                Hash = "Загрузка...",
                SizeInBytes = bytes
            };
            results.Add(result);

            var hasher = limitMbPerSec > 0 ? new ThrottledFileHasher(limitMbPerSec) : new ThrottledFileHasher();
            string hash = await hasher.HashFileWithThrottleAsync(filePath, storageType, fileProgress, cancellationToken);


            result.Hash = hash;

            scanProgress.ProcessedFiles++;
            if (scanProgress.TotalFiles > 0)
            {
                int percent = (int)((scanProgress.ProcessedFiles * 100) / scanProgress.TotalFiles);
                progress?.Report(percent);
            }
        }
        catch (Exception ex)
        {
            logger?.Report($"Ошибка при обработке {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    private static async Task Process2FileAsync2(
        string filePath,
        string rootPath,
        FileResult result,
        ScanProgress scanProgress,
        CancellationToken cancellationToken,
        // int limitMbPerSec = 50,
        int limitMbPerSec = 0,
        StorageType storageType = StorageType.Ssd,
        IProgress<int>? progress = null,
        IProgress<string>? logger = null,
        bool excludeHashCheckFiles = false)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            long bytes = fileInfo.Length;

            var fileProgress = new Progress<(long read, long total, double speed)>((info) =>
            {
                double percentage = info.total > 0 ? info.read * 100.0 / info.total : 100.0;
                logger?.Report($"Файл: {Path.GetFileName(filePath)} - {percentage:F1}% ({info.speed:F2} мб/с)");
                // logger?.Report($"Файл: {Path.GetFileName(filePath)} - {(info.total / 1024.0 / 1024.0):F2} МБ");
            });


            result.RelativePath = Path.Combine(rootPath, filePath);
            result.Path = filePath;
            result.Hash = "Загрузка...";
            result.SizeInBytes = bytes;

            var hasher = limitMbPerSec > 0 ? new ThrottledFileHasher(limitMbPerSec) : new ThrottledFileHasher();
            string hash = await hasher.HashFileWithThrottleAsync(filePath, storageType, fileProgress, cancellationToken);

            result.Hash = hash;

            scanProgress.ProcessedFiles++;
            if (scanProgress.TotalFiles > 0)
            {
                int percent = (int)((scanProgress.ProcessedFiles * 100) / scanProgress.TotalFiles);
                progress?.Report(percent);
            }
        }
        catch (Exception ex)
        {
            logger?.Report($"Ошибка при обработке {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    private void OpenFileInEditor(string filePath)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true // Важно для Windows: откроет программу по умолчанию
            };

            // Корректировка для Linux, если UseShellExecute не срабатывает напрямую для файлов
            if (OperatingSystem.IsLinux())
            {
                psi.FileName = "xdg-open";
                psi.Arguments = $"\"{filePath}\"";
                psi.UseShellExecute = false;
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            // Обработка ошибок, если файл не удалось открыть автоматически
            Debug.WriteLine($"Не удалось открыть файл: {ex.Message}");
        }
    }



    public void ProcessDroppedItems(List<string> paths)
    {
        foreach (var path in paths)
        {

            if (Directory.Exists(path))
            {
                ComparisonFolders.Add(path);

                StartScanCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanStartScan));
            }
            else if (File.Exists(path))
            {
                // Это одиночный файл
                ComparisonFiles.Add(path);

                StartScanCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanStartScan));
            }

        }
    }



}
