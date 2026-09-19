using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    /// <summary>
    /// Diagnostics service providing technical details for troubleshooting.
    /// </summary>
    public interface IDiagnosticsService
    {
        DiagnosticsSnapshot GetCurrentSnapshot();
        Task<string> ExportDiagnosticsAsync(string? path = null);
        Task<string> ExportSessionLogsAsync(int maxEntries, string? path = null);
        void OpenLogFolder();
        void OpenCrashReportFolder();
        IReadOnlyList<StructuredLogEntry> GetRecentLogs(int count);
        AppHealthStatus GetHealthStatus();
    }

    /// <summary>
    /// Complete diagnostics snapshot of the application state.
    /// </summary>
    public class DiagnosticsSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AppInfo App { get; set; } = new();
        public SystemInfo System { get; set; } = new();
        public RuntimeInfo Runtime { get; set; } = new();
        public DatabaseInfo Databases { get; set; } = new();
        public ExtensionsInfo Extensions { get; set; } = new();
        public SessionInfo Session { get; set; } = new();
        public IReadOnlyList<StructuredLogEntry> RecentLogs { get; set; } = Array.Empty<StructuredLogEntry>();
        public CrashDiagnostics? LastCrash { get; set; }
        public AppHealthStatus Health { get; set; } = new();
    }

    public class AppInfo
    {
        public string Version { get; set; } = "";
        public string BuildDate { get; set; } = "";
        public string Configuration { get; set; } = "";
        public string Framework { get; set; } = "";
        public string ProcessArchitecture { get; set; } = "";
        public long WorkingSetMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    public class SystemInfo
    {
        public string OSVersion { get; set; } = "";
        public string OSBuild { get; set; } = "";
        public string OSArchitecture { get; set; } = "";
        public string WindowsDirectory { get; set; } = "";
        public string SystemDirectory { get; set; } = "";
        public string CurrentUser { get; set; } = "";
        public bool IsAdmin { get; set; }
        public string DotNetVersion { get; set; } = "";
        public long TotalPhysicalMemoryMB { get; set; }
        public int ProcessorCount { get; set; }
        public string TimeZone { get; set; } = "";
        public string Culture { get; set; } = "";
    }

    public class RuntimeInfo
    {
        public string DotNetRuntimeVersion { get; set; } = "";
        public string DotNetSdkVersion { get; set; } = "";
        public long Gen0Collections { get; set; }
        public long Gen1Collections { get; set; }
        public long Gen2Collections { get; set; }
        public long TotalAllocatedBytes { get; set; }
        public bool IsServerGC { get; set; }
    }

    public class DatabaseInfo
    {
        public string ActiveDatabase { get; set; } = "";
        public List<DatabaseEntry> Entries { get; set; } = new();
    }

    public class DatabaseEntry
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public int RuleCount { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsEnabled { get; set; }
        public string Path { get; set; } = "";
    }

    public class ExtensionsInfo
    {
        public int TotalCount { get; set; }
        public int LoadedCount { get; set; }
        public int EnabledCount { get; set; }
        public List<ExtensionEntry> Entries { get; set; } = new();
    }

    public class ExtensionEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Author { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsEnabled { get; set; }
        public bool IsLoaded { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SessionInfo
    {
        public string SessionId { get; set; } = "";
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public int ScansPerformed { get; set; }
        public int CleansPerformed { get; set; }
        public long TotalScannedBytes { get; set; }
        public long TotalCleanedBytes { get; set; }
        public int WarningsCount { get; set; }
        public int ErrorsCount { get; set; }
    }

    public class AppHealthStatus
    {
        public HealthState Overall { get; set; } = HealthState.Healthy;
        public List<HealthIssue> Issues { get; set; } = new();
        public DateTime LastCheck { get; set; } = DateTime.UtcNow;
    }

    public enum HealthState
    {
        Healthy,
        Warning,
        Degraded,
        Critical
    }

    public class HealthIssue
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public HealthSeverity Severity { get; set; }
        public string Category { get; set; } = "";
        public string? Remediation { get; set; }
    }

    public enum HealthSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public class DiagnosticsService : IDiagnosticsService
    {
        private readonly ILogger<DiagnosticsService> _logger;
        private readonly IStructuredLogger _structuredLogger;
        private readonly IResilienceService _resilienceService;
        private readonly ISystemScanner _systemScanner;
        private readonly IWinapp2Service _winapp2Service;
        private readonly IExtensionManager _extensionManager;
        private readonly ISettingsService _settingsService;
        private readonly DateTime _startTime = DateTime.UtcNow;
        private int _scansPerformed = 0;
        private int _cleansPerformed = 0;
        private long _totalScannedBytes = 0;
        private long _totalCleanedBytes = 0;
        private int _warningsCount = 0;
        private int _errorsCount = 0;

        public DiagnosticsService(
            ILogger<DiagnosticsService> logger,
            IStructuredLogger structuredLogger,
            IResilienceService resilienceService,
            ISystemScanner systemScanner,
            IWinapp2Service winapp2Service,
            IExtensionManager extensionManager,
            ISettingsService settingsService)
        {
            _logger = logger;
            _structuredLogger = structuredLogger;
            _resilienceService = resilienceService;
            _systemScanner = systemScanner;
            _winapp2Service = winapp2Service;
            _extensionManager = extensionManager;
            _settingsService = settingsService;
        }

        public void RecordScan(long scannedBytes)
        {
            _scansPerformed++;
            _totalScannedBytes += scannedBytes;
        }

        public void RecordClean(long cleanedBytes, int warnings, int errors)
        {
            _cleansPerformed++;
            _totalCleanedBytes += cleanedBytes;
            _warningsCount += warnings;
            _errorsCount += errors;
        }

        public DiagnosticsSnapshot GetCurrentSnapshot()
        {
            var snapshot = new DiagnosticsSnapshot
            {
                App = GetAppInfo(),
                System = GetSystemInfo(),
                Runtime = GetRuntimeInfo(),
                Databases = GetDatabaseInfo(),
                Extensions = GetExtensionsInfo(),
                Session = GetSessionInfo(),
                RecentLogs = _structuredLogger.GetRecentLogsAsync(100).GetAwaiter().GetResult(),
                LastCrash = _resilienceService.GetLastCrashDiagnostics(),
                Health = GetHealthStatus()
            };

            return snapshot;
        }

        public async Task<string> ExportDiagnosticsAsync(string? path = null)
        {
            var snapshot = GetCurrentSnapshot();
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

            if (string.IsNullOrEmpty(path))
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                path = Path.Combine(desktop, $"WinCleaner-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            }

            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation("Diagnostics exported to: {Path}", path);
            return path;
        }

        public async Task<string> ExportSessionLogsAsync(int maxEntries, string? path = null)
        {
            var logs = await _structuredLogger.GetRecentLogsAsync(maxEntries);
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });

            if (string.IsNullOrEmpty(path))
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                path = Path.Combine(desktop, $"WinCleaner-Logs-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            }

            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation("Session logs exported to: {Path} ({Count} entries)", path, logs.Count);
            return path;
        }

        public void OpenLogFolder()
        {
            try
            {
                var logDir = _structuredLogger.GetLogDirectory();
                if (Directory.Exists(logDir))
                {
                    Process.Start(new ProcessStartInfo(logDir) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open log folder");
            }
        }

        public void OpenCrashReportFolder()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string crashDir = Path.Combine(localAppData, "WinCleaner", "CrashReports");
                if (Directory.Exists(crashDir))
                {
                    Process.Start(new ProcessStartInfo(crashDir) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open crash report folder");
            }
        }

        public IReadOnlyList<StructuredLogEntry> GetRecentLogs(int count)
        {
            return _structuredLogger.GetRecentLogsAsync(count).GetAwaiter().GetResult();
        }

        public AppHealthStatus GetHealthStatus()
        {
            var health = new AppHealthStatus { Overall = HealthState.Healthy };

            // Check for previous crash
            if (_resilienceService.HadUnexpectedShutdown)
            {
                health.Overall = HealthState.Warning;
                health.Issues.Add(new HealthIssue
                {
                    Id = "previous-crash",
                    Title = "Previous session ended unexpectedly",
                    Description = "The application did not shut down cleanly in the previous session. This may indicate a crash or forced termination.",
                    Severity = HealthSeverity.Warning,
                    Category = "Stability",
                    Remediation = "Check crash reports in Diagnostics. If this persists, try reinstalling or reporting the issue."
                });
            }

            // Check memory
            var process = Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / 1024 / 1024;
            if (workingSetMB > 500)
            {
                health.Overall = HealthState.Degraded;
                health.Issues.Add(new HealthIssue
                {
                    Id = "high-memory",
                    Title = "High memory usage",
                    Description = $"Current memory usage is {workingSetMB} MB, which exceeds the recommended 500 MB threshold.",
                    Severity = HealthSeverity.Warning,
                    Category = "Performance",
                    Remediation = "Restart the application to free memory. If persistent, check for memory leaks."
                });
            }

            // Check disk space for logs
            try
            {
                var logDir = _structuredLogger.GetLogDirectory();
                if (Directory.Exists(logDir))
                {
                    var drive = new DriveInfo(Path.GetPathRoot(logDir)!);
                    var freePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                    if (freePercent < 5)
                    {
                        health.Overall = HealthState.Critical;
                        health.Issues.Add(new HealthIssue
                        {
                            Id = "low-disk",
                            Title = "Low disk space",
                            Description = $"Log drive has only {freePercent:F1}% free space ({drive.AvailableFreeSpace / 1024 / 1024 / 1024:F1} GB of {drive.TotalSize / 1024 / 1024 / 1024:F1} GB).",
                            Severity = HealthSeverity.Error,
                            Category = "Storage",
                            Remediation = "Free up disk space. Consider clearing old logs or moving log directory."
                        });
                    }
                }
            }
            catch { }

            // Check for recent errors in logs
            if (_errorsCount > 10)
            {
                if (health.Overall == HealthState.Healthy) health.Overall = HealthState.Warning;
                health.Issues.Add(new HealthIssue
                {
                    Id = "recent-errors",
                    Title = "Multiple recent errors",
                    Description = $"{_errorsCount} errors recorded in current session. This may indicate recurring issues.",
                    Severity = HealthSeverity.Warning,
                    Category = "Reliability",
                    Remediation = "Review error logs in Diagnostics. Check for permission issues or disk problems."
                });
            }

            health.LastCheck = DateTime.UtcNow;
            return health;
        }

        private AppInfo GetAppInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var process = Process.GetCurrentProcess();

            return new AppInfo
            {
                Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? assembly.GetName().Version?.ToString() ?? "1.0.0",
                BuildDate = GetBuildDate(assembly),
                Configuration = GetBuildConfiguration(),
                Framework = ".NET " + Environment.Version.ToString(3),
                ProcessArchitecture = Environment.Is64BitProcess ? "x64" : "x86",
                WorkingSetMemoryMB = process.WorkingSet64 / 1024 / 1024,
                ThreadCount = process.Threads.Count,
                Uptime = DateTime.UtcNow - _startTime
            };
        }

        private SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                OSVersion = Environment.OSVersion.VersionString,
                OSBuild = GetWindowsBuild(),
                OSArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                WindowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                SystemDirectory = Environment.SystemDirectory,
                CurrentUser = Environment.UserName,
                IsAdmin = IsRunningAsAdmin(),
                DotNetVersion = Environment.Version.ToString(),
                TotalPhysicalMemoryMB = GetTotalPhysicalMemoryMB(),
                ProcessorCount = Environment.ProcessorCount,
                TimeZone = TimeZoneInfo.Local.Id,
                Culture = System.Globalization.CultureInfo.CurrentCulture.Name
            };
        }

        private RuntimeInfo GetRuntimeInfo()
        {
            return new RuntimeInfo
            {
                DotNetRuntimeVersion = RuntimeInformation.FrameworkDescription,
                DotNetSdkVersion = GetDotNetSdkVersion(),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalAllocatedBytes = GC.GetTotalAllocatedBytes(),
                IsServerGC = System.Runtime.GCSettings.IsServerGC
            };
        }

        private DatabaseInfo GetDatabaseInfo()
        {
            var info = new DatabaseInfo();
            try
            {
                var databases = _winapp2Service.GetAllDatabasesAsync().GetAwaiter().GetResult();
                foreach (var db in databases)
                {
                    info.Entries.Add(new DatabaseEntry
                    {
                        Name = db.Name,
                        Version = db.Version ?? "unknown",
                        RuleCount = db.EntryCount,
                        SizeBytes = 0, // Size not available in database model
                        LastUpdated = db.LastUpdated,
                        IsEnabled = db.IsEnabled,
                        Path = db.LocalPath
                    });
                }
                info.ActiveDatabase = databases.FirstOrDefault(d => d.IsEnabled)?.Name ?? "none";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get database info");
            }
            return info;
        }

        private ExtensionsInfo GetExtensionsInfo()
        {
            var info = new ExtensionsInfo();
            try
            {
                var extensions = _extensionManager.GetExtensionsAsync().GetAwaiter().GetResult();
                info.TotalCount = extensions.Count;
                info.LoadedCount = extensions.Count(e => e.IsLoaded);
                info.EnabledCount = extensions.Count(e => e.IsEnabled);
                foreach (var ext in extensions)
                {
                    info.Entries.Add(new ExtensionEntry
                    {
                        Id = ext.Id,
                        Name = ext.Name,
                        Version = ext.Version,
                        Author = ext.Author,
                        Type = ext.Type.ToString(),
                        IsEnabled = ext.IsEnabled,
                        IsLoaded = ext.IsLoaded,
                        ErrorMessage = ext.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get extension info");
            }
            return info;
        }

        private SessionInfo GetSessionInfo()
        {
            return new SessionInfo
            {
                SessionId = Guid.NewGuid().ToString("N")[..12],
                StartTime = _startTime,
                Uptime = DateTime.UtcNow - _startTime,
                ScansPerformed = _scansPerformed,
                CleansPerformed = _cleansPerformed,
                TotalScannedBytes = _totalScannedBytes,
                TotalCleanedBytes = _totalCleanedBytes,
                WarningsCount = _warningsCount,
                ErrorsCount = _errorsCount
            };
        }

        private static string GetBuildDate(Assembly assembly)
        {
            try
            {
                var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (attr != null && attr.InformationalVersion.Contains("+"))
                {
                    var parts = attr.InformationalVersion.Split('+');
                    if (parts.Length > 1 && DateTime.TryParse(parts[1], out var dt))
                        return dt.ToString("yyyy-MM-dd");
                }

                // Fallback: use file creation time
                var baseDir = AppContext.BaseDirectory;
                var location = Path.Combine(baseDir, "WinCleaner.dll");
                if (File.Exists(location))
                    return File.GetCreationTimeUtc(location).ToString("yyyy-MM-dd");
            }
            catch { }
            return "unknown";
        }

        private static string GetBuildConfiguration()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        private static string GetWindowsBuild()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    var build = key.GetValue("CurrentBuildNumber")?.ToString();
                    var ubr = key.GetValue("UBR")?.ToString();
                    if (!string.IsNullOrEmpty(build))
                        return $"{build}.{ubr ?? "0"}";
                }
            }
            catch { }
            return Environment.OSVersion.Version.Build.ToString();
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private static long GetTotalPhysicalMemoryMB()
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    if (obj["TotalPhysicalMemory"] is ulong total)
                        return (long)(total / 1024 / 1024);
                }
                return 0;
            }
            catch { return 0; }
        }

        private static string GetDotNetSdkVersion()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit(2000);
                return process?.StandardOutput.ReadToEnd().Trim() ?? "unknown";
            }
            catch { return "unknown"; }
        }
    }
}
