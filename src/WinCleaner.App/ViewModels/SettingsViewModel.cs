using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    /// <summary>
    /// Settings - all user preferences with proper persistence.
    /// </summary>
    public sealed partial class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeConfigurationStore _themeStore;
        private readonly ILogger<SettingsViewModel> _logger;

        [ObservableProperty]
        private AppSettings _settings;

        [ObservableProperty]
        private AppTheme _selectedTheme;

        [ObservableProperty]
        private bool _useSystemTheme;

        [ObservableProperty]
        private string _accentColor = "#0078D4";

        [ObservableProperty]
        private bool _enableAnimations = true;

        [ObservableProperty]
        private bool _enableTransparency = true;

        [ObservableProperty]
        private double _uiScale = 1.0;

        [ObservableProperty]
        private string _language = "vi-VN";

        [ObservableProperty]
        private ObservableCollection<string> _availableLanguages = new() { "vi-VN", "en-US" };

        [ObservableProperty]
        private bool _autoCloseAfterClean;

        [ObservableProperty]
        private bool _minimizeToTray = true;

        [ObservableProperty]
        private bool _showHiddenFiles;

        [ObservableProperty]
        private bool _confirmBeforeDelete = true;

        [ObservableProperty]
        private int _maxLogEntries = 10000;

        [ObservableProperty]
        private ObservableCollection<string> _excludedPaths = new();

        [ObservableProperty]
        private ObservableCollection<string> _customPaths = new();

        public SettingsViewModel(
            ISettingsService settingsService,
            IThemeConfigurationStore themeStore,
            ILogger<SettingsViewModel> logger)
        {
            _settingsService = settingsService;
            _themeStore = themeStore;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                Settings = await _settingsService.LoadAsync();
                await _themeStore.InitializeAsync();

                // Sync from settings
                SelectedTheme = Settings.Theme switch
                {
                    "Light" => AppTheme.Light,
                    "Dark" => AppTheme.Dark,
                    _ => AppTheme.System
                };
                UseSystemTheme = Settings.Theme == "System" || _themeStore.Settings.UseSystemTheme;
                AccentColor = _themeStore.Settings.AccentColor;
                EnableAnimations = _themeStore.Settings.EnableAnimations;
                EnableTransparency = _themeStore.Settings.EnableTransparency;
                UiScale = _themeStore.Settings.UiScale;
                Language = Settings.Language;
                AutoCloseAfterClean = Settings.AutoCloseAfterClean;
                MinimizeToTray = Settings.MinimizeToTray;
                ShowHiddenFiles = Settings.ShowHiddenFiles;
                ConfirmBeforeDelete = Settings.ConfirmBeforeDelete;
                MaxLogEntries = Settings.MaxLogEntries;
                ExcludedPaths = new ObservableCollection<string>(Settings.ExcludedPaths);
                CustomPaths = new ObservableCollection<string>(Settings.CustomPaths);

                LogInfo("Settings loaded");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load settings: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                LogInfo("Saving settings...");

                // Update theme store
                await _themeStore.SetThemeAsync(UseSystemTheme ? AppTheme.System : SelectedTheme);
                await _themeStore.SetAccentColorAsync(AccentColor);
                await _themeStore.SetAnimationsEnabledAsync(EnableAnimations);
                await _themeStore.SetTransparencyEnabledAsync(EnableTransparency);
                await _themeStore.SetUiScaleAsync(UiScale);
                await _themeStore.SetUseSystemThemeAsync(UseSystemTheme);

                // Update settings
                Settings.Theme = UseSystemTheme ? "System" : SelectedTheme.ToString();
                Settings.Language = Language;
                Settings.AutoCloseAfterClean = AutoCloseAfterClean;
                Settings.MinimizeToTray = MinimizeToTray;
                Settings.ShowHiddenFiles = ShowHiddenFiles;
                Settings.ConfirmBeforeDelete = ConfirmBeforeDelete;
                Settings.MaxLogEntries = MaxLogEntries;
                Settings.ExcludedPaths = ExcludedPaths.ToList();
                Settings.CustomPaths = CustomPaths.ToList();

                await _settingsService.SaveAsync(Settings);

                LogSuccess("Settings saved");
            }
            catch (Exception ex)
            {
                LogError($"Failed to save settings: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ResetToDefaultsAsync()
        {
            try
            {
                IsBusy = true;
                await _settingsService.ResetAsync();
                Settings = await _settingsService.LoadAsync();
                await InitializeAsync();
                LogSuccess("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                LogError($"Failed to reset: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportSettingsAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = $"WinCleaner_Settings_{DateTime.Now:yyyyMMdd}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _settingsService.ExportAsync(dialog.FileName);
                    LogSuccess($"Settings exported to {dialog.FileName}");
                }
                catch (Exception ex)
                {
                    LogError($"Export failed: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task ImportSettingsAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _settingsService.ImportAsync(dialog.FileName);
                    await InitializeAsync();
                    LogSuccess("Settings imported");
                }
                catch (Exception ex)
                {
                    LogError($"Import failed: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void AddExcludedPath()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Select Folder",
                Filter = "Folders|*.*"
            };
            dialog.FileName = "";

            if (dialog.ShowDialog() == true)
            {
                var folder = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folder) && !ExcludedPaths.Contains(folder))
                    ExcludedPaths.Add(folder);
            }
        }

        [RelayCommand]
        private void RemoveExcludedPath(string path)
        {
            ExcludedPaths.Remove(path);
        }

        [RelayCommand]
        private void AddCustomPath()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Select Folder",
                Filter = "Folders|*.*"
            };
            dialog.FileName = "";

            if (dialog.ShowDialog() == true)
            {
                var folder = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folder) && !CustomPaths.Contains(folder))
                    CustomPaths.Add(folder);
            }
        }

        [RelayCommand]
        private void RemoveCustomPath(string path)
        {
            CustomPaths.Remove(path);
        }
    }
}