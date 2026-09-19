using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WinCleaner.Design;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    /// <summary>
    /// Main application shell - coordinates navigation and global state.
    /// Single source of truth for current view, theme, and app-level commands.
    /// </summary>
    public sealed partial class ShellViewModel : BaseViewModel
    {
        private readonly IThemeConfigurationStore _themeStore;
        private readonly IResilienceService _resilience;
        private readonly IDiagnosticsService _diagnostics;

        [ObservableProperty]
        private BaseViewModel _currentView = null!;

        [ObservableProperty]
        private bool _isSidebarOpen = true;

        [ObservableProperty]
        private string _windowTitle = "WinCleaner";

        // Navigation history for back button
        private readonly Stack<string> _navigationHistory = new();

        public ObservableCollection<NavItem> NavigationItems { get; } = new()
        {
            new NavItem("Health Check", "HealthCheck", Icons.Monitor, "System health & quick clean"),
            new NavItem("Advanced Clean", "AdvancedClean", Icons.Broom, "Deep cleaning with safety review"),
            new NavItem("Storage", "Storage", Icons.Drive, "Disk usage analysis & large files"),
            new NavItem("Live Timeline", "LiveTimeline", Icons.Clock, "Real-time operation progress"),
            new NavItem("Manual Cleanup", "ManualCleanup", Icons.Folder, "Explore & clean custom paths"),
            new NavItem("Settings", "Settings", Icons.Settings, "Preferences & configuration"),
            new NavItem("Diagnostics", "Diagnostics", Icons.Info, "Logs, crash reports & health"),
        };

        private readonly IServiceProvider _services;

        public ShellViewModel(
            IThemeConfigurationStore themeStore,
            IResilienceService resilience,
            IDiagnosticsService diagnostics,
            IServiceProvider services,
            HealthCheckViewModel healthCheckVM,
            AdvancedCleanViewModel advancedCleanVM,
            StorageViewModel storageVM,
            ManualCleanupViewModel manualCleanupVM,
            SettingsViewModel settingsVM,
            DiagnosticsViewModel diagnosticsVM,
            LiveTimelineViewModel liveTimelineVM)
        {
            _themeStore = themeStore;
            _resilience = resilience;
            _diagnostics = diagnostics;
            _services = services;

            // Initialize with Health Check view
            CurrentView = healthCheckVM;
            _navigationHistory.Push("HealthCheck");

            // Initialize HealthCheck (auto-scan)
            _ = Task.Run(async () => await healthCheckVM.InitializeAsync());

            // Subscribe to theme changes
            _themeStore.SettingsChanged += (s, e) => OnPropertyChanged(nameof(IsDarkTheme));
        }

        public bool IsDarkTheme => _themeStore.Settings.CurrentTheme == AppTheme.Dark ||
                                       (_themeStore.Settings.UseSystemTheme && IsSystemDark());

        public bool CanGoBack => _navigationHistory.Count > 1;

        [RelayCommand]
        private void Navigate(string viewName)
        {
            if (viewName == CurrentView?.GetType().Name.Replace("ViewModel", ""))
                return;

            _navigationHistory.Push(viewName);
            OnPropertyChanged(nameof(CanGoBack));

            CurrentView = viewName switch
            {
                "HealthCheck" => _services.GetRequiredService<HealthCheckViewModel>(),
                "AdvancedClean" => _services.GetRequiredService<AdvancedCleanViewModel>(),
                "Storage" => _services.GetRequiredService<StorageViewModel>(),
                "LiveTimeline" => _services.GetRequiredService<LiveTimelineViewModel>(),
                "ManualCleanup" => _services.GetRequiredService<ManualCleanupViewModel>(),
                "Settings" => _services.GetRequiredService<SettingsViewModel>(),
                "Diagnostics" => _services.GetRequiredService<DiagnosticsViewModel>(),
                _ => CurrentView
            };
        }

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

        [RelayCommand]
        private void GoBack()
        {
            if (_navigationHistory.Count > 1)
            {
                _navigationHistory.Pop(); // Remove current
                var previous = _navigationHistory.Peek(); // Get previous
                OnPropertyChanged(nameof(CanGoBack));
                
                CurrentView = previous switch
                {
                    "HealthCheck" => _services.GetRequiredService<HealthCheckViewModel>(),
                    "AdvancedClean" => _services.GetRequiredService<AdvancedCleanViewModel>(),
                    "Storage" => _services.GetRequiredService<StorageViewModel>(),
                    "LiveTimeline" => _services.GetRequiredService<LiveTimelineViewModel>(),
                    "ManualCleanup" => _services.GetRequiredService<ManualCleanupViewModel>(),
                    "Settings" => _services.GetRequiredService<SettingsViewModel>(),
                    "Diagnostics" => _services.GetRequiredService<DiagnosticsViewModel>(),
                    _ => CurrentView
                };
            }
        }

        [RelayCommand]
        private async Task ToggleThemeAsync()
        {
            await _themeStore.ToggleThemeAsync();
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        [RelayCommand]
        private async Task ExportDiagnosticsAsync()
        {
            try
            {
                IsBusy = true;
                var path = await _diagnostics.ExportDiagnosticsAsync();
                LogSuccess($"Diagnostics exported to: {path}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to export diagnostics: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int val && val == 0;
            }
            catch { return false; }
        }
    }

    public sealed class NavItem
    {
        public string Label { get; }
        public string ViewName { get; }
        public System.Windows.Media.Geometry Icon { get; }
        public string Description { get; }

        public NavItem(string label, string viewName, System.Windows.Media.Geometry icon, string description)
        {
            Label = label;
            ViewName = viewName;
            Icon = icon;
            Description = description;
        }
    }
}