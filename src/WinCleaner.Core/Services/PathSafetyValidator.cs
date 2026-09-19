using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IPathSafetyValidator
    {
        PathSafetyResult ValidatePath(string path, CleanCategory category, ItemRiskLevel riskLevel);
        List<PathSafetyResult> ValidatePaths(IEnumerable<string> paths, CleanCategory category, ItemRiskLevel riskLevel);
        bool IsPathProtected(string path);
        List<string> GetProtectedPathPatterns();
    }

    [SupportedOSPlatform("windows")]
    public class PathSafetyValidator : IPathSafetyValidator
    {
        private readonly ILogger<PathSafetyValidator> _logger;
        private readonly HashSet<string> _protectedPaths;
        private readonly HashSet<string> _criticalPaths;
        private Dictionary<string, PathSafetyLevel> _pathPatterns;

        public PathSafetyValidator(ILogger<PathSafetyValidator> logger)
        {
            _logger = logger;
            _protectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _criticalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _pathPatterns = new Dictionary<string, PathSafetyLevel>(StringComparer.OrdinalIgnoreCase);
            InitializeProtectedPaths();
        }

        private void InitializeProtectedPaths()
        {
            // Critical system paths - NEVER delete
            _criticalPaths.UnionWith(new[]
            {
                @"C:\Windows\System32",
                @"C:\Windows\SysWOW64",
                @"C:\Windows\WinSxS",
                @"C:\Windows\Servicing",
                @"C:\Windows\System32\config",
                @"C:\Windows\System32\drivers",
                @"C:\Windows\Boot",
                @"C:\Windows\SystemResources",
                @"C:\Program Files\Windows Defender",
                @"C:\Program Files\Windows Security",
                @"C:\System Volume Information",
                @"C:\Recovery",
                @"C:\$Recycle.Bin",
                @"C:\pagefile.sys",
                @"C:\hiberfil.sys",
                @"C:\swapfile.sys",
                @"C:\Windows\CSC",
                @"C:\Windows\Fonts",
                @"C:\Windows\Globalization",
                @"C:\Windows\IME",
                @"C:\Windows\InputMethod",
                @"C:\Windows\Speech",
                @"C:\Windows\System32\wbem",
                @"C:\Windows\System32\WindowsPowerShell",
                @"C:\Windows\System32\WDI",
                @"C:\Windows\System32\DriverStore",
            });

            // Protected paths - require explicit confirmation
            _protectedPaths.UnionWith(new[]
            {
                @"C:\Windows",
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                @"C:\ProgramData\Microsoft",
                @"C:\Users\Default",
                @"C:\Users\Public",
                @"C:\Windows\Logs",
                @"C:\Windows\Temp",
                @"C:\Windows\Prefetch",
                @"C:\Windows\SoftwareDistribution",
                @"C:\Windows\Installer",
                @"C:\Windows\Microsoft.NET",
                @"C:\Windows\assembly",
                @"C:\Users\All Users",
            });

            // Path patterns with safety levels
            _pathPatterns = new Dictionary<string, PathSafetyLevel>(StringComparer.OrdinalIgnoreCase)
            {
                // Critical - system core
                { @"C:\Windows\System32\", PathSafetyLevel.Critical },
                { @"C:\Windows\SysWOW64\", PathSafetyLevel.Critical },
                { @"C:\Windows\WinSxS\", PathSafetyLevel.Critical },
                { @"C:\Windows\Servicing\", PathSafetyLevel.Critical },
                { @"C:\Windows\System32\config\", PathSafetyLevel.Critical },
                { @"C:\Windows\Boot\", PathSafetyLevel.Critical },
                { @"C:\System Volume Information\", PathSafetyLevel.Critical },
                { @"C:\Recovery\", PathSafetyLevel.Critical },
                { @"C:\pagefile.sys", PathSafetyLevel.Critical },
                { @"C:\hiberfil.sys", PathSafetyLevel.Critical },

                // Protected - system areas
                { @"C:\Windows\", PathSafetyLevel.Protected },
                { @"C:\Program Files\", PathSafetyLevel.Protected },
                { @"C:\Program Files (x86)\", PathSafetyLevel.Protected },
                { @"C:\ProgramData\", PathSafetyLevel.Protected },
                { @"C:\Users\Default", PathSafetyLevel.Protected },
                { @"C:\Users\Public", PathSafetyLevel.Protected },

                // Caution - user data areas that might have important files
                { @"C:\Users\", PathSafetyLevel.Safe },
                { @"AppData\Local\Microsoft", PathSafetyLevel.Safe },
                { @"AppData\Roaming\Microsoft", PathSafetyLevel.Safe },
                { @"Documents", PathSafetyLevel.Caution },
                { @"Pictures", PathSafetyLevel.Caution },
                { @"Videos", PathSafetyLevel.Caution },
                { @"Music", PathSafetyLevel.Caution },
                { @"Desktop", PathSafetyLevel.Caution },
                { @"Downloads", PathSafetyLevel.Caution },

                // Safe - known cache/temp locations
                { @"AppData\Local\Temp", PathSafetyLevel.Safe },
                { @"AppData\Local\CrashDumps", PathSafetyLevel.Safe },
                { @"AppData\Local\Microsoft\Windows\INetCache", PathSafetyLevel.Safe },
                { @"AppData\Local\Microsoft\Windows\WebCache", PathSafetyLevel.Safe },
                { @"AppData\Local\Microsoft\Edge\User Data\Default\Cache", PathSafetyLevel.Safe },
                { @"AppData\Local\Google\Chrome\User Data\Default\Cache", PathSafetyLevel.Safe },
                { @"AppData\Local\Mozilla\Firefox\Profiles", PathSafetyLevel.Safe },
                { @"AppData\Local\Packages", PathSafetyLevel.Safe },
                { @"Local\Temp", PathSafetyLevel.Safe },
                { @"Temp\", PathSafetyLevel.Safe },
                { @"\Temp\", PathSafetyLevel.Safe },
                { @"\Cache\", PathSafetyLevel.Safe },
                { @"\CrashDumps\", PathSafetyLevel.Safe },
                { @"\Logs\", PathSafetyLevel.Safe },
            };
        }

        public PathSafetyResult ValidatePath(string path, CleanCategory category, ItemRiskLevel riskLevel)
        {
            var result = new PathSafetyResult
            {
                Path = path ?? ""
            };

            if (string.IsNullOrWhiteSpace(path))
            {
                result.SafetyLevel = PathSafetyLevel.Critical;
                result.Reasons.Add("Empty or null path");
                return result;
            }

            try
            {
                var normalizedPath = NormalizePath(path);
                result.Path = normalizedPath;

                // Check critical paths first
                if (IsCriticalPath(normalizedPath))
                {
                    result.SafetyLevel = PathSafetyLevel.Critical;
                    result.Reasons.Add("Critical system path - deletion may cause system instability");
                    result.Warnings.Add("This path contains essential Windows system files");
                    return result;
                }

                // Check protected paths
                if (IsProtectedPath(normalizedPath))
                {
                    result.SafetyLevel = PathSafetyLevel.Protected;
                    result.Reasons.Add("Protected system area");
                    result.Warnings.Add("Deletion requires explicit confirmation and admin rights");
                    return result;
                }

                // Check pattern-based classification
                var patternResult = ClassifyByPattern(normalizedPath);
                if (patternResult != PathSafetyLevel.Safe)
                {
                    result.SafetyLevel = patternResult;
                    switch (patternResult)
                    {
                        case PathSafetyLevel.Caution:
                            result.Reasons.Add("User data area - verify before deletion");
                            result.Warnings.Add("May contain personal files or application settings");
                            break;
                        case PathSafetyLevel.Protected:
                            result.Reasons.Add("Protected system area");
                            result.Warnings.Add("Requires explicit confirmation");
                            break;
                    }
                    return result;
                }

                // Category-based adjustments
                var categorySafety = GetCategorySafetyLevel(category);
                if (categorySafety > result.SafetyLevel)
                {
                    result.SafetyLevel = categorySafety;
                    result.Reasons.Add($"Category {category} implies {categorySafety} safety level");
                }

                // Risk level adjustments
                var riskSafety = GetRiskSafetyLevel(riskLevel);
                if (riskSafety > result.SafetyLevel)
                {
                    result.SafetyLevel = riskSafety;
                    result.Reasons.Add($"Risk level {riskLevel} implies {riskSafety} safety level");
                }

                // Default to safe for known cache patterns
                if (result.SafetyLevel == PathSafetyLevel.Safe)
                {
                    result.Reasons.Add("Known safe cleanup location");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating path: {Path}", path);
                result.SafetyLevel = PathSafetyLevel.Critical;
                result.Reasons.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        public List<PathSafetyResult> ValidatePaths(IEnumerable<string> paths, CleanCategory category, ItemRiskLevel riskLevel)
        {
            var results = new List<PathSafetyResult>();
            foreach (var path in paths)
            {
                results.Add(ValidatePath(path, category, riskLevel));
            }
            return results;
        }

        public bool IsPathProtected(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            var normalized = NormalizePath(path);
            return IsCriticalPath(normalized) || IsProtectedPath(normalized);
        }

        public List<string> GetProtectedPathPatterns()
        {
            return _pathPatterns.Keys.ToList();
        }

        private string NormalizePath(string path)
        {
            try
            {
                // Handle environment variables
                path = Environment.ExpandEnvironmentVariables(path);
                
                // Get full path
                var fullPath = Path.GetFullPath(path);
                
                // Normalize separators
                return fullPath.TrimEnd('\\', '/');
            }
            catch
            {
                return path;
            }
        }

        private bool IsCriticalPath(string path)
        {
            foreach (var critical in _criticalPaths)
            {
                if (path.StartsWith(critical, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool IsProtectedPath(string path)
        {
            foreach (var protectedPath in _protectedPaths)
            {
                if (path.StartsWith(protectedPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private PathSafetyLevel ClassifyByPattern(string path)
        {
            PathSafetyLevel highest = PathSafetyLevel.Safe;
            
            foreach (var pattern in _pathPatterns)
            {
                if (path.Contains(pattern.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (pattern.Value > highest)
                        highest = pattern.Value;
                }
            }
            
            return highest;
        }

        private PathSafetyLevel GetCategorySafetyLevel(CleanCategory category)
        {
            return category switch
            {
                CleanCategory.SystemRestore => PathSafetyLevel.Critical,
                CleanCategory.Hibernation => PathSafetyLevel.Critical,
                CleanCategory.WindowsOld => PathSafetyLevel.Protected,
                CleanCategory.CompactOS => PathSafetyLevel.Protected,
                CleanCategory.DriverStore => PathSafetyLevel.Protected,
                CleanCategory.WindowsUpdate => PathSafetyLevel.Caution,
                CleanCategory.SystemTemp => PathSafetyLevel.Safe,
                CleanCategory.UserTemp => PathSafetyLevel.Safe,
                CleanCategory.BrowserCache => PathSafetyLevel.Safe,
                CleanCategory.RecycleBin => PathSafetyLevel.Caution,
                CleanCategory.SystemLogs => PathSafetyLevel.Safe,
                CleanCategory.DevToolsCache => PathSafetyLevel.Safe,
                CleanCategory.DiscordOldVersions => PathSafetyLevel.Safe,
                CleanCategory.PlaywrightBrowsers => PathSafetyLevel.Safe,
                CleanCategory.MinecraftTemp => PathSafetyLevel.Safe,
                CleanCategory.UpdaterCaches => PathSafetyLevel.Safe,
                CleanCategory.GameData => PathSafetyLevel.Caution,
                CleanCategory.UserPrograms => PathSafetyLevel.Caution,
                CleanCategory.OtherUsers => PathSafetyLevel.Caution,
                _ => PathSafetyLevel.Safe
            };
        }

        private PathSafetyLevel GetRiskSafetyLevel(ItemRiskLevel riskLevel)
        {
            return riskLevel switch
            {
                ItemRiskLevel.Critical => PathSafetyLevel.Critical,
                ItemRiskLevel.High => PathSafetyLevel.Protected,
                ItemRiskLevel.Medium => PathSafetyLevel.Caution,
                ItemRiskLevel.Low => PathSafetyLevel.Safe,
                ItemRiskLevel.Safe => PathSafetyLevel.Safe,
                _ => PathSafetyLevel.Safe
            };
        }
    }
}