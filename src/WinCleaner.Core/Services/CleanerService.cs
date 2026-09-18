using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public class CleanerService : ICleanerService
    {
        private bool _dryRun = false;

        public CleanerService()
        {
        }

        public bool DryRunMode
        {
            get => _dryRun;
            set => _dryRun = value;
        }

        public async Task<CleanResult> CleanAsync(List<CleanCategoryGroup> groups, IProgress<string> progress, IProgress<LogEntry> logProgress, CancellationToken cancellationToken = default)
        {
            var result = new CleanResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var selectedItems = groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).ToList();
            result.TotalItems = selectedItems.Count;
            result.TotalScannedSize = selectedItems.Sum(i => i.SizeBytes);

            Log(logProgress, LogLevel.Info, $"Bắt đầu dọn dẹp {result.TotalItems} mục ({FormatBytes(result.TotalScannedSize)})", "Cleaner");
            Log(logProgress, LogLevel.Info, _dryRun ? "CHẾ ĐỘ DRY-RUN - Không xóa thực tế" : "Chế độ thực thi", "Cleaner");

            progress?.Report($"Đang dọn dẹp {result.TotalItems} mục...");

            foreach (var item in selectedItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (item.SizeBytes == 0)
                {
                    item.Status = CleanStatus.Skipped;
                    result.SkippedItems++;
                    Log(logProgress, LogLevel.Warning, $"Bỏ qua (kích thước 0): {item.Name}", "Cleaner");
                    continue;
                }

                try
                {
                    item.Status = CleanStatus.Cleaning;
                    progress?.Report($"Đang dọn: {item.Name} ({item.SizeFormatted})");
                    Log(logProgress, LogLevel.Info, $"Đang dọn: {item.Name} ({item.SizeFormatted})", "Cleaner");

                    bool success = await CleanItemAsync(item, progress, logProgress, cancellationToken);
                    
                    if (success)
                    {
                        item.Status = CleanStatus.Cleaned;
                        result.CleanedItems++;
                        result.TotalCleanedSize += item.CleanedBytes;
                        Log(logProgress, LogLevel.Success, $"✓ Đã dọn: {item.Name} - Giải phóng {item.CleanedFormatted}", "Cleaner");
                    }
                    else
                    {
                        item.Status = CleanStatus.Failed;
                        result.FailedItems++;
                        Log(logProgress, LogLevel.Error, $"✗ Thất bại: {item.Name}", "Cleaner");
                    }
                }
                catch (Exception ex)
                {
                    item.Status = CleanStatus.Failed;
                    result.FailedItems++;
                    result.Errors.Add($"{item.Name}: {ex.Message}");
                    Log(logProgress, LogLevel.Error, $"✗ Lỗi {item.Name}: {ex.Message}", "Cleaner");
                }
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            // Group by category
            foreach (var group in groups)
            {
                long catCleaned = group.Items.Where(i => i.IsSelected && i.Status == CleanStatus.Cleaned).Sum(i => i.CleanedBytes);
                if (catCleaned > 0)
                    result.CleanedByCategory[group.Category] = catCleaned;
            }

            Log(logProgress, LogLevel.Success, $"=== HOÀN TẤT ===", "Cleaner");
            Log(logProgress, LogLevel.Success, $"Đã dọn: {result.CleanedItems}/{result.TotalItems} mục", "Cleaner");
            Log(logProgress, LogLevel.Success, $"Giải phóng: {FormatBytes(result.TotalCleanedSize)}", "Cleaner");
            Log(logProgress, LogLevel.Success, $"Thời gian: {result.Duration:mm\\:ss\\.ff}", "Cleaner");
            if (result.FailedItems > 0)
                Log(logProgress, LogLevel.Warning, $"Thất bại: {result.FailedItems} mục", "Cleaner");

            return result;
        }

        public async Task<bool> CleanItemAsync(CleanItem item, IProgress<string> progress, IProgress<LogEntry> logProgress, CancellationToken cancellationToken = default)
        {
            if (item.CleanAction == null)
                return false;

            if (_dryRun)
            {
                // Simulate cleaning in dry-run mode
                await Task.Delay(100, cancellationToken);
                item.CleanedBytes = item.SizeBytes;
                progress?.Report($"[DRY-RUN] Sẽ xóa: {item.Name} ({item.SizeFormatted})");
                return true;
            }

            try
            {
                await item.CleanAction(item, progress);
                return item.CleanedBytes > 0 || item.Status == CleanStatus.Cleaned;
            }
            catch (Exception ex)
            {
                Log(logProgress, LogLevel.Error, $"Lỗi khi dọn {item.Name}: {ex.Message}", "Cleaner");
                return false;
            }
        }

        private void Log(IProgress<LogEntry> logProgress, LogLevel level, string message, string source)
        {
            logProgress?.Report(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source
            });
        }

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
    }
}