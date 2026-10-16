using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using FileHelper.ViewModels;
using Avalonia;
using Avalonia.Media; // Важно: добавляем для работы с цветами Brushes
using System.Collections.ObjectModel;
using System.Linq;

namespace FileHelper.Views;


public partial class FileHasherView : UserControl
{
    public FileHasherView()
    {
        InitializeComponent();

        DragDrop.AddDragOverHandler(DropZone1, OnDragOver);
        DragDrop.AddDropHandler(DropZone1, OnDrop);

        DragDrop.AddDragEnterHandler(DropZone1, OnDragEnter);
        DragDrop.AddDragLeaveHandler(DropZone1, OnDragLeave);
    }

    // Срабатывает ОДИН РАЗ, когда папка заходит внутрь зоны
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        // Проверяем, что тащат именно файлы/папки, а не случайный текст
        if (e.DataTransfer.Formats.Contains(DataFormat.File) /*|| e.DataTransfer.Formats.Contains(DataFormat.File)*/)
        {
            // МЕНЯЕМ ВИЗУАЛ: делаем рамку зеленой, фон чуть светлее, а текст белым
            DropZone1.BorderBrush = Brush.Parse("#0072a3");
            // DropZone1.Background = Brush.Parse("#353535");
            // DropZone1.BorderThickness = new Thickness(6);



            // DropZone1Text.Foreground = Brushes.White;
            // DropZone1Text.Text = "Отпустите мышь, чтобы прикрепить";
        }
    }

    // Срабатывает, когда папку увели обратно или сбросили
    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        // ВОЗВРАЩАЕМ КАК БЫЛО: сбрасываем цвета к стандартным
        ResetDropZoneVisuals();
    }

    private void ResetDropZoneVisuals()
    {
        DropZone1.BorderBrush = Brush.Parse("#00a2e8"); // Старый серый цвет рамки
        // DropZone1.BorderThickness = new Thickness(2);
        // DropZone1.Padding = new Thickness(0);
        // DropZone1.Margin = new Thickness(4);
        // DropZone1.Background = Brush.Parse("#2b2b2b");   // Старый темный фон

        // DropZone1Text.Foreground = Brush.Parse("#888888"); // Серый текст
        // DropZone1Text.Text = "Перетащите папку сюда";
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var storageItems = e.DataTransfer.TryGetFiles();

        if (storageItems == null) return;

        // Создаем список для путей
        var pathsToProcess = new List<string>();

        foreach (var item in storageItems)
        {
            string? localPath = item?.Path?.LocalPath;
            if (!string.IsNullOrEmpty(localPath))
            {
                pathsToProcess.Add(localPath);
            }

        }

        // ПЕРЕДАЧА ВО VIEWMODEL:
        // Проверяем, что DataContext установлен и это именно наша ViewModel
        if (DataContext is FileHasherViewModel viewModel && pathsToProcess.Count > 0)
        {
            viewModel.ProcessDroppedItems(pathsToProcess);
            // viewModel.ProcessDroppedFolders(pathsToProcess);

        }
    }

    private void ProcessFolder(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath);
    }
}
