using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IWinapp2ToCleanItemConverter
    {
        List<CleanItem> ConvertEntries(List<Winapp2Entry> entries);
        CleanItem ConvertEntry(Winapp2Entry entry);
    }

    public class Winapp2ToCleanItemConverter : IWinapp2ToCleanItemConverter
    {
        public List<CleanItem> ConvertEntries(List<Winapp2Entry> entries)
        {
            var items = new List<CleanItem>();
            foreach (var entry in entries)
            {
                var item = ConvertEntry(entry);
                if (item != null)
                    items.Add(item);
            }
            return items;
        }

        public CleanItem ConvertEntry(Winapp2Entry entry)
        {
            if (entry == null) return null;

            var item = new CleanItem
            {
                Id = $"winapp2_{SanitizeId(entry.Name)}",
                Name = entry.Name,
                Description = entry.Warning ?? entry.Section,
                Category = MapSectionToCategory(entry.Section),
                RiskLevel = entry.RiskLevel,
                RequiresAdmin = entry.RiskLevel >= ItemRiskLevel.High,
                IsSystemItem = false,
                SupportedProfiles = GetSupportedProfiles(entry.RiskLevel),
                SizeCalculator = (ci) => CalculateEntrySize(entry),
                CleanAction = async (ci, prog) => await CleanEntryAsync(entry, ci, prog)
            };

            return item;
        }

        private string SanitizeId(string name)
        {
            return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        }

        private CleanCategory MapSectionToCategory(string section)
        {
            if (string.IsNullOrEmpty(section))
                return CleanCategory.DevToolsCache;

            var lower = section.ToLowerInvariant();

            if (lower.Contains("browser") || lower.Contains("chrome") || lower.Contains("firefox") || 
                lower.Contains("edge") || lower.Contains("opera") || lower.Contains("vivaldi") || 
                lower.Contains("brave") || lower.Contains("internet explorer"))
                return CleanCategory.BrowserCache;

            if (lower.Contains("system") || lower.Contains("windows") || lower.Contains("temp") ||
                lower.Contains("prefetch") || lower.Contains("log") || lower.Contains("update"))
                return CleanCategory.SystemTemp;

            if (lower.Contains("visual studio") || lower.Contains("vscode") || lower.Contains("vs code") ||
                lower.Contains("dotnet") || lower.Contains("nuget") || lower.Contains("npm") ||
                lower.Contains("yarn") || lower.Contains("node") || lower.Contains("python") ||
                lower.Contains("java") || lower.Contains("gradle") || lower.Contains("maven") ||
                lower.Contains("go") || lower.Contains("rust") || lower.Contains("flutter"))
                return CleanCategory.DevToolsCache;

            if (lower.Contains("game") || lower.Contains("steam") || lower.Contains("origin") ||
                lower.Contains("epic") || lower.Contains("uplay") || lower.Contains("battle.net") ||
                lower.Contains("roblox") || lower.Contains("minecraft"))
                return CleanCategory.GameData;

            if (lower.Contains("discord") || lower.Contains("teams") || lower.Contains("skype") ||
                lower.Contains("slack") || lower.Contains("telegram") || lower.Contains("whatsapp") ||
                lower.Contains("zoom"))
                return CleanCategory.UpdaterCaches;

            if (lower.Contains("adobe") || lower.Contains("office") || lower.Contains("acrobat") ||
                lower.Contains("reader"))
                return CleanCategory.UserTemp;

            return CleanCategory.DevToolsCache;
        }

        private string[] GetSupportedProfiles(ItemRiskLevel risk)
        {
            return risk switch
            {
                ItemRiskLevel.Safe => new[] { "Safe", "Deep", "Custom", "Nuclear" },
                ItemRiskLevel.Low => new[] { "Safe", "Deep", "Custom", "Nuclear" },
                ItemRiskLevel.Medium => new[] { "Deep", "Custom", "Nuclear" },
                ItemRiskLevel.High => new[] { "Custom", "Nuclear" },
                ItemRiskLevel.Critical => new[] { "Nuclear" },
                _ => new[] { "Custom", "Nuclear" }
            };
        }

        private long CalculateEntrySize(Winapp2Entry entry)
        {
            long totalSize = 0;

            foreach (var fileKey in entry.FileKeys)
            {
                try
                {
                    string expandedPath = ExpandPath(fileKey.Path);
                    string pattern = fileKey.Pattern;
                    bool recurse = fileKey.Recurse;
                    string exclude = fileKey.Exclude;

                    if (string.IsNullOrEmpty(expandedPath) || !Directory.Exists(expandedPath))
                        continue;

                    var files = recurse 
                        ? Directory.GetFiles(expandedPath, pattern, SearchOption.AllDirectories)
                        : Directory.GetFiles(expandedPath, pattern, SearchOption.TopDirectoryOnly);

                    if (!string.IsNullOrEmpty(exclude))
                    {
                        var excludePatterns = exclude.Split(',', ';');
                        files = files.Where(f => !excludePatterns.Any(ep => 
                            MatchesPattern(Path.GetFileName(f), ep.Trim()))).ToArray();
                    }

                    foreach (var file in files)
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            totalSize += fi.Length;
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return totalSize;
        }

        private async Task CleanEntryAsync(Winapp2Entry entry, CleanItem item, IProgress<string> progress)
        {
            long totalCleaned = 0;
            int filesCleaned = 0;

            foreach (var fileKey in entry.FileKeys)
            {
                try
                {
                    string expandedPath = ExpandPath(fileKey.Path);
                    string pattern = fileKey.Pattern;
                    bool recurse = fileKey.Recurse;
                    string exclude = fileKey.Exclude;

                    if (string.IsNullOrEmpty(expandedPath) || !Directory.Exists(expandedPath))
                        continue;

                    var files = recurse 
                        ? Directory.GetFiles(expandedPath, pattern, SearchOption.AllDirectories)
                        : Directory.GetFiles(expandedPath, pattern, SearchOption.TopDirectoryOnly);

                    if (!string.IsNullOrEmpty(exclude))
                    {
                        var excludePatterns = exclude.Split(',', ';');
                        files = files.Where(f => !excludePatterns.Any(ep => 
                            MatchesPattern(Path.GetFileName(f), ep.Trim()))).ToArray();
                    }

                    foreach (var file in files)
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            long size = fi.Length;
                            fi.Delete();
                            totalCleaned += size;
                            filesCleaned++;
                            
                            progress?.Report($"Deleted: {file} ({FormatBytes(size)})");
                        }
                        catch (Exception ex)
                        {
                            progress?.Report($"Failed to delete {file}: {ex.Message}");
                        }
                    }
                }
                catch { }
            }

            item.CleanedBytes = totalCleaned;
            progress?.Report($"Completed {entry.Name}: {filesCleaned} files, {FormatBytes(totalCleaned)} freed");
        }

        private string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            string expanded = path;

            // Winapp2 known variables
            var knownVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) },
                { "%LocalAppData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) },
                { "%CommonAppData%", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) },
                { "%ProgramFiles%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) },
                { "%ProgramFiles(x86)%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) },
                { "%SystemDrive%", Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 3) },
                { "%WinDir%", Environment.GetFolderPath(Environment.SpecialFolder.Windows) },
                { "%SystemRoot%", Environment.GetFolderPath(Environment.SpecialFolder.Windows) },
                { "%Temp%", Path.GetTempPath() },
                { "%Tmp%", Path.GetTempPath() },
                { "%UserProfile%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { "%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                { "%Music%", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                { "%Pictures%", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                { "%Videos%", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                { "%Downloads%", GetDownloadsPath() },
                { "%Desktop%", Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                { "%StartMenu%", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) },
                { "%Programs%", Environment.GetFolderPath(Environment.SpecialFolder.Programs) },
                { "%Startup%", Environment.GetFolderPath(Environment.SpecialFolder.Startup) },
                { "%Recent%", Environment.GetFolderPath(Environment.SpecialFolder.Recent) },
                { "%SendTo%", Environment.GetFolderPath(Environment.SpecialFolder.SendTo) },
                { "%Fonts%", Environment.GetFolderPath(Environment.SpecialFolder.Fonts) },
                { "%Templates%", Environment.GetFolderPath(Environment.SpecialFolder.Templates) },
                { "%Favorites%", Environment.GetFolderPath(Environment.SpecialFolder.Favorites) },
                { "%NetHood%", "" },
                { "%PrintHood%", "" },
                { "%Cookies%", GetCookiesPath() },
                { "%History%", GetHistoryPath() },
                { "%Cache%", GetCachePath() },
                { "%LocalSettings%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) }
            };

            foreach (var kvp in knownVars)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    expanded = expanded.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Standard environment variables
            expanded = Environment.ExpandEnvironmentVariables(expanded);

            return expanded;
        }

        private string GetDownloadsPath()
        {
            try
            {
                var downloadsGuid = new Guid("{374DE290-123F-4565-9164-39C4925E467B}");
                var path = GetKnownFolderPath(downloadsGuid);
                return path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            catch
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
        }

        private string GetCookiesPath()
        {
            try
            {
                var cookiesGuid = new Guid("{2B0F765D-C0E9-4171-908E-08A611A84FF6}");
                return GetKnownFolderPath(cookiesGuid) ?? "";
            }
            catch { return ""; }
        }

        private string GetHistoryPath()
        {
            try
            {
                var historyGuid = new Guid("{D9DC8A3B-B784-432E-A781-5A1130A75963}");
                return GetKnownFolderPath(historyGuid) ?? "";
            }
            catch { return ""; }
        }

        private string GetCachePath()
        {
            try
            {
                var cacheGuid = new Guid("{352481E8-33BE-4251-BA85-6007CAEDCF9D}");
                return GetKnownFolderPath(cacheGuid) ?? "";
            }
            catch { return ""; }
        }

        private string GetKnownFolderPath(Guid folderId)
        {
            try
            {
                // Use SHGetKnownFolderPath via P/Invoke or fallback
                return "";
            }
            catch { return ""; }
        }

        private bool MatchesPattern(string fileName, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".")
                + "$";

            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
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
}