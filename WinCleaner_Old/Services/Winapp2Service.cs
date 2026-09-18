using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IWinapp2Service
    {
        Task<Winapp2Database> DownloadAndParseAsync(string url, string name, CancellationToken cancellationToken = default);
        Task<Winapp2Database> ParseFromFileAsync(string filePath, string name, CancellationToken cancellationToken = default);
        Task<List<Winapp2Database>> GetAllDatabasesAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateDatabaseAsync(Winapp2Database database, CancellationToken cancellationToken = default);
        List<Winapp2Entry> FilterEnabledEntries(List<Winapp2Database> databases);
        string ExpandEnvironmentVariables(string path);
        bool CheckDetection(Winapp2Entry entry);
    }

    public class Winapp2Service : IWinapp2Service
    {
        private readonly HttpClient _httpClient;
        private readonly string _dataDirectory;

        public Winapp2Service(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataDirectory = Path.Combine(appData, "WinCleaner", "Databases");
            Directory.CreateDirectory(_dataDirectory);
        }

        public async Task<Winapp2Database> DownloadAndParseAsync(string url, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                string content = await _httpClient.GetStringAsync(url, cancellationToken);
                
                string fileName = $"{name}.ini";
                string filePath = Path.Combine(_dataDirectory, fileName);
                await File.WriteAllTextAsync(filePath, content, cancellationToken);

                return await ParseFromFileAsync(filePath, name, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download and parse {name}: {ex.Message}", ex);
            }
        }

        public async Task<Winapp2Database> ParseFromFileAsync(string filePath, string name, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Database file not found: {filePath}");

            string content = await File.ReadAllTextAsync(filePath, cancellationToken);
            
            var database = new Winapp2Database
            {
                Name = name,
                LocalPath = filePath,
                LastUpdated = File.GetLastWriteTimeUtc(filePath)
            };

            var entries = ParseIniContent(content);
            database.Entries = entries;
            database.EntryCount = entries.Count;

            // Extract version from header comments
            var versionMatch = Regex.Match(content, @"Version[:\s]+([\d.]+)", RegexOptions.IgnoreCase);
            if (versionMatch.Success)
                database.Version = versionMatch.Groups[1].Value;

            return database;
        }

        public async Task<List<Winapp2Database>> GetAllDatabasesAsync(CancellationToken cancellationToken = default)
        {
            var databases = new List<Winapp2Database>();

            // Built-in databases
            var builtInDatabases = new[]
            {
                new { Name = "Winapp2", Url = Winapp2Constants.Winapp2Url },
                new { Name = "Winapp3", Url = Winapp2Constants.Winapp3Url }
            };

            foreach (var db in builtInDatabases)
            {
                string localPath = Path.Combine(_dataDirectory, $"{db.Name}.ini");
                if (File.Exists(localPath))
                {
                    try
                    {
                        var parsed = await ParseFromFileAsync(localPath, db.Name, cancellationToken);
                        databases.Add(parsed);
                    }
                    catch { }
                }
            }

            // Custom databases
            string customDir = Path.Combine(_dataDirectory, "Custom");
            if (Directory.Exists(customDir))
            {
                foreach (var file in Directory.GetFiles(customDir, "*.ini"))
                {
                    try
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        var parsed = await ParseFromFileAsync(file, name, cancellationToken);
                        databases.Add(parsed);
                    }
                    catch { }
                }
            }

            return databases;
        }

        public async Task<bool> UpdateDatabaseAsync(Winapp2Database database, CancellationToken cancellationToken = default)
        {
            try
            {
                if (database.Name.Equals("Winapp2", StringComparison.OrdinalIgnoreCase))
                {
                    await DownloadAndParseAsync(Winapp2Constants.Winapp2Url, "Winapp2", cancellationToken);
                    return true;
                }
                else if (database.Name.Equals("Winapp3", StringComparison.OrdinalIgnoreCase))
                {
                    await DownloadAndParseAsync(Winapp2Constants.Winapp3Url, "Winapp3", cancellationToken);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public List<Winapp2Entry> FilterEnabledEntries(List<Winapp2Database> databases)
        {
            var allEntries = new List<Winapp2Entry>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var db in databases.Where(d => d.IsEnabled))
            {
                foreach (var entry in db.Entries.Where(e => e.IsEnabled))
                {
                    // Deduplication: first match wins
                    if (!seenNames.Contains(entry.Name))
                    {
                        seenNames.Add(entry.Name);
                        allEntries.Add(entry);
                    }
                }
            }

            return allEntries;
        }

        public string ExpandEnvironmentVariables(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            string expanded = path;

            // Expand known Winapp2 variables
            foreach (var kvp in Winapp2Constants.KnownEnvironmentVariables)
            {
                string envValue = GetEnvironmentVariableValue(kvp.Value);
                if (!string.IsNullOrEmpty(envValue))
                {
                    expanded = expanded.Replace(kvp.Key, envValue, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Expand standard Windows environment variables
            expanded = Environment.ExpandEnvironmentVariables(expanded);

            return expanded;
        }

        private string GetEnvironmentVariableValue(string specialFolderName)
        {
            try
            {
                if (Enum.TryParse<Environment.SpecialFolder>(specialFolderName, true, out var folder))
                {
                    return Environment.GetFolderPath(folder);
                }
            }
            catch { }
            return "";
        }

        public bool CheckDetection(Winapp2Entry entry)
        {
            if (entry == null) return false;

            // Check Detect (Registry)
            if (!string.IsNullOrEmpty(entry.Detect))
            {
                if (CheckRegistryDetection(entry.Detect))
                    return true;
            }

            // Check DetectFile
            if (!string.IsNullOrEmpty(entry.DetectFile))
            {
                if (CheckFileDetection(entry.DetectFile))
                    return true;
            }

            // Check DetectOS
            if (!string.IsNullOrEmpty(entry.DetectOS))
            {
                if (CheckOSDetection(entry.DetectOS))
                    return true;
            }

            // If no detection criteria, assume present
            if (!entry.HasDetection)
                return true;

            return false;
        }

        private bool CheckRegistryDetection(string detect)
        {
            try
            {
                // Parse Detect=HKCU\Software\Path or HKLM\Software\Path
                if (detect.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) ||
                    detect.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) ||
                    detect.StartsWith("HKCR", StringComparison.OrdinalIgnoreCase) ||
                    detect.StartsWith("HKU", StringComparison.OrdinalIgnoreCase) ||
                    detect.StartsWith("HKCC", StringComparison.OrdinalIgnoreCase))
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        detect.Substring(detect.IndexOf('\\') + 1));
                    return key != null;
                }
            }
            catch { }
            return false;
        }

        private bool CheckFileDetection(string detectFile)
        {
            try
            {
                string expanded = ExpandEnvironmentVariables(detectFile);
                
                // Handle wildcards in path
                if (expanded.Contains('*') || expanded.Contains('?'))
                {
                    string directory = Path.GetDirectoryName(expanded);
                    string pattern = Path.GetFileName(expanded);
                    
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        return Directory.GetFiles(directory, pattern).Length > 0;
                    }
                }
                else
                {
                    return File.Exists(expanded) || Directory.Exists(expanded);
                }
            }
            catch { }
            return false;
        }

        private bool CheckOSDetection(string detectOS)
        {
            try
            {
                var version = Environment.OSVersion.Version;
                // Parse DetectOS=10.0|11.0 etc.
                var parts = detectOS.Split('|', ',');
                foreach (var part in parts)
                {
                    if (Version.TryParse(part.Trim(), out var osVer))
                    {
                        if (version >= osVer)
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private List<Winapp2Entry> ParseIniContent(string content)
        {
            var entries = new List<Winapp2Entry>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            Winapp2Entry currentEntry = null;
            int lineNumber = 0;

            foreach (var rawLine in lines)
            {
                lineNumber++;
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                // Section header [SectionName]
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // Save previous entry
                    if (currentEntry != null && !string.IsNullOrEmpty(currentEntry.Name))
                    {
                        entries.Add(currentEntry);
                    }

                    // Start new entry
                    currentEntry = new Winapp2Entry
                    {
                        Section = line.Substring(1, line.Length - 2).Trim()
                    };
                    continue;
                }

                if (currentEntry == null) continue;

                // Parse key=value
                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0) continue;

                string key = line.Substring(0, eqIndex).Trim();
                string value = line.Substring(eqIndex + 1).Trim();

                switch (key.ToLowerInvariant())
                {
                    case "name":
                        currentEntry.Name = value;
                        break;
                    case "detect":
                        currentEntry.Detect = value;
                        break;
                    case "detectfile":
                        currentEntry.DetectFile = value;
                        break;
                    case "detectos":
                        currentEntry.DetectOS = value;
                        break;
                    case "warning":
                        currentEntry.Warning = value;
                        break;
                    case "default":
                        currentEntry.Default = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
                        break;
                    case "rebootok":
                        int.TryParse(value, out int rebootOk);
                        currentEntry.RebootOk = rebootOk;
                        break;
                    case "filekey":
                    case "filekey1":
                    case "filekey2":
                    case "filekey3":
                    case "filekey4":
                    case "filekey5":
                        currentEntry.FileKeys.Add(ParseFileKey(value));
                        break;
                    case "regkey":
                    case "regkey1":
                    case "regkey2":
                    case "regkey3":
                    case "regkey4":
                    case "regkey5":
                        currentEntry.RegKeys.Add(ParseRegKey(value));
                        break;
                }
            }

            // Don't forget the last entry
            if (currentEntry != null && !string.IsNullOrEmpty(currentEntry.Name))
            {
                entries.Add(currentEntry);
            }

            return entries;
        }

        private Winapp2FileKey ParseFileKey(string value)
        {
            // Format: Path|Pattern|Recurse|Exclude
            // Example: %AppData%\Mozilla\Firefox\Profiles\*|*.sqlite|RECURSE
            var parts = value.Split('|');
            
            var fileKey = new Winapp2FileKey
            {
                Path = parts.Length > 0 ? parts[0].Trim() : "",
                Pattern = parts.Length > 1 ? parts[1].Trim() : "*.*",
                Recurse = parts.Length > 2 && parts[2].Trim().Equals("RECURSE", StringComparison.OrdinalIgnoreCase),
                Exclude = parts.Length > 3 ? parts[3].Trim() : ""
            };

            return fileKey;
        }

        private Winapp2RegKey ParseRegKey(string value)
        {
            // Format: Key|Action|ValueName|Recurse
            // Example: HKCU\Software\MyApp|DeleteKey||RECURSE
            var parts = value.Split('|');
            
            var regKey = new Winapp2RegKey
            {
                Key = parts.Length > 0 ? parts[0].Trim() : "",
                Action = parts.Length > 1 ? parts[1].Trim() : "DeleteKey",
                ValueName = parts.Length > 2 ? parts[2].Trim() : "",
                Recurse = parts.Length > 3 && parts[3].Trim().Equals("RECURSE", StringComparison.OrdinalIgnoreCase)
            };

            return regKey;
        }
    }
}