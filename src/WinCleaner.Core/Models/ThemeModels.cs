using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public class ThemeSettings : ObservableObject
    {
        private AppTheme _currentTheme = AppTheme.System;
        private bool _useSystemTheme = true;
        private string _accentColor = "#0078D4";
        private bool _enableAnimations = true;
        private bool _enableTransparency = true;
        private double _uiScale = 1.0;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        public bool UseSystemTheme
        {
            get => _useSystemTheme;
            set => SetProperty(ref _useSystemTheme, value);
        }

        public string AccentColor
        {
            get => _accentColor;
            set => SetProperty(ref _accentColor, value);
        }

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set => SetProperty(ref _enableAnimations, value);
        }

        public bool EnableTransparency
        {
            get => _enableTransparency;
            set => SetProperty(ref _enableTransparency, value);
        }

        public double UiScale
        {
            get => _uiScale;
            set => SetProperty(ref _uiScale, Math.Clamp(value, 0.5, 2.0));
        }

        public string CurrentThemeDisplayName => CurrentTheme switch
        {
            AppTheme.Light => "Light",
            AppTheme.Dark => "Dark",
            AppTheme.System => "System Default",
            _ => "System Default"
        };

        /// <summary>
        /// Creates a deep clone of the ThemeSettings for change tracking
        /// </summary>
        public ThemeSettings Clone()
        {
            return new ThemeSettings
            {
                CurrentTheme = CurrentTheme,
                UseSystemTheme = UseSystemTheme,
                AccentColor = AccentColor,
                EnableAnimations = EnableAnimations,
                EnableTransparency = EnableTransparency,
                UiScale = UiScale
            };
        }
    }
}
