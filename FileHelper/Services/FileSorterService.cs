using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using FileHelper.Models;
namespace MyAvaloniaApp.Services
{
    public class FileSorterService
    {
        // Метод принимает путь к папке, которую нужно убрать
        public void SortFilesByExtension(string sourceFolder, IReadOnlyCollection<FileSortCategory> categories, bool showFileInfo = false, IProgress<int>? progress = null, IProgress<string>? logger = null)
        {
            /*if (!Directory.Exists(sourceFolder)) return;

            // Берем все файлы в папке
            string[] files = Directory.GetFiles(sourceFolder);

            foreach (string filePath in files)
            {
                string extension = Path.GetExtension(filePath).TrimStart('.').ToLower();

                // Если у файла нет расширения, пропускаем его
                if (string.IsNullOrEmpty(extension)) continue;

                // Создаем путь для новой папки (например, "Изображения" или "txt")
                string targetFolder = Path.Combine(sourceFolder, extension.ToUpper());

                // Создаем папку, если её нет
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                // Переносим файл в новую папку
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(targetFolder, fileName);

                File.Move(filePath, destPath);
            }*/

            // Шаблон: дд-мм-гггг_чч-мм-сс (например, 02-08-2026_21-17-40)
            // \d{2} - две цифры, \d{4} - четыре цифры
            string pattern = @"^\d{2}-\d{2}-\d{4}_\d{2}-\d{2}-\d{2}$";
            Regex regex = new Regex(pattern);


            int totalItems = 0;
            int processedItems = 0;

            var categoryFiles = categories
                .Where(category => category.IsEnabled)
                .ToDictionary(category => category, _ => new List<string>());
            List<string> otherFiles = new List<string>();

            // Список папок
            List<string> directoryList = new List<string>();

            // Список ошибок при перемещении
            List<string> failedOperations = new List<string>();

            // Флаг для вывода информации о файлах
            bool showFileInfoBool = showFileInfo;

            // Переменная для хранения пути к выбранной папке
            string folderPath = sourceFolder;


            if (showFileInfo)
            {
                showFileInfoBool = true;
            }

            DirectoryInfo subDir = new DirectoryInfo(folderPath);

            if (!Directory.Exists(folderPath))
            {
                // Console.WriteLine("Указанный путь не найден");
                progress?.Report(100);
                return;
            }

            if (subDir.GetFiles().Length == 0 && subDir.GetDirectories().Length == 0)
            {
                // Console.WriteLine("Папка пуста");
                progress?.Report(100);
                return;
            }


            string[] files = Directory.GetFiles(folderPath);

            string[] directories = Directory.GetDirectories(folderPath);

            // Подсчитываем общее количество элементов для прогресса
            totalItems = files.Length + directories.Length;

            if (files.Length > 0)
            {
                if (showFileInfoBool)
                {
                    // Console.WriteLine("Найденные файлы:");
                }

                foreach (string file in files)
                {
                    if (showFileInfoBool)
                    {
                        // Console.WriteLine(file);
                    }


                    string ext = Path.GetExtension(file);
                    var matchedCategory = categories.FirstOrDefault(category => category.IsEnabled &&
                        FileSorterSettingsService.GetExtensions(category).Contains(ext, StringComparer.OrdinalIgnoreCase));
                    if (matchedCategory is not null)
                    {
                        categoryFiles[matchedCategory].Add(file);
                        continue;
                    }

                    // Если файл не подходит ни под одну категорию, добавляем его в список прочих файлов
                    if (showFileInfoBool)
                    {
                        Console.WriteLine($"Файл {file} не подходит ни под одну категорию, будет перемещён в папку 'Другое'");
                    }
                    otherFiles.Add(file);
                }
            }




            if (directories.Length > 0)
            {
                if (showFileInfoBool)
                {
                    Console.WriteLine("Найденные папки:");
                }

                foreach (string directory in directories)
                {
                    /*if (showFileInfoBool)
                    {
                        Console.WriteLine(directory);
                    }
                    directoryList.Add(directory);*/

                    // Добавляем только папки которые не подходят под маску dd-dd-dddd_dd-dd-dd
                    string folderName = Path.GetFileName(directory);
                    if (!regex.IsMatch(folderName))
                    {
                        if (showFileInfoBool)
                        {
                            Console.WriteLine(directory);
                        }
                        directoryList.Add(directory);
                    }
                }
            }


            if (files.Length == 0 && directories.Length == 0)
            {
                Console.WriteLine("Папка пуста");
                progress?.Report(100);
                return;
            }


            // Начинаем сортировку файлов и папок
            Console.WriteLine();
            // string newFolderPath = Path.Combine(folderPath, DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss"));
            // Directory.CreateDirectory(newFolderPath);
            string newFolderPath = folderPath;


            foreach (var (category, filesInCategory) in categoryFiles)
            {
                if (filesInCategory.Count == 0)
                    continue;

                var destinationFolder = Path.Combine(newFolderPath, category.Name.Trim());
                Directory.CreateDirectory(destinationFolder);
                MoveFiles(filesInCategory, destinationFolder, failedOperations, progress, logger, totalItems, ref processedItems, showFileInfoBool);
            }

            /*if (otherFiles.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(newFolderPath, "Другое"));

                MoveFiles(otherFiles, Path.Combine(newFolderPath, "Другое"), failedOperations, progress, logger, totalItems, ref processedItems, showFileInfoBool);
            }

            if (directoryList.Count > 0)
            {
                Directory.CreateDirectory(Path.Combine(newFolderPath, "Папки"));

                MoveDirectories(directoryList, Path.Combine(newFolderPath, "Папки"), failedOperations, progress, logger, totalItems, ref processedItems, showFileInfoBool);
            }*/

            // Вывод отчёта об ошибках в конце
            Console.WriteLine();
            if (failedOperations.Count > 0)
            {
                Console.WriteLine($"Сортировка завершена с ошибками. Не удалось переместить {failedOperations.Count} элементов:");
                foreach (var error in failedOperations)
                {
                    Console.WriteLine($" - {error}");
                }
                Console.WriteLine("Эти файлы/папки остались на своих исходных местах и не были удалены.");
            }
            else
            {
                Console.WriteLine("Все файлы и папки успешно отсортированы!");
            }

            // Console.WriteLine("Нажмите любую клавишу для выхода...");
            // Console.ReadKey();

            progress?.Report(100);
        }

        private static void MoveFiles(List<string> files, string destinationFolder, List<string> failedOperations, IProgress<int>? progress, IProgress<string>? logger, int totalItems, ref int processedItems, bool showFileInfoBool)
        {
            foreach (string file in files)
            {
                // Thread.Sleep(500); // <-- Задержка 500мс для теста лоадера

                try
                {
                    string newFilePath = Path.Combine(destinationFolder, Path.GetFileName(file));
                    File.Move(file, newFilePath);
                    if (showFileInfoBool) logger?.Report($"{Path.GetFileName(destinationFolder)}: {Path.GetFileName(file)}");
                    // if (showFileInfoBool) logger?.Report($"{Path.GetFileName(file)} > {Path.GetFileName(destinationFolder)}");
                }
                catch (Exception ex)
                {
                    failedOperations.Add($"Файл: {file} | Ошибка: {ex.GetType().Name} - {ex.Message}");
                }

                processedItems++;
                if (totalItems > 0) progress?.Report((int)((double)processedItems / totalItems * 100));
            }
        }

        private static void MoveDirectories(List<string> directories, string destinationFolder, List<string> failedOperations, IProgress<int>? progress, IProgress<string>? logger, int totalItems, ref int processedItems, bool showFileInfoBool)
        {
            foreach (string directory in directories)
            {
                // Thread.Sleep(500); // <-- Задержка 500мс для теста лоадера
                try
                {
                    string newDirectoryPath = Path.Combine(destinationFolder, Path.GetFileName(directory));
                    Directory.Move(directory, newDirectoryPath);
                    if (showFileInfoBool) logger?.Report($"sort dir {Path.GetFileName(directory)}");
                }
                catch (Exception ex)
                {
                    failedOperations.Add($"Папка: {directory} | Ошибка: {ex.GetType().Name} - {ex.Message}");
                }

                processedItems++;
                if (totalItems > 0) progress?.Report((int)((double)processedItems / totalItems * 100));
            }
        }

    }

}