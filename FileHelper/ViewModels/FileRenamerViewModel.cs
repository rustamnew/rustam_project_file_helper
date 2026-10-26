using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FileHelper.ViewModels;

public partial class FoundItem : ObservableObject
{
    [ObservableProperty]
    private string originalName = string.Empty;

    [ObservableProperty]
    private string replacedName = string.Empty;

}

public partial class FileRenamerViewModel : ViewModelBase
{
    public ObservableCollection<FoundItem> FoundItemsList { get; } = new();
    public ObservableCollection<string> FoundItems { get; } = new();
    public ObservableCollection<string> ReplacedItems { get; } = new();

    [ObservableProperty]
    public partial string Title { get; set; } = "Переименование";

    [ObservableProperty]
    public partial string FindQuery { get; set; } = string.Empty;

    partial void OnFindQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            FoundItems.Clear();
            ReplacedItems.Clear();
            return;
        }

        if (IsReadyForSearching)
        {
            SearchFilesInFolder(value, SelectedFolder);
        }

        RenameFileCommand.NotifyCanExecuteChanged();
        RenameFilesCommand.NotifyCanExecuteChanged();
        UpdateReplacePreview();
    }


    [ObservableProperty]
    public partial string ReplaceQuery { get; set; } = string.Empty;

    partial void OnReplaceQueryChanged(string value)
    {
        UpdateReplacePreview();
    }

    private void UpdateReplacePreview()
    {
        ReplacedItems.Clear();

        if (string.IsNullOrEmpty(FindQuery) || ReplaceQuery == null)
        {
            return;
        }

        foreach (var item in FoundItems)
        {
            var directory = Path.GetDirectoryName(item);
            var fileName = ReplaceText(Path.GetFileName(item), FindQuery, ReplaceQuery, FlagMatchCase, FlagMatchWholeWord, FlagUseRegular);
            var replacedItem = string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName);
            ReplacedItems.Add(replacedItem);
        }
    }

    /// <summary>
    /// Универсальный метод замены с поддержкой регулярных выражений и целых слов
    /// </summary>
    private string ReplaceText(string input, string find, string replace, bool matchCase, bool matchWholeWord, bool useRegular)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(find))
            return input;

        try
        {
            RegexOptions options = (matchCase ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant;

            // Если регулярные выражения выключены, экранируем спецсимволы
            string pattern = useRegular ? find : Regex.Escape(find);

            // Если требуется поиск только целых слов, оборачиваем паттерн в границы слова \b
            if (matchWholeWord)
            {
                pattern = $@"\b(?:{pattern})\b";
            }

            return Regex.Replace(input, pattern, replace, options);
        }
        catch (ArgumentException)
        {
            // Если пользователь ввел невалидное регулярное выражение, возвращаем исходную строку
            return input;
        }
    }

    [RelayCommand]
    public void RemoveItem(string item)
    {
        if (string.IsNullOrWhiteSpace(item))
            return;

        for (var i = FoundItems.Count - 1; i >= 0; i--)
        {
            if (string.Equals(FoundItems[i], item, StringComparison.OrdinalIgnoreCase))
            {
                FoundItems.RemoveAt(i);
            }
        }

        UpdateReplacePreview();
    }

    [ObservableProperty]
    public partial string SelectedFolder { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool FlagMatchCase { get; set; } = false;

    partial void OnFlagMatchCaseChanged(bool value)
    {
        if (IsReadyForSearching) SearchFilesInFolder(FindQuery, SelectedFolder);
    }

    [ObservableProperty]
    public partial bool FlagMatchWholeWord { get; set; } = false;

    partial void OnFlagMatchWholeWordChanged(bool value)
    {
        if (IsReadyForSearching) SearchFilesInFolder(FindQuery, SelectedFolder);
    }

    [ObservableProperty]
    public partial bool FlagUseRegular { get; set; } = false;

    partial void OnFlagUseRegularChanged(bool value)
    {
        if (IsReadyForSearching) SearchFilesInFolder(FindQuery, SelectedFolder);
    }

    [ObservableProperty]
    public partial bool FlagRecursiveSearch { get; set; } = false;

    partial void OnFlagRecursiveSearchChanged(bool value)
    {
        if (IsReadyForSearching) SearchFilesInFolder(FindQuery, SelectedFolder);
    }

    [ObservableProperty]
    public partial bool FlagFieldsExpanded { get; set; } = false;

    [ObservableProperty]
    public partial bool? FolderFound { get; set; } = null;

    [ObservableProperty]
    public partial bool IsReadyForSearching { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSearching { get; set; } = false;

    [ObservableProperty]
    public partial bool IsRenaming { get; set; } = false;

    [ObservableProperty]
    public partial int LoadingProgress { get; set; } = 0;

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LogMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool SeparateMediaFiles { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowFileLogs { get; set; } = false;

    public bool IsFolderValid => !string.IsNullOrWhiteSpace(SelectedFolder) && FolderFound == true;
    public bool IsFolderError => !string.IsNullOrWhiteSpace(SelectedFolder) && FolderFound == false;

    // private bool CanSubmitFolder() => !string.IsNullOrWhiteSpace(SelectedFolder) && FolderFound == true && !IsReadyForSearching;
    private bool CanSearchFolder() => IsReadyForSearching;
    private bool CanRenameFile() => FoundItems.Count > 0 && !IsRenaming;
    private bool CanRenameFiles() => FoundItems.Count > 0 && !IsRenaming;

    partial void OnSelectedFolderChanged(string value)
    {
        var text = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            FolderFound = null;
            IsReadyForSearching = false;
        }
        else
        {
            FolderFound = Directory.Exists(text);
            IsReadyForSearching = false;
        }

        if (FolderFound == true)
        {
            IsReadyForSearching = true;
        }

        OnPropertyChanged(nameof(IsFolderValid));
        OnPropertyChanged(nameof(IsFolderError));
        // SubmitFolderCommand.NotifyCanExecuteChanged();


    }

    partial void OnFolderFoundChanged(bool? value)
    {
        OnPropertyChanged(nameof(IsFolderValid));
        OnPropertyChanged(nameof(IsFolderError));
        // SubmitFolderCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsReadyForSearchingChanged(bool value)
    {
        // SubmitFolderCommand.NotifyCanExecuteChanged();
        SearchFolderCommand.NotifyCanExecuteChanged();
    }

    // [RelayCommand(CanExecute = nameof(CanSubmitFolder))]
    /*private void SubmitFolder()
    {
        if (IsReadyForSearching)
            return;

        IsReadyForSearching = true;
        // SearchFilesInFolder(FindQuery, SelectedFolder);
    }*/

    [RelayCommand(CanExecute = nameof(CanSearchFolder))]
    public async Task SearchFolder()
    {
        IsSearching = true;
        LoadingProgress = 0;
        LogMessage = string.Empty;

        var progress = new Progress<int>(percent => LoadingProgress = percent);
        var logger = new Progress<string>(msg => LogMessage = msg + "\n" + LogMessage);

        await Task.Run(() =>
        {
            // Здесь можно добавить дополнительную логику, если потребуется
        });

        Message = "Проверка завершена!";
        IsSearching = false;
    }

    [RelayCommand(CanExecute = nameof(CanRenameFile))]
    public async Task RenameFile()
    {
        IsRenaming = true;
        LoadingProgress = 0;
        LogMessage = "";

        IProgress<int> progress = new Progress<int>(percent => LoadingProgress = percent);
        IProgress<string> logger = new Progress<string>(msg => LogMessage = msg + "\n" + LogMessage);

        if (FoundItems.Count > 0)
        {
            await Task.Run(() =>
            {
                string originalFileName = FoundItems[0];
                string newFileName = ReplacedItems[0];

                string originalFilePath = Path.Combine(SelectedFolder, originalFileName);
                string newFilePath = Path.Combine(SelectedFolder, newFileName);

                try
                {
                    if (File.Exists(originalFilePath))
                    {
                        File.Move(originalFilePath, newFilePath);
                        logger?.Report($"Переименован: {originalFileName} -> {newFileName}");
                    }
                    else
                    {
                        logger?.Report($"Файл не найден: {originalFileName}");
                    }
                }
                catch (Exception ex)
                {
                    logger?.Report($"Ошибка при переименовании {originalFileName}: {ex.Message}");
                }

                progress.Report(100);
            });

            FoundItems.RemoveAt(0);
            ReplacedItems.RemoveAt(0);

            RenameFileCommand.NotifyCanExecuteChanged();
            RenameFilesCommand.NotifyCanExecuteChanged();
        }

        Message = "Переименование завершено!";
        IsRenaming = false;
    }

    [RelayCommand(CanExecute = nameof(CanRenameFiles))]
    public async Task RenameFiles()
    {
        IsRenaming = true;
        LoadingProgress = 0;
        LogMessage = "";

        IProgress<int> progress = new Progress<int>(percent => LoadingProgress = percent);
        IProgress<string> logger = new Progress<string>(msg => LogMessage = msg + "\n" + LogMessage);

        await Task.Run(() =>
        {
            for (int i = 0; i < FoundItems.Count; i++)
            {
                string originalFileName = FoundItems[i];
                string newFileName = ReplacedItems[i];

                string originalFilePath = Path.Combine(SelectedFolder, originalFileName);
                string newFilePath = Path.Combine(SelectedFolder, newFileName);

                try
                {
                    if (File.Exists(originalFilePath))
                    {
                        File.Move(originalFilePath, newFilePath);
                        logger?.Report($"Переименован: {originalFileName} -> {newFileName}");
                        Console.WriteLine($"Переименован: {originalFileName} -> {newFileName}");
                    }
                    else
                    {
                        logger?.Report($"Файл не найден: {originalFileName}");
                        Console.WriteLine($"Переименован: {originalFileName} -> {newFileName}");
                    }
                }
                catch (Exception ex)
                {
                    logger?.Report($"Ошибка при переименовании {originalFileName}: {ex.Message}");
                }

                int percentComplete = (int)((i + 1) / (double)FoundItems.Count * 100);
                progress.Report(percentComplete);
            }
        });

        FoundItems.Clear();
        ReplacedItems.Clear();

        RenameFileCommand.NotifyCanExecuteChanged();
        RenameFilesCommand.NotifyCanExecuteChanged();

        Message = "Переименование завершено!";
        IsRenaming = false;
    }

    [RelayCommand]
    public async Task SelectFolder()
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
                SelectedFolder = folders[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            SelectedFolder = $"Ошибка: {ex.Message}";
            FolderFound = false;
        }
    }

    /// <summary>
    /// Исправленный метод поиска. Убрана опасная рекурсия, добавлена корректная поддержка всех флагов.
    /// </summary>
    private void SearchFilesInFolder(string query, string folderPath)
    {
        FoundItems.Clear();
        ReplacedItems.Clear();

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        // ИСПРАВЛЕНИЕ: Используем SearchOption в зависимости от флага, без ручной рекурсии
        var searchOption = FlagRecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory.EnumerateFiles(folderPath, "*", searchOption);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            bool isMatch = false;

            if (FlagUseRegular)
            {
                try
                {
                    RegexOptions options = (FlagMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant;
                    string pattern = query; // Не экранируем, так как это регулярное выражение

                    if (FlagMatchWholeWord)
                    {
                        pattern = $@"\b(?:{pattern})\b";
                    }

                    isMatch = Regex.IsMatch(fileName, pattern, options);
                }
                catch (ArgumentException)
                {
                    isMatch = false; // Игнорируем невалидные регулярные выражения при вводе
                }
            }
            else
            {
                if (FlagMatchWholeWord)
                {
                    // Для поиска целых слов без регулярных выражений также удобно использовать Regex с экранированием
                    string pattern = $@"\b(?:{Regex.Escape(query)})\b";
                    RegexOptions options = (FlagMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase) | RegexOptions.CultureInvariant;
                    isMatch = Regex.IsMatch(fileName, pattern, options);
                }
                else
                {
                    var comparison = FlagMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    isMatch = fileName.Contains(query, comparison);
                }
            }

            if (isMatch)
            {
                FoundItems.Add(Path.GetRelativePath(folderPath, file));
            }
        }

        UpdateReplacePreview();
        RenameFileCommand.NotifyCanExecuteChanged();
        RenameFilesCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public void MoveUp()
    {
        // Логика перемещения элемента вверх в списке
    }

    [RelayCommand]
    public void MoveDown()
    {
        // Логика перемещения элемента вниз в списке
    }

    [RelayCommand]
    public void Clear()
    {
        FindQuery = string.Empty;
        ReplaceQuery = string.Empty;
        FoundItems.Clear();
        ReplacedItems.Clear();
        LogMessage = string.Empty;
        Message = string.Empty;
    }
}