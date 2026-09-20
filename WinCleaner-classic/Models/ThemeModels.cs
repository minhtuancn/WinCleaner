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

    public enum UpdateChannel
    {
        Stable,
        Beta,
        Preview
    }

    public class ThemeSettings : ObservableObject
    {
        private AppTheme _currentTheme = AppTheme.System;
        private bool _useSystemTheme = true;
        private string _accentColor = "#0078D4";
        private bool _enableAnimations = true;
        private bool _enableTransparency = true;
        private double _uiScale = 1.0;

        // Update settings
        private bool _autoCheckUpdates = true;
        private UpdateChannel _updateChannel = UpdateChannel.Stable;
        private bool _autoDownloadUpdates = false;
        private bool _autoInstallUpdates = false;

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

        // Update settings properties
        public bool AutoCheckUpdates
        {
            get => _autoCheckUpdates;
            set => SetProperty(ref _autoCheckUpdates, value);
        }

        public UpdateChannel UpdateChannel
        {
            get => _updateChannel;
            set => SetProperty(ref _updateChannel, value);
        }

        public bool AutoDownloadUpdates
        {
            get => _autoDownloadUpdates;
            set => SetProperty(ref _autoDownloadUpdates, value);
        }

        public bool AutoInstallUpdates
        {
            get => _autoInstallUpdates;
            set => SetProperty(ref _autoInstallUpdates, value);
        }
    }
}