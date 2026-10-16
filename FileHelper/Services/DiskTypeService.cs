using System;
using System.IO;
using System.Runtime.InteropServices;
#if WINDOWS
using System.Management; // Requires NuGet: System.Management
#endif

public static class DiskTypeDetector
{
    public static string GetDiskType(string driveLetterOrPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsDiskType(driveLetterOrPath);
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxDiskType();
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetMacDiskType();
        }

        return "Unknown OS";
    }

    #region Windows Implementation
    private static string GetWindowsDiskType(string driveLetter)
    {
        return "Unknown";
        /*try
        {
            // Standardize drive letter to query (e.g., "C:")
            string cleanLetter = driveLetter.TrimEnd('\\').Replace(":", "");

            // Query WMI Storage namespace for the physical disk media type
            using var searcher = new ManagementObjectSearcher(
                @"\\.\root\Microsoft\Windows\Storage",
                "SELECT MediaType FROM MSFT_PhysicalDisk");

            foreach (ManagementObject disk in searcher.Get())
            {
                int mediaType = Convert.ToInt32(disk["MediaType"]);
                return mediaType switch
                {
                    3 => "HDD",
                    4 => "SSD",
                    5 => "SCM (Storage Class Memory)",
                    _ => "Unspecified"
                };
            }
        }
        catch
        {
            // Fallback if WMI fails or lacks permissions
        }
        return "Unknown";*/
    }
    #endregion

    #region Linux Implementation
    private static string GetLinuxDiskType()
    {
        try
        {
            // Linux marks non-rotational disks (SSDs) with a '0' in /sys/block/sdX/queue/rotational
            // Checking the primary drive template
            if (File.Exists("/sys/block/sda/queue/rotational"))
            {
                string status = File.ReadAllText("/sys/block/sda/queue/rotational").Trim();
                return status == "0" ? "SSD" : "HDD";
            }
            if (File.Exists("/sys/block/nvme0n1/queue/rotational"))
            {
                return "SSD (NVMe)";
            }
        }
        catch { }
        return "Unknown";
    }
    #endregion

    #region macOS Implementation
    private static string GetMacDiskType()
    {
        // macOS requires reading system profile parameters via terminal
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "diskutil",
                    Arguments = "info /",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("Solid State") || output.Contains("Yes (Solid State)"))
                return "SSD";
            if (output.Contains("Hard Disk"))
                return "HDD";
        }
        catch { }
        return "Unknown";
    }
    #endregion
}
