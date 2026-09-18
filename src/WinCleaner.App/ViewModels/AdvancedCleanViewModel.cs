using System;
using System.Collections.ObjectModel;
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
    /// Advanced Clean - detailed category view with safety review.
    /// </summary>
    public sealed partial class AdvancedCleanViewModel : BaseViewModel
    {
        private readonly ISystemScanner _scanner;
        private readonly ICleanupPlanService _planService;

        [ObservableProperty]
        private ObservableCollection<CleanCategoryGroup> _groups = new();

        [ObservableProperty]
        private CleanProfile _selectedProfile = CleanProfile.Safe;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private long _totalScannableSize;

        [ObservableProperty]
        private int _totalItems;

        [ObservableProperty]
        private CleanupPlan _validatedPlan;

        public ObservableCollection<CleanProfile> AvailableProfiles { get; } = new()
        {
            CleanProfile.Safe,
            CleanProfile.Deep,
            CleanProfile.Custom
        };

        public AdvancedCleanViewModel(ISystemScanner scanner, ICleanupPlanService planService)
        {
            _scanner = scanner;
            _planService = planService;
        }

        [RelayCommand]
        private async Task ScanAsync()
        {
            if (IsScanning) return;

            try
            {
                IsScanning = true;
                IsBusy = true;
                LogInfo($"Starting {SelectedProfile} scan...");

                Groups = new ObservableCollection<CleanCategoryGroup>(await _scanner.ScanAsync(SelectedProfile));

                TotalItems = Groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).Count();
                TotalScannableSize = Groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).Sum(i => i.SizeBytes);

                LogSuccess($"Scan complete: {TotalItems} items, {FormatBytes(TotalScannableSize)}");
            }
            catch (Exception ex)
            {
                LogError($"Scan failed: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ValidatePlanAsync()
        {
            if (Groups == null || !Groups.Any()) return;

            try
            {
                IsBusy = true;
                LogInfo("Validating cleanup plan...");

                var plan = await _planService.CreatePlanAsync(Groups.ToList(), SelectedProfile);
                ValidatedPlan = await _planService.ValidatePlanAsync(plan);

                if (ValidatedPlan.IsValid)
                {
                    LogSuccess($"Plan validated: {ValidatedPlan.ReadySteps}/{ValidatedPlan.TotalSteps} ready, {FormatBytes(ValidatedPlan.SafeSizeBytes)} safe");
                }
                else
                {
                    LogWarning($"Plan has issues: {ValidatedPlan.ValidationErrors.Count} errors, {ValidatedPlan.ValidationWarnings.Count} warnings");
                }
            }
            catch (Exception ex)
            {
                LogError($"Validation failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExecutePlanAsync()
        {
            if (ValidatedPlan == null || !ValidatedPlan.IsValid) return;

            try
            {
                IsBusy = true;
                LogInfo("Executing cleanup plan...");

                var result = await _planService.ExecutePlanAsync(
                    ValidatedPlan,
                    new Progress<string>(msg => LogInfo(msg)),
                    new Progress<LogEntry>(e => LogMessage?.Invoke($"[{e.Timestamp:HH:mm:ss}] [{e.Level}] {e.Message}")),
                    default);

                LogSuccess($"Clean complete: {result.SuccessfulOperations}/{result.TotalSteps} succeeded, {result.TotalCleanedFormatted} freed");
                
                // Refresh scan
                await ScanAsync();
            }
            catch (Exception ex)
            {
                LogError($"Execution failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ToggleCategory(CleanCategoryGroup group)
        {
            group.IsAllSelected = !group.IsAllSelected;
            UpdateTotals();
        }

        [RelayCommand]
        private void ToggleItem(CleanItem item)
        {
            item.IsSelected = !item.IsSelected;
            UpdateTotals();
        }

        partial void OnSelectedProfileChanged(CleanProfile value)
        {
            // Re-scan when profile changes
            _ = ScanAsync();
        }

        private void UpdateTotals()
        {
            if (Groups == null) return;
            TotalItems = Groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).Count();
            TotalScannableSize = Groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).Sum(i => i.SizeBytes);
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