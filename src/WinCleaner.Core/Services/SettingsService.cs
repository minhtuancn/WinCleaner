using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ISettingsService
    {
        Task<AppSettings> LoadAsync();
        Task SaveAsync(AppSettings settings);
        AppSettings GetDefaults();
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "WinCleaner");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        public async Task<AppSettings> LoadAsync()
        {
            if (!File.Exists(_settingsPath))
                return GetDefaults();

            try
            {
                string json = await File.ReadAllTextAsync(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, GetJsonOptions());
                return settings ?? GetDefaults();
            }
            catch
            {
                return GetDefaults();
            }
        }

        public async Task SaveAsync(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, GetJsonOptions());
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public AppSettings GetDefaults()
        {
            return new AppSettings
            {
                SelectedProfile = CleanProfile.Safe,
                DryRunMode = false,
                AutoCloseAfterClean = false,
                MinimizeToTray = true,
                ShowHiddenFiles = false,
                ConfirmBeforeDelete = true,
                MaxLogEntries = 10000,
                Theme = "System",
                Language = "vi-VN",
                ExcludedPaths = new List<string>(),
                CustomPaths = new List<string>()
            };
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