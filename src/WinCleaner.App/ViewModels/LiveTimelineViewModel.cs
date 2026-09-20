using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinCleaner.Core.Services;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.ViewModels
{
    /// <summary>
    /// Live operation timeline - real-time progress with stages, items, and cancellation.
    /// Per #22: shows current stage, elapsed, warnings, items, size; allows cancel; throttled UI updates.
    /// </summary>
    public sealed partial class LiveTimelineViewModel : BaseViewModel
    {
        private readonly ISystemScanner _scanner;
        private readonly ICleanupPlanService _planService;
        private readonly IResilienceService _resilience;
        private readonly IAppRunningGuard _appGuard;
        private readonly IPathSafetyValidator _pathValidator;
        private readonly IResourceService _resourceService;
        private readonly System.Timers.Timer _uiThrottleTimer;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _currentStage = "Sẵn sàng";

        [ObservableProperty]
        private TimeSpan _elapsedTime;

        [ObservableProperty]
        private long _itemsFound;

        [ObservableProperty]
        private long _totalSizeBytes;

        [ObservableProperty]
        private int _warningsCount;

        [ObservableProperty]
        private double _progressPercent;

        [ObservableProperty]
        private ObservableCollection<TimelineEvent> _events = new();

        [ObservableProperty]
        private ObservableCollection<CleanCategoryGroup> _groups = new();

        private readonly string[] _eventTypeFilterOptions = new[] { "All", "Debug", "Info", "Success", "Warning", "Error", "Cancel" };

        public string[] EventTypeFilterOptions => _eventTypeFilterOptions;

        private Stopwatch _stopwatch = new();
        private CancellationTokenSource _cts;
        private DateTime _lastUiUpdate = DateTime.MinValue;
        private const int UI_UPDATE_THROTTLE_MS = 150; // 100-250ms per spec

        public string ElapsedFormatted => ElapsedTime.ToString(@"hh\:mm\:ss");
        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);
        public bool CanCancel => IsRunning;

        public LiveTimelineViewModel(
            ISystemScanner scanner,
            ICleanupPlanService planService,
            IResilienceService resilience,
            IAppRunningGuard appGuard,
            IPathSafetyValidator pathValidator,
            IResourceService resourceService)
        {
            _scanner = scanner;
            _planService = planService;
            _resilience = resilience;
            _appGuard = appGuard;
            _pathValidator = pathValidator;
            _resourceService = resourceService;

            _uiThrottleTimer = new System.Timers.Timer(UI_UPDATE_THROTTLE_MS);
            _uiThrottleTimer.Elapsed += (s, e) => FlushPendingUpdates();
            _uiThrottleTimer.AutoReset = true;
            
            CurrentStage = _resourceService.GetString("LiveTimeline.Ready");
        }

        [RelayCommand]
        private async Task StartScanAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            CurrentStage = _resourceService.GetString("LiveTimeline.InitializingScan");
            ProgressPercent = 0;
            ItemsFound = 0;
            TotalSizeBytes = 0;
            WarningsCount = 0;
            Events.Clear();
            _stopwatch.Restart();
            _cts = new CancellationTokenSource();
            _uiThrottleTimer.Start();

            AddEvent("Scan", _resourceService.GetString("LiveTimeline.ScanStarted"), TimelineEventType.Info);

            try
            {
                var groups = await _scanner.ScanAsync(CleanProfile.Safe, 
                    new Progress<string>(msg => SafeUpdateStage(msg)), 
                    _cts.Token);

                if (_cts.Token.IsCancellationRequested) return;

                Groups = new ObservableCollection<CleanCategoryGroup>(groups);
                
                CurrentStage = _resourceService.GetString("LiveTimeline.SafetyAnalysis");
                AddEvent("Safety", _resourceService.GetString("LiveTimeline.CheckingPaths"), TimelineEventType.Info);

                // Create and validate plan
                var selectedItems = groups.SelectMany(g => g.Items.Where(i => i.IsSelected)).ToList();
                var plan = await _planService.CreatePlanAsync(groups, CleanProfile.Safe);
                plan = await _planService.ValidatePlanAsync(plan, _cts.Token);

                WarningsCount = plan.ValidationWarnings.Count;
                foreach (var warning in plan.ValidationWarnings)
                {
                    AddEvent("Warning", warning, TimelineEventType.Warning);
                }

                if (!plan.IsValid)
                {
                    CurrentStage = _resourceService.GetString("LiveTimeline.ValidationError");
                    AddEvent("Error", _resourceService.GetString("LiveTimeline.InvalidPlan", string.Join("; ", plan.ValidationErrors)), TimelineEventType.Error);
                    return;
                }

                CurrentStage = _resourceService.GetString("LiveTimeline.CleanupStarted");
                AddEvent("Clean", _resourceService.GetString("LiveTimeline.CleanupStarted"), TimelineEventType.Info);

                var result = await _planService.ExecutePlanAsync(plan,
                    new Progress<string>(msg => SafeUpdateStage(msg)),
                    new Progress<LogEntry>(e => AddEvent(e.Source, e.Message, MapLogLevel(e.Level))),
                    _cts.Token);

                CurrentStage = result.OverallStatus == CleanupOverallStatus.Completed 
                    ? _resourceService.GetString("LiveTimeline.Completed") 
                    : result.OverallStatus == CleanupOverallStatus.Cancelled 
                        ? _resourceService.GetString("LiveTimeline.Cancelled") 
                        : _resourceService.GetString("LiveTimeline.CompletedWithWarnings");

                AddEvent("Complete", $"Dọn dẹp xong: {result.SuccessfulOperations}/{result.TotalSteps} thành công, {result.TotalCleanedFormatted} giải phóng", 
                    result.OverallStatus == CleanupOverallStatus.Completed ? TimelineEventType.Success : TimelineEventType.Warning);
            }
            catch (OperationCanceledException)
            {
                CurrentStage = _resourceService.GetString("LiveTimeline.CancelledByUser");
                AddEvent("Cancel", _resourceService.GetString("LiveTimeline.CleanupCancelled"), TimelineEventType.Warning);
            }
            catch (Exception ex)
            {
                CurrentStage = _resourceService.GetString("LiveTimeline.Error");
                AddEvent("Error", _resourceService.GetString("LiveTimeline.UnexpectedError", ex.Message), TimelineEventType.Error);
            }
            finally
            {
                _stopwatch.Stop();
                _uiThrottleTimer.Stop();
                IsRunning = false;
                FlushPendingUpdates();
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
            CurrentStage = _resourceService.GetString("LiveTimeline.Cancelling");
            AddEvent("Cancel", _resourceService.GetString("LiveTimeline.CancelRequested"), TimelineEventType.Warning);
        }

        [RelayCommand]
        private void ClearEvents()
        {
            Events.Clear();
            WarningsCount = 0;
        }

        private void SafeUpdateStage(string message)
        {
            _pendingStage = message;
            RequestUiUpdate();
        }

        private string? _pendingStage;
        private long _pendingItemsFound;
        private long _pendingTotalSize;

        private void RequestUiUpdate()
        {
            var now = DateTime.Now;
            if ((now - _lastUiUpdate).TotalMilliseconds >= UI_UPDATE_THROTTLE_MS)
            {
                FlushPendingUpdates();
            }
        }

        private void FlushPendingUpdates()
        {
            if (!string.IsNullOrEmpty(_pendingStage))
            {
                CurrentStage = _pendingStage;
                _pendingStage = null;
            }
            if (_pendingItemsFound > 0)
            {
                ItemsFound = _pendingItemsFound;
                _pendingItemsFound = 0;
            }
            if (_pendingTotalSize > 0)
            {
                TotalSizeBytes = _pendingTotalSize;
                _pendingTotalSize = 0;
            }

            ElapsedTime = _stopwatch.Elapsed;
            _lastUiUpdate = DateTime.Now;
        }

        private void AddEvent(string source, string message, TimelineEventType type)
        {
            var evt = new TimelineEvent
            {
                Timestamp = DateTime.Now,
                Source = source,
                Message = message,
                Type = type
            };
            Events.Insert(0, evt); // Newest first
        }

        private TimelineEventType MapLogLevel(Models.LogLevel level)
        {
            return level switch
            {
                Models.LogLevel.Debug => TimelineEventType.Debug,
                Models.LogLevel.Info => TimelineEventType.Info,
                Models.LogLevel.Success => TimelineEventType.Success,
                Models.LogLevel.Warning => TimelineEventType.Warning,
                Models.LogLevel.Error => TimelineEventType.Error,
                _ => TimelineEventType.Info
            };
        }

        public void Dispose()
        {
            _uiThrottleTimer?.Stop();
            _uiThrottleTimer?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            _stopwatch.Stop();
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;
            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }
            return $"{dblBytes:0.##} {suffixes[i]}";
        }
    }

    public sealed class TimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = "";
        public string Message { get; set; } = "";
        public TimelineEventType Type { get; set; }
        public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
    }
}