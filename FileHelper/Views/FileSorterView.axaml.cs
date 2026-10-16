using Avalonia.Controls;
using Avalonia.Interactivity;
using FileHelper.ViewModels;

namespace FileHelper.Views;

public partial class FileSorterView : UserControl
{
    public FileSorterView()
    {
        InitializeComponent();
    }

    /*
    private void CloseCategoriesFlyout(object? sender, RoutedEventArgs e)
    {
        CategoriesButton.Flyout?.Hide();
    }
    */

    private void CancelCategoriesFlyout(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FileSorterViewModel viewModel)
            viewModel.CancelCategoryCommand.Execute(null);

        CategoriesButton.Flyout?.Hide();
    }
}
