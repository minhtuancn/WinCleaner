using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.Versioning;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IWindowStateService
    {
        Task<WindowStateData> LoadStateAsync();
        Task SaveStateAsync(WindowStateData state);
        Task<WindowStateData> GetDefaultStateAsync();
        Task<bool> ResetToDefaultAsync();
    }

    [SupportedOSPlatform("windows")]
    public class WindowStateService : IWindowStateService
    {
        private readonly string _stateFilePath;
        private readonly object _lock = new();

        public WindowStateService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string stateDir = Path.Combine(appData, "WinCleaner", "WindowState");
            Directory.CreateDirectory(stateDir);
            _stateFilePath = Path.Combine(stateDir, "window_state.json");
        }

        public async Task<WindowStateData> LoadStateAsync()
        {
            if (!File.Exists(_stateFilePath))
                return await GetDefaultStateAsync();

            try
            {
                string json = await File.ReadAllTextAsync(_stateFilePath);
                var state = JsonSerializer.Deserialize<WindowStateData>(json, GetJsonOptions());
                return state ?? await GetDefaultStateAsync();
            }
            catch
            {
                return await GetDefaultStateAsync();
            }
        }

        public async Task SaveStateAsync(WindowStateData state)
        {
            if (state == null) return;

            try
            {
                var json = JsonSerializer.Serialize(state, GetJsonOptions());
                await File.WriteAllTextAsync(_stateFilePath, json);
            }
            catch { }
        }

        public async Task<WindowStateData> GetDefaultStateAsync()
        {
            return new WindowStateData
            {
                Width = 1400,
                Height = 900,
                Left = 100,
                Top = 100,
                WindowState = WindowState.Normal,
                IsMaximized = false,
                SelectedProfile = CleanProfile.Safe,
                DryRunMode = false,
                AutoScrollLog = true,
                TreeViewExpandedStates = new Dictionary<string, bool>(),
                ColumnWidths = new Dictionary<string, double>(),
                LastActiveTab = "Cleaner",
                SplitterDistance = 1050,
                LogScrollPosition = 0,
                LastUpdated = DateTime.Now
            };
        }

        public async Task<bool> ResetToDefaultAsync()
        {
            try
            {
                var defaultState = await GetDefaultStateAsync();
                await SaveStateAsync(defaultState);
                return true;
            }
            catch { return false; }
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

    public class WindowStateData
    {
        public double Width { get; set; } = 1400;
        public double Height { get; set; } = 900;
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public WindowState WindowState { get; set; } = WindowState.Normal;
        public bool IsMaximized { get; set; } = false;
        public CleanProfile SelectedProfile { get; set; } = CleanProfile.Safe;
        public bool DryRunMode { get; set; } = false;
        public bool AutoScrollLog { get; set; } = true;
        public Dictionary<string, bool> TreeViewExpandedStates { get; set; } = new();
        public Dictionary<string, double> ColumnWidths { get; set; } = new();
        public string LastActiveTab { get; set; } = "Cleaner";
        public double SplitterDistance { get; set; } = 1050;
        public double LogScrollPosition { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}