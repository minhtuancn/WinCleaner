using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum PathSafetyLevel
    {
        Safe,
        Caution,
        Protected,
        Critical
    }

    public enum AppRunningAction
    {
        Skip,
        Warn,
        RequireClose,
        ForceClose
    }

    public class PathSafetyResult
    {
        public string Path { get; set; } = "";
        public PathSafetyLevel SafetyLevel { get; set; }
        public List<string> Reasons { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsSafeToDelete => SafetyLevel == PathSafetyLevel.Safe;
        public bool RequiresConfirmation => SafetyLevel >= PathSafetyLevel.Caution;
    }

    public class CleanupPlanStep : ObservableObject
    {
        private CleanupOperationStatus _status = CleanupOperationStatus.Pending;
        private string _userMessage = "";

        public string StepId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string CandidateId { get; set; } = "";
        public string RuleId { get; set; } = "";
        public string Path { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public CleanCategory Category { get; set; }
        public ItemRiskLevel RiskLevel { get; set; }
        public long SizeBytes { get; set; }
        public PathSafetyResult SafetyResult { get; set; } = new();
        public List<string> RunningProcesses { get; set; } = new();
        public AppRunningAction AppAction { get; set; } = AppRunningAction.Warn;
        public bool RequiresAdmin { get; set; }
        public bool RequiresAppClose { get; set; }
        public CleanupOperationStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        public string UserMessage
        {
            get => _userMessage;
            set => SetProperty(ref _userMessage, value);
        }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExecutedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public CleanupOperationResult? ExecutionResult { get; set; }

        private bool _isValidated;
        public bool IsValidated
        {
            get => _isValidated;
            set => SetProperty(ref _isValidated, value);
        }
        public bool IsReadyToExecute => IsValidated && Status == CleanupOperationStatus.Pending && SafetyResult?.IsSafeToDelete == true;
        public bool RequiresUserConfirmation => SafetyResult.RequiresConfirmation || RequiresAppClose;
    }

    public class CleanupPlan : ObservableObject
    {
        private CleanupOverallStatus _overallStatus = CleanupOverallStatus.Pending;
        private int _totalSteps;
        private int _validatedSteps;
        private int _readySteps;
        private long _totalSizeBytes;
        private long _safeSizeBytes;
        private long _cautionSizeBytes;
        private long _protectedSizeBytes;

        public string PlanId { get; set; } = Guid.NewGuid().ToString("N")[..12];
        public string Name { get; set; } = "Cleanup Plan";
        public CleanProfile Profile { get; set; }
        public ObservableCollection<CleanupPlanStep> Steps { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public CleanupOverallStatus OverallStatus
        {
            get => _overallStatus;
            set => SetProperty(ref _overallStatus, value);
        }
        public int TotalSteps
        {
            get => _totalSteps;
            set => SetProperty(ref _totalSteps, value);
        }
        public int ValidatedSteps
        {
            get => _validatedSteps;
            set => SetProperty(ref _validatedSteps, value);
        }
        public int ReadySteps
        {
            get => _readySteps;
            set => SetProperty(ref _readySteps, value);
        }
        public long TotalSizeBytes
        {
            get => _totalSizeBytes;
            set => SetProperty(ref _totalSizeBytes, value);
        }
        public long SafeSizeBytes
        {
            get => _safeSizeBytes;
            set => SetProperty(ref _safeSizeBytes, value);
        }
        public long CautionSizeBytes
        {
            get => _cautionSizeBytes;
            set => SetProperty(ref _cautionSizeBytes, value);
        }
        public long ProtectedSizeBytes
        {
            get => _protectedSizeBytes;
            set => SetProperty(ref _protectedSizeBytes, value);
        }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ValidatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public TimeSpan? TotalDuration { get; set; }

        public bool IsValid => ValidationErrors.Count == 0 && ValidatedSteps > 0;
        public bool HasWarnings => ValidationWarnings.Count > 0;
        public bool HasProtectedItems => ProtectedSizeBytes > 0;
        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);
        public string SafeSizeFormatted => FormatBytes(SafeSizeBytes);
        public string CautionSizeFormatted => FormatBytes(CautionSizeBytes);
        public string ProtectedSizeFormatted => FormatBytes(ProtectedSizeBytes);

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

    public class CleanupExecutionResult
    {
        public string PlanId { get; set; } = "";
        public CleanupOverallStatus OverallStatus { get; set; }
        public List<CleanupOperationResult> OperationResults { get; set; } = new();
        public long TotalCleanedBytes { get; set; }
        public int TotalSteps { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public int SkippedOperations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public bool WasCancelled { get; set; }
        public string? RollbackToken { get; set; }

        public string TotalCleanedFormatted => FormatBytes(TotalCleanedBytes);

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
}