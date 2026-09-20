using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Core.Services;
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
        private readonly IResourceService _resourceService;
        private readonly DispatcherTimer _dateTimeTimer;
        
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

        [ObservableProperty]
        private ObservableCollection<InfoCardModel> _systemInfoCards = new();

        [ObservableProperty]
        private ObservableCollection<ProfileOption> _availableProfiles = new();

        [ObservableProperty]
        private ObservableCollection<CustomFolderModel> _customFolders = new();

        [ObservableProperty]
        private ObservableCollection<SavedProfileModel> _savedProfiles = new();

        [ObservableProperty]
        private SavedProfileModel? _selectedSavedProfile;

        [ObservableProperty]
        private CleanItem? _selectedItem;

        [ObservableProperty]
        private string _currentDateTime = "";

        public bool HasSelectedItem => SelectedItem != null;

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
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }
        public ICommand AddCustomFolderCommand { get; }
        public ICommand RemoveCustomFolderCommand { get; }
        public ICommand SaveCurrentProfileCommand { get; }
        public ICommand LoadSavedProfileCommand { get; }

        public MainViewModel(
            ISystemScanner scanner,
            ICleanerService cleaner,
            ISettingsService settingsService,
            ILogger<MainViewModel> logger,
            IResourceService resourceService)
        {
            _scanner = scanner;
            _cleaner = cleaner;
            _settingsService = settingsService;
            _logger = logger;
            _resourceService = resourceService;

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
            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
            AddCustomFolderCommand = new RelayCommand(AddCustomFolder);
            RemoveCustomFolderCommand = new RelayCommand<CustomFolderModel>(RemoveCustomFolder);
            SaveCurrentProfileCommand = new RelayCommand(SaveCurrentProfile);
            LoadSavedProfileCommand = new RelayCommand<SavedProfileModel>(LoadSavedProfile);

            _dateTimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _dateTimeTimer.Tick += (s, e) => CurrentDateTime = DateTime.Now.ToString("F", System.Globalization.CultureInfo.CurrentUICulture);
            _dateTimeTimer.Start();
            CurrentDateTime = DateTime.Now.ToString("F", System.Globalization.CultureInfo.CurrentUICulture);

            InitializeProfiles();
            LoadCustomFolders();
            LoadSavedProfiles();
            LoadSystemInfoCards();

            _ = InitializeAsync();
        }

        private void InitializeProfiles()
        {
            AvailableProfiles = new ObservableCollection<ProfileOption>
            {
                new ProfileOption { Profile = CleanProfile.Safe, DisplayName = _resourceService.GetString("Main.ProfileSafe"), Description = _resourceService.GetString("Main.ProfileSafeDesc") },
                new ProfileOption { Profile = CleanProfile.Deep, DisplayName = _resourceService.GetString("Main.ProfileDeep"), Description = _resourceService.GetString("Main.ProfileDeepDesc") },
                new ProfileOption { Profile = CleanProfile.Custom, DisplayName = _resourceService.GetString("Main.ProfileCustom"), Description = _resourceService.GetString("Main.ProfileCustomDesc") },
                new ProfileOption { Profile = CleanProfile.Nuclear, DisplayName = _resourceService.GetString("Main.ProfileNuclear"), Description = _resourceService.GetString("Main.ProfileNuclearDesc") }
            };
        }

        private void LoadSystemInfoCards()
        {
            SystemInfoCards = new ObservableCollection<InfoCardModel>
            {
                new InfoCardModel { Icon = "🖥", Title = "OS", Value = SystemInfo.OSVersion },
                new InfoCardModel { Icon = "💻", Title = "Arch", Value = SystemInfo.OSArchitecture },
                new InfoCardModel { Icon = "🧠", Title = "RAM", Value = FormatBytes(SystemInfo.TotalPhysicalMemory) },
                new InfoCardModel { Icon = "⚙", Title = "CPU", Value = $"{SystemInfo.ProcessorCount} cores" },
                new InfoCardModel { Icon = "👤", Title = "User", Value = SystemInfo.CurrentUser },
                new InfoCardModel { Icon = "🔐", Title = _resourceService.GetString("Common.Permission"), Value = SystemInfo.IsAdmin ? _resourceService.GetString("Main.PermissionAdmin") : _resourceService.GetString("Main.PermissionUser") }
            };
        }

        partial void OnSystemInfoChanged(SystemInfoModel value)
        {
            LoadSystemInfoCards();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var settings = await _settingsService.LoadAsync();
                SelectedProfile = settings.SelectedProfile;
                DryRunMode = settings.DryRunMode;
                _cleaner.DryRunMode = DryRunMode;

                // Load custom folders from settings
                if (settings.CustomPaths != null)
                {
                    foreach (var path in settings.CustomPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            CustomFolders.Add(new CustomFolderModel { Path = path });
                        }
                    }
                }

                LogInfo(_resourceService.GetString("Main.Initializing"));
                await LoadSystemInfoAsync();
                await LoadDrivesAsync();
                await LoadUserProfilesAsync();
                await ScanAsync();
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.InitError", ex.Message));
            }
        }

        private async Task LoadSystemInfoAsync()
        {
            try
            {
                SystemInfo = await _scanner.GetSystemInfoAsync();
                LogInfo(_resourceService.GetString("Main.OSInfo", SystemInfo.OSVersion, SystemInfo.OSArchitecture));
                LogInfo(_resourceService.GetString("Main.UserInfo", SystemInfo.CurrentUser, SystemInfo.IsAdmin ? _resourceService.GetString("Main.PermissionAdmin") : _resourceService.GetString("Main.PermissionUser")));
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.SystemInfoError", ex.Message));
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
                LogInfo(_resourceService.GetString("Main.DrivesFound", drives.Count));
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.DrivesError", ex.Message));
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
                LogInfo(_resourceService.GetString("Main.UserProfilesFound", profiles.Count));
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.UserProfilesError", ex.Message));
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
            _ = SaveSettingsAsync();
        }

        partial void OnSelectedProfileChanged(CleanProfile value)
        {
            _ = SaveSettingsAsync();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var settings = new AppSettings
                {
                    SelectedProfile = SelectedProfile,
                    DryRunMode = DryRunMode,
                    CustomPaths = CustomFolders.Select(f => f.Path).ToList()
                };
                await _settingsService.SaveAsync(settings);
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.SaveSettingsError", ex.Message));
            }
        }

        public async Task ScanAsync()
        {
            if (IsScanning || IsCleaning) return;

            _scanCts = new CancellationTokenSource();
            IsScanning = true;
            IsProgressIndeterminate = true;
            ScanProgressText = _resourceService.GetString("Main.Scanning");
            OverallProgress = 0;
            LogEntries.Clear();

            try
            {
                LogInfo(_resourceService.GetString("Main.ScanStarted", SelectedProfile));

                var groups = await _scanner.ScanAsync(SelectedProfile,
                    new Progress<string>(msg =>
                    {
                        ScanProgressText = msg;
                        LogDebug(msg);
                    }),
                    _scanCts.Token);

                // Organize groups into System and Applications
                var organizedGroups = OrganizeGroups(groups);
                
                CleanGroups.Clear();
                foreach (var g in organizedGroups)
                {
                    g.PropertyChanged += Group_PropertyChanged;
                    foreach (var item in g.Items)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                    CleanGroups.Add(g);
                }

                UpdateTotals();
                LogSuccess(_resourceService.GetString("Main.ScanCompleted", TotalItems, FormatBytes(TotalScannableSize)));
                ScanProgressText = _resourceService.GetString("Main.ScanCompletedShort");
            }
            catch (OperationCanceledException)
            {
                LogWarning(_resourceService.GetString("Main.ScanCancelled"));
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.ScanError", ex.Message));
            }
            finally
            {
                IsScanning = false;
                IsProgressIndeterminate = false;
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

        partial void OnSelectedItemChanged(CleanItem? value)
        {
            OnPropertyChanged(nameof(HasSelectedItem));
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
                    _resourceService.GetString("Main.ConfirmCleanMessage", SelectedItems, FormatBytes(TotalSelectedSize)),
                    _resourceService.GetString("Main.ConfirmCleanTitle"),
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
                LogInfo(_resourceService.GetString("Main.CleanStarted", SelectedItems));

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
                ScanProgressText = _resourceService.GetString("Main.CleanCompletedShort");

                LogSuccess(_resourceService.GetString("Main.CleanCompleted", result.CleanedItems, result.TotalItems));
                LogSuccess(_resourceService.GetString("Main.CleanFreed", FormatBytes(result.TotalCleanedSize)));
                LogSuccess(_resourceService.GetString("Main.CleanDuration", result.Duration.ToString(@"mm\:ss\.ff")));

                if (result.FailedItems > 0)
                    LogWarning(_resourceService.GetString("Main.CleanFailedItems", result.FailedItems));

                UpdateTotals();
            }
            catch (OperationCanceledException)
            {
                LogWarning(_resourceService.GetString("Main.CleanCancelled"));
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.CleanError", ex.Message));
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
                LogWarning(_resourceService.GetString("Main.CancellingScan"));
            }
            if (IsCleaning)
            {
                _cleanCts?.Cancel();
                LogWarning(_resourceService.GetString("Main.CancellingClean"));
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

        private void ExpandAll()
        {
            foreach (var group in CleanGroups)
            {
                group.IsExpanded = true;
            }
        }

        private void CollapseAll()
        {
            foreach (var group in CleanGroups)
            {
                group.IsExpanded = false;
            }
        }

        private void AddCustomFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = _resourceService.GetString("Main.SelectFolderTitle"),
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var folder = new CustomFolderModel { Path = dialog.FolderName };
                CustomFolders.Add(folder);
                _ = SaveSettingsAsync();
                LogInfo(_resourceService.GetString("Main.FolderAdded", dialog.FolderName));
            }
        }

        private void RemoveCustomFolder(CustomFolderModel? folder)
        {
            if (folder != null)
            {
                CustomFolders.Remove(folder);
                _ = SaveSettingsAsync();
                LogInfo(_resourceService.GetString("Main.FolderRemoved", folder.Path));
            }
        }

        private void LoadCustomFolders()
        {
            CustomFolders = new ObservableCollection<CustomFolderModel>();
        }

        private void LoadSavedProfiles()
        {
            SavedProfiles = new ObservableCollection<SavedProfileModel>
            {
                new SavedProfileModel { Name = _resourceService.GetString("Main.ProfileSafe"), Profile = CleanProfile.Safe, CreatedDate = DateTime.Now },
                new SavedProfileModel { Name = _resourceService.GetString("Main.ProfileDeep"), Profile = CleanProfile.Deep, CreatedDate = DateTime.Now }
            };
        }

        private void SaveCurrentProfile()
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox(_resourceService.GetString("Main.SaveProfileTitle"), _resourceService.GetString("Main.SaveProfilePrompt"), _resourceService.GetString("Main.SaveProfileDefault", DateTime.Now));
            if (!string.IsNullOrWhiteSpace(name))
            {
                var profile = new SavedProfileModel
                {
                    Name = name,
                    Profile = SelectedProfile,
                    CreatedDate = DateTime.Now,
                    CustomFolders = CustomFolders.Select(f => f.Path).ToList()
                };
                SavedProfiles.Add(profile);
                LogInfo(_resourceService.GetString("Main.ProfileSaved", name));
            }
        }

        private void LoadSavedProfile(SavedProfileModel? profile)
        {
            if (profile != null)
            {
                SelectedProfile = profile.Profile;
                CustomFolders.Clear();
                foreach (var path in profile.CustomFolders)
                {
                    if (Directory.Exists(path))
                        CustomFolders.Add(new CustomFolderModel { Path = path });
                }
                _ = SaveSettingsAsync();
                _ = ScanAsync();
                LogInfo(_resourceService.GetString("Main.ProfileLoaded", profile.Name));
            }
        }

        partial void OnSelectedSavedProfileChanged(SavedProfileModel? value)
        {
            if (value != null)
                LoadSavedProfile(value);
        }

        private async Task ExportLogAsync()
        {
            try
            {
                string fileName = $"WinCleaner_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var lines = LogEntries.Select(e => $"[{e.FormattedTime}] [{e.Level}] [{e.Source}] {e.Message}");
                await File.WriteAllLinesAsync(path, lines);

                LogSuccess(_resourceService.GetString("Main.LogExported", path));
            }
            catch (Exception ex)
            {
                LogError(_resourceService.GetString("Main.LogExportError", ex.Message));
            }
        }

        private void OpenSettings()
        {
            LogInfo(_resourceService.GetString("Main.OpeningSettings"));
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

        private new void LogDebug(string message) => AddLog(new LogEntry { Level = Models.LogLevel.Debug, Message = message, Source = "Scanner" });
        private new void LogInfo(string message) => AddLog(new LogEntry { Level = Models.LogLevel.Info, Message = message, Source = "System" });
        private new void LogSuccess(string message) => AddLog(new LogEntry { Level = Models.LogLevel.Success, Message = message, Source = "Cleaner" });
        private new void LogWarning(string message) => AddLog(new LogEntry { Level = Models.LogLevel.Warning, Message = message, Source = "System" });
        private new void LogError(string message) => AddLog(new LogEntry { Level = Models.LogLevel.Error, Message = message, Source = "Error" });

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

    // Helper Models for UI
    public class InfoCardModel : ObservableObject
    {
        private string _icon = "";
        private string _title = "";
        private string _value = "";

        public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Value { get => _value; set => SetProperty(ref _value, value); }
    }

    public class ProfileOption : ObservableObject
    {
        private CleanProfile _profile;
        private string _displayName = "";
        private string _description = "";

        public CleanProfile Profile { get => _profile; set => SetProperty(ref _profile, value); }
        public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
    }

    public class CustomFolderModel : ObservableObject
    {
        private string _path = "";
        private long _size;

        public string Path { get => _path; set => SetProperty(ref _path, value); }
        public long Size { get => _size; set => SetProperty(ref _size, value); }
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

    public class SavedProfileModel : ObservableObject
    {
        private string _name = "";
        private CleanProfile _profile;
        private DateTime _createdDate;
        private List<string> _customFolders = new();

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public CleanProfile Profile { get => _profile; set => SetProperty(ref _profile, value); }
        public DateTime CreatedDate { get => _createdDate; set => SetProperty(ref _createdDate, value); }
        public List<string> CustomFolders { get => _customFolders; set => SetProperty(ref _customFolders, value); }
    }
}