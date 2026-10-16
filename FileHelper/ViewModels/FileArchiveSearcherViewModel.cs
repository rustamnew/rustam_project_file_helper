using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileHelper.Models;
using FileHelper.Services;

namespace FileHelper.ViewModels;

public partial class FileArchiveSearcherViewModel : ViewModelBase
{
    private readonly FileArchiveSearcherService searcherService = new();

    public ObservableCollection<string> FilesToFind { get; } = new();

    public ObservableCollection<string> Archives { get; } = new();

    public ObservableCollection<ArchiveSearchResult> Results { get; } = new();

    [ObservableProperty]
    private bool compareCrc32;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string message = string.Empty;

    public bool CanSearch => FilesToFind.Count > 0 && Archives.Count > 0 && !IsSearching;

    [RelayCommand]
    public async Task AddFiles()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выбрать файлы для поиска",
            AllowMultiple = true
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            if (File.Exists(path) && !FilesToFind.Contains(path, StringComparer.OrdinalIgnoreCase))
                FilesToFind.Add(path);
        }

        OnPropertyChanged(nameof(CanSearch));
    }

    [RelayCommand]
    public async Task AddArchives()
    {
        var topLevel = GetMainWindow();
        if (topLevel == null)
            return;

        var archives = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выбрать архивы",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Архивы")
                {
                    Patterns = new[] { "*.zip", "*.rar", "*.7z", "*.tar", "*.gz", "*.bz2", "*.xz", "*.lz", "*.lzip", "*.zst", "*.cab", "*.arj", "*.arc", "*.ace" }
                },
                new FilePickerFileType("Все файлы") { Patterns = new[] { "*" } }
            }
        });

        foreach (var archive in archives)
        {
            var path = archive.Path.LocalPath;
            if (File.Exists(path) && !Archives.Contains(path, StringComparer.OrdinalIgnoreCase))
                Archives.Add(path);
        }

        OnPropertyChanged(nameof(CanSearch));
    }

    [RelayCommand]
    public void RemoveFile(string path)
    {
        FilesToFind.Remove(path);
        OnPropertyChanged(nameof(CanSearch));
    }

    [RelayCommand]
    public void RemoveArchive(string path)
    {
        Archives.Remove(path);
        OnPropertyChanged(nameof(CanSearch));
    }

    [RelayCommand]
    public async Task Search()
    {
        if (!CanSearch)
            return;

        IsSearching = true;
        OnPropertyChanged(nameof(CanSearch));
        Message = "Поиск выполняется...";
        Results.Clear();

        try
        {
            var outcome = await searcherService.SearchAsync(FilesToFind, Archives, CompareCrc32);
            foreach (var result in outcome.Results)
                Results.Add(result);

            Message = $"Проверено файлов: {outcome.Results.Count}. Найдено: {outcome.Results.Count(result => result.IsFound)}. Не найдено: {outcome.Results.Count(result => !result.IsFound)}.";
            if (outcome.Errors.Count > 0)
                Message += $" Не удалось прочитать архивов: {outcome.Errors.Count}. " + string.Join("; ", outcome.Errors);
        }
        catch (Exception ex)
        {
            Message = $"Ошибка поиска: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
            OnPropertyChanged(nameof(CanSearch));
        }
    }

    private static TopLevel? GetMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

}