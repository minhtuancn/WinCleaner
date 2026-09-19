using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;
using WinCleaner.Services;

namespace WinCleaner.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdateService> _logger;
        private readonly IThemeConfigurationStore _themeStore;
        private readonly IResilienceService _resilienceService;
        private UpdateStatus _currentStatus = UpdateStatus.Idle;

        public event EventHandler<UpdateStatusChangedEventArgs>? UpdateStatusChanged;

        public UpdateService(
            HttpClient httpClient,
            ILogger<UpdateService> logger,
            IThemeConfigurationStore themeStore,
            IResilienceService resilienceService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _themeStore = themeStore;
            _resilienceService = resilienceService;
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            if (!_themeStore.Settings.AutoCheckUpdates)
            {
                _logger.LogInformation("Auto update check disabled");
                return null;
            }

            await SetStatusAsync(UpdateStatus.Checking, "Checking for updates...");

            UpdateInfo? updateInfo = null;

            try
            {
                var result = await _resilienceService.ExecuteWithIsolationAsync(async ct =>
                {
                    var response = await _httpClient.GetAsync(
                        "https://api.github.com/repos/minhtuancn/WinCleaner/releases/latest",
                        ct);
                    
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync(ct);
                    
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    var tagName = root.GetProperty("tag_name").GetString() ?? "";
                    var version = tagName.TrimStart('v');
                    var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.0.0";
                    
                    if (IsNewerVersion(version, currentVersion))
                    {
                        var assets = root.GetProperty("assets").EnumerateArray();
                        string? downloadUrl = null;
                        
                        foreach (var asset in assets)
                        {
                            var name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(".msi") && name.Contains("WinCleaner-"))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                break;
                            }
                        }
                        
                        updateInfo = new UpdateInfo
                        {
                            Version = version,
                            ReleaseNotes = root.GetProperty("body").GetString() ?? "",
                            DownloadUrl = downloadUrl ?? "",
                            ReleaseDate = root.GetProperty("published_at").GetDateTime(),
                            IsPrerelease = root.GetProperty("prerelease").GetBoolean(),
                            IsMandatory = false,
                            SizeBytes = assets.FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith(".msi") == true)
                                .GetProperty("size").GetInt64()
                        };
                    }
                }, "CheckForUpdates", "", "", CleanCategory.SystemTemp, ItemRiskLevel.Safe, 0, cancellationToken: cancellationToken);

                if (updateInfo != null)
                {
                    await SetStatusAsync(UpdateStatus.Available, $"Update available: v{updateInfo.Version}", updateInfo);
                    return updateInfo;
                }
                else
                {
                    await SetStatusAsync(UpdateStatus.UpToDate, "You have the latest version");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for updates");
                await SetStatusAsync(UpdateStatus.Failed, $"Update check failed: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(update.DownloadUrl))
            {
                _logger.LogError("No download URL for update");
                return false;
            }

            await SetStatusAsync(UpdateStatus.Downloading, "Downloading update...", update, 0);

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"WinCleaner-{update.Version}.msi");
                
                using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadedBytes = 0L;
                
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = File.Create(tempPath);
                
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    downloadedBytes += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        var percent = (double)downloadedBytes / totalBytes * 100;
                        progress?.Report(percent);
                        await SetStatusAsync(UpdateStatus.Downloading, "Downloading update...", update, percent);
                    }
                }
                
                // Verify checksum
                if (!string.IsNullOrEmpty(update.Checksum))
                {
                    var fileHash = await ComputeSHA256Async(tempPath, cancellationToken);
                    if (!fileHash.Equals(update.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("Checksum verification failed");
                        await SetStatusAsync(UpdateStatus.Failed, "Checksum verification failed");
                        return false;
                    }
                }
                
                await SetStatusAsync(UpdateStatus.Installing, "Installing update...", update, 100);
                
                // Launch installer silently
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/i \"{tempPath}\" /qn /norestart",
                    Verb = "runas",
                    UseShellExecute = true
                };
                
                System.Diagnostics.Process.Start(processInfo);
                
                await SetStatusAsync(UpdateStatus.Completed, "Update installed successfully", update, 100);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download/install update");
                await SetStatusAsync(UpdateStatus.Failed, $"Update failed: {ex.Message}");
                return false;
            }
        }

        private bool IsNewerVersion(string remote, string local)
        {
            try
            {
                var remoteVersion = Version.Parse(remote.Split('-')[0]);
                var localVersion = Version.Parse(local.Split('-')[0]);
                return remoteVersion > localVersion;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ComputeSHA256Async(string filePath, CancellationToken cancellationToken)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private async Task SetStatusAsync(UpdateStatus status, string message, UpdateInfo? updateInfo = null, double progress = 0)
        {
            _currentStatus = status;
            await Task.Run(() =>
            {
                UpdateStatusChanged?.Invoke(this, new UpdateStatusChangedEventArgs
                {
                    Status = status,
                    Message = message,
                    Progress = progress,
                    UpdateInfo = updateInfo
                });
            });
        }
    }
}