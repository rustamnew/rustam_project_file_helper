using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using FileHelper.Models;
using MyAvaloniaApp.Services;

namespace FileHelper.ViewModels;

public partial class FileSorterViewModel : ViewModelBase
{
    private readonly FileSorterService _fileSorterService = new FileSorterService();
    private readonly FileSorterSettingsService _settingsService = new();

    public ObservableCollection<FileSortCategory> Categories { get; }

    public ObservableCollection<FileSortCategory> CategoriesSettings { get; }

    [ObservableProperty]
    public partial string CategoriesMessage { get; set; } = "";

    public FileSorterViewModel()
    {
        Categories = new ObservableCollection<FileSortCategory>(_settingsService.LoadCategories());

        // CategoriesSettings = new ObservableCollection<FileSortCategory>();
        CategoriesSettings = new ObservableCollection<FileSortCategory>(_settingsService.LoadCategories());
    }

    [ObservableProperty]
    public partial bool ShowFileLogs { get; set; } = false;

    [ObservableProperty]
    public partial bool IsReadyForSorting { get; set; } = false;

    // [ObservableProperty]
    // public partial bool IsExtractFilesEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSorting { get; set; } = false;

    [ObservableProperty]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial string LogMessage { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedFolder { get; set; } = "";

    private bool CanSubmitFolder() => !string.IsNullOrWhiteSpace(SelectedFolder) && FolderFound == true && !IsReadyForSorting;
    private bool CanSortFolder() => IsReadyForSorting;

    [ObservableProperty]
    public partial bool? FolderFound { get; set; } = null;

    public bool IsFolderValid => !string.IsNullOrWhiteSpace(SelectedFolder) && FolderFound == true;
    public bool IsFolderError => !string.IsNullOrWhiteSpace(SelectedFolder) && FolderFound == false;

    partial void OnSelectedFolderChanged(string value)
    {
        var text = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            FolderFound = null;
            IsReadyForSorting = false;
        }
        else
        {
            FolderFound = Directory.Exists(text);
            IsReadyForSorting = false;
        }

        if (FolderFound == true)
        {
            IsReadyForSorting = true;
        }

        OnPropertyChanged(nameof(IsFolderValid));
        OnPropertyChanged(nameof(IsFolderError));
        // SubmitFolderCommand.NotifyCanExecuteChanged();
        SortFolderCommand.NotifyCanExecuteChanged();
    }

    partial void OnFolderFoundChanged(bool? value)
    {
        OnPropertyChanged(nameof(IsFolderValid));
        OnPropertyChanged(nameof(IsFolderError));
        SubmitFolderCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsReadyForSortingChanged(bool value)
    {
        SubmitFolderCommand.NotifyCanExecuteChanged();
        SortFolderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSubmitFolder))]
    private void SubmitFolder()
    {
        if (IsReadyForSorting)
            return;

        IsReadyForSorting = true;
        // Message = $"Папка назначена: {SelectedFolder}";
    }

    [RelayCommand(CanExecute = nameof(CanSortFolder))]
    public async Task SortFolder()
    {
        try
        {
            // _settingsService.SaveCategories(Categories);
            _settingsService.SaveCategories(CategoriesSettings);
        }
        catch (InvalidOperationException ex)
        {
            Message = ex.Message;
            return;
        }
        catch (IOException ex)
        {
            Message = $"Не удалось сохранить категории: {ex.Message}";
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            Message = $"Нет доступа для сохранения категорий: {ex.Message}";
            return;
        }

        IsSorting = true;
        LoadingProgress = 0;
        LogMessage = "";

        // Progress<T> автоматически перекидывает обновления в UI-поток
        var progress = new Progress<int>(percent => LoadingProgress = percent);
        var logger = new Progress<string>(msg => LogMessage = msg + "\n" + LogMessage);

        // Task.Run выносит тяжелую операцию в фоновый поток, чтобы не было лагов интерфейса
        await Task.Run(() => _fileSorterService.SortFilesByExtension(
            SelectedFolder,
            Categories.ToList(),
            ShowFileLogs,
            progress,
            logger));

        Message = "Сортировка завершена!";
        IsSorting = false;
    }

    [RelayCommand]
    private void CancelCategory()
    {
        var categories = _settingsService.LoadCategories();
        CategoriesSettings.Clear();
        foreach (var category in categories)
            CategoriesSettings.Add(category);
    }

    [RelayCommand]
    private void AddCategory()
    {
        // Categories.Add(new FileSortCategory { Name = "Новая категория", Extensions = "." });
        CategoriesSettings.Add(new FileSortCategory { Name = "Новая категория", Extensions = "." });
    }

    [RelayCommand]
    private void RemoveCategory(FileSortCategory? category)
    {
        if (category is not null)
            // Categories.Remove(category);
            CategoriesSettings.Remove(category);
    }

    [RelayCommand]
    private void SaveCategories()
    {
        try
        {
            // _settingsService.SaveCategories(Categories);
            _settingsService.SaveCategories(CategoriesSettings);

            CategoriesMessage = "Категории сохранены.";
        }
        catch (InvalidOperationException ex)
        {
            CategoriesMessage = ex.Message;
            return;
        }
        catch (IOException ex)
        {
            CategoriesMessage = $"Не удалось сохранить категории: {ex.Message}";
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            CategoriesMessage = $"Нет доступа для сохранения категорий: {ex.Message}";
            return;
        }

        CategoriesMessage = "Категории сохранены.";
        Categories.Clear();
        foreach (var category in CategoriesSettings)
            Categories.Add(category);
    }


    [ObservableProperty]
    public partial int LoadingProgress { get; set; } = 0;


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
}