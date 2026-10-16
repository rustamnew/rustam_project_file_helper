using System;
using System.Collections.Generic;

namespace FileHelper.Models;

public sealed class ArchiveSearchResult
{
    public required string FilePath { get; init; }

    public required IReadOnlyList<string> Archives { get; init; }

    public bool IsFound => Archives.Count > 0;

    public string Status => IsFound ? "Найдено" : "Не найдено";

    public string ArchiveList => IsFound ? string.Join(Environment.NewLine, Archives) : "Не найдено ни в одном архиве";
}

public sealed class ArchiveSearchOutcome
{
    public required IReadOnlyList<ArchiveSearchResult> Results { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }
}