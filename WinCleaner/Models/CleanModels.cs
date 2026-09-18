using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum CleanCategory
    {
        SystemTemp,
        WindowsUpdate,
        BrowserCache,
        DevToolsCache,
        UserTemp,
        RecycleBin,
        SystemLogs,
        DriverStore,
        DiscordOldVersions,
        PlaywrightBrowsers,
        MinecraftTemp,
        UpdaterCaches,
        GameData,
        UserPrograms,
        OtherUsers,
        CompactOS,
        Hibernation,
        SystemRestore,
        WindowsOld
    }

    public enum CleanProfile
    {
        Safe,
        Deep,
        Custom,
        Nuclear
    }

    public enum CleanStatus
    {
        Pending,
        Scanning,
        Scanned,
        Cleaning,
        Cleaned,
        Failed,
        Skipped
    }

    public enum ItemRiskLevel
    {
        Safe,
        Low,
        Medium,
        High,
        Critical
    }

    public class CleanItem : ObservableObject
    {
        private CleanStatus _status = CleanStatus.Pending;
        private bool _isSelected = true;
        private long _sizeBytes;
        private long _cleanedBytes;
        private string _details = "";

        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Path { get; set; } = "";
        public CleanCategory Category { get; set; }
        public ItemRiskLevel RiskLevel { get; set; } = ItemRiskLevel.Safe;
        public long SizeBytes 
        { 
            get => _sizeBytes; 
            set => SetProperty(ref _sizeBytes, value); 
        }
        public long CleanedBytes
        {
            get => _cleanedBytes;
            set => SetProperty(ref _cleanedBytes, value);
        }
        public CleanStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public string Details
        {
            get => _details;
            set => SetProperty(ref _details, value);
        }
        public bool RequiresAdmin { get; set; } = false;
        public bool IsSystemItem { get; set; } = false;
        public string[]? SupportedProfiles { get; set; }
        public Func<CleanItem, IProgress<string>, Task>? CleanAction { get; set; }
        public Func<CleanItem, long>? SizeCalculator { get; set; }

        public string SizeFormatted => FormatBytes(SizeBytes);
        public string CleanedFormatted => FormatBytes(CleanedBytes);
        public double ProgressPercent => SizeBytes > 0 ? (double)CleanedBytes / SizeBytes * 100 : 0;

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

    public class CleanCategoryGroup : ObservableObject
    {
        private bool _isExpanded = true;
        private bool _isAllSelected = true;
        private CleanStatus _groupStatus = CleanStatus.Pending;

        public CleanCategory Category { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public ObservableCollection<CleanItem> Items { get; set; } = new();
        public ItemRiskLevel MaxRiskLevel { get; set; } = ItemRiskLevel.Safe;
        
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (SetProperty(ref _isAllSelected, value))
                {
                    foreach (var item in Items)
                        item.IsSelected = value;
                    OnPropertyChanged(nameof(TotalSize));
                    OnPropertyChanged(nameof(SelectedSize));
                    OnPropertyChanged(nameof(ItemCount));
                    OnPropertyChanged(nameof(SelectedCount));
                }
            }
        }

        public CleanStatus GroupStatus
        {
            get => _groupStatus;
            set => SetProperty(ref _groupStatus, value);
        }

        public long TotalSize => Items.Sum(i => i.SizeBytes);
        public long SelectedSize => Items.Where(i => i.IsSelected).Sum(i => i.SizeBytes);
        public long CleanedSize => Items.Sum(i => i.CleanedBytes);
        public int ItemCount => Items.Count;
        public int SelectedCount => Items.Count(i => i.IsSelected);
        public int CleanedCount => Items.Count(i => i.Status == CleanStatus.Cleaned);
        public int FailedCount => Items.Count(i => i.Status == CleanStatus.Failed);
    }

    public class DriveInfoModel : ObservableObject
    {
        private string _name = "";
        private string _label = "";
        private long _totalSize;
        private long _freeSpace;
        private bool _isSystemDrive;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Label { get => _label; set => SetProperty(ref _label, value); }
        public long TotalSize { get => _totalSize; set => SetProperty(ref _totalSize, value); }
        public long FreeSpace { get => _freeSpace; set => SetProperty(ref _freeSpace, value); }
        public long UsedSpace => TotalSize - FreeSpace;
        public double UsedPercent => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
        public bool IsSystemDrive { get => _isSystemDrive; set => SetProperty(ref _isSystemDrive, value); }
        public string DriveType { get; set; } = "";
        public string FileSystem { get; set; } = "";
        public bool IsReady { get; set; } = true;

        public string TotalSizeFormatted => FormatBytes(TotalSize);
        public string FreeSpaceFormatted => FormatBytes(FreeSpace);
        public string UsedSpaceFormatted => FormatBytes(UsedSpace);

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

    public class UserProfileModel : ObservableObject
    {
        private string _name = "";
        private string _sid = "";
        private string _path = "";
        private long _size;
        private bool _isCurrent;
        private bool _isActive;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string SID { get => _sid; set => SetProperty(ref _sid, value); }
        public string Path { get => _path; set => SetProperty(ref _path, value); }
        public long Size { get => _size; set => SetProperty(ref _size, value); }
        public bool IsCurrent { get => _isCurrent; set => SetProperty(ref _isCurrent, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
        public bool CanClean { get; set; } = true;

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

    public class SystemInfoModel : ObservableObject
    {
        public string OSVersion { get; set; } = "";
        public string OSBuild { get; set; } = "";
        public string OSArchitecture { get; set; } = "";
        public string WindowsDirectory { get; set; } = "";
        public string SystemDirectory { get; set; } = "";
        public string CurrentUser { get; set; } = "";
        public bool IsAdmin { get; set; }
        public string DotNetVersion { get; set; } = "";
        public long TotalPhysicalMemory { get; set; }
        public int ProcessorCount { get; set; }
    }

    public class LogEntry : ObservableObject
    {
        private DateTime _timestamp = DateTime.Now;
        private LogLevel _level = LogLevel.Info;
        private string _message = "";
        private string _source = "";

        public DateTime Timestamp { get => _timestamp; set => SetProperty(ref _timestamp, value); }
        public LogLevel Level { get => _level; set => SetProperty(ref _level, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public string Source { get => _source; set => SetProperty(ref _source, value); }
        public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success
    }

    public class CleanResult
    {
        public long TotalScannedSize { get; set; }
        public long TotalCleanedSize { get; set; }
        public int TotalItems { get; set; }
        public int CleanedItems { get; set; }
        public int FailedItems { get; set; }
        public int SkippedItems { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<CleanCategory, long> CleanedByCategory { get; set; } = new();
    }

    public class AppSettings : ObservableObject
    {
        private CleanProfile _selectedProfile = CleanProfile.Safe;
        private bool _dryRunMode = false;
        private bool _autoCloseAfterClean = false;
        private bool _minimizeToTray = true;
        private bool _showHiddenFiles = false;
        private bool _confirmBeforeDelete = true;
        private int _maxLogEntries = 10000;
        private string _theme = "System";
        private string _language = "vi-VN";
        private List<string> _excludedPaths = new();
        private List<string> _customPaths = new();

        public CleanProfile SelectedProfile
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value);
        }
        public bool DryRunMode
        {
            get => _dryRunMode;
            set => SetProperty(ref _dryRunMode, value);
        }
        public bool AutoCloseAfterClean
        {
            get => _autoCloseAfterClean;
            set => SetProperty(ref _autoCloseAfterClean, value);
        }
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetProperty(ref _minimizeToTray, value);
        }
        public bool ShowHiddenFiles
        {
            get => _showHiddenFiles;
            set => SetProperty(ref _showHiddenFiles, value);
        }
        public bool ConfirmBeforeDelete
        {
            get => _confirmBeforeDelete;
            set => SetProperty(ref _confirmBeforeDelete, value);
        }
        public int MaxLogEntries
        {
            get => _maxLogEntries;
            set => SetProperty(ref _maxLogEntries, value);
        }
        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }
        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }
        public List<string> ExcludedPaths
        {
            get => _excludedPaths;
            set => SetProperty(ref _excludedPaths, value);
        }
        public List<string> CustomPaths
        {
            get => _customPaths;
            set => SetProperty(ref _customPaths, value);
        }
    }
}

namespace WinCleaner.Models
{
    public class BytesToStringConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is long bytes)
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
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intVal)
            {
                string? param = parameter as string;
                if (param == "gt0")
                    return intVal > 0 ? Visibility.Visible : Visibility.Collapsed;
                if (param == "eq0")
                    return intVal == 0 ? Visibility.Visible : Visibility.Collapsed;
                return intVal > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RiskLevelToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ItemRiskLevel risk)
            {
                return risk switch
                {
                    ItemRiskLevel.Safe => Application.Current.FindResource("RiskSafeBrush"),
                    ItemRiskLevel.Low => Application.Current.FindResource("RiskLowBrush"),
                    ItemRiskLevel.Medium => Application.Current.FindResource("RiskMediumBrush"),
                    ItemRiskLevel.High => Application.Current.FindResource("RiskHighBrush"),
                    ItemRiskLevel.Critical => Application.Current.FindResource("RiskCriticalBrush"),
                    _ => Application.Current.FindResource("TextPrimaryBrush")
                };
            }
            return Application.Current.FindResource("TextPrimaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CleanStatusToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CleanStatus status)
            {
                return status switch
                {
                    CleanStatus.Cleaned => Application.Current.FindResource("SuccessBrush"),
                    CleanStatus.Cleaning => Application.Current.FindResource("PrimaryBrush"),
                    CleanStatus.Failed => Application.Current.FindResource("ErrorBrush"),
                    CleanStatus.Skipped => Application.Current.FindResource("WarningBrush"),
                    CleanStatus.Scanned => Application.Current.FindResource("TextSecondaryBrush"),
                    _ => Application.Current.FindResource("TextSecondaryBrush")
                };
            }
            return Application.Current.FindResource("TextSecondaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CleanProfileToStringConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CleanProfile profile)
            {
                return profile switch
                {
                    CleanProfile.Safe => "An toàn (Khuyến nghị)",
                    CleanProfile.Deep => "Sâu (Nâng cao)",
                    CleanProfile.Custom => "Tùy chỉnh",
                    CleanProfile.Nuclear => "Cực đại (Chuyên gia)",
                    _ => profile.ToString()
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToCollectionConverter : System.Windows.Data.IValueConverter
    {
        public static readonly EnumToCollectionConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return Enum.GetValues(enumValue.GetType());
            }
            if (value is Type type && type.IsEnum)
            {
                return Enum.GetValues(type);
            }
            return Array.Empty<object>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RiskLevelToTooltipConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ItemRiskLevel risk)
            {
                return risk switch
                {
                    ItemRiskLevel.Safe => "An toàn - Cache tạm, log, file rác hệ thống",
                    ItemRiskLevel.Low => "Thấp - Cache trình duyệt, dev tools",
                    ItemRiskLevel.Medium => "Trung bình - Dữ liệu game, app user",
                    ItemRiskLevel.High => "Cao - Driver, app quan trọng, user khác",
                    ItemRiskLevel.Critical => "Nguy hiểm - Hệ thống, compact OS, hibernation",
                    _ => ""
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CleanStatusToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CleanStatus status)
            {
                return status == CleanStatus.Cleaning ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LogLevelToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Debug => Application.Current.FindResource("TextDisabledBrush"),
                    LogLevel.Info => Application.Current.FindResource("TextPrimaryBrush"),
                    LogLevel.Warning => Application.Current.FindResource("WarningBrush"),
                    LogLevel.Error => Application.Current.FindResource("ErrorBrush"),
                    LogLevel.Success => Application.Current.FindResource("SuccessBrush"),
                    _ => Application.Current.FindResource("TextPrimaryBrush")
                };
            }
            return Application.Current.FindResource("TextPrimaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LogLevelToBackgroundConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Debug => Application.Current.FindResource("BackgroundBrush"),
                    LogLevel.Info => Application.Current.FindResource("SurfaceBrush"),
                    LogLevel.Warning => new SolidColorBrush(Color.FromArgb(30, 255, 140, 0)),
                    LogLevel.Error => new SolidColorBrush(Color.FromArgb(30, 209, 52, 56)),
                    LogLevel.Success => new SolidColorBrush(Color.FromArgb(30, 16, 124, 16)),
                    _ => Application.Current.FindResource("SurfaceBrush")
                };
            }
            return Application.Current.FindResource("SurfaceBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToAdminTextConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isAdmin)
                return isAdmin ? "Administrator" : "Standard User";
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DriveUsageToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double percent)
            {
                if (percent >= 90) return Application.Current.FindResource("ErrorBrush");
                if (percent >= 75) return Application.Current.FindResource("WarningBrush");
                return Application.Current.FindResource("SuccessBrush");
            }
            return Application.Current.FindResource("PrimaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}