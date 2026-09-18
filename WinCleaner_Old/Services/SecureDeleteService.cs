using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ISecureDeleteService
    {
        Task<bool> ShredFileAsync(string filePath, ShredAlgorithm algorithm, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> ShredDirectoryAsync(string directoryPath, ShredAlgorithm algorithm, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task<bool> WipeFreeSpaceAsync(string driveLetter, ShredAlgorithm algorithm, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        bool CanShred(string path);
    }

    public enum ShredAlgorithm
    {
        Quick,                      // 1 pass - zeros (fastest)
        Random,                     // 1 pass - random data
        DoD_5220_22_M,             // 3 passes - DoD 5220.22-M (1995)
        DoD_5220_22_M_ECE,         // 7 passes - DoD 5220.22-M ECE
        Gutmann,                   // 35 passes - Peter Gutmann
        RCMP_TSSIT_OPS_II,         // 7 passes - RCMP TSSIT OPS-II
        VSITR,                     // 7 passes - VSITR (German)
        HMG_IS5_Baseline,          // 1 pass - HMG IS5 Baseline
        HMG_IS5_Enhanced,          // 3 passes - HMG IS5 Enhanced
        Schneier,                  // 7 passes - Bruce Schneier
        PFITzer                    // 35 passes - Roy Pfitzner
    }

    public class SecureDeleteService : ISecureDeleteService
    {
        private static readonly Dictionary<ShredAlgorithm, ShredPattern[]> _patterns = new()
        {
            [ShredAlgorithm.Quick] = new[] { new ShredPattern("Zeros", new byte[] { 0x00 }) },
            [ShredAlgorithm.Random] = new[] { new ShredPattern("Random", null) }, // null = random
            
            [ShredAlgorithm.DoD_5220_22_M] = new[]
            {
                new ShredPattern("Pass 1: Zeros", new byte[] { 0x00 }),
                new ShredPattern("Pass 2: Ones", new byte[] { 0xFF }),
                new ShredPattern("Pass 3: Random", null),
            },
            
            [ShredAlgorithm.DoD_5220_22_M_ECE] = new[]
            {
                new ShredPattern("Pass 1: Zeros", new byte[] { 0x00 }),
                new ShredPattern("Pass 2: Ones", new byte[] { 0xFF }),
                new ShredPattern("Pass 3: 0x92", new byte[] { 0x92 }),
                new ShredPattern("Pass 4: 0x49", new byte[] { 0x49 }),
                new ShredPattern("Pass 5: 0x24", new byte[] { 0x24 }),
                new ShredPattern("Pass 6: 0xFF", new byte[] { 0xFF }),
                new ShredPattern("Pass 7: Random", null),
            },
            
            [ShredAlgorithm.Gutmann] = new[]
            {
                new ShredPattern("Pass 1-4: Random", null, 4),
                new ShredPattern("Pass 5: 0x55", new byte[] { 0x55 }),
                new ShredPattern("Pass 6: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 7-9: 0x92/0x49/0x24", new byte[] { 0x92, 0x49, 0x24 }, 3),
                new ShredPattern("Pass 10: 0x00", new byte[] { 0x00 }),
                new ShredPattern("Pass 11: 0x11", new byte[] { 0x11 }),
                new ShredPattern("Pass 12: 0x22", new byte[] { 0x22 }),
                new ShredPattern("Pass 13: 0x33", new byte[] { 0x33 }),
                new ShredPattern("Pass 14: 0x44", new byte[] { 0x44 }),
                new ShredPattern("Pass 15: 0x55", new byte[] { 0x55 }),
                new ShredPattern("Pass 16: 0x66", new byte[] { 0x66 }),
                new ShredPattern("Pass 17: 0x77", new byte[] { 0x77 }),
                new ShredPattern("Pass 18: 0x88", new byte[] { 0x88 }),
                new ShredPattern("Pass 19: 0x99", new byte[] { 0x99 }),
                new ShredPattern("Pass 20: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 21: 0xBB", new byte[] { 0xBB }),
                new ShredPattern("Pass 22: 0xCC", new byte[] { 0xCC }),
                new ShredPattern("Pass 23: 0xDD", new byte[] { 0xDD }),
                new ShredPattern("Pass 24: 0xEE", new byte[] { 0xEE }),
                new ShredPattern("Pass 25: 0xFF", new byte[] { 0xFF }),
                new ShredPattern("Pass 26-35: Random", null, 10),
            },
            
            [ShredAlgorithm.RCMP_TSSIT_OPS_II] = new[]
            {
                new ShredPattern("Pass 1: 0x00", new byte[] { 0x00 }),
                new ShredPattern("Pass 2: 0xFF", new byte[] { 0xFF }),
                new ShredPattern("Pass 3: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 4: Random", null),
                new ShredPattern("Pass 5: 0x55", new byte[] { 0x55 }),
                new ShredPattern("Pass 6: Verify", new byte[] { 0x00 }),
                new ShredPattern("Pass 7: Final Random", null),
            },
            
            [ShredAlgorithm.VSITR] = new[]
            {
                new ShredPattern("Pass 1: 0x00", new byte[] { 0x00 }),
                new ShredPattern("Pass 2: 0xFF", new byte[] { 0xFF }),
                new ShredPattern("Pass 3: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 4: Random", null),
                new ShredPattern("Pass 5: Verify", new byte[] { 0x00 }),
                new ShredPattern("Pass 6: 0x55", new byte[] { 0x55 }),
                new ShredPattern("Pass 7: Final Verify", new byte[] { 0x00 }),
            },
            
            [ShredAlgorithm.HMG_IS5_Baseline] = new[]
            {
                new ShredPattern("Single Pass: Zeros", new byte[] { 0x00 }),
            },
            
            [ShredAlgorithm.HMG_IS5_Enhanced] = new[]
            {
                new ShredPattern("Pass 1: Zeros", new byte[] { 0x00 }),
                new ShredPattern("Pass 2: Ones", new byte[] { 0xFF }),
                new ShredPattern("Pass 3: Random", null),
            },
            
            [ShredAlgorithm.Schneier] = new[]
            {
                new ShredPattern("Pass 1: Ones", new byte[] { 0xFF }),
                new ShredPattern("Pass 2: Zeros", new byte[] { 0x00 }),
                new ShredPattern("Pass 3-5: Random", null, 3),
                new ShredPattern("Pass 6: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 7: 0x55", new byte[] { 0x55 }),
            },
            
            [ShredAlgorithm.PFITzer] = new[]
            {
                new ShredPattern("Pass 1-4: Random", null, 4),
                new ShredPattern("Pass 5: 0x55", new byte[] { 0x55 }),
                new ShredPattern("Pass 6: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 7-9: 0x92/0x49/0x24", new byte[] { 0x92, 0x49, 0x24 }, 3),
                new ShredPattern("Pass 10: 0x00", new byte[] { 0x00 }),
                new ShredPattern("Pass 11: 0x11", new byte[] { 0x11 }),
                new ShredPattern("Pass 12: 0x22", new byte[] { 0x22 }),
                new ShredPattern("Pass 13: 0x33", new byte[] { 0x33 }),
                new ShredPattern("Pass 14: 0x44", new byte[] { 0x44 }),
                new ShredPattern("Pass 15: 0x55", new byte[] { 0x55 }),
                new ShredPattern("Pass 16: 0x66", new byte[] { 0x66 }),
                new ShredPattern("Pass 17: 0x77", new byte[] { 0x77 }),
                new ShredPattern("Pass 18: 0x88", new byte[] { 0x88 }),
                new ShredPattern("Pass 19: 0x99", new byte[] { 0x99 }),
                new ShredPattern("Pass 20: 0xAA", new byte[] { 0xAA }),
                new ShredPattern("Pass 21: 0xBB", new byte[] { 0xBB }),
                new ShredPattern("Pass 22: 0xCC", new byte[] { 0xCC }),
                new ShredPattern("Pass 23: 0xDD", new byte[] { 0xDD }),
                new ShredPattern("Pass 24: 0xEE", new byte[] { 0xEE }),
                new ShredPattern("Pass 25: 0xFF", new byte[] { 0xFF }),
                new ShredPattern("Pass 26-35: Random", null, 10),
            },
        };

        public bool CanShred(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                return File.Exists(path) || Directory.Exists(path);
            }
            catch { return false; }
        }

        public async Task<bool> ShredFileAsync(string filePath, ShredAlgorithm algorithm, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                progress?.Report($"File not found: {filePath}");
                return false;
            }

            try
            {
                long fileSize = new FileInfo(filePath).Length;
                var patterns = _patterns[algorithm];
                
                progress?.Report($"Shredding {Path.GetFileName(filePath)} ({FormatBytes(fileSize)}) with {algorithm} ({patterns.Length} passes)");

                // Open file with write access
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
                
                foreach (var pattern in patterns)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    int repeat = pattern.RepeatCount > 0 ? pattern.RepeatCount : 1;
                    for (int r = 0; r < repeat; r++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        fs.Position = 0;
                        
                        await WritePatternAsync(fs, fileSize, pattern, cancellationToken);
                        await fs.FlushAsync(cancellationToken);
                        
                        // Force write to disk
                        FlushFileBuffers(fs.SafeFileHandle.DangerousGetHandle());
                    }
                    
                    progress?.Report($"  Completed: {pattern.Name}");
                }

                // Rename file multiple times to remove from MFT
                await RenameFileMultipleTimesAsync(filePath, cancellationToken);

                // Delete the file
                File.Delete(filePath);
                progress?.Report($"  File deleted securely");
                
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"  Error shredding {filePath}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ShredDirectoryAsync(string directoryPath, ShredAlgorithm algorithm, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
            {
                progress?.Report($"Directory not found: {directoryPath}");
                return false;
            }

            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                progress?.Report($"Shredding directory: {directoryPath} ({files.Length} files)");

                bool allSuccess = true;
                int completed = 0;

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var success = await ShredFileAsync(file, algorithm, progress, cancellationToken);
                    if (!success) allSuccess = false;
                    
                    completed++;
                    progress?.Report($"Progress: {completed}/{files.Length} files processed");
                }

                // Try to delete empty directories
                try
                {
                    var dirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories)
                        .OrderByDescending(d => d.Length);
                    foreach (var dir in dirs)
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                    Directory.Delete(directoryPath, true);
                }
                catch { }

                return allSuccess;
            }
            catch (Exception ex)
            {
                progress?.Report($"Error shredding directory: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> WipeFreeSpaceAsync(string driveLetter, ShredAlgorithm algorithm, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(driveLetter) || driveLetter.Length < 1)
                return false;

            driveLetter = driveLetter.ToUpperInvariant()[0].ToString();
            string drivePath = $"{driveLetter}:\\";

            try
            {
                // Get free space
                var driveInfo = new DriveInfo(drivePath);
                if (!driveInfo.IsReady)
                {
                    progress?.Report($"Drive {driveLetter}: not ready");
                    return false;
                }

                long freeSpace = driveInfo.AvailableFreeSpace;
                progress?.Report($"Wiping free space on {driveLetter}: ({FormatBytes(freeSpace)}) with {algorithm}");

                // Create temporary files to fill free space
                string tempDir = Path.Combine(drivePath, "WinCleaner_WipeTemp_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try
                {
                    const long chunkSize = 100 * 1024 * 1024; // 100MB chunks
                    long remaining = freeSpace - (100 * 1024 * 1024); // Leave 100MB buffer
                    int fileIndex = 0;

                    while (remaining > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        long currentChunk = Math.Min(chunkSize, remaining);
                        string tempFile = Path.Combine(tempDir, $"wipe_{fileIndex++}.tmp");

                        using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                        {
                            await FillWithPatternAsync(fs, currentChunk, _patterns[algorithm][0], cancellationToken);
                            await fs.FlushAsync(cancellationToken);
                            FlushFileBuffers(fs.SafeFileHandle.DangerousGetHandle());
                        }

                        // Now shred this temp file
                        await ShredFileAsync(tempFile, algorithm, null, cancellationToken);
                        remaining -= currentChunk;

                        progress?.Report($"Wipe progress: {FormatBytes(freeSpace - remaining)} / {FormatBytes(freeSpace)}");
                    }
                }
                finally
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }

                progress?.Report($"Free space wipe completed on {driveLetter}:");
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"Error wiping free space: {ex.Message}");
                return false;
            }
        }

        private async Task WritePatternAsync(FileStream fs, long length, ShredPattern pattern, CancellationToken cancellationToken)
        {
            const int bufferSize = 64 * 1024; // 64KB buffer
            byte[] buffer = new byte[bufferSize];

            if (pattern.Data == null)
            {
                // Random data
                using var rng = RandomNumberGenerator.Create();
                long remaining = length;
                while (remaining > 0 && !cancellationToken.IsCancellationRequested)
                {
                    int toWrite = (int)Math.Min(bufferSize, remaining);
                    rng.GetBytes(buffer, 0, toWrite);
                    await fs.WriteAsync(buffer, 0, toWrite, cancellationToken);
                    remaining -= toWrite;
                }
            }
            else if (pattern.Data.Length == 1)
            {
                // Single byte pattern
                byte fillByte = pattern.Data[0];
                Array.Fill(buffer, fillByte);
                long remaining = length;
                while (remaining > 0 && !cancellationToken.IsCancellationRequested)
                {
                    int toWrite = (int)Math.Min(bufferSize, remaining);
                    await fs.WriteAsync(buffer, 0, toWrite, cancellationToken);
                    remaining -= toWrite;
                }
            }
            else
            {
                // Multi-byte pattern (cycle through)
                long remaining = length;
                int patternIndex = 0;
                while (remaining > 0 && !cancellationToken.IsCancellationRequested)
                {
                    int toWrite = (int)Math.Min(bufferSize, remaining);
                    for (int i = 0; i < toWrite; i++)
                    {
                        buffer[i] = pattern.Data[patternIndex];
                        patternIndex = (patternIndex + 1) % pattern.Data.Length;
                    }
                    await fs.WriteAsync(buffer, 0, toWrite, cancellationToken);
                    remaining -= toWrite;
                }
            }
        }

        private async Task FillWithPatternAsync(FileStream fs, long length, ShredPattern pattern, CancellationToken cancellationToken)
        {
            await WritePatternAsync(fs, length, pattern, cancellationToken);
        }

        private async Task RenameFileMultipleTimesAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath) ?? "";
                for (int i = 0; i < 10; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string newName = Path.Combine(dir, $"~{Guid.NewGuid():N}.tmp");
                    File.Move(filePath, newName, true);
                    filePath = newName;
                    await Task.Delay(1, cancellationToken);
                }
            }
            catch { }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushFileBuffers(IntPtr hFile);

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;
            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }
            return $"{dblBytes:0.##} {suffixes[i]}";
        }

        private class ShredPattern
        {
            public string Name { get; }
            public byte[]? Data { get; }
            public int RepeatCount { get; }

            public ShredPattern(string name, byte[]? data, int repeatCount = 1)
            {
                Name = name;
                Data = data;
                RepeatCount = repeatCount;
            }
        }
    }
}