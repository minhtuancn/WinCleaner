using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    /// <summary>
    /// Resilience service providing fault isolation, crash protection, and diagnostics.
    /// Implements the production resilience contract from Issue #21.
    /// </summary>
    public interface IResilienceService
    {
        /// <summary>
        /// Execute an operation with fault isolation - failures are captured as structured results.
        /// </summary>
        Task<CleanupOperationResult> ExecuteWithIsolationAsync(
            Func<CancellationToken, Task> operation,
            string candidateId,
            string ruleId,
            string path,
            CleanCategory category,
            ItemRiskLevel riskLevel,
            long sizeBytes,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute with retry policy for transient failures.
        /// </summary>
        Task<CleanupOperationResult> ExecuteWithRetryAsync(
            Func<CancellationToken, Task> operation,
            string candidateId,
            string ruleId,
            string path,
            CleanCategory category,
            ItemRiskLevel riskLevel,
            long sizeBytes,
            int maxRetries = 2,
            TimeSpan? retryDelay = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get crash diagnostics for the last session.
        /// </summary>
        CrashDiagnostics GetLastCrashDiagnostics();

        /// <summary>
        /// Report a fatal crash and generate diagnostic file.
        /// </summary>
        void ReportCrash(Exception ex, string context);

        /// <summary>
        /// Handle an unhandled UI exception (WPF dispatcher). Logs, reports crash and returns true to mark handled.
        /// UI-layer specific hooks live in the infrastructure layer; Core only classifies and logs.
        /// </summary>
        bool HandleUiException(Exception ex, string context);

        /// <summary>
        /// Check if previous session ended unexpectedly.
        /// </summary>
        bool HadUnexpectedShutdown { get; }
    }

    /// <summary>
    /// Crash diagnostics information.
    /// </summary>
    public class CrashDiagnostics
    {
        public DateTime CrashTime { get; set; }
        public string ExceptionType { get; set; } = "";
        public string ExceptionMessage { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public string Context { get; set; } = "";
        public string AppVersion { get; set; } = "";
        public string OSVersion { get; set; } = "";
        public string DotNetVersion { get; set; } = "";
        public string ProcessArchitecture { get; set; } = "";
        public long WorkingSetMemory { get; set; }
        public int ThreadCount { get; set; }
        public Dictionary<string, string> Environment { get; set; } = new();
    }

    [SupportedOSPlatform("windows")]
    public class ResilienceService : IResilienceService
    {
        private readonly ILogger<ResilienceService> _logger;
        private readonly IStructuredLogger _structuredLogger;
        private readonly string _crashReportPath;
        private bool _hadUnexpectedShutdown;
        private readonly object _crashLock = new();

        public bool HadUnexpectedShutdown => _hadUnexpectedShutdown;

        public ResilienceService(ILogger<ResilienceService> logger, IStructuredLogger structuredLogger)
        {
            _logger = logger;
            _structuredLogger = structuredLogger;

            // Setup crash report path
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string crashDir = Path.Combine(localAppData, "WinCleaner", "CrashReports");
            Directory.CreateDirectory(crashDir);
            _crashReportPath = Path.Combine(crashDir, $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

            // Check for previous unexpected shutdown
            CheckPreviousSession();

            // Register global exception handlers
            RegisterGlobalExceptionHandlers();
        }

        private void CheckPreviousSession()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string markerFile = Path.Combine(localAppData, "WinCleaner", ".session-active");

                if (File.Exists(markerFile))
                {
                    _hadUnexpectedShutdown = true;
                    _logger.LogWarning("Previous session ended unexpectedly (session marker found)");
                    
                    // Read previous session info if available
                    try
                    {
                        var content = File.ReadAllText(markerFile);
                        _logger.LogInformation("Previous session info: {Content}", content);
                    }
                    catch { }
                }

                // Create new session marker
                File.WriteAllText(markerFile, JsonSerializer.Serialize(new
                {
                    SessionId = Guid.NewGuid().ToString("N")[..12],
                    StartTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ProcessId = Environment.ProcessId,
                    Version = GetAppVersion()
                }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check/create session marker");
            }
        }

        private void RegisterGlobalExceptionHandlers()
        {
            // AppDomain unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    _logger.LogCritical(ex, "AppDomain unhandled exception (terminating)");
                    _structuredLogger.Log(new StructuredLogEntry
                    {
                        Level = "Critical",
                        Component = "AppDomain",
                        Action = "UnhandledException",
                        Result = "Crash",
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message,
                        StackTrace = ex.StackTrace ?? "",
                        Message = "AppDomain unhandled exception - process terminating"
                    });
                    ReportCrash(ex, "AppDomain.UnhandledException");
                }
            };

            // TaskScheduler unobserved task exceptions
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                _logger.LogError(e.Exception, "Unobserved task exception");
                _structuredLogger.Log(new StructuredLogEntry
                {
                    Level = "Error",
                    Component = "TaskScheduler",
                    Action = "UnobservedTaskException",
                    Result = "Warning",
                    ExceptionType = e.Exception.GetType().Name,
                    ExceptionMessage = e.Exception.Message,
                    StackTrace = e.Exception.StackTrace ?? "",
                    Message = "Unobserved task exception captured"
                });
                e.SetObserved(); // Prevent crash
            };
        }

        public async Task<CleanupOperationResult> ExecuteWithIsolationAsync(
            Func<CancellationToken, Task> operation,
            string candidateId,
            string ruleId,
            string path,
            CleanCategory category,
            ItemRiskLevel riskLevel,
            long sizeBytes,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            var stopwatch = Stopwatch.StartNew();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (timeout.HasValue)
            {
                linkedCts.CancelAfter(timeout.Value);
            }

            _structuredLogger.Log(new StructuredLogEntry
            {
                Level = "Debug",
                OperationId = operationId,
                CandidateId = candidateId,
                RuleId = ruleId,
                Component = "ResilienceService",
                Action = "ExecuteWithIsolation",
                Path = path,
                Result = "Started",
                Message = $"Starting isolated execution for {candidateId}"
            });

            try
            {
                await operation(linkedCts.Token);
                stopwatch.Stop();

                var result = new CleanupOperationResult
                {
                    OperationId = operationId,
                    CandidateId = candidateId,
                    RuleId = ruleId,
                    Path = path,
                    Category = category,
                    RiskLevel = riskLevel,
                    SizeBytes = sizeBytes,
                    Status = CleanupOperationStatus.Success,
                    Duration = stopwatch.Elapsed,
                    UserSafeMessage = "Completed successfully"
                };

                _structuredLogger.Log(new StructuredLogEntry
                {
                    Level = "Info",
                    OperationId = operationId,
                    CandidateId = candidateId,
                    RuleId = ruleId,
                    Component = "ResilienceService",
                    Action = "ExecuteWithIsolation",
                    Path = path,
                    Result = "Success",
                    Duration = stopwatch.Elapsed,
                    SizeBytes = sizeBytes,
                    Message = $"Completed {candidateId} in {stopwatch.Elapsed.TotalMilliseconds:F0}ms"
                });

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                return new CleanupOperationResult
                {
                    OperationId = operationId,
                    CandidateId = candidateId,
                    RuleId = ruleId,
                    Path = path,
                    Category = category,
                    RiskLevel = riskLevel,
                    SizeBytes = sizeBytes,
                    Status = CleanupOperationStatus.Cancelled,
                    Duration = stopwatch.Elapsed,
                    UserSafeMessage = "Operation cancelled"
                };
            }
            catch (OperationCanceledException) when (timeout.HasValue && !cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                return new CleanupOperationResult
                {
                    OperationId = operationId,
                    CandidateId = candidateId,
                    RuleId = ruleId,
                    Path = path,
                    Category = category,
                    RiskLevel = riskLevel,
                    SizeBytes = sizeBytes,
                    Status = CleanupOperationStatus.Timeout,
                    Duration = stopwatch.Elapsed,
                    Retryable = true,
                    UserSafeMessage = $"Operation timed out after {timeout.Value.TotalSeconds}s"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var result = CleanupOperationResult.FromException(ex, candidateId, ruleId, path, category, riskLevel, sizeBytes);
                result.OperationId = operationId;
                result.Duration = stopwatch.Elapsed;

                _structuredLogger.Log(new StructuredLogEntry
                {
                    Level = result.IsRecoverableFailure ? "Warning" : "Error",
                    OperationId = operationId,
                    CandidateId = candidateId,
                    RuleId = ruleId,
                    Component = "ResilienceService",
                    Action = "ExecuteWithIsolation",
                    Path = path,
                    Result = result.Status.ToString(),
                    HResult = result.HResult,
                    Win32ErrorCode = result.Win32ErrorCode,
                    ExceptionType = result.ExceptionType,
                    ExceptionMessage = result.ExceptionMessage,
                    StackTrace = result.StackTrace,
                    Duration = stopwatch.Elapsed,
                    SizeBytes = sizeBytes,
                    Message = $"Failed {candidateId}: {result.Status} - {ex.Message}"
                });

                return result;
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        public async Task<CleanupOperationResult> ExecuteWithRetryAsync(
            Func<CancellationToken, Task> operation,
            string candidateId,
            string ruleId,
            string path,
            CleanCategory category,
            ItemRiskLevel riskLevel,
            long sizeBytes,
            int maxRetries = 2,
            TimeSpan? retryDelay = null,
            CancellationToken cancellationToken = default)
        {
            var delay = retryDelay ?? TimeSpan.FromSeconds(1);
            Exception? lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var result = await ExecuteWithIsolationAsync(
                    operation, candidateId, ruleId, path, category, riskLevel, sizeBytes, cancellationToken: cancellationToken);

                if (result.IsSuccess || !result.Retryable || attempt == maxRetries)
                {
                    if (attempt > 0)
                    {
                        _structuredLogger.Log(new StructuredLogEntry
                        {
                            Level = "Info",
                            CandidateId = candidateId,
                            RuleId = ruleId,
                            Component = "ResilienceService",
                            Action = "Retry",
                            Path = path,
                            Result = result.IsSuccess ? "SuccessAfterRetry" : "FailedAfterRetries",
                            Message = $"Attempt {attempt + 1}/{maxRetries + 1} for {candidateId}"
                        });
                    }
                    return result;
                }

                lastException = new Exception(result.ExceptionMessage) { HResult = result.HResult ?? 0 };

                _structuredLogger.Log(new StructuredLogEntry
                {
                    Level = "Warning",
                    CandidateId = candidateId,
                    RuleId = ruleId,
                    Component = "ResilienceService",
                    Action = "Retry",
                    Path = path,
                    Result = "RetryScheduled",
                    Message = $"Attempt {attempt + 1} failed ({result.Status}), retrying in {delay.TotalSeconds}s..."
                });

                try
                {
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
                }
                catch (OperationCanceledException)
                {
                    return new CleanupOperationResult
                    {
                        CandidateId = candidateId,
                        RuleId = ruleId,
                        Path = path,
                        Category = category,
                        RiskLevel = riskLevel,
                        SizeBytes = sizeBytes,
                        Status = CleanupOperationStatus.Cancelled,
                        UserSafeMessage = "Retry cancelled"
                    };
                }
            }

            return new CleanupOperationResult
            {
                CandidateId = candidateId,
                RuleId = ruleId,
                Path = path,
                Category = category,
                RiskLevel = riskLevel,
                SizeBytes = sizeBytes,
                Status = CleanupOperationStatus.Failed,
                ExceptionType = lastException?.GetType().Name ?? "Exception",
                ExceptionMessage = lastException?.Message ?? "Max retries exceeded",
                UserSafeMessage = $"Failed after {maxRetries + 1} attempts"
            };
        }

        public CrashDiagnostics GetLastCrashDiagnostics()
        {
            try
            {
                if (File.Exists(_crashReportPath))
                {
                    var json = File.ReadAllText(_crashReportPath);
                    return JsonSerializer.Deserialize<CrashDiagnostics>(json) ?? new CrashDiagnostics();
                }
            }
            catch { }

            return new CrashDiagnostics();
        }

        public bool HandleUiException(Exception ex, string context)
        {
            try
            {
                _logger.LogCritical(ex, "UI unhandled exception ({Context})", context);
                _structuredLogger.Log(new StructuredLogEntry
                {
                    Level = "Critical",
                    Component = "UI",
                    Action = "UnhandledException",
                    Result = "Crash",
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace ?? "",
                    Message = $"UI unhandled exception: {context}"
                });
                ReportCrash(ex, context);
                return true; // Mark handled - fail one operation, not the application
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to handle UI exception");
                return true;
            }
        }

        public void ReportCrash(Exception ex, string context)
        {
            lock (_crashLock)
            {
                try
                {
                    var diagnostics = new CrashDiagnostics
                    {
                        CrashTime = DateTime.UtcNow,
                        ExceptionType = ex.GetType().Name,
                        ExceptionMessage = ex.Message,
                        StackTrace = ex.StackTrace ?? "",
                        Context = context,
                        AppVersion = GetAppVersion(),
                        OSVersion = Environment.OSVersion.ToString(),
                        DotNetVersion = Environment.Version.ToString(),
                        ProcessArchitecture = Environment.Is64BitProcess ? "x64" : "x86",
                        WorkingSetMemory = Process.GetCurrentProcess().WorkingSet64,
                        ThreadCount = Process.GetCurrentProcess().Threads.Count
                    };

                    // Capture environment
                    foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                    {
                        if (entry.Key is string key && entry.Value is string value)
                        {
                            // Only capture safe environment variables
                            if (!IsSensitiveEnvVar(key))
                                diagnostics.Environment[key] = value;
                        }
                    }

                    var json = JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_crashReportPath, json);

                    // Also write a human-readable summary
                    var summaryPath = _crashReportPath.Replace(".json", ".txt");
                    var summary = $@"WinCleaner Crash Report
================================
Time: {diagnostics.CrashTime:yyyy-MM-dd HH:mm:ss} UTC
Version: {diagnostics.AppVersion}
OS: {diagnostics.OSVersion}
.NET: {diagnostics.DotNetVersion}
Architecture: {diagnostics.ProcessArchitecture}
Memory: {diagnostics.WorkingSetMemory / 1024 / 1024} MB
Threads: {diagnostics.ThreadCount}

Exception: {diagnostics.ExceptionType}: {diagnostics.ExceptionMessage}
Context: {diagnostics.Context}

Stack Trace:
{diagnostics.StackTrace}

Environment Variables:
{string.Join("\n", diagnostics.Environment.Select(kvp => $"  {kvp.Key}={kvp.Value}"))}
";
                    File.WriteAllText(summaryPath, summary);

                    _logger.LogCritical("Crash report written: {Path}", _crashReportPath);
                }
                catch (Exception reportEx)
                {
                    _logger.LogError(reportEx, "Failed to write crash report");
                }
            }
        }

        private static string GetAppVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? assembly.GetName().Version?.ToString()
                    ?? "unknown";
                return version;
            }
            catch { return "unknown"; }
        }

        private static bool IsSensitiveEnvVar(string key)
        {
            var sensitive = new[] { "PASSWORD", "SECRET", "TOKEN", "KEY", "CREDENTIAL", "PRIVATE" };
            return sensitive.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase));
        }
    }
}
