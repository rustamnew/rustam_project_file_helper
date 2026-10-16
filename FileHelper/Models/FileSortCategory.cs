using CommunityToolkit.Mvvm.ComponentModel;

namespace FileHelper.Models;

public partial class FileSortCategory : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Extensions { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool HasNameError { get; set; }

    [ObservableProperty]
    public partial bool HasExtensionsError { get; set; }

    partial void OnNameChanged(string value) => HasNameError = false;

    partial void OnExtensionsChanged(string value) => HasExtensionsError = false;
}