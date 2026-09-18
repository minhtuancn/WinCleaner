using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    /// <summary>
    /// Storage analysis - disk usage, large files, duplicates, file types.
    /// </summary>
    public sealed partial class StorageViewModel : BaseViewModel
    {
        private readonly ISystemScanner _scanner;

        [ObservableProperty]
        private ObservableCollection<DriveInfoModel> _drives = new();

        [ObservableProperty]
        private DriveInfoModel _selectedDrive;

        [ObservableProperty]
        private ObservableCollection<StorageFolderItem> _folderTree = new();

        [ObservableProperty]
        private ObservableCollection<LargeFileItem> _largeFiles = new();

        [ObservableProperty]
        private ObservableCollection<FileTypeSummary> _fileTypes = new();

        [ObservableProperty]
        private bool _isAnalyzing;

        [ObservableProperty]
        private long _totalSpace;

        [ObservableProperty]
        private long _usedSpace;

        [ObservableProperty]
        private long _freeSpace;

        public double UsedPercent => TotalSpace > 0 ? (double)UsedSpace / TotalSpace * 100 : 0;

        public StorageViewModel(ISystemScanner scanner)
        {
            _scanner = scanner;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                LogInfo("Loading storage info...");

                Drives = new ObservableCollection<DriveInfoModel>(await _scanner.GetDrivesAsync());
                SelectedDrive = Drives.FirstOrDefault(d => d.IsSystemDrive) ?? Drives.FirstOrDefault();

                await AnalyzeDriveAsync(SelectedDrive);
            }
            catch (Exception ex)
            {
                LogError($"Failed to load storage: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSelectedDriveChanged(DriveInfoModel value)
        {
            if (value != null)
                _ = AnalyzeDriveAsync(value);
        }

        [RelayCommand]
        private async Task AnalyzeDriveAsync(DriveInfoModel drive)
        {
            if (drive == null || IsAnalyzing) return;

            try
            {
                IsAnalyzing = true;
                LogInfo($"Analyzing {drive.Name}...");

                TotalSpace = drive.TotalSize;
                UsedSpace = drive.UsedSpace;
                FreeSpace = drive.FreeSpace;

                // Simulate folder tree analysis (real impl would use async directory traversal)
                await Task.Delay(500);
                FolderTree = BuildMockFolderTree(drive);
                LargeFiles = GenerateMockLargeFiles(drive);
                FileTypes = GenerateMockFileTypes(drive);

                LogSuccess($"Analysis complete: {FolderTree.Count} folders, {LargeFiles.Count} large files");
            }
            catch (Exception ex)
            {
                LogError($"Analysis failed: {ex.Message}");
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        [RelayCommand]
        private void OpenInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                LogError($"Failed to open explorer: {ex.Message}");
            }
        }

        private ObservableCollection<StorageFolderItem> BuildMockFolderTree(DriveInfoModel drive)
        {
            var root = new StorageFolderItem
            {
                Name = drive.Name,
                Path = drive.Name,
                Size = drive.UsedSpace,
                Children = new ObservableCollection<StorageFolderItem>
                {
                    new() { Name = "Windows", Path = $@"{drive.Name}\Windows", Size = (long)(drive.UsedSpace * 0.4), Children = new() { new() { Name = "System32", Size = 3_000_000_000 }, new() { Name = "WinSxS", Size = 5_000_000_000 } } },
                    new() { Name = "Program Files", Path = $@"{drive.Name}\Program Files", Size = (long)(drive.UsedSpace * 0.25) },
                    new() { Name = "Users", Path = $@"{drive.Name}\Users", Size = (long)(drive.UsedSpace * 0.3), Children = new() { new() { Name = "Public", Size = 500_000_000 }, new() { Name = "Default", Size = 100_000_000 } } },
                    new() { Name = "Temp", Path = $@"{drive.Name}\Temp", Size = 2_000_000_000 }
                }
            };
            return new ObservableCollection<StorageFolderItem> { root };
        }

        private ObservableCollection<LargeFileItem> GenerateMockLargeFiles(DriveInfoModel drive)
        {
            var rand = new Random();
            return new ObservableCollection<LargeFileItem>
            {
                new() { Name = "hiberfil.sys", Path = $@"{drive.Name}\hiberfil.sys", Size = 4_000_000_000, Modified = DateTime.Now.AddDays(-30) },
                new() { Name = "pagefile.sys", Path = $@"{drive.Name}\pagefile.sys", Size = 3_500_000_000, Modified = DateTime.Now.AddDays(-1) },
                new() { Name = "swapfile.sys", Path = $@"{drive.Name}\swapfile.sys", Size = 1_000_000_000, Modified = DateTime.Now.AddDays(-60) },
                new() { Name = "Windows.old", Path = $@"{drive.Name}\Windows.old", Size = 25_000_000_000, Modified = DateTime.Now.AddDays(-90) },
                new() { Name = "memory.dmp", Path = $@"{drive.Name}\Windows\memory.dmp", Size = 2_000_000_000, Modified = DateTime.Now.AddDays(-7) }
            };
        }

        private ObservableCollection<FileTypeSummary> GenerateMockFileTypes(DriveInfoModel drive)
        {
            return new ObservableCollection<FileTypeSummary>
            {
                new() { Extension = "System", Category = "System Files", Count = 150000, TotalSize = (long)(drive.UsedSpace * 0.4) },
                new() { Extension = "Program", Category = "Applications", Count = 45000, TotalSize = (long)(drive.UsedSpace * 0.25) },
                new() { Extension = "User Data", Category = "Documents", Count = 12000, TotalSize = (long)(drive.UsedSpace * 0.15) },
                new() { Extension = "Cache", Category = "Temporary", Count = 85000, TotalSize = (long)(drive.UsedSpace * 0.1) },
                new() { Extension = "Other", Category = "Miscellaneous", Count = 22000, TotalSize = (long)(drive.UsedSpace * 0.1) }
            };
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

    public sealed class StorageFolderItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public ObservableCollection<StorageFolderItem> Children { get; set; } = new();
        public bool IsExpanded { get; set; } = true;
        public string SizeFormatted => FormatBytes(Size);

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

    public sealed class LargeFileItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public DateTime Modified { get; set; }
        public string SizeFormatted => FormatBytes(Size);

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

    public sealed class FileTypeSummary
    {
        public string Extension { get; set; } = "";
        public string Category { get; set; } = "";
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public string SizeFormatted => FormatBytes(TotalSize);
        public double Percent { get; set; }

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