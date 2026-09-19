using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IAppxService
    {
        Task<List<AppxPackage>> GetAllPackagesAsync();
        Task<AppxPackage?> GetPackageAsync(string fullName);
        Task<bool> UninstallPackageAsync(string fullName);
        Task<bool> UninstallPackagesAsync(string[] fullNames);
        Task<List<AppxPackage>> GetPackagesByTypeAsync(AppxPackageType type);
        Task<List<AppxPackage>> GetSelectedPackagesAsync();
        Task<bool> SelectPackageAsync(string fullName, bool selected);
        Task<AppxDatabase> GetDatabaseAsync(string name);
        Task<bool> UpdateDatabaseAsync(string name);
        Task<AppxDatabase> ParseWinappxAsync(string filePath);
        Task<List<AppxPackage>> GetSystemPackagesAsync();
        Task<AppxDatabase> GetBuiltinDatabaseAsync();
    }

    [SupportedOSPlatform("windows")]
    public class AppxService : IAppxService
    {
        private readonly string _dataDirectory;
        private readonly object _lock = new();
        private List<AppxPackage> _systemPackages = new();
        private readonly Dictionary<string, AppxDatabase> _databases = new();

        public AppxService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataDirectory = Path.Combine(appData, "WinCleaner", "Appx");
            Directory.CreateDirectory(_dataDirectory);
            
            _ = LoadBuiltinDatabaseAsync();
        }

        public async Task<List<AppxPackage>> GetAllPackagesAsync()
        {
            await RefreshSystemPackagesAsync();
            lock (_lock)
            {
                return _systemPackages.OrderBy(p => p.Name).ToList();
            }
        }

        public async Task<AppxPackage?> GetPackageAsync(string fullName)
        {
            await RefreshSystemPackagesAsync();
            lock (_lock)
            {
                return _systemPackages.FirstOrDefault(p => p.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<bool> UninstallPackageAsync(string fullName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Get-AppxPackage -Name '{fullName}' | Remove-AppxPackage\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode == 0)
                    {
                        lock (_lock)
                        {
                            _systemPackages.RemoveAll(p => p.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
                        }
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public async Task<bool> UninstallPackagesAsync(string[] fullNames)
        {
            bool allSuccess = true;
            foreach (var fullName in fullNames)
            {
                var success = await UninstallPackageAsync(fullName);
                if (!success) allSuccess = false;
            }
            return allSuccess;
        }

        public async Task<List<AppxPackage>> GetPackagesByTypeAsync(AppxPackageType type)
        {
            await RefreshSystemPackagesAsync();
            lock (_lock)
            {
                return _systemPackages.Where(p => p.Type == type).OrderBy(p => p.Name).ToList();
            }
        }

        public async Task<List<AppxPackage>> GetSelectedPackagesAsync()
        {
            lock (_lock)
            {
                return _systemPackages.Where(p => p.Selected).ToList();
            }
        }

        public async Task<bool> SelectPackageAsync(string fullName, bool selected)
        {
            lock (_lock)
            {
                var pkg = _systemPackages.FirstOrDefault(p => p.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
                if (pkg != null)
                {
                    pkg.Selected = selected;
                    return true;
                }
            }
            return false;
        }

        public async Task<AppxDatabase> GetDatabaseAsync(string name)
        {
            if (_databases.TryGetValue(name, out var existingDb))
                return existingDb;

            var path = Path.Combine(_dataDirectory, $"{name}.json");
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                var loadedDb = JsonSerializer.Deserialize<AppxDatabase>(json, GetJsonOptions());
                if (loadedDb != null)
                {
                    _databases[name] = loadedDb;
                    return loadedDb;
                }
            }
            return new AppxDatabase { Name = name };
        }

        public async Task<bool> UpdateDatabaseAsync(string name)
        {
            try
            {
                var packages = await GetSystemPackagesAsync();
                var newDb = new AppxDatabase
                {
                    Name = name,
                    Packages = packages,
                    LastUpdated = DateTime.Now,
                    Version = "1.0"
                };

                var path = Path.Combine(_dataDirectory, $"{name}.json");
                var json = JsonSerializer.Serialize(newDb, GetJsonOptions());
                await File.WriteAllTextAsync(path, json);

                _databases[name] = newDb;
                return true;
            }
            catch { return false; }
        }

        public async Task<AppxDatabase> ParseWinappxAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return new AppxDatabase();

            var content = await File.ReadAllTextAsync(filePath);
            var parsedDb = new AppxDatabase
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                LocalPath = filePath,
                LastUpdated = File.GetLastWriteTimeUtc(filePath)
            };

            var entries = ParseWinappxContent(content);
            parsedDb.Packages = entries;
            // PackageCount is read-only, computed from Packages.Count

            return parsedDb;
        }

        public async Task<List<AppxPackage>> GetSystemPackagesAsync()
        {
            return await RefreshSystemPackagesAsync();
        }

        public async Task<AppxDatabase> GetBuiltinDatabaseAsync()
        {
            if (_databases.TryGetValue("Builtin", out var builtinDb))
                return builtinDb;

            var packages = await GetSystemPackagesAsync();
            var builtinDbObj = new AppxDatabase
            {
                Name = "Builtin",
                Packages = packages,
                LastUpdated = DateTime.Now,
                Version = "1.0",
                IsEnabled = true
            };

            _databases["Builtin"] = builtinDbObj;
            return builtinDbObj;
        }

        private async Task<List<AppxPackage>> RefreshSystemPackagesAsync()
        {
            try
            {
                // Use PowerShell to get all AppX packages
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Get-AppxPackage -AllUsers | Select-Object Name, PackageFullName, Publisher, Version, InstallLocation, IsFramework, IsBundle, IsResourcePackage | ConvertTo-Json -Depth 5\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = await proc.StandardOutput.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        try
                        {
                            var psPackages = JsonSerializer.Deserialize<List<PowerShellAppxPackage>>(output, GetJsonOptions());
                            if (psPackages != null)
                            {
                                var result = psPackages
                                    .Where(p => !p.IsResourcePackage && !string.IsNullOrEmpty(p.PackageFullName))
                                    .Select(p => new AppxPackage
                                    {
                                        Name = p.Name,
                                        FullName = p.PackageFullName,
                                        Publisher = p.Publisher ?? "",
                                        Version = p.Version ?? "",
                                        InstallLocation = p.InstallLocation ?? "",
                                        IsFramework = p.IsFramework,
                                        IsBundle = p.IsBundle,
                                        Type = ClassifyPackageType(p.Name, p.Publisher ?? ""),
                                        RiskLevel = AssessRiskLevel(p.Name, p.Publisher ?? ""),
                                        Size = GetPackageSize(p.InstallLocation ?? "")
                                    })
                                    .OrderBy(p => p.Name)
                                    .ToList();

                                lock (_lock)
                                {
                                    _systemPackages = result;
                                }
                                return result;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            lock (_lock)
            {
                return _systemPackages;
            }
        }

        private async Task LoadBuiltinDatabaseAsync()
        {
            try
            {
                await GetBuiltinDatabaseAsync();
            }
            catch { }
        }

        private List<AppxPackage> ParseWinappxContent(string content)
        {
            var packages = new List<AppxPackage>();
            // Winappx.ini format parsing would go here
            // Similar to Winapp2 parser but for AppX entries
            return packages;
        }

        private AppxPackageType ClassifyPackageType(string name, string publisher)
        {
            var lowerName = name.ToLowerInvariant();
            var lowerPublisher = publisher.ToLowerInvariant();

            // System packages
            if (lowerName.StartsWith("microsoft.windows.") || 
                lowerName.StartsWith("microsoft.system.") ||
                lowerPublisher.Contains("microsoft.windows"))
                return AppxPackageType.System;

            // Microsoft apps
            if (lowerPublisher.Contains("microsoft corporation") || 
                lowerPublisher.Contains("microsoft"))
            {
                if (lowerName.Contains("office") || lowerName.Contains("word") || 
                    lowerName.Contains("excel") || lowerName.Contains("powerpoint") ||
                    lowerName.Contains("onenote") || lowerName.Contains("outlook"))
                    return AppxPackageType.Office;
                
                if (lowerName.Contains("xbox") || lowerName.Contains("gaming") ||
                    lowerName.Contains("game"))
                    return AppxPackageType.Gaming;

                return AppxPackageType.Microsoft;
            }

            // Social
            if (lowerName.Contains("facebook") || lowerName.Contains("twitter") ||
                lowerName.Contains("instagram") || lowerName.Contains("whatsapp") ||
                lowerName.Contains("telegram") || lowerName.Contains("discord") ||
                lowerName.Contains("skype") || lowerName.Contains("teams"))
                return AppxPackageType.Social;

            // Media
            if (lowerName.Contains("spotify") || lowerName.Contains("netflix") ||
                lowerName.Contains("disney") || lowerName.Contains("hulu") ||
                lowerName.Contains("prime") || lowerName.Contains("video") ||
                lowerName.Contains("music") || lowerName.Contains("photo") ||
                lowerName.Contains("camera"))
                return AppxPackageType.Media;

            // Developer tools
            if (lowerName.Contains("visualstudio") || lowerName.Contains("vscode") ||
                lowerName.Contains("developer") || lowerName.Contains("terminal") ||
                lowerName.Contains("powershell") || lowerName.Contains("wsl") ||
                lowerName.Contains("docker") || lowerName.Contains("github"))
                return AppxPackageType.Developer;

            // Education
            if (lowerName.Contains("education") || lowerName.Contains("learning") ||
                lowerName.Contains("classroom") || lowerName.Contains("student"))
                return AppxPackageType.Education;

            return AppxPackageType.Unknown;
        }

        private AppxRiskLevel AssessRiskLevel(string name, string publisher)
        {
            var lowerName = name.ToLowerInvariant();
            var lowerPublisher = publisher.ToLowerInvariant();

            // Critical - System packages
            if (lowerName.StartsWith("microsoft.windows.") || 
                lowerName.StartsWith("microsoft.system."))
                return AppxRiskLevel.Critical;

            // High - Framework packages
            if (lowerName.Contains("framework") || lowerName.Contains("runtime"))
                return AppxRiskLevel.High;

            // Medium - Microsoft apps
            if (lowerPublisher.Contains("microsoft"))
                return AppxRiskLevel.Medium;

            // Low - Known safe apps
            var safeApps = new[] { "spotify", "netflix", "discord", "steam", "vscode", "chrome", "firefox" };
            if (safeApps.Any(s => lowerName.Contains(s)))
                return AppxRiskLevel.Low;

            return AppxRiskLevel.Medium;
        }

        private long GetPackageSize(string installLocation)
        {
            if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                return 0;

            try
            {
                return Directory.GetFiles(installLocation, "*", SearchOption.AllDirectories)
                    .Sum(f => new FileInfo(f).Length);
            }
            catch { return 0; }
        }

        private class PowerShellAppxPackage
        {
            public string? Name { get; set; }
            public string? PackageFullName { get; set; }
            public string? Publisher { get; set; }
            public string? Version { get; set; }
            public string? InstallLocation { get; set; }
            public bool IsFramework { get; set; }
            public bool IsBundle { get; set; }
            public bool IsResourcePackage { get; set; }
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