using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using WinCleaner.Models;

namespace WinCleaner.Models
{
    public interface IExtension
    {
        string Id { get; }
        string Name { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }
        string EntryPoint { get; }
        ExtensionType Type { get; }
        bool IsEnabled { get; set; }
        Task<bool> InitializeAsync(IServiceProvider services);
        Task<bool> ShutdownAsync();
        ExtensionMetadata GetMetadata();
    }

    public enum ExtensionType
    {
        Cleaner,
        Tool,
        Theme,
        Language,
        Analyzer
    }

    public class ExtensionMetadata
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string Author { get; set; } = "";
        public string Description { get; set; } = "";
        public string EntryPoint { get; set; } = "";
        public ExtensionType Type { get; set; } = ExtensionType.Cleaner;
        public string[] Dependencies { get; set; } = Array.Empty<string>();
        public string MinWinCleanerVersion { get; set; } = "1.0.0";
        public string[] SupportedOS { get; set; } = new[] { "Windows10", "Windows11" };
        public string License { get; set; } = "MIT";
        public string RepositoryUrl { get; set; } = "";
        public string IconPath { get; set; } = "";
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class ExtensionInfo : ObservableObject
    {
        private string _id = "";
        private string _name = "";
        private string _version = "";
        private string _author = "";
        private string _description = "";
        private ExtensionType _type = ExtensionType.Cleaner;
        private bool _isEnabled = false;
        private bool _isLoaded = false;
        private string _assemblyPath = "";
        private string _errorMessage = "";
        private DateTime _loadedAt = DateTime.MinValue;
        private ExtensionMetadata _metadata = new();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

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

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ExtensionType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public bool IsLoaded
        {
            get => _isLoaded;
            set => SetProperty(ref _isLoaded, value);
        }

        public string AssemblyPath
        {
            get => _assemblyPath;
            set => SetProperty(ref _assemblyPath, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public DateTime LoadedAt
        {
            get => _loadedAt;
            set => SetProperty(ref _loadedAt, value);
        }

        public ExtensionMetadata Metadata
        {
            get => _metadata;
            set => SetProperty(ref _metadata, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }

    public class ExtensionLoadedEventArgs : EventArgs
    {
        public string ExtensionId { get; set; } = "";
        public string ExtensionName { get; set; } = "";
    }

    public class ExtensionUnloadedEventArgs : EventArgs
    {
        public string ExtensionId { get; set; } = "";
        public string ExtensionName { get; set; } = "";
    }

    public interface ICleanerExtension : IExtension
    {
        Task<List<CleanItem>> GetCleanItemsAsync();
        Task<bool> CleanAsync(CleanItem item, IProgress<string> progress);
        CleanCategory Category { get; }
    }

    public interface IAnalyzerExtension : IExtension
    {
        Task<AnalysisResult> AnalyzeAsync(AnalysisContext context);
        AnalysisCategory Category { get; }
    }

    public class AnalysisResult
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public AnalysisSeverity Severity { get; set; } = AnalysisSeverity.Info;
        public List<AnalysisItem> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class AnalysisItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; } = 0;
        public AnalysisSeverity Severity { get; set; } = AnalysisSeverity.Info;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public enum AnalysisSeverity
    {
        Info,
        Warning,
        Critical,
        Safe
    }

    public enum AnalysisCategory
    {
        DiskSpace,
        Performance,
        Security,
        Privacy,
        Registry,
        Startup,
        Network
    }

    public class AnalysisContext
    {
        public List<CleanItem> CleanItems { get; set; } = new();
        public List<AppxPackage> AppxPackages { get; set; } = new();
        public List<ScheduledTask> ScheduledTasks { get; set; } = new();
        public Dictionary<string, object> CustomData { get; set; } = new();
    }
}