using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    /// <summary>
    /// Structured log entry with all fields required for diagnostics.
    /// </summary>
    public class StructuredLogEntry
    {
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        public string Level { get; set; } = "Info";
        public string SessionId { get; set; } = "";
        public string OperationId { get; set; } = "";
        public string RuleId { get; set; } = "";
        public string CandidateId { get; set; } = "";
        public string Component { get; set; } = "";
        public string Path { get; set; } = "";
        public string Action { get; set; } = "";
        public string Result { get; set; } = "";
        public int? HResult { get; set; }
        public int? Win32ErrorCode { get; set; }
        public string ExceptionType { get; set; } = "";
        public string ExceptionMessage { get; set; } = "";
        public string StackTrace { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public long SizeBytes { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Configuration for structured logging.
    /// </summary>
    public class StructuredLoggingOptions
    {
        public string LogDirectory { get; set; } = "";
        public int MaxLogFiles { get; set; } = 10;
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxTotalLogSizeMB { get; set; } = 100;
        public int MaxLogAgeDays { get; set; } = 30;
        public int FlushIntervalSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// Structured logging service with bounded retention, privacy-safe output.
    /// Logs to rolling JSON files under %LocalAppData%\WinCleaner\Logs\.
    /// </summary>
    public interface IStructuredLogger
    {
        void Log(StructuredLogEntry entry);
        Task FlushAsync(CancellationToken ct = default);
        Task<IReadOnlyList<StructuredLogEntry>> GetRecentLogsAsync(int count, CancellationToken ct = default);
        string GetLogDirectory();
    }

    public class StructuredLogger : IStructuredLogger, IDisposable
    {
        private readonly IOptionsMonitor<StructuredLoggingOptions> _options;
        private readonly ILogger<StructuredLogger> _logger;
        private readonly ConcurrentQueue<StructuredLogEntry> _queue = new();
        private readonly Timer _flushTimer;
        private readonly string _logDirectory;
        private readonly string _sessionId;
        private long _currentFileSize;
        private int _currentFileIndex;
        private StreamWriter? _currentWriter;
        private readonly object _fileLock = new();
        private bool _disposed;

        public StructuredLogger(IOptionsMonitor<StructuredLoggingOptions> options, ILogger<StructuredLogger> logger)
        {
            _options = options;
            _logger = logger;
            _sessionId = Guid.NewGuid().ToString("N")[..12];

            // Initialize log directory
            var opts = _options.CurrentValue;
            if (string.IsNullOrEmpty(opts.LogDirectory))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDirectory = Path.Combine(localAppData, "WinCleaner", "Logs");
            }
            else
            {
                _logDirectory = opts.LogDirectory;
            }

            Directory.CreateDirectory(_logDirectory);

            // Cleanup old logs on startup
            _ = Task.Run(() => CleanupOldLogsAsync(CancellationToken.None));

            // Start flush timer
            _flushTimer = new Timer(async _ => await FlushAsync(CancellationToken.None), null,
                TimeSpan.FromSeconds(opts.FlushIntervalSeconds),
                TimeSpan.FromSeconds(opts.FlushIntervalSeconds));

            _logger.LogInformation("Structured logger initialized. Session: {SessionId}, Dir: {Dir}", _sessionId, _logDirectory);
        }

        public void Log(StructuredLogEntry entry)
        {
            if (_disposed) return;

            entry.SessionId = entry.SessionId ?? _sessionId;
            entry.Timestamp = entry.Timestamp ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            // Sanitize sensitive data
            SanitizeEntry(entry);

            _queue.Enqueue(entry);
        }

        public async Task FlushAsync(CancellationToken ct = default)
        {
            if (_disposed || _queue.IsEmpty) return;

            var opts = _options.CurrentValue;
            var batch = new List<StructuredLogEntry>(Math.Min(opts.BatchSize, _queue.Count));

            while (batch.Count < opts.BatchSize && _queue.TryDequeue(out var entry))
            {
                batch.Add(entry);
            }

            if (batch.Count == 0) return;

            try
            {
                await WriteBatchAsync(batch, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush structured logs");
                // Re-queue entries on failure
                foreach (var e in batch)
                    _queue.Enqueue(e);
            }
        }

        private async Task WriteBatchAsync(List<StructuredLogEntry> batch, CancellationToken ct)
        {
            var opts = _options.CurrentValue;
            string filePath;

            lock (_fileLock)
            {
                // Rotate if needed
                if (_currentWriter != null && _currentFileSize > opts.MaxLogFileSizeMB * 1024 * 1024)
                {
                    _currentWriter.Dispose();
                    _currentWriter = null;
                }

                if (_currentWriter == null)
                {
                    _currentFileIndex = (_currentFileIndex + 1) % opts.MaxLogFiles;
                    filePath = Path.Combine(_logDirectory, $"wincleaner-log-{_currentFileIndex:D3}.jsonl");
                    _currentWriter = new StreamWriter(filePath, append: true) { AutoFlush = false };
                    _currentFileSize = new FileInfo(filePath).Length;
                }
                else
                {
                    filePath = Path.Combine(_logDirectory, $"wincleaner-log-{_currentFileIndex:D3}.jsonl");
                }
            }

            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

            foreach (var entry in batch)
            {
                ct.ThrowIfCancellationRequested();
                var json = JsonSerializer.Serialize(entry, jsonOptions);
                await _currentWriter.WriteLineAsync(json);
                _currentFileSize += json.Length + 1; // +1 for newline
            }

            await _currentWriter.FlushAsync(ct);
        }

        private void SanitizeEntry(StructuredLogEntry entry)
        {
            // Remove sensitive data patterns
            if (!string.IsNullOrEmpty(entry.Message))
            {
                // Redact potential passwords, tokens, paths with credentials
                entry.Message = RedactSensitive(entry.Message);
            }
            if (!string.IsNullOrEmpty(entry.ExceptionMessage))
            {
                entry.ExceptionMessage = RedactSensitive(entry.ExceptionMessage);
            }
            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                entry.StackTrace = RedactSensitive(entry.StackTrace);
            }
            if (!string.IsNullOrEmpty(entry.Path))
            {
                // Keep path but redact potential sensitive folders
                entry.Path = RedactPath(entry.Path);
            }
        }

        private string RedactSensitive(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Redact common patterns
            var patterns = new[]
            {
                @"password[""'\s:=]+[^""'\s]{3,}",
                @"token[""'\s:=]+[^""'\s]{10,}",
                @"secret[""'\s:=]+[^""'\s]{5,}",
                @"key[""'\s:=]+[^""'\s]{10,}",
                @"authorization[""'\s:=]+[^""'\s]{10,}",
                @"cookie[""'\s:=]+[^""'\s]{10,}",
            };

            var result = input;
            foreach (var pattern in patterns)
            {
                result = System.Text.RegularExpressions.Regex.Replace(result, pattern, m => 
                    m.Value.Substring(0, Math.Min(15, m.Value.Length)) + "[REDACTED]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return result;
        }

        private string RedactPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Redact known sensitive paths
            var sensitiveFolders = new[]
            {
                @"\AppData\Local\Microsoft\Edge\User Data\Default\Cookies",
                @"\AppData\Local\Google\Chrome\User Data\Default\Cookies",
                @"\AppData\Local\Microsoft\Edge\User Data\Default\Login Data",
                @"\AppData\Local\Google\Chrome\User Data\Default\Login Data",
                @"\AppData\Roaming\Mozilla\Firefox\Profiles\.*\cookies\.sqlite",
                @"\AppData\Roaming\Mozilla\Firefox\Profiles\.*\logins\.json",
            };

            var result = path;
            foreach (var folder in sensitiveFolders)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(result, folder, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    result = System.Text.RegularExpressions.Regex.Replace(result, folder, "[SENSITIVE_PATH]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
            }
            return result;
        }

        public async Task<IReadOnlyList<StructuredLogEntry>> GetRecentLogsAsync(int count, CancellationToken ct = default)
        {
            var result = new List<StructuredLogEntry>();
            var opts = _options.CurrentValue;

            var files = Directory.GetFiles(_logDirectory, "wincleaner-log-*.jsonl")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .ToList();

            foreach (var file in files)
            {
                if (result.Count >= count) break;
                ct.ThrowIfCancellationRequested();

                try
                {
                    var lines = await File.ReadAllLinesAsync(file, ct);
                    for (int i = lines.Length - 1; i >= 0 && result.Count < count; i--)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        try
                        {
                            var entry = JsonSerializer.Deserialize<StructuredLogEntry>(lines[i]);
                            if (entry != null) result.Add(entry);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return result;
        }

        public string GetLogDirectory() => _logDirectory;

        private void CleanupOldLogsAsync(CancellationToken ct)
        {
            try
            {
                var opts = _options.CurrentValue;
                var cutoff = DateTime.UtcNow.AddDays(-opts.MaxLogAgeDays);
                long totalSize = 0;

                var files = Directory.GetFiles(_logDirectory, "wincleaner-log-*.jsonl")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();

                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();

                    bool delete = false;
                    if (file.LastWriteTimeUtc < cutoff)
                        delete = true;
                    else if (totalSize + file.Length > opts.MaxTotalLogSizeMB * 1024 * 1024)
                        delete = true;

                    if (delete)
                    {
                        try { file.Delete(); } catch { }
                    }
                    else
                    {
                        totalSize += file.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Log cleanup failed");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _flushTimer?.Dispose();
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();

            lock (_fileLock)
            {
                _currentWriter?.Dispose();
                _currentWriter = null;
            }
        }
    }
}
