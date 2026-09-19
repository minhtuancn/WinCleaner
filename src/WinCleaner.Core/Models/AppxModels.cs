using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum AppxPackageType
    {
        System,
        Microsoft,
        Office,
        Gaming,
        Social,
        Media,
        Developer,
        Education,
        Unknown
    }

    public enum AppxRiskLevel
    {
        Safe,
        Low,
        Medium,
        High,
        Critical
    }

    public enum AppxCleanableCategory
    {
        Cache,
        TemporaryFiles,
        Logs,
        Cookies,
        LocalStorage,
        IndexedDB,
        CrashReports,
        Telemetry,
        All
    }

    public class AppxCleanableItem : ObservableObject
    {
        private string _packageFullName = "";
        private string _packageName = "";
        private AppxCleanableCategory _category;
        private string _path = "";
        private string _pattern = "*.*";
        private bool _recurse = true;
        private long _estimatedSize = 0;
        private int _fileCount = 0;
        private bool _isSelected = false;
        private string _description = "";

        public string PackageFullName
        {
            get => _packageFullName;
            set => SetProperty(ref _packageFullName, value);
        }

        public string PackageName
        {
            get => _packageName;
            set => SetProperty(ref _packageName, value);
        }

        public AppxCleanableCategory Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string Pattern
        {
            get => _pattern;
            set => SetProperty(ref _pattern, value);
        }

        public bool Recurse
        {
            get => _recurse;
            set => SetProperty(ref _recurse, value);
        }

        public long EstimatedSize
        {
            get => _estimatedSize;
            set => SetProperty(ref _estimatedSize, value);
        }

        public int FileCount
        {
            get => _fileCount;
            set => SetProperty(ref _fileCount, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string EstimatedSizeFormatted => FormatBytes(EstimatedSize);

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

    public class AppxPackage : ObservableObject
    {
        private string _name = "";
        private string _fullName = "";
        private string _publisher = "";
        private string _version = "";
        private string _installLocation = "";
        private AppxPackageType _type = AppxPackageType.Unknown;
        private AppxRiskLevel _riskLevel = AppxRiskLevel.Medium;
        private long _size = 0;
        private bool _isFramework = false;
        private bool _isBundle = false;
        private bool _selected = false;
        private string _description = "";
        private string[] _dependencies = Array.Empty<string>();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Publisher
        {
            get => _publisher;
            set => SetProperty(ref _publisher, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string InstallLocation
        {
            get => _installLocation;
            set => SetProperty(ref _installLocation, value);
        }

        public AppxPackageType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public AppxRiskLevel RiskLevel
        {
            get => _riskLevel;
            set => SetProperty(ref _riskLevel, value);
        }

        public long Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public bool IsFramework
        {
            get => _isFramework;
            set => SetProperty(ref _isFramework, value);
        }

        public bool IsBundle
        {
            get => _isBundle;
            set => SetProperty(ref _isBundle, value);
        }

        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string[] Dependencies
        {
            get => _dependencies;
            set => SetProperty(ref _dependencies, value);
        }

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

    public class AppxDatabase : ObservableObject
    {
        private string _name = "";
        private string _version = "";
        private string _url = "";
        private string _localPath = "";
        private DateTime _lastUpdated = DateTime.MinValue;
        private bool _isEnabled = true;
        private List<AppxPackage> _packages = new();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public string LocalPath
        {
            get => _localPath;
            set => SetProperty(ref _localPath, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public List<AppxPackage> Packages
        {
            get => _packages;
            set => SetProperty(ref _packages, value);
        }

        public int PackageCount => _packages.Count;
        public int SelectedPackageCount => _packages.Count(p => p.Selected);
        public long TotalSelectedSize => _packages.Where(p => p.Selected).Sum(p => p.Size);
    }
}