using System;
using System.Collections.ObjectModel;
using System.IO;
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
    /// Manual Cleanup Explorer - lazy-loaded folder tree with safety classification.
    /// </summary>
    public sealed partial class ManualCleanupViewModel : BaseViewModel
    {
        private readonly IPathSafetyValidator _pathValidator;
        private readonly IAppRunningGuard _appGuard;

        [ObservableProperty]
        private ObservableCollection<ManualTreeNode> _rootNodes = new();

        [ObservableProperty]
        private ManualTreeNode _selectedNode;

        [ObservableProperty]
        private string _customPath = "";

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private NodeDetailViewModel _nodeDetail;

        public ManualCleanupViewModel(IPathSafetyValidator pathValidator, IAppRunningGuard appGuard)
        {
            _pathValidator = pathValidator;
            _appGuard = appGuard;

            // Initialize with common root paths
            RootNodes = new ObservableCollection<ManualTreeNode>
            {
                new ManualTreeNode { Name = "C:\\", Path = @"C:\", IsRoot = true, SafetyLevel = PathSafetyLevel.Caution },
                new ManualTreeNode { Name = "User Profile", Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), SafetyLevel = PathSafetyLevel.Caution },
                new ManualTreeNode { Name = "Temp", Path = Path.GetTempPath(), SafetyLevel = PathSafetyLevel.Safe }
            };
        }

        [RelayCommand]
        private async Task AddCustomPathAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomPath) || !Directory.Exists(CustomPath)) return;

            var safety = _pathValidator.ValidatePath(CustomPath, CleanCategory.UserTemp, ItemRiskLevel.Low);
            
            RootNodes.Add(new ManualTreeNode
            {
                Name = Path.GetFileName(CustomPath) ?? CustomPath,
                Path = CustomPath,
                IsRoot = true,
                SafetyLevel = safety.SafetyLevel
            });

            CustomPath = "";
        }

        [RelayCommand]
        private async Task LoadChildrenAsync(ManualTreeNode node)
        {
            if (node == null || node.IsLoaded || IsScanning) return;

            try
            {
                IsScanning = true;
                node.IsLoading = true;

                await Task.Run(() => LoadDirectory(node));

                node.IsLoaded = true;
                node.IsExpanded = true;
                LogSuccess($"Loaded {node.Children.Count} items from {node.Name}");
            }
            catch (UnauthorizedAccessException)
            {
                LogWarning($"Access denied: {node.Path}");
                node.SafetyLevel = PathSafetyLevel.Protected;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load {node.Path}: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
                node.IsLoading = false;
            }
        }

        private void LoadDirectory(ManualTreeNode parent)
        {
            try
            {
                var dirs = Directory.GetDirectories(parent.Path);
                foreach (var dir in dirs)
                {
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        var size = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                        
                        var safety = _pathValidator.ValidatePath(dir, CleanCategory.UserTemp, ItemRiskLevel.Low);
                        var processes = _appGuard.GetRunningProcessesForPath(dir);

                        var child = new ManualTreeNode
                        {
                            Name = di.Name,
                            Path = dir,
                            Size = size,
                            SafetyLevel = safety.SafetyLevel,
                            RunningProcesses = processes,
                            RequiresAppClose = processes.Count > 0
                        };

                        parent.Children.Add(child);
                    }
                    catch { /* Skip inaccessible directories */ }
                }
            }
            catch { /* Skip inaccessible parent */ }
        }

        [RelayCommand]
        private void SelectNode(ManualTreeNode node)
        {
            SelectedNode = node;
            UpdateNodeDetail(node);
        }

        [RelayCommand]
        private void ToggleNode(ManualTreeNode node)
        {
            node.IsExpanded = !node.IsExpanded;
            if (node.IsExpanded && !node.IsLoaded)
                _ = LoadChildrenAsync(node);
        }

        private void UpdateNodeDetail(ManualTreeNode node)
        {
            NodeDetail = new NodeDetailViewModel
            {
                Name = node.Name,
                Path = node.Path,
                Size = node.Size,
                SafetyLevel = node.SafetyLevel,
                ItemCount = node.Children.Count,
                RunningProcesses = node.RunningProcesses,
                RequiresAppClose = node.RequiresAppClose,
                SafetyReasons = _pathValidator.ValidatePath(node.Path, CleanCategory.UserTemp, ItemRiskLevel.Low).Reasons
            };
        }
    }

    public sealed class ManualTreeNode : ObservableObject
    {
        private bool _isExpanded;
        private bool _isLoading;
        private bool _isLoaded;

        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public bool IsRoot { get; set; }
        public long Size { get; set; }
        public PathSafetyLevel SafetyLevel { get; set; } = PathSafetyLevel.Safe;
        public ObservableCollection<ManualTreeNode> Children { get; } = new();
        public List<string> RunningProcesses { get; set; } = new();
        public bool RequiresAppClose { get; set; }

        public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool IsLoaded { get => _isLoaded; set => SetProperty(ref _isLoaded, value); }

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

    public sealed class NodeDetailViewModel : ObservableObject
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public PathSafetyLevel SafetyLevel { get; set; }
        public int ItemCount { get; set; }
        public List<string> RunningProcesses { get; set; } = new();
        public bool RequiresAppClose { get; set; }
        public List<string> SafetyReasons { get; set; } = new();
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
}