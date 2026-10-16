using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileHelper.Models;
using SharpCompress.Archives;

namespace FileHelper.Services;

public sealed class FileArchiveSearcherService
{
    private static readonly uint[] Crc32Table = CreateCrc32Table();

    public Task<ArchiveSearchOutcome> SearchAsync(
        IEnumerable<string> filePaths,
        IEnumerable<string> archivePaths,
        bool compareCrc32) =>
        Task.Run(() => Search(filePaths.ToArray(), archivePaths.ToArray(), compareCrc32));

    private static ArchiveSearchOutcome Search(string[] filePaths, string[] archivePaths, bool compareCrc32)
    {
        var matches = filePaths.ToDictionary(path => path, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);
        var filesByName = filePaths
            .GroupBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var fileCrcs = compareCrc32
            ? filePaths.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(path => path, ComputeFileCrc32, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        foreach (var archivePath in archivePaths)
        {
            try
            {
                using var archive = ArchiveFactory.OpenArchive(archivePath);
                var entriesByName = archive.Entries
                    .Where(entry => !entry.IsDirectory && !string.IsNullOrEmpty(entry.Key))
                    .Select(entry => new { Entry = entry, Name = GetEntryName(entry.Key!) })
                    .Where(item => !string.IsNullOrEmpty(item.Name))
                    .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase);

                foreach (var entryGroup in entriesByName)
                {
                    if (!filesByName.TryGetValue(entryGroup.Key, out var candidates))
                        continue;

                    var entryCrcs = compareCrc32
                        ? entryGroup.Select(item => ComputeEntryCrc32(item.Entry)).ToArray()
                        : Array.Empty<uint>();

                    foreach (var candidate in candidates)
                    {
                        var found = !compareCrc32 || entryCrcs.Contains(fileCrcs[candidate]);

                        if (found && !matches[candidate].Contains(archivePath, StringComparer.OrdinalIgnoreCase))
                            matches[candidate].Add(archivePath);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(archivePath)}: {ex.Message}");
            }
        }

        return new ArchiveSearchOutcome
        {
            Results = filePaths.Select(path => new ArchiveSearchResult
            {
                FilePath = path,
                Archives = matches[path]
            }).ToArray(),
            Errors = errors
        };
    }

    private static string GetEntryName(string entryKey)
    {
        var normalizedKey = entryKey.Replace('\\', '/');
        var separatorIndex = normalizedKey.LastIndexOf('/');
        return separatorIndex >= 0 ? normalizedKey[(separatorIndex + 1)..] : normalizedKey;
    }

    private static uint ComputeFileCrc32(string path)
    {
        using var stream = File.OpenRead(path);
        return ComputeCrc32(stream);
    }

    private static uint ComputeEntryCrc32(IArchiveEntry entry)
    {
        using var stream = entry.OpenEntryStream();
        return ComputeCrc32(stream);
    }

    private static uint ComputeCrc32(Stream stream)
    {
        var buffer = new byte[81920];
        var crc = uint.MaxValue;
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var index = 0; index < bytesRead; index++)
                crc = Crc32Table[(crc ^ buffer[index]) & 0xff] ^ (crc >> 8);
        }

        return ~crc;
    }

    private static uint[] CreateCrc32Table()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var value = index;
            for (var bit = 0; bit < 8; bit++)
                value = (value & 1) != 0 ? 0xedb88320 ^ (value >> 1) : value >> 1;
            table[index] = value;
        }

        return table;
    }
}