using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace MyAvaloniaApp.Services;

public class HashFileParser
{
    /// <summary>
    /// Пара "имя файла / хеш".
    /// </summary>
    public sealed class FileHash
    {
        public string FileName { get; set; } = string.Empty;   // только имя (без пути)
        public string FullPath { get; set; } = string.Empty;   // как в исходнике (может быть с путём)
        public string Hash { get; set; } = string.Empty;   // SHA256 в верхнем регистре
    }

    /// <summary>
    /// Парсит файл, автоматически определяя его формат.
    /// </summary>
    public static List<FileHash> Parse(string filePath, Encoding? encoding = null)
    {
        encoding ??= new UTF8Encoding(false);
        string content = File.ReadAllText(filePath, encoding);

        if (LooksLikeType1(content))
            return ParseType1(content);

        if (LooksLikeType2(content))
            return ParseType2(content);

        throw new FormatException("Не удалось определить формат файла.");
    }

    public static int CheckHashFileType(string filePath, Encoding? encoding = null)
    {
        encoding ??= new UTF8Encoding(false);
        string content = File.ReadAllText(filePath, encoding);

        if (LooksLikeType1(content))
            return 1;

        if (LooksLikeType2(content))
            return 2;

        return 0;
    }

    // ---------- Определение формата ----------

    private static bool LooksLikeType1(string content)
    {
        // Тип 1: строки вида "имя;ХЕШ" (64 hex-символа)
        // Достаточно найти хотя бы одну такую строку.
        return Regex.IsMatch(content,
            @"^[^;\r\n]+;[0-9A-Fa-f]{64}\s*$",
            RegexOptions.Multiline);
    }

    private static bool LooksLikeType2(string content)
    {
        // Тип 2: блоки "Хэш SHA256 <путь>" и следом строка с хешем
        return Regex.IsMatch(content,
            @"^Хэш\s+SHA256\s+.+\r?\n[0-9A-Fa-f]{64}\s*$",
            RegexOptions.Multiline);
    }

    // ---------- Парсер 1-го типа ----------
    // Формат:  FileName.ext;A780...F7367
    // Имя файла может содержать пробелы, кириллицу, подкаталоги (слэш/бэкслэш).
    private static List<FileHash> ParseType1(string content)
    {
        var result = new List<FileHash>();

        // Разделитель — последняя точка с запятой в строке (на случай, если в имени есть ';').
        var lineRe = new Regex(
            @"^(?<path>.+);(?<hash>[0-9A-Fa-f]{64})\s*$",
            RegexOptions.Multiline);

        foreach (Match m in lineRe.Matches(content))
        {
            string path = m.Groups["path"].Value.Trim();
            string hash = m.Groups["hash"].Value.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(path)) continue;

            result.Add(new FileHash
            {
                FullPath = path,
                FileName = Path.GetFileName(path),
                Hash = hash
            });
        }
        return result;
    }

    // ---------- Парсер 2-го типа ----------
    // Формат:
    //   Хэш SHA256 C:\Users\...\file.ext
    //   A78026705C9B993E5B32F3091038C93704C1D0BFBDD573A8577E2DE9B54F7367
    private static List<FileHash> ParseType2(string content)
    {
        var result = new List<FileHash>();

        var blockRe = new Regex(
            @"^Хэш\s+SHA256\s+(?<path>.+?)\s*\r?\n(?<hash>[0-9A-Fa-f]{64})\s*$",
            RegexOptions.Multiline);

        foreach (Match m in blockRe.Matches(content))
        {
            string path = m.Groups["path"].Value.Trim();
            string hash = m.Groups["hash"].Value.ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(path)) continue;

            result.Add(new FileHash
            {
                FullPath = path,
                FileName = Path.GetFileName(path),
                Hash = hash
            });
        }
        return result;
    }
}