using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        ThemeSettings Settings { get; }
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        Task InitializeAsync();
        Task<bool> SetThemeAsync(AppTheme theme);
        Task<bool> SetAccentColorAsync(string color);
        Task<bool> SetAnimationsEnabledAsync(bool enabled);
        Task<bool> SetTransparencyEnabledAsync(bool enabled);
        Task<bool> SetUiScaleAsync(double scale);
        Task<bool> SetUseSystemThemeAsync(bool useSystem);
        Task<bool> ToggleThemeAsync();
        Task<bool> ApplyThemeAsync(AppTheme theme);
        bool IsDarkThemeActive();
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public AppTheme PreviousTheme { get; set; }
        public AppTheme NewTheme { get; set; }
    }

    [SupportedOSPlatform("windows")]
    public class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        
        private AppTheme _currentTheme = AppTheme.System;
        private ThemeSettings _settings = new();

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnThemeChanged(value);
                }
            }
        }

        public ThemeSettings Settings => _settings;

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public ThemeService(ILogger<ThemeService> logger)
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
                // Load saved settings
                _settings = await LoadSettingsAsync();

                // Determine initial theme
                if (_settings.UseSystemTheme)
                {
                    CurrentTheme = GetSystemTheme();
                    SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
                }
                else
                {
                    CurrentTheme = _settings.CurrentTheme;
                }

                // Apply theme to application
                await ApplyThemeAsync(CurrentTheme);
                
                _logger.LogInformation("Theme service initialized with theme: {Theme}", CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize theme service");
                CurrentTheme = AppTheme.System;
            }
        }

        public async Task<bool> SetThemeAsync(AppTheme theme)
        {
            if (theme == CurrentTheme) return true;

            var previousTheme = CurrentTheme;
            
            try
            {
                var success = await ApplyThemeAsync(theme);
                if (success)
                {
                    CurrentTheme = theme;
                    _settings.CurrentTheme = theme;
                    _settings.UseSystemTheme = theme == AppTheme.System;
                    
                    await SaveSettingsAsync();
                    
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                    {
                        PreviousTheme = previousTheme,
                        NewTheme = theme
                    });
                    
                    _logger.LogInformation("Theme changed from {Previous} to {Current}", previousTheme, theme);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set theme to {Theme}", theme);
            }
            
            return false;
        }

        public async Task<bool> SetAccentColorAsync(string color)
        {
            try
            {
                if (IsValidHexColor(color))
                {
                    _settings.AccentColor = color;
                    await SaveSettingsAsync();
                    await ApplyAccentColorAsync(color);
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
                _settings.EnableAnimations = enabled;
                await SaveSettingsAsync();
                await ApplyAnimationsSettingAsync(enabled);
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
                _settings.EnableTransparency = enabled;
                await SaveSettingsAsync();
                await ApplyTransparencySettingAsync(enabled);
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
                scale = Math.Clamp(scale, 0.5, 2.0);
                _settings.UiScale = scale;
                await SaveSettingsAsync();
                await ApplyUiScaleAsync(scale);
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
                _settings.UseSystemTheme = useSystem;
                
                if (useSystem)
                {
                    SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
                    await SetThemeAsync(GetSystemTheme());
                }
                else
                {
                    SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
                    await SetThemeAsync(_settings.CurrentTheme);
                }

                await SaveSettingsAsync();
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
            var newTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            return await SetThemeAsync(newTheme);
        }

        public async Task<bool> ApplyThemeAsync(AppTheme theme)
        {
            try
            {
                var actualTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
                
                // Apply theme to WPF application
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dictionaries = Application.Current.Resources.MergedDictionaries;
                    
                    // Remove existing theme dictionaries
                    var themeDicts = dictionaries.Where(d => d.Source != null && 
                        (d.Source.OriginalString.Contains("Light") || 
                         d.Source.OriginalString.Contains("Dark") ||
                         d.Source.OriginalString.Contains("Theme"))).ToList();
                    
                    foreach (var dict in themeDicts)
                    {
                        dictionaries.Remove(dict);
                    }

                    // Add new theme dictionary
                    var themeDict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/WinCleaner;component/Resources/Themes/{actualTheme}.xaml", UriKind.Absolute)
                    };
                    dictionaries.Add(themeDict);
                });

                // Apply accent color
                await ApplyAccentColorAsync(_settings.AccentColor);
                
                // Apply animations setting
                await ApplyAnimationsSettingAsync(_settings.EnableAnimations);
                
                // Apply transparency setting
                await ApplyTransparencySettingAsync(_settings.EnableTransparency);
                
                // Apply UI scale
                await ApplyUiScaleAsync(_settings.UiScale);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme");
                return false;
            }
        }

        public bool IsDarkThemeActive()
        {
            var actualTheme = CurrentTheme == AppTheme.System ? GetSystemTheme() : CurrentTheme;
            return actualTheme == AppTheme.Dark;
        }

        private AppTheme GetSystemTheme()
        {
            try
            {
                // Check Windows 10/11 theme setting
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

        private async void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General || 
                e.Category == UserPreferenceCategory.VisualStyle)
            {
                if (_settings.UseSystemTheme)
                {
                    var systemTheme = GetSystemTheme();
                    if (systemTheme != CurrentTheme)
                    {
                        await ApplyThemeAsync(systemTheme);
                    }
                }
            }
        }

        private async Task ApplyAccentColorAsync(string color)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var colorBrush = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
                    Application.Current.Resources["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(colorBrush);
                    Application.Current.Resources["PrimaryDarkBrush"] = new System.Windows.Media.SolidColorBrush(GetDarkerColor(colorBrush));
                    Application.Current.Resources["PrimaryLightBrush"] = new System.Windows.Media.SolidColorBrush(GetLighterColor(colorBrush));
                    Application.Current.Resources["FocusBrush"] = new System.Windows.Media.SolidColorBrush(colorBrush);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply accent color");
            }
        }

        private System.Windows.Media.Color GetDarkerColor(System.Windows.Media.Color color)
        {
            const double factor = 0.7;
            return System.Windows.Media.Color.FromRgb(
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor));
        }

        private System.Windows.Media.Color GetLighterColor(System.Windows.Media.Color color)
        {
            return System.Windows.Media.Color.FromRgb(
                (byte)Math.Min(255, color.R * 1.3),
                (byte)Math.Min(255, color.G * 1.3),
                (byte)Math.Min(255, color.B * 1.3));
        }

        private async Task ApplyAnimationsSettingAsync(bool enabled)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Resources["EnableAnimations"] = enabled;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply animations setting");
            }
        }

        private async Task ApplyTransparencySettingAsync(bool enabled)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Resources["EnableTransparency"] = enabled;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply transparency setting");
            }
        }

        private async Task ApplyUiScaleAsync(double scale)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Resources["UiScale"] = scale;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply UI scale");
            }
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

        private void OnThemeChanged(AppTheme theme)
        {
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
            {
                PreviousTheme = _currentTheme,
                NewTheme = theme
            });
        }
    }
}