using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ISystemScanner
    {
        Task<List<DriveInfoModel>> GetDrivesAsync();
        Task<List<UserProfileModel>> GetUserProfilesAsync();
        Task<SystemInfoModel> GetSystemInfoAsync();
        Task<List<CleanCategoryGroup>> ScanAsync(CleanProfile profile, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task<long> CalculateItemSizeAsync(CleanItem item, CancellationToken cancellationToken = default);
    }

    public interface ICleanerService
    {
        Task<CleanResult> CleanAsync(List<CleanCategoryGroup> groups, IProgress<string> progress, IProgress<LogEntry> logProgress, CancellationToken cancellationToken = default);
        Task<bool> CleanItemAsync(CleanItem item, IProgress<string> progress, IProgress<LogEntry> logProgress, CancellationToken cancellationToken = default);
        bool DryRunMode { get; set; }
    }

    public class SystemScanner : ISystemScanner
    {
        private readonly ICleanerService _cleanerService;

        public SystemScanner(ICleanerService cleanerService)
        {
            _cleanerService = cleanerService;
        }

        public async Task<List<DriveInfoModel>> GetDrivesAsync()
        {
            return await Task.Run(() =>
            {
                var drives = new List<DriveInfoModel>();
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    
                    try
                    {
                        var model = new DriveInfoModel
                        {
                            Name = drive.Name.TrimEnd('\\'),
                            Label = drive.VolumeLabel,
                            TotalSize = drive.TotalSize,
                            FreeSpace = drive.AvailableFreeSpace,
                            DriveType = drive.DriveType.ToString(),
                            FileSystem = drive.DriveFormat,
                            IsSystemDrive = drive.Name.StartsWith(Environment.SystemDirectory.Substring(0, 1), StringComparison.OrdinalIgnoreCase),
                            IsReady = true
                        };
                        drives.Add(model);
                    }
                    catch
                    {
                        // Skip inaccessible drives
                    }
                }
                return drives;
            });
        }

        public async Task<List<UserProfileModel>> GetUserProfilesAsync()
        {
            return await Task.Run(() =>
            {
                var profiles = new List<UserProfileModel>();
                string currentUser = Environment.UserName;
                string currentUserDomain = Environment.UserDomainName;
                
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT SID, LocalPath, LastUseTime FROM Win32_UserProfile WHERE Special = FALSE");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string sid = obj["SID"]?.ToString() ?? "";
                        string path = obj["LocalPath"]?.ToString() ?? "";
                        string name = Path.GetFileName(path);
                        
                        long size = 0;
                        if (Directory.Exists(path))
                        {
                            try
                            {
                                size = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
                            }
                            catch { }
                        }

                        profiles.Add(new UserProfileModel
                        {
                            Name = name,
                            SID = sid,
                            Path = path,
                            Size = size,
                            IsCurrent = name.Equals(currentUser, StringComparison.OrdinalIgnoreCase),
                            IsActive = true,
                            CanClean = !name.Equals(currentUser, StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
                catch
                {
                    // Fallback: scan C:\Users
                    string usersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "..", "Users");
                    if (Directory.Exists(usersPath))
                    {
                        foreach (var dir in Directory.GetDirectories(usersPath))
                        {
                            string name = Path.GetFileName(dir);
                            if (name == "Public" || name == "Default" || name.StartsWith("Default")) continue;
                            
                            long size = 0;
                            try { size = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length); } catch { }
                            
                            profiles.Add(new UserProfileModel
                            {
                                Name = name,
                                Path = dir,
                                Size = size,
                                IsCurrent = name.Equals(currentUser, StringComparison.OrdinalIgnoreCase),
                                IsActive = true,
                                CanClean = !name.Equals(currentUser, StringComparison.OrdinalIgnoreCase)
                            });
                        }
                    }
                }
                return profiles;
            });
        }

        public async Task<SystemInfoModel> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                using var os = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber, OSArchitecture, WindowsDirectory, SystemDirectory FROM Win32_OperatingSystem").Get().Cast<ManagementObject>().FirstOrDefault();
                
                return new SystemInfoModel
                {
                    OSVersion = os?["Caption"]?.ToString() ?? Environment.OSVersion.VersionString,
                    OSBuild = os?["BuildNumber"]?.ToString() ?? Environment.OSVersion.Version.Build.ToString(),
                    OSArchitecture = os?["OSArchitecture"]?.ToString() ?? (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"),
                    WindowsDirectory = os?["WindowsDirectory"]?.ToString() ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    SystemDirectory = os?["SystemDirectory"]?.ToString() ?? Environment.SystemDirectory,
                    CurrentUser = Environment.UserName,
                    IsAdmin = IsRunningAsAdmin(),
                    DotNetVersion = RuntimeInformation.FrameworkDescription,
                    TotalPhysicalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
                    ProcessorCount = Environment.ProcessorCount
                };
            });
        }

        public async Task<List<CleanCategoryGroup>> ScanAsync(CleanProfile profile, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            var groups = new List<CleanCategoryGroup>();
            var systemDrive = Environment.SystemDirectory.Substring(0, 1) + ":\\";
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var tempPath = Path.GetTempPath();
            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var systemDir = Environment.SystemDirectory;

            progress?.Report("Đang quét hệ thống...");

            // System Temp
            var systemTempGroup = CreateCategoryGroup(CleanCategory.SystemTemp, "System Temp", "Tệp tạm thời của hệ thống Windows", ItemRiskLevel.Safe);
            systemTempGroup.Items.AddRange(new[]
            {
                CreateItem("sys_temp", "Windows Temp", "C:\\Windows\\Temp", CleanCategory.SystemTemp, ItemRiskLevel.Safe, true),
                CreateItem("user_temp", "User Temp", tempPath, CleanCategory.SystemTemp, ItemRiskLevel.Safe),
                CreateItem("system_temp_var", "System Temp (%TEMP%)", Environment.ExpandEnvironmentVariables("%TEMP%"), CleanCategory.SystemTemp, ItemRiskLevel.Safe),
                CreateItem("prefetch", "Prefetch", "C:\\Windows\\Prefetch", CleanCategory.SystemTemp, ItemRiskLevel.Low, true),
            });
            groups.Add(systemTempGroup);

            // Windows Update
            var wuGroup = CreateCategoryGroup(CleanCategory.WindowsUpdate, "Windows Update", "Cache tải xuống Windows Update", ItemRiskLevel.Safe);
            wuGroup.Items.Add(CreateItem("wu_download", "SoftwareDistribution\\Download", "C:\\Windows\\SoftwareDistribution\\Download", CleanCategory.WindowsUpdate, ItemRiskLevel.Safe, true));
            groups.Add(wuGroup);

            // Browser Cache
            var browserGroup = CreateCategoryGroup(CleanCategory.BrowserCache, "Trình duyệt Web", "Cache và dữ liệu duyệt web", ItemRiskLevel.Safe);
            browserGroup.Items.AddRange(new[]
            {
                CreateItem("chrome_cache", "Google Chrome", GetChromeCachePath(), CleanCategory.BrowserCache, ItemRiskLevel.Safe),
                CreateItem("edge_cache", "Microsoft Edge", GetEdgeCachePath(), CleanCategory.BrowserCache, ItemRiskLevel.Safe),
                CreateItem("firefox_cache", "Mozilla Firefox", GetFirefoxCachePath(), CleanCategory.BrowserCache, ItemRiskLevel.Safe),
                CreateItem("brave_cache", "Brave Browser", GetBraveCachePath(), CleanCategory.BrowserCache, ItemRiskLevel.Safe),
                CreateItem("opera_cache", "Opera", GetOperaCachePath(), CleanCategory.BrowserCache, ItemRiskLevel.Safe),
                CreateItem("vivaldi_cache", "Vivaldi", GetVivaldiCachePath(), CleanCategory.BrowserCache, ItemRiskLevel.Safe),
            });
            groups.Add(browserGroup);

            // Dev Tools Cache
            var devGroup = CreateCategoryGroup(CleanCategory.DevToolsCache, "Công cụ phát triển", "Cache của các công cụ lập trình", ItemRiskLevel.Safe);
            devGroup.Items.AddRange(new[]
            {
                CreateItem("npm_cache", "npm cache", Path.Combine(localAppData, "npm-cache"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("yarn_cache", "Yarn cache", Path.Combine(localAppData, "Yarn"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("uv_cache", "uv cache (Python)", Path.Combine(localAppData, "uv"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("pip_cache", "pip cache", Path.Combine(localAppData, "pip", "Cache"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("nuget_cache", "NuGet cache", Path.Combine(localAppData, "NuGet", "Cache"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("go_cache", "Go build cache", Path.Combine(localAppData, "go-build"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("flutter_cache", "Flutter/Pub cache", Path.Combine(localAppData, "Pub", "Cache"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("dotnet_cache", ".NET/NuGet packages", Path.Combine(userProfile, ".nuget", "packages"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("gradle_cache", "Gradle cache", Path.Combine(userProfile, ".gradle", "caches"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("maven_cache", "Maven cache", Path.Combine(userProfile, ".m2", "repository"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("cargo_cache", "Rust Cargo cache", Path.Combine(userProfile, ".cargo", "registry", "cache"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("node_gyp", "node-gyp cache", Path.Combine(localAppData, "node-gyp"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("electron_cache", "Electron cache", Path.Combine(localAppData, "electron", "Cache"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("playwright_cache", "Playwright browsers", Path.Combine(localAppData, "ms-playwright"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
                CreateItem("cypress_cache", "Cypress cache", Path.Combine(localAppData, "Cypress"), CleanCategory.DevToolsCache, ItemRiskLevel.Safe),
            });
            groups.Add(devGroup);

            // User Temp
            var userTempGroup = CreateCategoryGroup(CleanCategory.UserTemp, "User Temp", "Thư mục tạm của người dùng hiện tại", ItemRiskLevel.Safe);
            userTempGroup.Items.Add(CreateItem("user_temp_main", "%TEMP%", tempPath, CleanCategory.UserTemp, ItemRiskLevel.Safe));
            userTempGroup.Items.Add(CreateItem("user_tmp", "%TMP%", Environment.ExpandEnvironmentVariables("%TMP%"), CleanCategory.UserTemp, ItemRiskLevel.Safe));
            groups.Add(userTempGroup);

            // Recycle Bin
            var recycleGroup = CreateCategoryGroup(CleanCategory.RecycleBin, "Thùng rác", "Các file đã xóa trong thùng rác", ItemRiskLevel.Low);
            recycleGroup.Items.Add(CreateItem("recycle_bin", "Thùng rác tất cả ổ", "", CleanCategory.RecycleBin, ItemRiskLevel.Low, true, true));
            groups.Add(recycleGroup);

            // System Logs
            var logsGroup = CreateCategoryGroup(CleanCategory.SystemLogs, "Log hệ thống", "Các file log của Windows", ItemRiskLevel.Low);
            logsGroup.Items.AddRange(new[]
            {
                CreateItem("windows_logs", "Windows\\Logs", "C:\\Windows\\Logs", CleanCategory.SystemLogs, ItemRiskLevel.Low, true),
                CreateItem("system32_logs", "System32\\LogFiles", "C:\\Windows\\System32\\LogFiles", CleanCategory.SystemLogs, ItemRiskLevel.Low, true),
                CreateItem("wmi_logs", "WMI Logs", "C:\\Windows\\System32\\wbem\\Logs", CleanCategory.SystemLogs, ItemRiskLevel.Low, true),
                CreateItem("panther_logs", "Panther (Setup logs)", "C:\\Windows\\Panther", CleanCategory.SystemLogs, ItemRiskLevel.Low, true),
            });
            groups.Add(logsGroup);

            if (profile >= CleanProfile.Deep)
            {
                // Driver Store
                var driverGroup = CreateCategoryGroup(CleanCategory.DriverStore, "Driver Store", "Driver cũ trong DriverStore (cẩn thận)", ItemRiskLevel.High);
                driverGroup.Items.Add(CreateItem("driver_store", "DriverStore\\FileRepository", "C:\\Windows\\System32\\DriverStore\\FileRepository", CleanCategory.DriverStore, ItemRiskLevel.High, true));
                groups.Add(driverGroup);

                // Discord Old Versions
                var discordGroup = CreateCategoryGroup(CleanCategory.DiscordOldVersions, "Discord", "Phiên bản cũ của Discord", ItemRiskLevel.Low);
                discordGroup.Items.Add(CreateItem("discord_old", "Discord old versions", GetDiscordPath(), CleanCategory.DiscordOldVersions, ItemRiskLevel.Low));
                groups.Add(discordGroup);

                // Playwright
                var pwGroup = CreateCategoryGroup(CleanCategory.PlaywrightBrowsers, "Playwright", "Browser binaries của Playwright", ItemRiskLevel.Safe);
                pwGroup.Items.Add(CreateItem("playwright_browsers", "Playwright browsers", Path.Combine(localAppData, "ms-playwright"), CleanCategory.PlaywrightBrowsers, ItemRiskLevel.Safe));
                groups.Add(pwGroup);

                // Minecraft Temp
                var mcGroup = CreateCategoryGroup(CleanCategory.MinecraftTemp, "Minecraft Temp", "Trạng thái tạm của Minecraft", ItemRiskLevel.Safe);
                mcGroup.Items.Add(CreateItem("mc_temp", "TmpMinecraftLocalState", Path.Combine(localAppData, "TmpMinecraftLocalState"), CleanCategory.MinecraftTemp, ItemRiskLevel.Safe));
                groups.Add(mcGroup);

                // Updater Caches
                var updaterGroup = CreateCategoryGroup(CleanCategory.UpdaterCaches, "Updater Caches", "Cache của các trình cập nhật ứng dụng", ItemRiskLevel.Safe);
                var updaterPaths = new[]
                {
                    "antigravity-updater", "lobehub-desktop-updater", "sklauncher-updater",
                    "deplao-updater", "agent-canvas-updater", "open-webui-updater",
                    "marktext-updater", "zalo-updater", "bitwarden-updater",
                    "copilot-updater", "orca-updater", "github-copilot-git"
                };
                foreach (var u in updaterPaths)
                {
                    var path = Path.Combine(localAppData, u);
                    if (Directory.Exists(path))
                        updaterGroup.Items.Add(CreateItem($"updater_{u}", u, path, CleanCategory.UpdaterCaches, ItemRiskLevel.Safe));
                }
                groups.Add(updaterGroup);
            }

            if (profile >= CleanProfile.Custom)
            {
                // Game Data
                var gameGroup = CreateCategoryGroup(CleanCategory.GameData, "Dữ liệu Game", "Cache và dữ liệu game (Roblox, etc.)", ItemRiskLevel.Medium);
                gameGroup.Items.AddRange(new[]
                {
                    CreateItem("roblox_data", "Roblox", Path.Combine(localAppData, "Roblox"), CleanCategory.GameData, ItemRiskLevel.Medium),
                    CreateItem("roblox_pcgdk", "Roblox PC GDK", Path.Combine(localAppData, "RobloxPCGDK"), CleanCategory.GameData, ItemRiskLevel.Medium),
                    CreateItem("lunar_client", "Lunar Client", Path.Combine(userProfile, ".lunarclient"), CleanCategory.GameData, ItemRiskLevel.Medium),
                    CreateItem("minecraft_x", "Minecraft X", Path.Combine(userProfile, ".minecraftx"), CleanCategory.GameData, ItemRiskLevel.Medium),
                    CreateItem("curseforge", "CurseForge", Path.Combine(userProfile, "curseforge"), CleanCategory.GameData, ItemRiskLevel.Medium),
                });
                groups.Add(gameGroup);

                // User Programs
                var progGroup = CreateCategoryGroup(CleanCategory.UserPrograms, "User Programs", "Ứng dụng cài đặt cho user (AppData\\Local\\Programs)", ItemRiskLevel.High);
                progGroup.Items.Add(CreateItem("user_programs", "AppData\\Local\\Programs", Path.Combine(localAppData, "Programs"), CleanCategory.UserPrograms, ItemRiskLevel.High));
                groups.Add(progGroup);

                // Other Users
                var otherUsersGroup = CreateCategoryGroup(CleanCategory.OtherUsers, "Người dùng khác", "Dữ liệu của các tài khoản khác", ItemRiskLevel.High);
                var users = await GetUserProfilesAsync();
                foreach (var u in users.Where(x => x.CanClean && x.Size > 0))
                {
                    otherUsersGroup.Items.Add(CreateItem($"user_{u.Name}", u.Name, u.Path, CleanCategory.OtherUsers, ItemRiskLevel.High));
                }
                groups.Add(otherUsersGroup);
            }

            if (profile >= CleanProfile.Nuclear)
            {
                // Nuclear options
                var nuclearGroup = CreateCategoryGroup(CleanCategory.CompactOS, "Nâng cao", "Tùy chọn nâng cao (chỉ cho expert)", ItemRiskLevel.Critical);
                nuclearGroup.Items.AddRange(new[]
                {
                    CreateItem("compact_os", "Compact OS", "compact.exe /CompactOS:always", CleanCategory.CompactOS, ItemRiskLevel.Critical, true, false, true),
                    CreateItem("hibernation", "Hibernation file", "powercfg /h off", CleanCategory.Hibernation, ItemRiskLevel.Critical, true, false, true),
                    CreateItem("system_restore", "System Restore space", "vssadmin resize shadowstorage", CleanCategory.SystemRestore, ItemRiskLevel.Critical, true, false, true),
                });
                groups.Add(nuclearGroup);

                var winOldGroup = CreateCategoryGroup(CleanCategory.WindowsOld, "Windows.old", "Thư mục Windows cũ sau nâng cấp", ItemRiskLevel.High);
                winOldGroup.Items.Add(CreateItem("windows_old", "Windows.old", "C:\\Windows.old", CleanCategory.WindowsOld, ItemRiskLevel.High, true));
                winOldGroup.Items.Add(CreateItem("windows_bt", "$Windows.~BT", "C:\\$Windows.~BT", CleanCategory.WindowsOld, ItemRiskLevel.High, true));
                groups.Add(winOldGroup);
            }

            // Calculate sizes
            progress?.Report("Đang tính toán dung lượng...");
            await CalculateAllSizes(groups, progress, cancellationToken);

            return groups;
        }

        private CleanCategoryGroup CreateCategoryGroup(CleanCategory category, string name, string description, ItemRiskLevel maxRisk)
        {
            return new CleanCategoryGroup
            {
                Category = category,
                Name = name,
                Description = description,
                MaxRiskLevel = maxRisk
            };
        }

        private CleanItem CreateItem(string id, string name, string path, CleanCategory category, ItemRiskLevel risk, bool isSystem = false, bool isSpecial = false, bool isCommand = false)
        {
            var item = new CleanItem
            {
                Id = id,
                Name = name,
                Path = path,
                Category = category,
                RiskLevel = risk,
                IsSystemItem = isSystem,
                RequiresAdmin = isSystem || risk >= ItemRiskLevel.High,
                Description = isCommand ? $"Lệnh: {path}" : $"Đường dẫn: {path}"
            };

            if (isCommand)
            {
                item.CleanAction = async (ci, prog) => await ExecuteCommandAsync(ci, prog);
                item.SizeCalculator = (ci) => EstimateCommandSize(ci);
            }
            else if (isSpecial && id == "recycle_bin")
            {
                item.CleanAction = async (ci, prog) => await EmptyRecycleBinAsync(ci, prog);
                item.SizeCalculator = (ci) => GetRecycleBinSize();
            }
            else
            {
                item.CleanAction = async (ci, prog) => await DeletePathAsync(ci, prog);
                item.SizeCalculator = (ci) => GetDirectorySize(ci.Path);
            }

            return item;
        }

        private async Task CalculateAllSizes(List<CleanCategoryGroup> groups, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            var allItems = groups.SelectMany(g => g.Items).Where(i => i.SizeCalculator != null).ToList();
            int completed = 0;

            foreach (var item in allItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    item.SizeBytes = await Task.Run(() => item.SizeCalculator!(item), cancellationToken);
                }
                catch
                {
                    item.SizeBytes = 0;
                }
                completed++;
                if (completed % 10 == 0)
                    progress?.Report($"Đã quét {completed}/{allItems.Count} mục...");
            }
        }

        public async Task<long> CalculateItemSizeAsync(CleanItem item, CancellationToken cancellationToken = default)
        {
            if (item.SizeCalculator != null)
            {
                return await Task.Run(() => item.SizeCalculator!(item), cancellationToken);
            }
            return 0;
        }

        private long GetDirectorySize(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return 0;
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .AsParallel()
                    .Sum(f => new FileInfo(f).Length);
            }
            catch { return 0; }
        }

        private long GetRecycleBinSize()
        {
            long total = 0;
            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    string recyclePath = Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin");
                    if (Directory.Exists(recyclePath))
                    {
                        total += GetDirectorySize(recyclePath);
                    }
                }
            }
            catch { }
            return total;
        }

        private long EstimateCommandSize(CleanItem item)
        {
            return item.Id switch
            {
                "compact_os" => 2L * 1024 * 1024 * 1024, // ~2GB estimate
                "hibernation" => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, // RAM size
                "system_restore" => 5L * 1024 * 1024 * 1024, // ~5GB estimate
                _ => 0
            };
        }

        private async Task ExecuteCommandAsync(CleanItem item, IProgress<string> progress)
        {
            try
            {
                progress?.Report($"Thực thi: {item.Path}");
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {item.Path}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas"
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode != 0)
                    {
                        string err = await proc.StandardError.ReadToEndAsync();
                        throw new Exception($"Exit code {proc.ExitCode}: {err}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi thực thi lệnh: {ex.Message}");
            }
        }

        private async Task EmptyRecycleBinAsync(CleanItem item, IProgress<string> progress)
        {
            try
            {
                progress?.Report("Đang dọn thùng rác...");
                // Use SHEmptyRecycleBin via P/Invoke or Shell32
                await Task.Run(() =>
                {
                    // PowerShell approach
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-Command \"Clear-RecycleBin -Force -Confirm:$false\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi dọn thùng rác: {ex.Message}");
            }
        }

        private async Task DeletePathAsync(CleanItem item, IProgress<string> progress)
        {
            if (string.IsNullOrEmpty(item.Path) || !Directory.Exists(item.Path))
                return;

            progress?.Report($"Đang xóa: {item.Path}");
            var files = Directory.GetFiles(item.Path, "*", SearchOption.AllDirectories);
            int deleted = 0;
            long deletedBytes = 0;

            foreach (var file in files)
            {
                try
                {
                    var fi = new FileInfo(file);
                    long size = fi.Length;
                    fi.Delete();
                    deletedBytes += size;
                    deleted++;
                    item.CleanedBytes = deletedBytes;
                }
                catch { }
            }

            // Delete empty directories
            try
            {
                var dirs = Directory.GetDirectories(item.Path, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length);
                foreach (var dir in dirs)
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private string GetChromeCachePath()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
            if (Directory.Exists(path))
            {
                var profiles = Directory.GetDirectories(path, "Profile*");
                if (profiles.Length > 0)
                    return Path.Combine(profiles[0], "Cache", "Cache_Data");
                return Path.Combine(path, "Default", "Cache", "Cache_Data");
            }
            return "";
        }

        private string GetEdgeCachePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default", "Cache", "Cache_Data");
        }

        private string GetFirefoxCachePath()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mozilla", "Firefox", "Profiles");
            if (Directory.Exists(path))
            {
                var profiles = Directory.GetDirectories(path);
                if (profiles.Length > 0)
                    return Path.Combine(profiles[0], "cache2");
            }
            return "";
        }

        private string GetBraveCachePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "User Data", "Default", "Cache", "Cache_Data");
        }

        private string GetOperaCachePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Opera Software", "Opera Stable", "Cache", "Cache_Data");
        }

        private string GetVivaldiCachePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vivaldi", "User Data", "Default", "Cache", "Cache_Data");
        }

        private string GetDiscordPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Discord");
        }
    }

    // Extension method for ObservableCollection
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null || items == null) return;
            foreach (var item in items)
                collection.Add(item);
        }
    }
}