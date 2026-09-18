using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    /// <summary>
    /// Interface for applying theme to WPF application
    /// </summary>
    public interface IWpfThemeApplicator
    {
        Task ApplyThemeAsync(AppTheme theme, ThemeSettings settings);
        Task ApplyAccentColorAsync(string color);
        Task ApplyAnimationsSettingAsync(bool enabled);
        Task ApplyTransparencySettingAsync(bool enabled);
        Task ApplyUiScaleAsync(double scale);
        bool IsDarkThemeActive(AppTheme currentTheme);
    }

    /// <summary>
    /// WPF-specific theme applicator - handles ResourceDictionary and WPF resource updates
    /// </summary>
    public class WpfThemeApplicator : IWpfThemeApplicator
    {
        private readonly ILogger<WpfThemeApplicator> _logger;

        public WpfThemeApplicator(ILogger<WpfThemeApplicator> logger)
        {
            _logger = logger;
        }

        public async Task ApplyThemeAsync(AppTheme theme, ThemeSettings settings)
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

                // Apply other settings
                await ApplyAccentColorAsync(settings.AccentColor);
                await ApplyAnimationsSettingAsync(settings.EnableAnimations);
                await ApplyTransparencySettingAsync(settings.EnableTransparency);
                await ApplyUiScaleAsync(settings.UiScale);

                _logger.LogInformation("WPF theme applied: {Theme}", actualTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply WPF theme");
            }
        }

        public async Task ApplyAccentColorAsync(string color)
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

        public async Task ApplyAnimationsSettingAsync(bool enabled)
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

        public async Task ApplyTransparencySettingAsync(bool enabled)
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

        public async Task ApplyUiScaleAsync(double scale)
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

        public bool IsDarkThemeActive(AppTheme currentTheme)
        {
            var actualTheme = currentTheme == AppTheme.System ? GetSystemTheme() : currentTheme;
            return actualTheme == AppTheme.Dark;
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

        private System.Windows.Media.Color GetDarkerColor(System.Windows.Media.Color color)
        {
            var factor = 0.7;
            return System.Windows.Media.Color.FromRgb(
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor));
        }

        private System.Windows.Media.Color GetLighterColor(System.Windows.Media.Color color)
        {
            var factor = 1.3;
            return System.Windows.Media.Color.FromRgb(
                (byte)Math.Min(255, color.R * factor),
                (byte)Math.Min(255, color.G * factor),
                (byte)Math.Min(255, color.B * factor));
        }
    }
}
