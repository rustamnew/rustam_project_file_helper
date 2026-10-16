using CommunityToolkit.Mvvm.ComponentModel;

namespace FileHelper.ViewModels;

public enum Tab
{

    FileSorter,
    FileHasher,
    FileRenamer,
    FileHashReader,
    FileArchiveSearcher
}

public partial class MainViewModel : ViewModelBase
{
    public FileSorterViewModel FileSorterViewModel { get; } = new();
    public FileHasherViewModel FileHasherViewModel { get; } = new();
    public FileRenamerViewModel FileRenamerViewModel { get; } = new();
    public FileHashReaderViewModel FileHashReaderViewModel { get; } = new();

    public FileArchiveSearcherViewModel FileArchiveSearcherViewModel { get; } = new();

    [ObservableProperty]
    public partial Tab CurrentTab { get; set; } = Tab.FileSorter;
}
