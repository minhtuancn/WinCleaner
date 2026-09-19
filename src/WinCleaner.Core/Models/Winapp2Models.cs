using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum Winapp2EntryType
    {
        File,
        Registry
    }

    public enum DetectType
    {
        Registry,
        File,
        OS
    }

    public class Winapp2Entry : ObservableObject
    {
        private string _name = "";
        private string _section = "";
        private string _detect = "";
        private string _detectFile = "";
        private string _detectOS = "";
        private string _warning = "";
        private bool _default = true;
        private int _rebootOk = 0;
        private List<Winapp2FileKey> _fileKeys = new();
        private List<Winapp2RegKey> _regKeys = new();
        private List<Winapp2ExcludeKey> _excludeKeys = new();
        private string _sourceDatabase = "";
        private bool _isEnabled = true;
        private long _estimatedSize = 0;
        private int _matchedFilesCount = 0;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Section
        {
            get => _section;
            set => SetProperty(ref _section, value);
        }

        public string Detect
        {
            get => _detect;
            set => SetProperty(ref _detect, value);
        }

        public string DetectFile
        {
            get => _detectFile;
            set => SetProperty(ref _detectFile, value);
        }

        public string DetectOS
        {
            get => _detectOS;
            set => SetProperty(ref _detectOS, value);
        }

        public string Warning
        {
            get => _warning;
            set => SetProperty(ref _warning, value);
        }

        public bool Default
        {
            get => _default;
            set => SetProperty(ref _default, value);
        }

        public int RebootOk
        {
            get => _rebootOk;
            set => SetProperty(ref _rebootOk, value);
        }

        public List<Winapp2FileKey> FileKeys
        {
            get => _fileKeys;
            set => SetProperty(ref _fileKeys, value);
        }

        public List<Winapp2RegKey> RegKeys
        {
            get => _regKeys;
            set => SetProperty(ref _regKeys, value);
        }

        public List<Winapp2ExcludeKey> ExcludeKeys
        {
            get => _excludeKeys;
            set => SetProperty(ref _excludeKeys, value);
        }

        public string SourceDatabase
        {
            get => _sourceDatabase;
            set => SetProperty(ref _sourceDatabase, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public long EstimatedSize
        {
            get => _estimatedSize;
            set => SetProperty(ref _estimatedSize, value);
        }

        public int MatchedFilesCount
        {
            get => _matchedFilesCount;
            set => SetProperty(ref _matchedFilesCount, value);
        }

        public string EstimatedSizeFormatted => FormatBytes(EstimatedSize);

        public string RawText { get; set; } = "";

        public bool HasDetection => !string.IsNullOrEmpty(Detect) || !string.IsNullOrEmpty(DetectFile) || !string.IsNullOrEmpty(DetectOS);

        public ItemRiskLevel RiskLevel
        {
            get
            {
                if (!string.IsNullOrEmpty(Warning) && Warning.Contains("registry", StringComparison.OrdinalIgnoreCase))
                    return ItemRiskLevel.High;
                if (RegKeys.Count > 0)
                    return ItemRiskLevel.Medium;
                return ItemRiskLevel.Safe;
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

    public class Winapp2FileKey : ObservableObject
    {
        private string _path = "";
        private string _pattern = "*.*";
        private bool _recurse = false;
        private string _exclude = "";
        private long _calculatedSize = 0;
        private int _filesFound = 0;

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

        public string Exclude
        {
            get => _exclude;
            set => SetProperty(ref _exclude, value);
        }

        public long CalculatedSize
        {
            get => _calculatedSize;
            set => SetProperty(ref _calculatedSize, value);
        }

        public int FilesFound
        {
            get => _filesFound;
            set => SetProperty(ref _filesFound, value);
        }

        public string CalculatedSizeFormatted => FormatBytes(CalculatedSize);

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

    public class Winapp2RegKey : ObservableObject
    {
        private string _key = "";
        private string _action = "DeleteKey";
        private string _valueName = "";
        private bool _recurse = false;

        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public string Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }

        public string ValueName
        {
            get => _valueName;
            set => SetProperty(ref _valueName, value);
        }

        public bool Recurse
        {
            get => _recurse;
            set => SetProperty(ref _recurse, value);
        }
    }

    public class Winapp2ExcludeKey : ObservableObject
    {
        private string _path = "";
        private string _pattern = "*.*";
        private bool _recurse = false;

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
    }

    public class Winapp2Database : ObservableObject
    {
        private string _name = "";
        private string _version = "";
        private string _url = "";
        private string _localPath = "";
        private DateTime _lastUpdated = DateTime.MinValue;
        private bool _isEnabled = true;
        private int _entryCount = 0;
        private List<Winapp2Entry> _entries = new();

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

        public int EntryCount
        {
            get => _entryCount;
            set => SetProperty(ref _entryCount, value);
        }

        public List<Winapp2Entry> Entries
        {
            get => _entries;
            set => SetProperty(ref _entries, value);
        }
    }

    public static class Winapp2Constants
    {
        public static readonly Dictionary<string, string> KnownEnvironmentVariables = new(StringComparer.OrdinalIgnoreCase)
        {
            { "%AppData%", "AppData" },
            { "%LocalAppData%", "LocalApplicationData" },
            { "%CommonAppData%", "CommonApplicationData" },
            { "%ProgramFiles%", "ProgramFiles" },
            { "%ProgramFiles(x86)%", "ProgramFilesX86" },
            { "%SystemDrive%", "SystemDrive" },
            { "%WinDir%", "Windows" },
            { "%SystemRoot%", "Windows" },
            { "%Temp%", "Temp" },
            { "%Tmp%", "Temp" },
            { "%UserProfile%", "UserProfile" },
            { "%Documents%", "MyDocuments" },
            { "%Music%", "MyMusic" },
            { "%Pictures%", "MyPictures" },
            { "%Videos%", "MyVideos" },
            { "%Downloads%", "Downloads" },
            { "%Desktop%", "Desktop" },
            { "%StartMenu%", "StartMenu" },
            { "%Programs%", "Programs" },
            { "%Startup%", "Startup" },
            { "%Recent%", "Recent" },
            { "%SendTo%", "SendTo" },
            { "%Fonts%", "Fonts" },
            { "%Templates%", "Templates" },
            { "%Favorites%", "Favorites" },
            { "%NetHood%", "NetHood" },
            { "%PrintHood%", "PrintHood" },
            { "%Cookies%", "Cookies" },
            { "%History%", "History" },
            { "%Cache%", "Cache" },
            { "%LocalSettings%", "LocalSettings" }
        };

        public static readonly string Winapp2Url = "https://raw.githubusercontent.com/MoscaDotTo/Winapp2/master/Winapp2.ini";
        public static readonly string Winapp3Url = "https://raw.githubusercontent.com/MoscaDotTo/Winapp2/master/Winapp3.ini";
    }
}