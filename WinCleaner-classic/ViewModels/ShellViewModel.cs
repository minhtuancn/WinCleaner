using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WinCleaner.Design;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    public sealed partial class ShellViewModel : BaseViewModel
    {
        private readonly IThemeService _themeService;
        private readonly IServiceProvider _services;

        [ObservableProperty]
        private INavigableViewModel _currentView = null!;

        [ObservableProperty]
        private bool _isSidebarOpen = true;

        [ObservableProperty]
        private string _windowTitle = "WinCleaner";

        public ObservableCollection<NavItem> NavigationItems { get; } = new()
        {
            new NavItem("Dashboard", Icons.Monitor, typeof(MainViewModel)),
            new NavItem("Cleaner", Icons.Broom, typeof(CleanerViewModel)),
            new NavItem("Tools", Icons.Folder, typeof(ToolsViewModel)),
            new NavItem("Settings", Icons.Settings, typeof(SettingsViewModel)),
        };

        public ShellViewModel(
            IThemeService themeService,
            IServiceProvider services,
            MainViewModel dashboardVM,
            CleanerViewModel cleanerVM,
            ToolsViewModel toolsVM,
            SettingsViewModel settingsVM)
        {
            _themeService = themeService;
            _services = services;

            CurrentView = dashboardVM;

            _themeService.ThemeChanged += (s, e) => OnPropertyChanged(nameof(IsDarkTheme));
        }

        public bool IsDarkTheme => _themeService.IsDarkThemeActive();

        [RelayCommand]
        private void Navigate(string viewName)
        {
            if (viewName == CurrentView?.GetType().Name.Replace("ViewModel", ""))
                return;

            CurrentView = viewName switch
            {
                "Dashboard" => _services.GetRequiredService<MainViewModel>(),
                "Cleaner" => _services.GetRequiredService<CleanerViewModel>(),
                "Tools" => _services.GetRequiredService<ToolsViewModel>(),
                "Settings" => _services.GetRequiredService<SettingsViewModel>(),
                _ => CurrentView
            };
        }

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

        [RelayCommand]
        private async Task ToggleThemeAsync()
        {
            await _themeService.ToggleThemeAsync();
            OnPropertyChanged(nameof(IsDarkTheme));
        }
    }
}