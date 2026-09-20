using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WinCleaner.Design;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel, INavigableViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<SettingsViewModel> _logger;

        [ObservableProperty]
        private AppSettings _settings = new();

        [ObservableProperty]
        private AppTheme _selectedTheme = AppTheme.System;

        [ObservableProperty]
        private bool _useSystemTheme = true;

        [ObservableProperty]
        private string _accentColor = "#0078D4";

        [ObservableProperty]
        private bool _enableAnimations = true;

        [ObservableProperty]
        private bool _enableTransparency = true;

        [ObservableProperty]
        private double _uiScale = 1.0;

        [ObservableProperty]
        private string _selectedLanguage = "en";

        [ObservableProperty]
        private ObservableCollection<string> _languages = new() { "en", "vi", "de", "fr", "zh-CN", "zh-TW" };

        [ObservableProperty]
        private bool _autoCheckUpdates = true;

        [ObservableProperty]
        private UpdateChannel _selectedChannel = UpdateChannel.Stable;

        [ObservableProperty]
        private bool _autoDownloadUpdates = false;

        [ObservableProperty]
        private bool _autoInstallUpdates = false;

        [ObservableProperty]
        private bool _createRestorePoint = true;

        [ObservableProperty]
        private int _logRetentionDays = 30;

        public ObservableCollection<AppTheme> AvailableThemes { get; } = new() { AppTheme.Light, AppTheme.Dark, AppTheme.System };

        public ObservableCollection<UpdateChannel> AvailableChannels { get; } = new() 
        { 
            UpdateChannel.Stable, 
            UpdateChannel.Beta, 
            UpdateChannel.Preview 
        };

        public string Title => "Settings";
        public Geometry Icon => Icons.Settings;
        public bool IsSelected { get; set; }

        public SettingsViewModel(
            ISettingsService settingsService,
            IThemeService themeService,
            ILocalizationService localizationService,
            ILogger<SettingsViewModel> logger)
        {
            _settingsService = settingsService;
            _themeService = themeService;
            _localizationService = localizationService;
            _logger = logger;

            _themeService.ThemeChanged += (s, e) => OnPropertyChanged(nameof(IsDarkTheme));
        }

        public bool IsDarkTheme => _themeService.IsDarkThemeActive();

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                Settings = await _settingsService.LoadAsync();
                await _themeService.InitializeAsync();
                await _localizationService.InitializeAsync();

                SelectedTheme = Settings.Theme switch
                {
                    "Light" => AppTheme.Light,
                    "Dark" => AppTheme.Dark,
                    _ => AppTheme.System
                };
                UseSystemTheme = Settings.Theme == "System" || _themeService.Settings.UseSystemTheme;
                AccentColor = _themeService.Settings.AccentColor;
                EnableAnimations = _themeService.Settings.EnableAnimations;
                EnableTransparency = _themeService.Settings.EnableTransparency;
                UiScale = _themeService.Settings.UiScale;
                SelectedLanguage = Settings.Language;
                AutoCheckUpdates = _themeService.Settings.AutoCheckUpdates;
                SelectedChannel = _themeService.Settings.UpdateChannel;
                AutoDownloadUpdates = _themeService.Settings.AutoDownloadUpdates;
                AutoInstallUpdates = _themeService.Settings.AutoInstallUpdates;

                var availableLanguages = await _localizationService.GetAvailableLanguagesAsync();
                Languages.Clear();
                foreach (var lang in availableLanguages)
                {
                    Languages.Add(lang.ToString());
                }

                _logger.LogInformation("Settings loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
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
                _logger.LogInformation("Saving settings...");

                await _themeService.SetThemeAsync(UseSystemTheme ? AppTheme.System : SelectedTheme);
                await _themeService.SetAccentColorAsync(AccentColor);
                await _themeService.SetAnimationsEnabledAsync(EnableAnimations);
                await _themeService.SetTransparencyEnabledAsync(EnableTransparency);
                await _themeService.SetUiScaleAsync(UiScale);
                await _themeService.SetUseSystemThemeAsync(UseSystemTheme);
                await _themeService.SetUpdateSettingsAsync(AutoCheckUpdates, SelectedChannel, AutoDownloadUpdates, AutoInstallUpdates);

                await _localizationService.SetLanguageAsync(Enum.Parse<SupportedLanguage>(SelectedLanguage));

                Settings.Theme = UseSystemTheme ? "System" : SelectedTheme.ToString();
                Settings.Language = SelectedLanguage;
                Settings.AutoCloseAfterClean = Settings.AutoCloseAfterClean;
                Settings.MinimizeToTray = Settings.MinimizeToTray;
                Settings.ShowHiddenFiles = Settings.ShowHiddenFiles;
                Settings.ConfirmBeforeDelete = Settings.ConfirmBeforeDelete;
                Settings.MaxLogEntries = Settings.MaxLogEntries;
                Settings.ExcludedPaths = Settings.ExcludedPaths;
                Settings.CustomPaths = Settings.CustomPaths;

                await _settingsService.SaveAsync(Settings);

                _logger.LogInformation("Settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void PickAccentColor()
        {
            try
            {
                var dialog = new Views.ColorPickerDialog
                {
                    Color = (Color)ColorConverter.ConvertFromString(AccentColor),
                    Owner = Application.Current.MainWindow
                };
                
                if (dialog.ShowDialog() == true)
                {
                    var color = dialog.Color;
                    AccentColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pick accent color");
            }
        }

        [RelayCommand]
        private async Task ResetToDefaultsAsync()
        {
            try
            {
                IsBusy = true;
                var defaults = _settingsService.GetDefaults();
                Settings = defaults;
                await InitializeAsync();
                _logger.LogInformation("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset settings");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}