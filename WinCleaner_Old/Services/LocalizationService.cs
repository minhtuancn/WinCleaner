using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ILocalizationService
    {
        SupportedLanguage CurrentLanguage { get; }
        event EventHandler<LanguageChangedEventArgs> LanguageChanged;
        Task InitializeAsync();
        Task<bool> SetLanguageAsync(SupportedLanguage language);
        Task<bool> LoadLanguagePackAsync(SupportedLanguage language);
        string GetString(string key, params object[] args);
        string GetString(string key, CultureInfo culture, params object[] args);
        bool HasTranslation(string key);
        Task<bool> LoadCustomLanguagePackAsync(string filePath);
        Task<bool> ExportLanguagePackAsync(SupportedLanguage language, string filePath);
        Task<ObservableCollection<SupportedLanguage>> GetAvailableLanguagesAsync();
        Task<Dictionary<string, string>> GetMissingTranslationsAsync(SupportedLanguage language);
        double GetTranslationCompletionPercentage(SupportedLanguage language);
    }

    public class LanguageChangedEventArgs : EventArgs
    {
        public SupportedLanguage PreviousLanguage { get; set; }
        public SupportedLanguage NewLanguage { get; set; }
    }

    [SupportedOSPlatform("windows")]
    public class LocalizationService : ObservableObject, ILocalizationService
    {
        private readonly ILogger<LocalizationService> _logger;
        private readonly string _languagesDirectory;
        private readonly string _resourcesDirectory;
        private readonly object _lock = new();
        
        private SupportedLanguage _currentLanguage = SupportedLanguage.English;
        private readonly Dictionary<string, string> _translations = new();
        private readonly Dictionary<SupportedLanguage, Dictionary<string, string>> _languagePacks = new();
        private readonly Dictionary<SupportedLanguage, LanguagePack> _languagePacksInfo = new();
        private bool _isInitialized = false;

        public SupportedLanguage CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (SetProperty(ref _currentLanguage, value))
                {
                    OnLanguageChanged(value);
                }
            }
        }

        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        public LocalizationService(ILogger<LocalizationService> logger)
        {
            _logger = logger;
            
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _languagesDirectory = Path.Combine(appData, "WinCleaner", "Languages");
            _resourcesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Languages");
            
            Directory.CreateDirectory(_languagesDirectory);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // Load built-in English (default)
                await LoadBuiltinLanguagePackAsync();
                
                // Try to load user's preferred language
                var settings = await LoadSettingsAsync();
                if (settings.UseSystemLanguage)
                {
                    var systemLang = DetectSystemLanguage();
                    await SetLanguageAsync(systemLang);
                }
                else
                {
                    await SetLanguageAsync(settings.CurrentLanguage);
                }

                _isInitialized = true;
                _logger.LogInformation("Localization service initialized with language: {Language}", CurrentLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize localization service");
                // Fallback to English
                CurrentLanguage = SupportedLanguage.English;
                _isInitialized = true;
            }
        }

        public async Task<bool> SetLanguageAsync(SupportedLanguage language)
        {
            if (language == CurrentLanguage) return true;

            var previousLanguage = CurrentLanguage;
            
            try
            {
                var success = await LoadLanguagePackAsync(language);
                if (success)
                {
                    var previous = CurrentLanguage;
                    CurrentLanguage = language;
                    
                    // Save setting
                    await SaveLanguageSettingAsync(language);
                    
                    LanguageChanged?.Invoke(this, new LanguageChangedEventArgs
                    {
                        PreviousLanguage = previous,
                        NewLanguage = language
                    });
                    
                    _logger.LogInformation("Language changed from {Previous} to {Current}", previous, language);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set language to {Language}", language);
            }
            
            return false;
        }

        public async Task<bool> LoadLanguagePackAsync(SupportedLanguage language)
        {
            lock (_lock)
            {
                // Check if already loaded
                if (_languagePacks.ContainsKey(language))
                {
                    ApplyTranslations(language);
                    return true;
                }
            }

            try
            {
                // Try to load from user directory first
                var userPackPath = Path.Combine(_languagesDirectory, $"{language}.json");
                if (File.Exists(userPackPath))
                {
                    var json = await File.ReadAllTextAsync(userPackPath);
                    var pack = JsonSerializer.Deserialize<LanguagePack>(json, GetJsonOptions());
                    if (pack != null)
                    {
                        lock (_lock)
                        {
                            _languagePacks[language] = pack.Translations;
                            _languagePacksInfo[language] = pack;
                        }
                        ApplyTranslations(language);
                        return true;
                    }
                }

                // Try to load from resources
                var resourcePack = await LoadBuiltinLanguagePackAsync(language);
                if (resourcePack != null)
                {
                    lock (_lock)
                    {
                        _languagePacks[language] = resourcePack.Translations;
                        _languagePacksInfo[language] = resourcePack;
                    }
                    ApplyTranslations(language);
                    return true;
                }

                _logger.LogWarning("Language pack not found for {Language}", language);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load language pack for {Language}", language);
                return false;
            }
        }

        private async Task<LanguagePack?> LoadBuiltinLanguagePackAsync(SupportedLanguage language = SupportedLanguage.English)
        {
            try
            {
                // Try to load from resources directory
                var resourcePath = Path.Combine(_resourcesDirectory, $"{language}.json");
                if (File.Exists(resourcePath))
                {
                    var json = await File.ReadAllTextAsync(resourcePath);
                    return JsonSerializer.Deserialize<LanguagePack>(json, GetJsonOptions());
                }

                // Try embedded resource
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"WinCleaner.Resources.Languages.{language}.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();
                    return JsonSerializer.Deserialize<LanguagePack>(json, GetJsonOptions());
                }

                // Fallback: create minimal English pack
                if (language == SupportedLanguage.English)
                {
                    return CreateDefaultEnglishPack();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load builtin language pack for {Language}", language);
            }
            return null;
        }

        private LanguagePack CreateDefaultEnglishPack()
        {
            return new LanguagePack
            {
                Language = SupportedLanguage.English,
                DisplayName = "English",
                NativeName = "English",
                CultureCode = "en-US",
                Version = "1.0.0",
                Author = "WinCleaner Team",
                IsComplete = true,
                CompletionPercentage = 100,
                Translations = new Dictionary<string, string>
                {
                    ["App.Title"] = "WinCleaner - Professional System Cleaner",
                    ["App.Description"] = "Professional System Cleaner for Windows",
                    ["Button.Scan"] = "Scan",
                    ["Button.Clean"] = "Clean",
                    ["Button.Cancel"] = "Cancel",
                    ["Button.SelectAll"] = "Select All",
                    ["Button.DeselectAll"] = "Deselect All",
                    ["Button.SelectSafe"] = "Select Safe",
                    ["Button.ExportLog"] = "Export Log",
                    ["Button.ClearLog"] = "Clear Log",
                    ["Button.OpenSettings"] = "Settings",
                    ["Button.RefreshDrives"] = "Refresh",
                    ["Label.Profile"] = "Profile",
                    ["Label.DryRun"] = "Dry Run",
                    ["Label.Filter"] = "Search",
                    ["Label.AutoScroll"] = "Auto Scroll",
                    ["Status.Ready"] = "Ready",
                    ["Status.Scanning"] = "Scanning...",
                    ["Status.Cleaning"] = "Cleaning...",
                    ["Status.Completed"] = "Completed",
                    ["Status.Cancelled"] = "Cancelled",
                    ["Status.Error"] = "Error",
                    ["Log.Level.Debug"] = "Debug",
                    ["Log.Level.Info"] = "Info",
                    ["Log.Level.Warning"] = "Warning",
                    ["Log.Level.Error"] = "Error",
                    ["Log.Level.Success"] = "Success",
                    ["StatusBar.Items"] = "Items: {0} | Selected: {1} | Size: {2}",
                    ["StatusBar.User"] = "User: {0} | {1}",
                    ["Menu.File"] = "File",
                    ["Menu.Edit"] = "Edit",
                    ["Menu.View"] = "View",
                    ["Menu.Tools"] = "Tools",
                    ["Menu.Help"] = "Help",
                    ["Menu.Language"] = "Language",
                    ["Menu.Settings"] = "Settings",
                    ["Menu.Exit"] = "Exit",
                    ["Message.ScanComplete"] = "Scan complete: {0} items, {1} can be cleaned",
                    ["Message.CleanComplete"] = "Clean complete: {0}/{1} items cleaned, {2} freed",
                    ["Message.CleanFailed"] = "Clean failed: {0} items failed",
                    ["Message.ConfirmClean"] = "About to clean {0} items, freeing {1}. Continue?",
                    ["Message.NoItemsSelected"] = "No items selected for cleaning",
                    ["Message.ScanFirst"] = "Please run a scan first",
                    ["Message.AdminRequired"] = "Administrator privileges required for this operation",
                    ["Message.ExtensionLoaded"] = "Extension loaded: {0}",
                    ["Message.ExtensionUnloaded"] = "Extension unloaded: {0}",
                    ["Message.ExtensionError"] = "Extension error: {0}",
                    ["Settings.Title"] = "Settings",
                    ["Settings.General"] = "General",
                    ["Settings.Advanced"] = "Advanced",
                    ["Settings.Language"] = "Language",
                    ["Settings.Theme"] = "Theme",
                    ["Settings.AutoScan"] = "Auto scan on startup",
                    ["Settings.AutoClean"] = "Auto clean on startup",
                    ["Settings.CreateRestorePoint"] = "Create restore point before cleaning",
                    ["Settings.ConfirmBeforeDelete"] = "Confirm before deleting",
                    ["Settings.DryRunDefault"] = "Default to dry-run mode",
                    ["Settings.LogLevel"] = "Log level",
                    ["Settings.LogRetention"] = "Log retention (days)",
                    ["Settings.ExportSettings"] = "Export settings",
                    ["Settings.ImportSettings"] = "Import settings",
                    ["Settings.ResetToDefaults"] = "Reset to defaults",
                    ["Profile.Safe"] = "Safe (Recommended)",
                    ["Profile.Deep"] = "Deep (Advanced)",
                    ["Profile.Custom"] = "Custom",
                    ["Profile.Nuclear"] = "Nuclear (Expert Only)",
                    ["Risk.Safe"] = "Safe",
                    ["Risk.Low"] = "Low",
                    ["Risk.Medium"] = "Medium",
                    ["Risk.High"] = "High",
                    ["Risk.Critical"] = "Critical",
                    ["Category.SystemTemp"] = "System Temp",
                    ["Category.WindowsUpdate"] = "Windows Update",
                    ["Category.BrowserCache"] = "Browser Cache",
                    ["Category.DevToolsCache"] = "Developer Tools Cache",
                    ["Category.UserTemp"] = "User Temp",
                    ["Category.RecycleBin"] = "Recycle Bin",
                    ["Category.SystemLogs"] = "System Logs",
                    ["Category.DriverStore"] = "Driver Store",
                    ["Category.DiscordOldVersions"] = "Discord Old Versions",
                    ["Category.PlaywrightBrowsers"] = "Playwright Browsers",
                    ["Category.MinecraftTemp"] = "Minecraft Temp",
                    ["Category.UpdaterCaches"] = "Updater Caches",
                    ["Category.GameData"] = "Game Data",
                    ["Category.UserPrograms"] = "User Programs",
                    ["Category.OtherUsers"] = "Other Users",
                    ["Category.CompactOS"] = "Compact OS",
                    ["Category.Hibernation"] = "Hibernation",
                    ["Category.SystemRestore"] = "System Restore",
                    ["Category.WindowsOld"] = "Windows.old"
                }
            };
        }

        private void ApplyTranslations(SupportedLanguage language)
        {
            if (_languagePacks.TryGetValue(language, out var translations))
            {
                lock (_lock)
                {
                    _translations.Clear();
                    foreach (var kvp in translations)
                    {
                        _translations[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        public string GetString(string key, params object[] args)
        {
            return GetString(key, CultureInfo.CurrentCulture, args);
        }

        public string GetString(string key, CultureInfo culture, params object[] args)
        {
            lock (_lock)
            {
                if (_translations.TryGetValue(key, out var translation))
                {
                    try
                    {
                        return args.Length > 0 ? string.Format(culture, translation, args) : translation;
                    }
                    catch
                    {
                        return translation;
                    }
                }
                
                // Fallback to English if not found
                if (CurrentLanguage != SupportedLanguage.English && 
                    _languagePacks.TryGetValue(SupportedLanguage.English, out var englishTranslations) &&
                    englishTranslations.TryGetValue(key, out var englishTranslation))
                {
                    try
                    {
                        return args.Length > 0 ? string.Format(culture, englishTranslation, args) : englishTranslation;
                    }
                    catch
                    {
                        return englishTranslation;
                    }
                }

                return key; // Return key if no translation found
            }
        }

        public bool HasTranslation(string key)
        {
            lock (_lock)
            {
                return _translations.ContainsKey(key);
            }
        }

        public async Task<bool> LoadCustomLanguagePackAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var pack = JsonSerializer.Deserialize<LanguagePack>(json, GetJsonOptions());
                
                if (pack != null)
                {
                    lock (_lock)
                    {
                        _languagePacks[pack.Language] = pack.Translations;
                        _languagePacksInfo[pack.Language] = pack;
                    }
                    
                    // Save to user languages directory
                    var destPath = Path.Combine(_languagesDirectory, $"{pack.Language}.json");
                    var serializedJson = JsonSerializer.Serialize(pack, GetJsonOptions());
                    await File.WriteAllTextAsync(destPath, serializedJson);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load custom language pack from {Path}", filePath);
            }
            
            return false;
        }

        public async Task<bool> ExportLanguagePackAsync(SupportedLanguage language, string filePath)
        {
            lock (_lock)
            {
                if (!_languagePacksInfo.TryGetValue(language, out var pack))
                    return false;

                try
                {
                    var exportJson = JsonSerializer.Serialize(pack, GetJsonOptions());
                    File.WriteAllText(filePath, exportJson);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to export language pack for {Language}", language);
                    return false;
                }
            }
        }

        public async Task<ObservableCollection<SupportedLanguage>> GetAvailableLanguagesAsync()
        {
            var languages = new ObservableCollection<SupportedLanguage>();
            
            // Add built-in languages
            var builtInLanguages = Enum.GetValues<SupportedLanguage>();
            foreach (var lang in builtInLanguages)
            {
                languages.Add(lang);
            }

            // Add custom languages from user directory
            if (Directory.Exists(_languagesDirectory))
            {
                var files = Directory.GetFiles(_languagesDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var pack = JsonSerializer.Deserialize<LanguagePack>(json, GetJsonOptions());
                        if (pack != null && !languages.Contains(pack.Language))
                        {
                            languages.Add(pack.Language);
                        }
                    }
                    catch { }
                }
            }

            return languages;
        }

        public async Task<Dictionary<string, string>> GetMissingTranslationsAsync(SupportedLanguage language)
        {
            var missing = new Dictionary<string, string>();
            
            if (!_languagePacksInfo.TryGetValue(SupportedLanguage.English, out var englishPack))
                return missing;

            if (!_languagePacksInfo.TryGetValue(language, out var targetPack))
                return missing;

            foreach (var kvp in englishPack.Translations)
            {
                if (!targetPack.Translations.ContainsKey(kvp.Key))
                {
                    missing[kvp.Key] = kvp.Value;
                }
            }

            return missing;
        }

        public double GetTranslationCompletionPercentage(SupportedLanguage language)
        {
            if (!_languagePacksInfo.TryGetValue(SupportedLanguage.English, out var englishPack))
                return 0;

            if (!_languagePacksInfo.TryGetValue(language, out var targetPack))
                return 0;

            var total = englishPack.Translations.Count;
            var translated = targetPack.Translations.Count;
            
            return total > 0 ? (double)translated / total * 100 : 0;
        }

        private void OnLanguageChanged(SupportedLanguage newLanguage)
        {
            LanguageChanged?.Invoke(this, new LanguageChangedEventArgs
            {
                PreviousLanguage = CurrentLanguage,
                NewLanguage = newLanguage
            });
        }

        private async Task<LocalizationSettings> LoadSettingsAsync()
        {
            var settingsPath = Path.Combine(_languagesDirectory, "settings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    return JsonSerializer.Deserialize<LocalizationSettings>(json, GetJsonOptions()) ?? new LocalizationSettings();
                }
                catch { }
            }
            return new LocalizationSettings();
        }

        private async Task SaveLanguageSettingAsync(SupportedLanguage language)
        {
            var settings = await LoadSettingsAsync();
            settings.CurrentLanguage = language;
            settings.UseSystemLanguage = false;
            
            var settingsPath = Path.Combine(_languagesDirectory, "settings.json");
            var json = JsonSerializer.Serialize(settings, GetJsonOptions());
            await File.WriteAllTextAsync(settingsPath, json);
        }

        private SupportedLanguage DetectSystemLanguage()
        {
            try
            {
                var culture = CultureInfo.CurrentUICulture;
                return culture.Name switch
                {
                    "vi-VN" or "vi" => SupportedLanguage.Vietnamese,
                    "zh-CN" or "zh-Hans" => SupportedLanguage.ChineseSimplified,
                    "zh-TW" or "zh-Hant" => SupportedLanguage.ChineseTraditional,
                    "ja-JP" or "ja" => SupportedLanguage.Japanese,
                    "ko-KR" or "ko" => SupportedLanguage.Korean,
                    "fr-FR" or "fr" => SupportedLanguage.French,
                    "de-DE" or "de" => SupportedLanguage.German,
                    "es-ES" or "es" => SupportedLanguage.Spanish,
                    "ru-RU" or "ru" => SupportedLanguage.Russian,
                    "pt-BR" or "pt" => SupportedLanguage.Portuguese,
                    "it-IT" or "it" => SupportedLanguage.Italian,
                    "nl-NL" or "nl" => SupportedLanguage.Dutch,
                    "pl-PL" or "pl" => SupportedLanguage.Polish,
                    "tr-TR" or "tr" => SupportedLanguage.Turkish,
                    "ar-SA" or "ar" => SupportedLanguage.Arabic,
                    "he-IL" or "he" => SupportedLanguage.Hebrew,
                    "hi-IN" or "hi" => SupportedLanguage.Hindi,
                    "th-TH" or "th" => SupportedLanguage.Thai,
                    "id-ID" or "id" => SupportedLanguage.Indonesian,
                    _ => SupportedLanguage.English
                };
            }
            catch
            {
                return SupportedLanguage.English;
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
    }
}