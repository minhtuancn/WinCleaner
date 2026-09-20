using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinCleaner.Design;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels;

public partial class CleanerViewModel : BaseViewModel, INavigableViewModel
{
    private readonly ISystemScanner _scanner;
    private readonly ICleanerService _cleaner;
    private readonly ILogger<CleanerViewModel> _logger;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _cleanCts;

    [ObservableProperty]
    private ObservableCollection<CleanCategoryGroup> _categories = new();

    [ObservableProperty]
    private CleanProfile _selectedProfile = CleanProfile.Safe;

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private bool _isCleaning = false;

    [ObservableProperty]
    private double _overallProgress = 0;

    [ObservableProperty]
    private string _scanProgressText = "";

    [ObservableProperty]
    private long _totalScannableSize = 0;

    [ObservableProperty]
    private long _totalSelectedSize = 0;

    [ObservableProperty]
    private long _totalCleanedSize = 0;

    [ObservableProperty]
    private int _totalItems = 0;

    [ObservableProperty]
    private int _selectedItems = 0;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = new();

    [ObservableProperty]
    private bool _autoScrollLog = true;

    [ObservableProperty]
    private string _filterText = "";

    public string Title => "Cleaner";
    public System.Windows.Media.Geometry Icon => Icons.Broom;
    public bool IsSelected { get; set; }

    public ICommand ScanCommand { get; }
    public ICommand CleanCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectSafeCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand ExportLogCommand { get; }

    public CleanerViewModel(
        ISystemScanner scanner,
        ICleanerService cleaner,
        ILogger<CleanerViewModel> logger)
    {
        _scanner = scanner;
        _cleaner = cleaner;
        _logger = logger;

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsScanning && !IsCleaning);
        CleanCommand = new AsyncRelayCommand(CleanAsync, () => !IsScanning && !IsCleaning && TotalSelectedSize > 0);
        CancelCommand = new RelayCommand(Cancel);
        SelectSafeCommand = new RelayCommand(SelectSafeItems);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        ExpandAllCommand = new RelayCommand(ExpandAll);
        CollapseAllCommand = new RelayCommand(CollapseAll);
        ClearLogCommand = new RelayCommand(() => LogEntries.Clear());
        ExportLogCommand = new AsyncRelayCommand(ExportLogAsync);

        InitializeProfiles();
    }

    public ObservableCollection<ProfileOption> AvailableProfiles { get; } = new();

    private void InitializeProfiles()
    {
        AvailableProfiles.Add(new ProfileOption { Profile = CleanProfile.Safe, DisplayName = "An toàn (Khuyến nghị)", Description = "Chỉ dọn các mục rủi ro thấp: cache temp, log, recycle bin" });
        AvailableProfiles.Add(new ProfileOption { Profile = CleanProfile.Deep, DisplayName = "Sâu (Nâng cao)", Description = "Bao gồm cache trình duyệt, dev tools, driver store cũ" });
        AvailableProfiles.Add(new ProfileOption { Profile = CleanProfile.Custom, DisplayName = "Tùy chỉnh", Description = "Chọn thủ công các mục cần dọn" });
        AvailableProfiles.Add(new ProfileOption { Profile = CleanProfile.Nuclear, DisplayName = "Cực đại (Chuyên gia)", Description = "Tất cả mục bao gồm compact OS, hibernation, system restore - CẢNH BÁO" });
    }

    partial void OnSelectedProfileChanged(CleanProfile value)
    {
        _ = ScanAsync();
    }

    partial void OnFilterTextChanged(string value)
    {
        // Filter logic can be applied here if needed
    }

    public async Task ScanAsync()
    {
        if (IsScanning || IsCleaning) return;

        _scanCts = new CancellationTokenSource();
        IsScanning = true;
        OverallProgress = 0;
        ScanProgressText = "Đang quét...";
        LogEntries.Clear();

        try
        {
            LogInfo($"Bắt đầu quét với profile: {SelectedProfile}");

            var groups = await _scanner.ScanAsync(SelectedProfile,
                new Progress<string>(msg =>
                {
                    ScanProgressText = msg;
                    LogDebug(msg);
                }),
                _scanCts.Token);

            var organizedGroups = OrganizeGroups(groups);

            Categories.Clear();
            foreach (var g in organizedGroups)
            {
                g.PropertyChanged += Group_PropertyChanged;
                foreach (var item in g.Items)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
                Categories.Add(g);
            }

            UpdateTotals();
            LogSuccess($"Quét hoàn tất: {TotalItems} mục, {FormatBytes(TotalScannableSize)} có thể dọn");
            ScanProgressText = "Quét hoàn tất";
        }
        catch (OperationCanceledException)
        {
            LogWarning("Quét đã bị hủy");
        }
        catch (Exception ex)
        {
            LogError($"Lỗi quét: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
            _scanCts = null;
            UpdateCommandStates();
        }
    }

    private List<CleanCategoryGroup> OrganizeGroups(List<CleanCategoryGroup> groups)
    {
        var systemCategories = new[]
        {
            CleanCategory.SystemTemp,
            CleanCategory.WindowsUpdate,
            CleanCategory.SystemLogs,
            CleanCategory.DriverStore,
            CleanCategory.CompactOS,
            CleanCategory.Hibernation,
            CleanCategory.SystemRestore,
            CleanCategory.WindowsOld,
            CleanCategory.RecycleBin
        };

        var appCategories = new[]
        {
            CleanCategory.BrowserCache,
            CleanCategory.DevToolsCache,
            CleanCategory.UserTemp,
            CleanCategory.DiscordOldVersions,
            CleanCategory.PlaywrightBrowsers,
            CleanCategory.MinecraftTemp,
            CleanCategory.UpdaterCaches,
            CleanCategory.GameData,
            CleanCategory.UserPrograms,
            CleanCategory.OtherUsers
        };

        var systemGroup = new CleanCategoryGroup
        {
            Category = CleanCategory.SystemTemp,
            Name = "🖥 Hệ thống",
            Description = "Các mục rác liên quan đến hệ điều hành Windows",
            Items = new ObservableCollection<CleanItem>(),
            MaxRiskLevel = ItemRiskLevel.Safe
        };

        var appGroup = new CleanCategoryGroup
        {
            Category = CleanCategory.BrowserCache,
            Name = "📱 Ứng dụng",
            Description = "Cache và dữ liệu tạm từ các ứng dụng cài đặt",
            Items = new ObservableCollection<CleanItem>(),
            MaxRiskLevel = ItemRiskLevel.Safe
        };

        var otherGroup = new CleanCategoryGroup
        {
            Category = CleanCategory.OtherUsers,
            Name = "📦 Khác",
            Description = "Các mục không phân loại được",
            Items = new ObservableCollection<CleanItem>(),
            MaxRiskLevel = ItemRiskLevel.Safe
        };

        foreach (var group in groups)
        {
            if (systemCategories.Contains(group.Category))
            {
                foreach (var item in group.Items)
                    systemGroup.Items.Add(item);
                systemGroup.MaxRiskLevel = (ItemRiskLevel)Math.Max((int)systemGroup.MaxRiskLevel, (int)group.MaxRiskLevel);
            }
            else if (appCategories.Contains(group.Category))
            {
                foreach (var item in group.Items)
                    appGroup.Items.Add(item);
                appGroup.MaxRiskLevel = (ItemRiskLevel)Math.Max((int)appGroup.MaxRiskLevel, (int)group.MaxRiskLevel);
            }
            else
            {
                foreach (var item in group.Items)
                    otherGroup.Items.Add(item);
                otherGroup.MaxRiskLevel = (ItemRiskLevel)Math.Max((int)otherGroup.MaxRiskLevel, (int)group.MaxRiskLevel);
            }
        }

        var result = new List<CleanCategoryGroup>();
        if (systemGroup.Items.Count > 0) result.Add(systemGroup);
        if (appGroup.Items.Count > 0) result.Add(appGroup);
        if (otherGroup.Items.Count > 0) result.Add(otherGroup);

        return result;
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanItem.IsSelected))
        {
            UpdateTotals();
            UpdateCommandStates();
        }
    }

    private void Group_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanCategoryGroup.IsAllSelected))
        {
            UpdateTotals();
            UpdateCommandStates();
        }
    }

    private void UpdateTotals()
    {
        TotalItems = Categories.Sum(g => g.ItemCount);
        SelectedItems = Categories.Sum(g => g.SelectedCount);
        TotalScannableSize = Categories.Sum(g => g.TotalSize);
        TotalSelectedSize = Categories.Sum(g => g.SelectedSize);
        TotalCleanedSize = Categories.Sum(g => g.CleanedSize);
    }

    private void UpdateCommandStates()
    {
        (CleanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        (ScanCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    public async Task CleanAsync()
    {
        if (IsCleaning || IsScanning) return;
        if (TotalSelectedSize == 0) return;

        _cleanCts = new CancellationTokenSource();
        IsCleaning = true;
        OverallProgress = 0;
        TotalCleanedSize = 0;

        try
        {
            LogInfo($"Bắt đầu dọn dẹp {SelectedItems} mục...");

            var result = await _cleaner.CleanAsync(Categories.ToList(),
                new Progress<string>(msg => ScanProgressText = msg),
                new Progress<LogEntry>(entry =>
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(entry);
                    });
                }),
                _cleanCts.Token);

            TotalCleanedSize = result.TotalCleanedSize;
            OverallProgress = 100;
            ScanProgressText = "Dọn dẹp hoàn tất";

            LogSuccess($"Hoàn tất! Đã dọn {result.CleanedItems}/{result.TotalItems} mục");
            LogSuccess($"Giải phóng: {FormatBytes(result.TotalCleanedSize)}");
            LogSuccess($"Thời gian: {result.Duration:mm\\:ss\\.ff}");

            if (result.FailedItems > 0)
                LogWarning($"Thất bại: {result.FailedItems} mục");

            UpdateTotals();
        }
        catch (OperationCanceledException)
        {
            LogWarning("Dọn dẹp đã bị hủy");
        }
        catch (Exception ex)
        {
            LogError($"Lỗi dọn dẹp: {ex.Message}");
        }
        finally
        {
            IsCleaning = false;
            _cleanCts = null;
            UpdateCommandStates();
        }
    }

    private void Cancel()
    {
        if (IsScanning)
        {
            _scanCts?.Cancel();
            LogWarning("Đang hủy quét...");
        }
        if (IsCleaning)
        {
            _cleanCts?.Cancel();
            LogWarning("Đang hủy dọn dẹp...");
        }
    }

    private void SelectAll()
    {
        foreach (var group in Categories)
        {
            group.IsAllSelected = true;
        }
    }

    private void DeselectAll()
    {
        foreach (var group in Categories)
        {
            group.IsAllSelected = false;
        }
    }

    private void SelectSafeItems()
    {
        foreach (var group in Categories)
        {
            foreach (var item in group.Items)
            {
                item.IsSelected = item.RiskLevel <= ItemRiskLevel.Low;
            }
            group.IsAllSelected = group.Items.All(i => i.IsSelected);
        }
    }

    private void ExpandAll()
    {
        foreach (var group in Categories)
        {
            group.IsExpanded = true;
        }
    }

    private void CollapseAll()
    {
        foreach (var group in Categories)
        {
            group.IsExpanded = false;
        }
    }

    private async Task ExportLogAsync()
    {
        try
        {
            string fileName = $"WinCleaner_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            var lines = LogEntries.Select(e => $"[{e.FormattedTime}] [{e.Level}] [{e.Source}] {e.Message}");
            await File.WriteAllLinesAsync(path, lines);

            LogSuccess($"Đã xuất log: {path}");
        }
        catch (Exception ex)
        {
            LogError($"Lỗi xuất log: {ex.Message}");
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

    // Helper Models for UI
    public class ProfileOption : ObservableObject
    {
        private CleanProfile _profile;
        private string _displayName = "";
        private string _description = "";

        public CleanProfile Profile { get => _profile; set => SetProperty(ref _profile, value); }
        public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
    }
}