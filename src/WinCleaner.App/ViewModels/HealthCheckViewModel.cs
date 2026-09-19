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
    /// Health Check dashboard - primary entry point.
    /// Shows system status, reclaimable space, and primary actions.
    /// </summary>
    public sealed partial class HealthCheckViewModel : BaseViewModel
    {
        private readonly ISystemScanner _scanner;
        private readonly ICleanupPlanService _planService;
        private readonly IThemeConfigurationStore _themeStore;

        [ObservableProperty]
        private SystemInfoModel _systemInfo = new();

        [ObservableProperty]
        private ObservableCollection<DriveInfoModel> _drives = new();

        [ObservableProperty]
        private long _reclaimableBytes;

        [ObservableProperty]
        private int _issuesFound;

        [ObservableProperty]
        private HealthStatus _overallStatus = HealthStatus.Unknown;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _lastScanTime = "Never";

        public ObservableCollection<HealthCategoryItem> Categories { get; } = new();

        public HealthCheckViewModel(
            ISystemScanner scanner,
            ICleanupPlanService planService,
            IThemeConfigurationStore themeStore)
        {
            _scanner = scanner;
            _planService = planService;
            _themeStore = themeStore;
        }

        [RelayCommand]
        private async Task ScanAsync()
        {
            if (IsScanning) return;

            try
            {
                IsScanning = true;
                IsBusy = true;
                LogInfo("Starting health scan...");

                var groups = await _scanner.ScanAsync(CleanProfile.Safe);
                var selectedItems = groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).ToList();

                ReclaimableBytes = selectedItems.Sum(i => i.SizeBytes);
                IssuesFound = selectedItems.Count;
                OverallStatus = ReclaimableBytes > 1_000_000_000 ? HealthStatus.Warning : 
                               ReclaimableBytes > 100_000_000 ? HealthStatus.Caution : HealthStatus.Good;

                // Group by category for display
                Categories.Clear();
                foreach (var group in groups.Where(g => g.Items.Any(i => i.IsSelected)))
                {
                    var catItems = group.Items.Where(i => i.IsSelected).ToList();
                    Categories.Add(new HealthCategoryItem
                    {
                        Category = group.Category,
                        Name = group.Name,
                        ItemCount = catItems.Count,
                        TotalSize = catItems.Sum(i => i.SizeBytes),
                        RiskLevel = catItems.Max(i => i.RiskLevel)
                    });
                }

                LastScanTime = DateTime.Now.ToString("HH:mm:ss");
                LogSuccess($"Scan complete: {IssuesFound} items, {FormatBytes(ReclaimableBytes)} reclaimable");
            }
            catch (Exception ex)
            {
                LogError($"Scan failed: {ex.Message}");
                OverallStatus = HealthStatus.Error;
            }
            finally
            {
                IsScanning = false;
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task QuickCleanAsync()
        {
            if (IssuesFound == 0) return;

            try
            {
                IsBusy = true;
                LogInfo("Starting quick clean...");

                var groups = await _scanner.ScanAsync(CleanProfile.Safe);
                var plan = await _planService.CreatePlanAsync(groups, CleanProfile.Safe);
                plan = await _planService.ValidatePlanAsync(plan);

                if (!plan.IsValid)
                {
                    LogWarning("Some items require review before cleaning");
                    return;
                }

                var result = await _planService.ExecutePlanAsync(plan, new Progress<string>(msg => LogInfo(msg)), new Progress<LogEntry>(e => LogMessage?.Invoke($"[{e.Timestamp:HH:mm:ss}] [{e.Level}] {e.Message}")), default);

                ReclaimableBytes -= result.TotalCleanedBytes;
                IssuesFound -= result.SuccessfulOperations;
                LogSuccess($"Quick clean complete: {result.TotalCleanedFormatted} freed");
            }
            catch (Exception ex)
            {
                LogError($"Clean failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenAdvancedClean()
        {
            // Navigation handled by ShellViewModel
        }

        public async Task InitializeAsync()
        {
            await LoadSystemInfoAsync();
            await ScanAsync();
        }

        private async Task LoadSystemInfoAsync()
        {
            try
            {
                SystemInfo = await _scanner.GetSystemInfoAsync();
                Drives = new ObservableCollection<DriveInfoModel>(await _scanner.GetDrivesAsync());
            }
            catch (Exception ex)
            {
                LogError($"Failed to load system info: {ex.Message}");
            }
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

    public enum HealthStatus
    {
        Unknown,
        Good,
        Caution,
        Warning,
        Error
    }

    public sealed class HealthCategoryItem
    {
        public CleanCategory Category { get; set; }
        public string Name { get; set; } = "";
        public int ItemCount { get; set; }
        public long TotalSize { get; set; }
        public ItemRiskLevel RiskLevel { get; set; }
        public string SizeFormatted => FormatBytes(TotalSize);

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