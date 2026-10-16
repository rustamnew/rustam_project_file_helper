using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

public enum StorageType
{
    Ssd,
    Hdd
}
public class ThrottledFileHasher
{

    private /*readonly*/ long _targetBytesPerSecond;
    private const long ProgressReportIntervalMs = 500;

    public ThrottledFileHasher(long targetMbPerSecond = 0)
    {
        _targetBytesPerSecond = targetMbPerSecond > 0 ? targetMbPerSecond * 1024L * 1024L : long.MaxValue;
    }

    public async Task<string> HashFileWithThrottleAsync(
        string filePath,
        StorageType storageType,
        IProgress<(long bytesRead, long totalSize, double speedMbPerSec)>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не указан.", nameof(filePath));

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Файл не найден.", filePath);

        long totalSize = fileInfo.Length;
        long totalBytesRead = 0;

        // ПОЛНОСТЬЮ АДАПТИВНЫЙ РАСЧЕТ БУФЕРА
        int bufferSize = CalculateOptimalBufferSize(totalSize, storageType);

        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        byte[] rentBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        Memory<byte> buffer = rentBuffer.AsMemory(0, bufferSize);

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        var stopwatch = Stopwatch.StartNew();
        long lastProgressReportMs = 0;
        int bytesRead;

        try
        {
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                incrementalHash.AppendData(buffer.Span[..bytesRead]);
                totalBytesRead += bytesRead;

                long elapsedMs = stopwatch.ElapsedMilliseconds;

                // Ограничение скорости
                if (_targetBytesPerSecond != long.MaxValue && elapsedMs > 0)
                {
                    long expectedBytes = (elapsedMs * _targetBytesPerSecond) / 1000L;
                    if (totalBytesRead > expectedBytes)
                    {
                        long excessBytes = totalBytesRead - expectedBytes;
                        int delayMs = (int)((excessBytes * 1000L) / _targetBytesPerSecond);

                        if (delayMs > 5)
                        {
                            await Task.Delay(delayMs, cancellationToken);
                            elapsedMs = stopwatch.ElapsedMilliseconds;
                        }
                    }
                }

                // Отчет о прогрессе
                if (progress != null && (elapsedMs - lastProgressReportMs >= ProgressReportIntervalMs))
                {
                    double elapsedSeconds = elapsedMs / 1000.0;
                    double currentSpeedMb = elapsedSeconds > 0 ? (totalBytesRead / 1024.0 / 1024.0) / elapsedSeconds : 0;

                    progress.Report((totalBytesRead, totalSize, currentSpeedMb));
                    lastProgressReportMs = elapsedMs;
                }
            }

            stopwatch.Stop();

            if (progress != null)
            {
                double totalSeconds = stopwatch.Elapsed.TotalSeconds;
                double finalSpeedMb = totalSeconds > 0 ? (totalBytesRead / 1024.0 / 1024.0) / totalSeconds : 0;
                progress.Report((totalBytesRead, totalSize, finalSpeedMb));
            }

            return Convert.ToHexString(incrementalHash.GetHashAndReset());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentBuffer);
        }
    }

    /// <summary>
    /// Динамически вычисляет размер буфера под текущий файл, тип диска и лимит скорости.
    /// </summary>
    private int CalculateOptimalBufferSize(long fileSize, StorageType storageType)
    {
        // 1. Задаем базовые границы для HDD и SSD для БОЛЬШИХ файлов
        int maxAllowedBuffer = storageType == StorageType.Hdd
            // ? 8 * 1024 * 1024 // Для HDD лучше читать кусками по 8 МБ
            ? 16 * 1024 * 1024

            : 2 * 1024 * 1024;  // Для SSD 2 МБ — «золотая середина»

        // 2. Корректируем буфер под ограничение скорости (Throttling)
        // Буфер не должен быть больше, чем объем данных за 0.5 секунды, иначе прогресс будет идти рывками
        if (_targetBytesPerSecond != long.MaxValue)
        {
            int halfSecondTargetBytes = (int)Math.Min(int.MaxValue, _targetBytesPerSecond / 2);
            // Берем минимальное из лимита диска и лимита скорости (но не меньше 64 КБ)
            maxAllowedBuffer = Math.Max(64 * 1024, Math.Min(maxAllowedBuffer, halfSecondTargetBytes));
        }

        // 3. Финальный выбор: если файл меньше максимального буфера, сжимаем буфер до размера файла
        // Сектора диска обычно по 4 КБ (4096 байт), поэтому меньше этого значения буфер делать не стоит
        if (fileSize < maxAllowedBuffer)
        {
            return (int)Math.Clamp(fileSize, 4096, maxAllowedBuffer);
        }

        return maxAllowedBuffer;
    }
}