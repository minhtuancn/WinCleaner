using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    /// <summary>
    /// Diagnostics - logs, crash reports, health status, export.
    /// </summary>
    public sealed partial class DiagnosticsViewModel : BaseViewModel
    {
        private readonly IDiagnosticsService _diagnostics;
        private readonly IStructuredLogger _logger;
        private readonly IResilienceService _resilience;

        [ObservableProperty]
        private DiagnosticsSnapshot _snapshot;

        [ObservableProperty]
        private ObservableCollection<StructuredLogEntry> _recentLogs = new();

        [ObservableProperty]
        private ObservableCollection<CrashReportItem> _crashReports = new();

        [ObservableProperty]
        private AppHealthStatus _healthStatus;

        [ObservableProperty]
        private bool _isLoading;

        public DiagnosticsViewModel(IDiagnosticsService diagnostics, IStructuredLogger logger, IResilienceService resilience)
        {
            _diagnostics = diagnostics;
            _logger = logger;
            _resilience = resilience;
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                IsBusy = true;
                LogInfo("Loading diagnostics...");

                Snapshot = _diagnostics.GetCurrentSnapshot();
                HealthStatus = _diagnostics.GetHealthStatus();

                var logs = await _logger.GetRecentLogsAsync(1000);
                RecentLogs = new ObservableCollection<StructuredLogEntry>(logs.OrderByDescending(l => l.Timestamp));

                var crashDiags = _resilience.GetLastCrashDiagnostics();
                CrashReports = new ObservableCollection<CrashReportItem>
                {
                    new CrashReportItem
                    {
                        Time = crashDiags.CrashTime,
                        ExceptionType = crashDiags.ExceptionType,
                        Message = crashDiags.ExceptionMessage,
                        Context = crashDiags.Context,
                        StackTrace = crashDiags.StackTrace
                    }
                }.Where(c => !string.IsNullOrEmpty(c.ExceptionType)).ToObservableCollection();

                LogSuccess("Diagnostics loaded");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load diagnostics: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportDiagnosticsAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = $"WinCleaner_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsBusy = true;
                    var path = await _diagnostics.ExportDiagnosticsAsync(dialog.FileName);
                    LogSuccess($"Diagnostics exported to {path}");
                }
                catch (Exception ex)
                {
                    LogError($"Export failed: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task ExportLogsAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Lines (*.jsonl)|*.jsonl|Text Files (*.txt)|*.txt",
                FileName = $"WinCleaner_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsBusy = true;
                    var path = await _diagnostics.ExportSessionLogsAsync(10000, dialog.FileName);
                    LogSuccess($"Logs exported to {path}");
                }
                catch (Exception ex)
                {
                    LogError($"Export failed: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            try
            {
                _diagnostics.OpenLogFolder();
            }
            catch (Exception ex)
            {
                LogError($"Failed to open log folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OpenCrashFolder()
        {
            try
            {
                _diagnostics.OpenCrashReportFolder();
            }
            catch (Exception ex)
            {
                LogError($"Failed to open crash folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            _logger.Clear();
            RecentLogs.Clear();
            LogInfo("Logs cleared");
        }
    }

    public sealed class CrashReportItem : ObservableObject
    {
        public DateTime Time { get; set; }
        public string ExceptionType { get; set; } = "";
        public string Message { get; set; } = "";
        public string Context { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public string FormattedTime => Time.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// Extension helper
public static class ObservableCollectionExtensions
{
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ObservableCollection<T>(source);
    }
}