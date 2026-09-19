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
        private readonly IThemeConfigurationStore _configurationStore;
        private readonly IWpfThemeApplicator _wpfApplicator;
        private readonly ILogger<ThemeService> _logger;

        public AppTheme CurrentTheme => _configurationStore.Settings.CurrentTheme;

        public ThemeSettings Settings => _configurationStore.Settings;

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public ThemeService(
            IThemeConfigurationStore configurationStore,
            IWpfThemeApplicator wpfApplicator,
            ILogger<ThemeService> logger)
        {
            _configurationStore = configurationStore;
            _wpfApplicator = wpfApplicator;
            _logger = logger;

            // Subscribe to configuration changes
            _configurationStore.SettingsChanged += OnConfigurationChanged;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _configurationStore.InitializeAsync();

                // Apply theme to WPF application
                await ApplyThemeAsync(CurrentTheme);
                
                _logger.LogInformation("Theme service initialized with theme: {Theme}", CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize theme service");
                // Fallback to system theme
                await _configurationStore.SetThemeAsync(AppTheme.System);
            }
        }

        public async Task<bool> SetThemeAsync(AppTheme theme)
        {
            if (theme == CurrentTheme) return true;

            var previousTheme = CurrentTheme;
            
            try
            {
                var success = await _configurationStore.SetThemeAsync(theme);
                if (success)
                {
                    await ApplyThemeAsync(theme);
                    
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
                var success = await _configurationStore.SetAccentColorAsync(color);
                if (success)
                {
                    await _wpfApplicator.ApplyAccentColorAsync(color);
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
                var success = await _configurationStore.SetAnimationsEnabledAsync(enabled);
                if (success)
                {
                    await _wpfApplicator.ApplyAnimationsSettingAsync(enabled);
                    return true;
                }
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
                var success = await _configurationStore.SetTransparencyEnabledAsync(enabled);
                if (success)
                {
                    await _wpfApplicator.ApplyTransparencySettingAsync(enabled);
                    return true;
                }
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
                var success = await _configurationStore.SetUiScaleAsync(scale);
                if (success)
                {
                    await _wpfApplicator.ApplyUiScaleAsync(scale);
                    return true;
                }
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
                var success = await _configurationStore.SetUseSystemThemeAsync(useSystem);
                if (success)
                {
                    await ApplyThemeAsync(CurrentTheme);
                    return true;
                }
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
                await _wpfApplicator.ApplyThemeAsync(theme, Settings);
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
            return _wpfApplicator.IsDarkThemeActive(CurrentTheme);
        }

        private void OnConfigurationChanged(object? sender, ThemeSettingsChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
            {
                PreviousTheme = e.PreviousSettings.CurrentTheme,
                NewTheme = e.NewSettings.CurrentTheme
            });
        }
    }
}
