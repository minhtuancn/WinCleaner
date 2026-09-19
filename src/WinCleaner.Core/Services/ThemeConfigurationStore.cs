using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    /// <summary>
    /// Interface for theme configuration persistence (CLI-compatible, no WPF dependencies)
    /// </summary>
    public interface IThemeConfigurationStore
    {
        ThemeSettings Settings { get; }
        event EventHandler<ThemeSettingsChangedEventArgs> SettingsChanged;
        Task InitializeAsync();
        Task<bool> SetThemeAsync(AppTheme theme);
        Task<bool> SetAccentColorAsync(string color);
        Task<bool> SetAnimationsEnabledAsync(bool enabled);
        Task<bool> SetTransparencyEnabledAsync(bool enabled);
        Task<bool> SetUiScaleAsync(double scale);
        Task<bool> SetUseSystemThemeAsync(bool useSystem);
        Task<bool> ToggleThemeAsync();
        Task<bool> SetUpdateSettingsAsync(bool autoCheck, UpdateChannel channel, bool autoDownload, bool autoInstall);
    }

    public class ThemeSettingsChangedEventArgs : EventArgs
    {
        public ThemeSettings PreviousSettings { get; set; }
        public ThemeSettings NewSettings { get; set; }
    }

    /// <summary>
    /// Theme configuration store - handles persistence only, no WPF dependencies.
    /// Used by both CLI and WPF app.
    /// </summary>
    public class ThemeConfigurationStore : IThemeConfigurationStore
    {
        private readonly ILogger<ThemeConfigurationStore> _logger;
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        
        private ThemeSettings _settings = new();

        public ThemeSettings Settings => _settings;

        public event EventHandler<ThemeSettingsChangedEventArgs> SettingsChanged;

        public ThemeConfigurationStore(ILogger<ThemeConfigurationStore> logger)
        {
            _logger = logger;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsDir = Path.Combine(appData, "WinCleaner", "Theme");
            Directory.CreateDirectory(settingsDir);
            _settingsFilePath = Path.Combine(settingsDir, "theme_settings.json");
        }

        public async Task InitializeAsync()
        {
            try
            {
                _settings = await LoadSettingsAsync();
                _logger.LogInformation("Theme configuration initialized: {Theme}", _settings.CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize theme configuration");
                _settings = new ThemeSettings();
            }
        }

        public async Task<bool> SetThemeAsync(AppTheme theme)
        {
            if (theme == _settings.CurrentTheme) return true;

            var previousSettings = _settings.Clone();
            
            try
            {
                _settings.CurrentTheme = theme;
                _settings.UseSystemTheme = theme == AppTheme.System;
                
                await SaveSettingsAsync();
                
                SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                {
                    PreviousSettings = previousSettings,
                    NewSettings = _settings.Clone()
                });
                
                _logger.LogInformation("Theme changed to {Theme}", theme);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set theme to {Theme}", theme);
                return false;
            }
        }

        public async Task<bool> SetAccentColorAsync(string color)
        {
            try
            {
                if (IsValidHexColor(color))
                {
                    var previousSettings = _settings.Clone();
                    _settings.AccentColor = color;
                    await SaveSettingsAsync();
                    
                    SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                    {
                        PreviousSettings = previousSettings,
                        NewSettings = _settings.Clone()
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set accent color");
            }
            return false;
        }

        public async Task<bool> SetAnimationsEnabledAsync(bool enabled)
        {
            try
            {
                var previousSettings = _settings.Clone();
                _settings.EnableAnimations = enabled;
                await SaveSettingsAsync();
                
                SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                {
                    PreviousSettings = previousSettings,
                    NewSettings = _settings.Clone()
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set animations setting");
            }
            return false;
        }

        public async Task<bool> SetTransparencyEnabledAsync(bool enabled)
        {
            try
            {
                var previousSettings = _settings.Clone();
                _settings.EnableTransparency = enabled;
                await SaveSettingsAsync();
                
                SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                {
                    PreviousSettings = previousSettings,
                    NewSettings = _settings.Clone()
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set transparency setting");
            }
            return false;
        }

        public async Task<bool> SetUiScaleAsync(double scale)
        {
            try
            {
                var previousSettings = _settings.Clone();
                _settings.UiScale = Math.Clamp(scale, 0.5, 2.0);
                await SaveSettingsAsync();
                
                SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                {
                    PreviousSettings = previousSettings,
                    NewSettings = _settings.Clone()
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set UI scale");
            }
            return false;
        }

        public async Task<bool> SetUseSystemThemeAsync(bool useSystem)
        {
            try
            {
                var previousSettings = _settings.Clone();
                _settings.UseSystemTheme = useSystem;
                
                if (useSystem)
                {
                    _settings.CurrentTheme = GetSystemTheme();
                }
                else
                {
                    // Keep current theme when disabling system theme
                }
                
                await SaveSettingsAsync();
                
                SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                {
                    PreviousSettings = previousSettings,
                    NewSettings = _settings.Clone()
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set use system theme");
            }
            return false;
        }

        public async Task<bool> ToggleThemeAsync()
        {
            var newTheme = _settings.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            return await SetThemeAsync(newTheme);
        }

        public async Task<bool> SetUpdateSettingsAsync(bool autoCheck, UpdateChannel channel, bool autoDownload, bool autoInstall)
        {
            try
            {
                var previousSettings = _settings.Clone();
                _settings.AutoCheckUpdates = autoCheck;
                _settings.UpdateChannel = channel;
                _settings.AutoDownloadUpdates = autoDownload;
                _settings.AutoInstallUpdates = autoInstall;
                await SaveSettingsAsync();
                
                SettingsChanged?.Invoke(this, new ThemeSettingsChangedEventArgs
                {
                    PreviousSettings = previousSettings,
                    NewSettings = _settings.Clone()
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set update settings");
                return false;
            }
        }

        private AppTheme GetSystemTheme()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0 ? AppTheme.Dark : AppTheme.Light;
                    }
                }
            }
            catch { }
            
            return AppTheme.Light;
        }

        private async Task<ThemeSettings> LoadSettingsAsync()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(json, GetJsonOptions());
                    return settings ?? new ThemeSettings();
                }
                catch { }
            }
            return new ThemeSettings();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, GetJsonOptions());
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save theme settings");
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        private bool IsValidHexColor(string color)
        {
            if (string.IsNullOrEmpty(color) || !color.StartsWith("#"))
                return false;
            
            return color.Length == 7 && uint.TryParse(color.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out _);
        }
    }
}
