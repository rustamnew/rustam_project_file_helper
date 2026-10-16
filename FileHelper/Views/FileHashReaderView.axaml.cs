using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FileHelper.ViewModels;

namespace FileHelper.Views;

public partial class FileHashReaderView : UserControl
{
    public FileHashReaderView()
    {
        InitializeComponent();

        DragDrop.AddDragOverHandler(DropZone1, OnDragOver);
        DragDrop.AddDropHandler(DropZone1, OnDrop);
        DragDrop.AddDragEnterHandler(DropZone1, OnDragEnter);
        DragDrop.AddDragLeaveHandler(DropZone1, OnDragLeave);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Formats.Contains(DataFormat.File))
        {
            DropZone1.BorderBrush = Brush.Parse("#0072a3");
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        ResetDropZoneVisuals();
    }

    private void ResetDropZoneVisuals()
    {
        DropZone1.BorderBrush = Brush.Parse("#00a2e8");
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        ResetDropZoneVisuals();

        var storageItems = e.DataTransfer.TryGetFiles();
        if (storageItems == null)
            return;

        var pathsToProcess = new List<string>();
        foreach (var item in storageItems)
        {
            string? localPath = item?.Path?.LocalPath;
            if (!string.IsNullOrEmpty(localPath))
                pathsToProcess.Add(localPath);
        }

        if (DataContext is FileHashReaderViewModel viewModel && pathsToProcess.Count > 0)
            viewModel.ProcessDroppedItems(pathsToProcess);
    }

    private void OnHashItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is SelectableTextBlock { DataContext: HashFile file })
        {
            file.IsRelatedFilesVisible = !file.IsRelatedFilesVisible;
        }
    }
    private void OnHashItemPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock { DataContext: HashFile file })
        {
            file.IsRelatedFilesVisible = !file.IsRelatedFilesVisible;
        }
    }

}


