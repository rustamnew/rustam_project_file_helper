using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyAvaloniaApp.Services;
namespace FileHelper.ViewModels;


using System.Diagnostics;



public partial class ComparisonHashResultFile : ObservableObject
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public string DateText => File.Exists(FilePath)
        ? File.GetLastWriteTime(FilePath).ToString("dd.MM.yyyy")
        : string.Empty;
    public bool HasDate => File.Exists(FilePath);

    [ObservableProperty]
    public partial bool? IsValid { get; set; } = null;
}

public partial class HashFile : ObservableObject
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public int FoundFiles { get; set; }
    public int ConfirmedFiles { get; set; }
    public List<HashFile> RelatedFiles { get; set; } = new();

    [ObservableProperty]
    public partial bool IsRelatedFilesVisible { get; set; } = false;

}
public partial class ComparisonResult : ObservableObject
{
    public string SourceFileName { get; set; } = string.Empty;
    // public int TotalFiles { get; set; } = 0;
    // public int ProceccedFiles { get; set; } = 0;
    public int HashConfirmed { get; set; } = 0;
    public int HashMismatch { get; set; } = 0;
    public int SingleFiles { get; set; } = 0;

    public List<HashFile> FileList { get; set; } = new();
}

public partial class HashHistoryFile : ObservableObject
{
    public string FilePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public partial class HashHistoryFolder : ObservableObject
{
    public string FolderPath { get; set; } = string.Empty;
    public ObservableCollection<HashHistoryFile> Files { get; } = new();

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public class HashHistorySelection
{
    public object Source { get; init; } = null!;
    public string Name { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public IReadOnlyList<HashHistoryFile> Files { get; init; } = Array.Empty<HashHistoryFile>();
}

public class HashHistoryEntry
{
    public string DateText { get; set; } = string.Empty;
    public ObservableCollection<HashHistoryFolder> Folders { get; } = new();
    public ObservableCollection<HashHistoryFile> Files { get; } = new();
    public bool HasFiles => Files.Count > 0;
}

public partial class FileHashReaderViewModel : ViewModelBase
{
    private bool _isHistoryComparison;

    public FileHashReaderViewModel()
    {
        RefreshHashHistory();
    }

    [ObservableProperty]
    public partial string Title { get; set; } = "Добавление файлов";

    [ObservableProperty]
    public partial bool ShowFileFullPath { get; set; } = false;

    partial void OnShowFileFullPathChanged(bool value)
    {
        if (_isHistoryComparison)
            CompareSelectedHistory();
        else
            ShowFileHash();
    }

    // [ObservableProperty]
    // public partial bool ShowFileSize { get; set; } = false;

    [ObservableProperty]
    public partial bool CanStart { get; set; } = false;

    public ObservableCollection<ComparisonHashResultFile> ComparisonFiles { get; } = new();

    public ObservableCollection<ComparisonResult> ComparisonResult { get; } = new();

    public ObservableCollection<HashHistoryEntry> HashHistory { get; } = new();

    public ObservableCollection<HashHistorySelection> SelectedHistoryItems { get; } = new();

    [ObservableProperty]
    public partial string HistoryMessage { get; set; } = string.Empty;

    [RelayCommand]
    public void RefreshHashHistory()
    {
        HashHistory.Clear();
        HistoryMessage = string.Empty;

        string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string historyPath = Path.Combine(applicationDataFolder, "FileHelper", "hash_log.txt");

        if (!File.Exists(historyPath))
        {
            HistoryMessage = "История хеширования не найдена.";
            return;
        }

        try
        {
            var parsedHistory = new List<HashHistoryEntry>();
            HashHistoryEntry? currentEntry = null;
            HashHistoryFolder? currentFolder = null;
            string? pendingPath = null;

            foreach (string line in File.ReadLines(historyPath))
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.Length == 0)
                    continue;

                if (DateTime.TryParseExact(trimmedLine, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime date))
                {
                    if (currentEntry != null)
                        parsedHistory.Add(currentEntry);

                    currentEntry = new HashHistoryEntry { DateText = date.ToString("yyyy-MM-dd HH:mm:ss") };
                    currentFolder = null;
                    pendingPath = null;
                    continue;
                }

                if (currentEntry == null)
                    continue;

                if (trimmedLine.StartsWith("[Folder]", StringComparison.OrdinalIgnoreCase))
                {
                    string folderPath = trimmedLine["[Folder]".Length..].Trim().TrimEnd(':').TrimEnd();
                    currentFolder = new HashHistoryFolder { FolderPath = folderPath };
                    currentEntry.Folders.Add(currentFolder);
                    pendingPath = null;
                    continue;
                }

                if (trimmedLine.Equals("[FolderFiles]:", StringComparison.OrdinalIgnoreCase))
                {
                    pendingPath = null;
                    continue;
                }

                if (trimmedLine.Equals("[Files]", StringComparison.OrdinalIgnoreCase))
                {
                    currentFolder = null;
                    pendingPath = null;
                    continue;
                }

                if (pendingPath == null)
                {
                    pendingPath = line;
                    continue;
                }

                if (IsSha256(trimmedLine))
                {
                    var historyFile = new HashHistoryFile
                    {
                        FilePath = pendingPath,
                        Hash = trimmedLine
                    };

                    if (currentFolder != null)
                        currentFolder.Files.Add(historyFile);
                    else
                        currentEntry.Files.Add(historyFile);

                    pendingPath = null;
                    continue;
                }

                pendingPath = line;
            }

            if (currentEntry != null)
            {
                parsedHistory.Add(currentEntry);
            }
                
            if (parsedHistory.Count > 0)
            {
                parsedHistory.Reverse();
            }

            foreach (var historyEntry in parsedHistory)
            {
                foreach (var folder in historyEntry.Folders)
                {
                    folder.PropertyChanged += OnHistorySelectionChanged;
                }

                foreach (var file in historyEntry.Files)
                {
                    file.PropertyChanged += OnHistorySelectionChanged;
                }

                HashHistory.Add(historyEntry);
            }

            if (HashHistory.Count == 0)
            {
                HistoryMessage = "В журнале нет записей о хешировании.";
            }
                
        }
        catch (Exception ex)
        {
            HashHistory.Clear();
            HistoryMessage = $"Не удалось прочитать историю: {ex.Message}";
        }
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }

    private void OnHistorySelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HashHistoryFile.IsSelected) &&
            e.PropertyName != nameof(HashHistoryFolder.IsSelected))
        {
            return;
        }

        if (sender is HashHistoryFolder folder)
        {
            var selectedItem = SelectedHistoryItems.FirstOrDefault(item => ReferenceEquals(item.Source, folder));
            if (folder.IsSelected && selectedItem == null)
            {
                SelectedHistoryItems.Add(new HashHistorySelection
                {
                    Source = folder,
                    Name = folder.FolderPath,
                    SourcePath = folder.FolderPath,
                    Files = folder.Files.ToList()
                });
            }
            else if (!folder.IsSelected && selectedItem != null)
            {
                SelectedHistoryItems.Remove(selectedItem);
            }
        }
        else if (sender is HashHistoryFile file)
        {
            var selectedItem = SelectedHistoryItems.FirstOrDefault(item => ReferenceEquals(item.Source, file));
            if (file.IsSelected && selectedItem == null)
            {
                SelectedHistoryItems.Add(new HashHistorySelection
                {
                    Source = file,
                    Name = file.FilePath,
                    SourcePath = file.FilePath,
                    Files = new[] { file }
                });
            }
            else if (!file.IsSelected && selectedItem != null)
            {
                SelectedHistoryItems.Remove(selectedItem);
            }
        }

        CompareSelectedHistoryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveSelectedHistoryItem(HashHistorySelection item)
    {
        SelectedHistoryItems.Remove(item);

        if (item.Source is HashHistoryFolder folder)
            folder.IsSelected = false;
        else if (item.Source is HashHistoryFile file)
            file.IsSelected = false;
    }


    [RelayCommand]
    public void RemoveComparisonFile(string filePath)
    {
        var item = ComparisonFiles.FirstOrDefault(x => x.FilePath == filePath);
        if (item != null)
            ComparisonFiles.Remove(item);
    }

    [RelayCommand]
    public void ShowFileHash()
    {
        _isHistoryComparison = false;
        var expandedRows = ComparisonResult
            .SelectMany(result => result.FileList)
            .GroupBy(file => (file.SourceFilePath, file.FilePath, file.FileHash))
            .ToDictionary(
                group => group.Key,
                group => new Queue<bool>(group.Select(file => file.IsRelatedFilesVisible)));

        ComparisonResult.Clear();
        var results = new List<ComparisonResult>();

        foreach (var file in ComparisonFiles.Where(item => item.IsValid == true))
        {
            var parsedFiles = HashFileParser.Parse(file.FilePath);
            parsedFiles = parsedFiles./*OrderBy(item => item.Hash).*/ToList();
            var result = new ComparisonResult
            {
                SourceFileName = ShowFileFullPath ? file.FilePath : Path.GetFileName(file.FilePath),
                // TotalFiles = parsedFiles.Count,
                // ProceccedFiles = parsedFiles.Count,
                FileList = parsedFiles.Select(parsedFile => new HashFile
                {
                    FileName = ShowFileFullPath ? parsedFile.FullPath : parsedFile.FileName,
                    FilePath = parsedFile.FullPath,
                    SourceFileName = ShowFileFullPath ? file.FilePath : Path.GetFileName(file.FilePath),
                    SourceFilePath = file.FilePath,
                    FileHash = parsedFile.Hash,
                    IsRelatedFilesVisible = expandedRows.TryGetValue(
                        (file.FilePath, parsedFile.FullPath, parsedFile.Hash),
                        out var rowStates) && rowStates.Count > 0 && rowStates.Dequeue(),
                    FileSize = File.Exists(parsedFile.FullPath)
                        ? new FileInfo(parsedFile.FullPath).Length.ToString()
                        : string.Empty
                }).ToList()
            };

            results.Add(result);
        }

        CalculateResultCounts(results);
    }

    private void CalculateResultCounts(List<ComparisonResult> results)
    {
        var allFiles = results.SelectMany(result => result.FileList).ToList();
        foreach (var file in allFiles)
        {
            string fileName = GetBaseFileName(file.FileName);
            file.RelatedFiles = allFiles.Where(candidate =>
                string.Equals(GetBaseFileName(candidate.FileName), fileName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.FileHash, file.FileHash, StringComparison.OrdinalIgnoreCase)).ToList();

            file.FoundFiles = allFiles.Count(candidate =>
                !ReferenceEquals(candidate, file) &&
                string.Equals(GetBaseFileName(candidate.FileName), fileName, StringComparison.OrdinalIgnoreCase));
            file.ConfirmedFiles = allFiles.Count(candidate =>
                !ReferenceEquals(candidate, file) &&
                string.Equals(candidate.FileHash, file.FileHash, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var result in results)
        {
            /*result.HashConfirmed = result.FileList.Count(file =>
                file.FoundFiles == file.ConfirmedFiles && file.FoundFiles > 1);*/
            result.HashConfirmed = result.FileList
                .Where(file => file.FoundFiles > 0 && file.FoundFiles == file.ConfirmedFiles)
                .Select(file => GetBaseFileName(file.FileName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();


            /*result.HashMismatch = result.FileList.Count(file =>
                file.FoundFiles > file.ConfirmedFiles);*/
            result.HashMismatch = result.FileList
                .Where(file => file.FoundFiles > file.ConfirmedFiles)
                .Select(file => GetBaseFileName(file.FileName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();


            /*result.SingleFiles = result.FileList.Count(file =>
                file.FoundFiles == 0 && file.ConfirmedFiles == 0);*/
            result.SingleFiles = result.FileList
                .Where(file => file.FoundFiles == 0 && file.ConfirmedFiles == 0)
                .Select(file => GetBaseFileName(file.FileName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        // Сортировка, 0 confirmed - поднимаем наверх
        foreach (var result in results)
        {
            result.FileList = result.FileList
            .OrderBy(file => file.FoundFiles != 0)
            .ThenBy(file => file.ConfirmedFiles != 0)
            .ToList();
            ComparisonResult.Add(result);
        }
    }

    private bool CanCompareSelectedHistory => SelectedHistoryItems.Count >= 2;

    [RelayCommand(CanExecute = nameof(CanCompareSelectedHistory))]
    private void CompareSelectedHistory()
    {
        _isHistoryComparison = true;
        var results = SelectedHistoryItems.Select(item => new ComparisonResult
        {
            SourceFileName = ShowFileFullPath
                ? item.SourcePath
                : Path.GetFileName(Path.TrimEndingDirectorySeparator(item.SourcePath)),
            FileList = item.Files.Select(file => new HashFile
            {
                FileName = ShowFileFullPath
                    ? GetHistoryFileFullPath(item, file)
                    : Path.GetFileName(file.FilePath),
                FilePath = ShowFileFullPath
                    ? GetHistoryFileFullPath(item, file)
                    : file.FilePath,
                SourceFileName = ShowFileFullPath
                    ? item.SourcePath
                    : Path.GetFileName(Path.TrimEndingDirectorySeparator(item.SourcePath)),
                SourceFilePath = item.SourcePath,
                FileHash = file.Hash
            }).ToList()
        }).ToList();

        ComparisonResult.Clear();
        CalculateResultCounts(results);
    }

    private static string GetHistoryFileFullPath(HashHistorySelection item, HashHistoryFile file)
    {
        if (Path.IsPathRooted(file.FilePath) || item.Source is not HashHistoryFolder)
            return file.FilePath;

        return Path.GetFullPath(Path.Combine(item.SourcePath, file.FilePath));
    }

    private static string GetBaseFileName(string path)
    {
        return Path.GetFileName(path.Replace('\\', '/'));
    }

    [RelayCommand]
    public void ShowFileHashHistory()
    {
        string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string historyFolder = Path.Combine(applicationDataFolder, "FileHelper");
        Directory.CreateDirectory(historyFolder);

        string historyPath = Path.Combine(historyFolder, "hash_log.txt");

        OpenFileInEditor(historyPath);
    }
    [RelayCommand]
    public async Task ClearFileHashHistory()
    {
        if (!await ConfirmClearHistoryAsync())
            return;

        string applicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string historyFolder = Path.Combine(applicationDataFolder, "FileHelper");
        Directory.CreateDirectory(historyFolder);

        string historyPath = Path.Combine(historyFolder, "hash_log.txt");

        // File.WriteAllText(historyPath, "");
        await File.WriteAllTextAsync(historyPath, "");
    }

    private static async Task<bool> ConfirmClearHistoryAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow == null)
        {
            return false;
        }

        var cancelButton = new Avalonia.Controls.Button
        {
            Content = "Отмена",
            IsCancel = true,
            IsDefault = true
        };
        var deleteButton = new Avalonia.Controls.Button
        {
            Content = "Удалить"
        };
        var confirmationWindow = new Avalonia.Controls.Window
        {
            Title = "Очистить историю?",
            Width = 360,
            SizeToContent = Avalonia.Controls.SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            Content = new Avalonia.Controls.StackPanel
            {
                Spacing = 16,
                Margin = new Avalonia.Thickness(20),
                Children =
                {
                    new Avalonia.Controls.TextBlock
                    {
                        Text = "История хеширования будет удалена. Продолжить?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new Avalonia.Controls.StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children =
                        {
                            cancelButton,
                            deleteButton
                        }
                    }
                }
            }
        };

        cancelButton.Click += (_, _) => confirmationWindow.Close(false);
        deleteButton.Click += (_, _) => confirmationWindow.Close(true);

        return await confirmationWindow.ShowDialog<bool>(desktop.MainWindow);
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
                    if (!ComparisonFiles.Any(item => item.FilePath == localPath) && File.Exists(localPath))
                    {
                        var hashFile = new ComparisonHashResultFile { FilePath = localPath };
                        ComparisonFiles.Add(hashFile);
                        CheckFileValid(hashFile);
                    }
                }
            }
        }
        catch /*(Exception ex)*/
        {
            // При необходимости можно добавить вывод ошибки
        }
    }



    public void ProcessDroppedItems(List<string> paths)
    {
        if (paths == null) return;

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                if (!ComparisonFiles.Any(item => item.FilePath == path))
                {
                    // ComparisonFiles.Add(new ComparisonHashResultFile { FilePath = path });

                    ComparisonHashResultFile hashFile = new ComparisonHashResultFile { FilePath = path };
                    ComparisonFiles.Add(hashFile);
                    CheckFileValid(hashFile);
                }
            }
        }
    }

    public void CheckFileValid(ComparisonHashResultFile hashFile)
    {
        // HashFileParser test = new HashFileParser();
        string path = hashFile.FilePath;
        int hashType = HashFileParser.CheckHashFileType(path);

        /*switch (hashType)
        {
            case 0:
                hashFile.IsValid = false;
                break;
            case 1:
                hashFile.IsValid = true;
                break;
            case 2:
                hashFile.IsValid = true;
                break;
        }*/
        switch (hashType)
        {
            case 0:
                hashFile.IsValid = false;
                break;
            default:
                hashFile.IsValid = true;
                if (!CanStart)
                {
                    CanStart = true;
                }
                break;
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
}