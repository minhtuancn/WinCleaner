using System;

namespace WinCleaner.Models
{
    // ===== Resilience / Operation Result Models (Issue #21) =====

    /// <summary>
    /// Normalized operation result for a single candidate/rule execution.
    /// Replaces raw exception propagation with structured, classifiable results.
    /// </summary>
    public enum CleanupOperationStatus
    {
        Success,
        Skipped,
        AccessDenied,
        Locked,
        NotFound,
        ChangedSinceScan,
        IoError,
        DeviceUnavailable,
        Timeout,
        Cancelled,
        UnsafePath,
        RequiresElevation,
        Unsupported,
        Failed
    }

    /// <summary>
    /// Result of a single cleanup candidate operation.
    /// Contains all information needed for diagnostics and reporting.
    /// </summary>
    public class CleanupOperationResult
    {
        public string OperationId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string CandidateId { get; set; } = "";
        public string RuleId { get; set; } = "";
        public string Path { get; set; } = "";
        public CleanupOperationStatus Status { get; set; }
        public int? HResult { get; set; }
        public int? Win32ErrorCode { get; set; }
        public string ExceptionType { get; set; } = "";
        public string ExceptionMessage { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public string UserSafeMessage { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public bool Retryable { get; set; }
        public bool RequiresElevation { get; set; }
        public CleanCategory Category { get; set; }
        public ItemRiskLevel RiskLevel { get; set; }
        public long SizeBytes { get; set; }

        public bool IsSuccess => Status == CleanupOperationStatus.Success;
        public bool IsRecoverableFailure => Status switch
        {
            CleanupOperationStatus.AccessDenied => true,
            CleanupOperationStatus.Locked => true,
            CleanupOperationStatus.NotFound => true,
            CleanupOperationStatus.ChangedSinceScan => true,
            CleanupOperationStatus.IoError => true,
            CleanupOperationStatus.DeviceUnavailable => true,
            CleanupOperationStatus.Timeout => true,
            CleanupOperationStatus.Cancelled => true,
            _ => false
        };

        /// <summary>
        /// Creates a user-friendly message for the operation result.
        /// </summary>
        public string GetUserMessage()
        {
            if (!string.IsNullOrEmpty(UserSafeMessage))
                return UserSafeMessage;

            return Status switch
            {
                CleanupOperationStatus.Success => "Deleted successfully",
                CleanupOperationStatus.Skipped => "Skipped",
                CleanupOperationStatus.AccessDenied => "Access denied - insufficient permissions",
                CleanupOperationStatus.Locked => "File is in use by another process",
                CleanupOperationStatus.NotFound => "File not found (may have been deleted already)",
                CleanupOperationStatus.ChangedSinceScan => "File changed since analysis - skipped for safety",
                CleanupOperationStatus.IoError => "I/O error reading/writing file",
                CleanupOperationStatus.DeviceUnavailable => "Drive or device unavailable",
                CleanupOperationStatus.Timeout => "Operation timed out",
                CleanupOperationStatus.Cancelled => "Cancelled by user",
                CleanupOperationStatus.UnsafePath => "Path failed safety validation",
                CleanupOperationStatus.RequiresElevation => "Requires administrator privileges",
                CleanupOperationStatus.Unsupported => "Operation not supported on this path",
                CleanupOperationStatus.Failed => "Operation failed",
                _ => "Unknown result"
            };
        }

        /// <summary>
        /// Creates a result from an exception with classification.
        /// </summary>
        public static CleanupOperationResult FromException(Exception ex, string candidateId, string ruleId, string path, CleanCategory category, ItemRiskLevel riskLevel, long sizeBytes)
        {
            var result = new CleanupOperationResult
            {
                CandidateId = candidateId,
                RuleId = ruleId,
                Path = path,
                Category = category,
                RiskLevel = riskLevel,
                SizeBytes = sizeBytes,
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace ?? ""
            };

            // Classify exception to status - use local variables for out params
            int? hResult = null;
            int? win32Error = null;
            bool retryable = false;
            bool requiresElevation = false;
            
            result.Status = ClassifyException(ex, out hResult, out win32Error, out retryable, out requiresElevation);
            result.HResult = hResult;
            result.Win32ErrorCode = win32Error;
            result.Retryable = retryable;
            result.RequiresElevation = requiresElevation;
            result.UserSafeMessage = result.GetUserMessage();

            return result;
        }

        private static CleanupOperationStatus ClassifyException(Exception ex, out int? hResult, out int? win32Error, out bool retryable, out bool requiresElevation)
        {
            hResult = null;
            win32Error = null;
            retryable = false;
            requiresElevation = false;

            // Check HResult for specific Windows errors
            int hr = ex.HResult;
            hResult = hr;
            int win32Err = hr & 0xFFFF;
            if (win32Err != 0) win32Error = win32Err;

            // Access denied
            if (ex is UnauthorizedAccessException || 
                hr == unchecked((int)0x80070005) || // E_ACCESSDENIED
                win32Err == 5) // ERROR_ACCESS_DENIED
            {
                requiresElevation = true;
                return CleanupOperationStatus.AccessDenied;
            }

            // File locked / in use
            if (ex is IOException ioEx)
            {
                // Check for sharing violation
                if (hr == unchecked((int)0x80070020) || // ERROR_SHARING_VIOLATION
                    win32Err == 32 || // ERROR_SHARING_VIOLATION
                    win32Err == 33) // ERROR_LOCK_VIOLATION
                {
                    retryable = true;
                    return CleanupOperationStatus.Locked;
                }

                // Disk full
                if (hr == unchecked((int)0x80070070) || // ERROR_DISK_FULL
                    win32Err == 112)
                {
                    return CleanupOperationStatus.IoError;
                }

                // Device not ready / unavailable
                if (win32Err == 21) // ERROR_NOT_READY
                {
                    return CleanupOperationStatus.DeviceUnavailable;
                }

                retryable = true;
                return CleanupOperationStatus.IoError;
            }

            // File not found
            if (ex is FileNotFoundException || 
                ex is DirectoryNotFoundException ||
                hr == unchecked((int)0x80070002) || // ERROR_FILE_NOT_FOUND
                win32Err == 2 || // ERROR_FILE_NOT_FOUND
                win32Err == 3) // ERROR_PATH_NOT_FOUND
            {
                return CleanupOperationStatus.NotFound;
            }

            // Timeout
            if (ex is TimeoutException || hr == unchecked((int)0x800705B4)) // ERROR_TIMEOUT
            {
                retryable = true;
                return CleanupOperationStatus.Timeout;
            }

            // Operation cancelled
            if (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                return CleanupOperationStatus.Cancelled;
            }

            // Path too long / unsafe
            if (ex is PathTooLongException || hr == unchecked((int)0x800700CE)) // ERROR_FILENAME_EXCED_RANGE
            {
                return CleanupOperationStatus.UnsafePath;
            }

            // Not supported
            if (ex is NotSupportedException || ex is PlatformNotSupportedException)
            {
                return CleanupOperationStatus.Unsupported;
            }

            return CleanupOperationStatus.Failed;
        }
    }

    /// <summary>
    /// Aggregate result for a cleanup plan execution.
    /// Supports partial completion with warnings/errors.
    /// </summary>
    public class CleanupPlanResult
    {
        public string PlanId { get; set; } = Guid.NewGuid().ToString("N")[..12];
        public string OperationType { get; set; } = "Clean";
        public CleanupOverallStatus OverallStatus { get; set; }
        public int TotalCandidates { get; set; }
        public int Succeeded { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public int AccessDenied { get; set; }
        public int Locked { get; set; }
        public int NotFound { get; set; }
        public int ChangedSinceScan { get; set; }
        public int IoError { get; set; }
        public int DeviceUnavailable { get; set; }
        public int Timeout { get; set; }
        public int Cancelled { get; set; }
        public int UnsafePath { get; set; }
        public int RequiresElevation { get; set; }
        public long TotalSizeBytes { get; set; }
        public long FreedSizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime CompletedAt { get; set; }
        public List<CleanupOperationResult> OperationResults { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        public bool IsCompleted => OverallStatus == CleanupOverallStatus.Completed || 
                                   OverallStatus == CleanupOverallStatus.CompletedWithWarnings;

        public void AddResult(CleanupOperationResult result)
        {
            OperationResults.Add(result);
            switch (result.Status)
            {
                case CleanupOperationStatus.Success:
                    Succeeded++;
                    FreedSizeBytes += result.SizeBytes;
                    break;
                case CleanupOperationStatus.Skipped:
                    Skipped++;
                    break;
                case CleanupOperationStatus.AccessDenied:
                    AccessDenied++; Failed++; break;
                case CleanupOperationStatus.Locked:
                    Locked++; Failed++; break;
                case CleanupOperationStatus.NotFound:
                    NotFound++; Skipped++; break;
                case CleanupOperationStatus.ChangedSinceScan:
                    ChangedSinceScan++; Skipped++; break;
                case CleanupOperationStatus.IoError:
                    IoError++; Failed++; break;
                case CleanupOperationStatus.DeviceUnavailable:
                    DeviceUnavailable++; Failed++; break;
                case CleanupOperationStatus.Timeout:
                    Timeout++; Failed++; break;
                case CleanupOperationStatus.Cancelled:
                    Cancelled++; break;
                case CleanupOperationStatus.UnsafePath:
                    UnsafePath++; Failed++; break;
                case CleanupOperationStatus.RequiresElevation:
                    RequiresElevation++; Failed++; break;
                default:
                    Failed++; break;
            }
        }

        public void FinalizeResult()
        {
            CompletedAt = DateTime.UtcNow;
            Duration = CompletedAt - StartedAt;

            if (Cancelled > 0 && Succeeded == 0)
            {
                OverallStatus = CleanupOverallStatus.Cancelled;
            }
            else if (Failed == 0 && (AccessDenied > 0 || Locked > 0 || IoError > 0 || DeviceUnavailable > 0))
            {
                OverallStatus = CleanupOverallStatus.CompletedWithWarnings;
            }
            else if (Failed > 0 && Succeeded > 0)
            {
                OverallStatus = CleanupOverallStatus.PartialFailure;
            }
            else if (Failed > 0 && Succeeded == 0)
            {
                OverallStatus = CleanupOverallStatus.Failed;
            }
            else
            {
                OverallStatus = CleanupOverallStatus.Completed;
            }
        }
    }

    public enum CleanupOverallStatus
    {
        Pending,
        Running,
        Completed,
        CompletedWithWarnings,
        PartialFailure,
        Failed,
        Cancelled
    }
}
