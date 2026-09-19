using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IAppRunningGuard
    {
        List<string> GetRunningProcessesForPath(string path);
        List<string> GetRunningProcessesForCategory(CleanCategory category);
        AppRunningAction GetRecommendedAction(List<string> runningProcesses, ItemRiskLevel riskLevel);
        Task<bool> WaitForProcessesToCloseAsync(IEnumerable<string> processNames, TimeSpan timeout, IProgress<string>? progress = null);
    }

    public class AppRunningGuard : IAppRunningGuard
    {
        private readonly ILogger<AppRunningGuard> _logger;
        private readonly Dictionary<CleanCategory, string[]> _categoryProcessMap;

        public AppRunningGuard(ILogger<AppRunningGuard> logger)
        {
            _logger = logger;
            _categoryProcessMap = InitializeCategoryProcessMap();
        }

        private Dictionary<CleanCategory, string[]> InitializeCategoryProcessMap()
        {
            var map = new Dictionary<CleanCategory, string[]>();
            map[CleanCategory.BrowserCache] = new[] { "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "iexplore" };
            map[CleanCategory.DevToolsCache] = new[] { "code", "devenv", "dotnet", "node", "npm", "yarn", "pnpm", "webpack", "vite" };
            map[CleanCategory.DiscordOldVersions] = new[] { "discord", "discordptb", "discordcanary" };
            map[CleanCategory.PlaywrightBrowsers] = new[] { "playwright", "chromium", "firefox", "webkit" };
            map[CleanCategory.MinecraftTemp] = new[] { "java", "javaw", "minecraft", "mclauncher" };
            map[CleanCategory.SystemTemp] = new[] { "explorer", "shell", "taskhost" };
            map[CleanCategory.WindowsUpdate] = new[] { "wuauclt", "tiworker", "trustedinstaller", "musnotifyicon" };
            map[CleanCategory.UserPrograms] = new[] { "steam", "origin", "epicgameslauncher", "ubisoftconnect", "gog", "battle.net" };
            map[CleanCategory.GameData] = new[] { "steam", "origin", "epicgameslauncher", "ubisoftconnect", "gog", "battle.net" };
            map[CleanCategory.UpdaterCaches] = new[] { "updater", "update", "installer", "setup" };
            return map;
        }

        public List<string> GetRunningProcessesForPath(string path)
        {
            var runningProcesses = new List<string>();
            
            try
            {
                var fileName = Path.GetFileName(path).ToLowerInvariant();
                var directory = Path.GetDirectoryName(path)?.ToLowerInvariant() ?? "";

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var processName = process.ProcessName.ToLowerInvariant();
                        var processPath = "";
                        
                        try
                        {
                            processPath = process.MainModule?.FileName?.ToLowerInvariant() ?? "";
                        }
                        catch
                        {
                            // Access denied to process modules
                        }

                        // Match by process name in path
                        if (!string.IsNullOrEmpty(fileName) && 
                            (processName.Contains(fileName) || fileName.Contains(processName)))
                        {
                            runningProcesses.Add(process.ProcessName);
                            continue;
                        }

                        // Match by process path in directory
                        if (!string.IsNullOrEmpty(processPath) && 
                            processPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
                        {
                            runningProcesses.Add(process.ProcessName);
                        }
                    }
                    catch
                    {
                        // Skip processes we can't access
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking running processes for path: {Path}", path);
            }

            return runningProcesses.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public List<string> GetRunningProcessesForCategory(CleanCategory category)
        {
            var runningProcesses = new List<string>();

            if (!_categoryProcessMap.TryGetValue(category, out var processNames))
                return runningProcesses;

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var processName = process.ProcessName.ToLowerInvariant();
                        
                        foreach (var targetName in processNames)
                        {
                            if (processName.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                            {
                                runningProcesses.Add(process.ProcessName);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Skip processes we can't access
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking running processes for category: {Category}", category);
            }

            return runningProcesses.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public AppRunningAction GetRecommendedAction(List<string> runningProcesses, ItemRiskLevel riskLevel)
        {
            if (runningProcesses.Count == 0)
                return AppRunningAction.Skip;

            // If high risk and apps running, require close
            if (riskLevel >= ItemRiskLevel.High)
                return AppRunningAction.RequireClose;

            // If medium risk, warn
            if (riskLevel == ItemRiskLevel.Medium)
                return AppRunningAction.Warn;

            // Low/Safe risk - just skip/warn
            return AppRunningAction.Warn;
        }

        public async Task<bool> WaitForProcessesToCloseAsync(IEnumerable<string> processNames, TimeSpan timeout, IProgress<string>? progress = null)
        {
            var targetNames = processNames.Select(n => n.ToLowerInvariant()).ToHashSet();
            var startTime = DateTime.UtcNow;
            var checkInterval = TimeSpan.FromSeconds(2);

            while (DateTime.UtcNow - startTime < timeout)
            {
                var stillRunning = new List<string>();

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var processName = process.ProcessName.ToLowerInvariant();
                        if (targetNames.Contains(processName))
                        {
                            stillRunning.Add(process.ProcessName);
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                if (stillRunning.Count == 0)
                {
                    progress?.Report("All target processes have closed");
                    return true;
                }

                progress?.Report($"Waiting for processes to close: {string.Join(", ", stillRunning)}");
                await Task.Delay(checkInterval);
            }

            progress?.Report($"Timeout waiting for processes: {string.Join(", ", targetNames)}");
            return false;
        }
    }
}