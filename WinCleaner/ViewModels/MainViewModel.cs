using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly ISystemScanner _scanner;
        private readonly ICleanerService _cleaner;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<MainViewModel> _logger;
        
        private CancellationTokenSource? _scanCts;
        private CancellationTokenSource? _cleanCts;

        [ObservableProperty]
        private ObservableCollection<DriveInfoModel> _drives = new();

        [ObservableProperty]
        private ObservableCollection<UserProfileModel> _userProfiles = new();

        [ObservableProperty]
        private SystemInfoModel _systemInfo = new();

        [ObservableProperty]
        private ObservableCollection<CleanCategoryGroup> _cleanGroups = new();

        [ObservableProperty]
        private CleanProfile _selectedProfile = CleanProfile.Safe;

        [ObservableProperty]
        private bool _dryRunMode = false;

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

        public ICommand ScanCommand { get; }
        public ICommand CleanCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand SelectSafeCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ExportLogCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand RefreshDrivesCommand { get; }
        public ICommand ProfileChangedCommand { get; }

        public MainViewModel(
            ISystemScanner scanner,
            ICleanerService cleaner,
            ISettingsService settingsService,
            ILogger<MainViewModel> logger)
        {
            _scanner = scanner;
            _cleaner = cleaner;
            _settingsService = settingsService;
            _logger = logger;

            ScanCommand = new AsyncRelayCommand(ScanAsync, () => !IsScanning && !IsCleaning);
            CleanCommand = new AsyncRelayCommand(CleanAsync, () => !IsScanning && !IsCleaning && TotalSelectedSize > 0);
            CancelCommand = new RelayCommand(Cancel);
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            SelectSafeCommand = new RelayCommand(SelectSafeItems);
            ClearLogCommand = new RelayCommand(() => LogEntries.Clear());
            ExportLogCommand = new AsyncRelayCommand(ExportLogAsync);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            RefreshDrivesCommand = new AsyncRelayCommand(LoadDrivesAsync);
            ProfileChangedCommand = new RelayCommand<CleanProfile>(OnProfileChanged);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var settings = await _settingsService.LoadAsync();
                SelectedProfile = settings.SelectedProfile;
                DryRunMode = settings.DryRunMode;
                _cleaner.DryRunMode = DryRunMode;

                LogInfo("Khởi tạo WinCleaner...");
                await LoadSystemInfoAsync();
                await LoadDrivesAsync();
                await LoadUserProfilesAsync();
                await ScanAsync();
            }
            catch (Exception ex)
            {
                LogError($"Lỗi khởi tạo: {ex.Message}");
            }
        }

        private async Task LoadSystemInfoAsync()
        {
            try
            {
                SystemInfo = await _scanner.GetSystemInfoAsync();
                LogInfo($"Hệ điều hành: {SystemInfo.OSVersion} ({SystemInfo.OSArchitecture})");
                LogInfo($"User: {SystemInfo.CurrentUser} | Admin: {SystemInfo.IsAdmin}");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi tải thông tin hệ thống: {ex.Message}");
            }
        }

        private async Task LoadDrivesAsync()
        {
            try
            {
                var drives = await _scanner.GetDrivesAsync();
                Drives.Clear();
                foreach (var d in drives)
                    Drives.Add(d);
                LogInfo($"Đã tìm thấy {drives.Count} ổ đĩa");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi tải ổ đĩa: {ex.Message}");
            }
        }

        private async Task LoadUserProfilesAsync()
        {
            try
            {
                var profiles = await _scanner.GetUserProfilesAsync();
                UserProfiles.Clear();
                foreach (var p in profiles)
                    UserProfiles.Add(p);
                LogInfo($"Đã tìm thấy {profiles.Count} hồ sơ người dùng");
            }
            catch (Exception ex)
            {
                LogError($"Lỗi tải hồ sơ người dùng: {ex.Message}");
            }
        }

        private void OnProfileChanged(CleanProfile profile)
        {
            SelectedProfile = profile;
            _ = ScanAsync();
        }

        partial void OnDryRunModeChanged(bool value)
        {
            _cleaner.DryRunMode = value;
            _ = _settingsService.SaveAsync(new AppSettings 
            { 
                SelectedProfile = SelectedProfile, 
                DryRunMode = value 
            });
        }

        partial void OnSelectedProfileChanged(CleanProfile value)
        {
            _ = _settingsService.SaveAsync(new AppSettings 
            { 
                SelectedProfile = value, 
                DryRunMode = DryRunMode 
            });
        }

        public async Task ScanAsync()
        {
            if (IsScanning || IsCleaning) return;

            _scanCts = new CancellationTokenSource();
            IsScanning = true;
            IsProgressIndeterminate = true;
            ScanProgressText = "Đang quét...";
            OverallProgress = 0;
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

                CleanGroups.Clear();
                foreach (var g in groups)
                {
                    g.PropertyChanged += Group_PropertyChanged;
                    foreach (var item in g.Items)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                    CleanGroups.Add(g);
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
                IsProgressIndeterminate = false;
                _scanCts = null;
                UpdateCommandStates();
            }
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
            TotalItems = CleanGroups.Sum(g => g.ItemCount);
            SelectedItems = CleanGroups.Sum(g => g.SelectedCount);
            TotalScannableSize = CleanGroups.Sum(g => g.TotalSize);
            TotalSelectedSize = CleanGroups.Sum(g => g.SelectedSize);
            TotalCleanedSize = CleanGroups.Sum(g => g.CleanedSize);
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

            if (DryRunMode == false)
            {
                var result = MessageBox.Show(
                    $"Sắp dọn dẹp {SelectedItems} mục, giải phóng {FormatBytes(TotalSelectedSize)}.\n\nTiếp tục?",
                    "Xác nhận dọn dẹp",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes) return;
            }

            _cleanCts = new CancellationTokenSource();
            IsCleaning = true;
            IsProgressIndeterminate = false;
            OverallProgress = 0;
            TotalCleanedSize = 0;

            try
            {
                LogInfo($"Bắt đầu dọn dẹp {SelectedItems} mục...");
                
                var result = await _cleaner.CleanAsync(CleanGroups.ToList(),
                    new Progress<string>(msg => ScanProgressText = msg),
                    new Progress<LogEntry>(entry => 
                    {
                        Application.Current?.Dispatcher.Invoke(() => 
                        {
                            LogEntries.Add(entry);
                            if (AutoScrollLog && LogEntries.Count > 0)
                            {
                                // Auto-scroll handled in UI
                            }
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
                IsProgressIndeterminate = false;
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
            foreach (var group in CleanGroups)
            {
                group.IsAllSelected = true;
            }
        }

        private void DeselectAll()
        {
            foreach (var group in CleanGroups)
            {
                group.IsAllSelected = false;
            }
        }

        private void SelectSafeItems()
        {
            foreach (var group in CleanGroups)
            {
                foreach (var item in group.Items)
                {
                    item.IsSelected = item.RiskLevel <= ItemRiskLevel.Low;
                }
                group.IsAllSelected = group.Items.All(i => i.IsSelected);
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

        private void OpenSettings()
        {
            LogInfo("Mở cài đặt...");
            // TODO: Open settings window
        }

        public void AddLog(LogEntry entry)
        {
            Application.Current?.Dispatcher.Invoke(() => 
            {
                LogEntries.Add(entry);
                if (LogEntries.Count > 10000)
                    LogEntries.RemoveAt(0);
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