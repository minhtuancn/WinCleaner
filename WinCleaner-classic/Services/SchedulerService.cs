using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ISchedulerService
    {
        Task<List<ScheduledTask>> GetTasksAsync();
        Task<ScheduledTask?> GetTaskAsync(string id);
        Task<bool> CreateTaskAsync(ScheduledTask task);
        Task<bool> UpdateTaskAsync(ScheduledTask task);
        Task<bool> DeleteTaskAsync(string id);
        Task<bool> EnableTaskAsync(string id, bool enabled);
        Task<bool> RunTaskAsync(string id);
        Task<ScheduledTask?> GetTaskByNameAsync(string name);
        Task<List<ScheduledTask>> GetSystemTasksAsync();
        Task<bool> SyncWithSystemAsync();
    }

    [SupportedOSPlatform("windows")]
    public class SchedulerService : ISchedulerService
    {
        private readonly string _tasksFilePath;
        private readonly object _lock = new();
        private List<ScheduledTask> _tasks = new();
        private readonly SchedulerSettings _settings = new();

        public SchedulerService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string tasksDir = Path.Combine(appData, "WinCleaner", "Scheduler");
            Directory.CreateDirectory(tasksDir);
            _tasksFilePath = Path.Combine(tasksDir, "scheduled_tasks.json");
            
            _ = LoadTasksAsync();
        }

        private async Task LoadTasksAsync()
        {
            if (File.Exists(_tasksFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(_tasksFilePath);
                    var tasks = JsonSerializer.Deserialize<List<ScheduledTask>>(json, GetJsonOptions());
                    lock (_lock)
                    {
                        _tasks = tasks ?? new List<ScheduledTask>();
                    }
                }
                catch
                {
                    lock (_lock)
                    {
                        _tasks = new List<ScheduledTask>();
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    _tasks = new List<ScheduledTask>();
                }
            }
        }

        private async Task SaveTasksAsync()
        {
            List<ScheduledTask> tasksCopy;
            lock (_lock)
            {
                tasksCopy = _tasks.ToList();
            }

            var json = JsonSerializer.Serialize(tasksCopy, GetJsonOptions());
            await File.WriteAllTextAsync(_tasksFilePath, json);
        }

        public async Task<List<ScheduledTask>> GetTasksAsync()
        {
            await SyncWithSystemAsync();
            lock (_lock)
            {
                return _tasks.OrderBy(t => t.Name).ToList();
            }
        }

        public async Task<ScheduledTask?> GetTaskAsync(string id)
        {
            lock (_lock)
            {
                return _tasks.FirstOrDefault(t => t.Id == id);
            }
        }

        public async Task<ScheduledTask?> GetTaskByNameAsync(string name)
        {
            lock (_lock)
            {
                return _tasks.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<bool> CreateTaskAsync(ScheduledTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Name))
                return false;

            if (await GetTaskByNameAsync(task.Name) != null)
                return false;

            task.Id = Guid.NewGuid().ToString();
            task.CreatedDate = DateTime.Now;
            task.ModifiedDate = DateTime.Now;

            lock (_lock)
            {
                _tasks.Add(task);
            }

            await SaveTasksAsync();
            
            if (task.Enabled)
            {
                await RegisterSystemTaskAsync(task);
            }

            return true;
        }

        public async Task<bool> UpdateTaskAsync(ScheduledTask task)
        {
            bool updated = false;
            var existing = await GetTaskAsync(task.Id);
            if (existing == null) return false;

            lock (_lock)
            {
                existing.Name = task.Name;
                existing.Description = task.Description;
                existing.TriggerType = task.TriggerType;
                existing.StartTime = task.StartTime;
                existing.DaysOfWeek = task.DaysOfWeek;
                existing.DayOfMonth = task.DayOfMonth;
                existing.IdleMinutes = task.IdleMinutes;
                existing.Action = task.Action;
                existing.Profile = task.Profile;
                existing.Databases = task.Databases;
                existing.Enabled = task.Enabled;
                existing.RunOnlyIfNetworkAvailable = task.RunOnlyIfNetworkAvailable;
                existing.StartWhenAvailable = task.StartWhenAvailable;
                existing.AllowHardTerminate = task.AllowHardTerminate;
                existing.MaxRunTime = task.MaxRunTime;
                existing.ModifiedDate = DateTime.Now;
                updated = true;
            }

            if (updated)
            {
                await SaveTasksAsync();
                await UnregisterSystemTaskAsync(existing.Name);
                if (existing.Enabled)
                {
                    await RegisterSystemTaskAsync(existing);
                }
            }

            return updated;
        }

        public async Task<bool> DeleteTaskAsync(string id)
        {
            var task = await GetTaskAsync(id);
            if (task == null) return false;

            lock (_lock)
            {
                _tasks.Remove(task);
            }

            await SaveTasksAsync();
            await UnregisterSystemTaskAsync($"{_settings.TaskNamePrefix}{task.Name}");

            return true;
        }

        public async Task<bool> EnableTaskAsync(string id, bool enabled)
        {
            var task = await GetTaskAsync(id);
            if (task == null) return false;

            task.Enabled = enabled;
            task.ModifiedDate = DateTime.Now;

            await SaveTasksAsync();

            if (enabled)
            {
                await RegisterSystemTaskAsync(task);
            }
            else
            {
                await UnregisterSystemTaskAsync($"{_settings.TaskNamePrefix}{task.Name}");
            }

            return true;
        }

        public async Task<bool> RunTaskAsync(string id)
        {
            var task = await GetTaskAsync(id);
            if (task == null) return false;

            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return false;

                var args = task.GetCommandLineArgs();
                
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    
                    var taskObj = await GetTaskAsync(task.Id);
                    if (taskObj != null)
                    {
                        taskObj.LastRunTime = DateTime.Now;
                        taskObj.RunCount++;
                        taskObj.LastRunSuccess = proc.ExitCode == 0;
                        taskObj.LastRunMessage = proc.ExitCode == 0 ? "Completed successfully" : $"Exit code: {proc.ExitCode}";
                        taskObj.ModifiedDate = DateTime.Now;
                        await SaveTasksAsync();
                    }

                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                var taskObj = await GetTaskAsync(task.Id);
                if (taskObj != null)
                {
                    taskObj.LastRunTime = DateTime.Now;
                    taskObj.LastRunSuccess = false;
                    taskObj.LastRunMessage = ex.Message;
                    taskObj.ModifiedDate = DateTime.Now;
                    await SaveTasksAsync();
                }
            }

            return false;
        }

        public async Task<List<ScheduledTask>> GetSystemTasksAsync()
        {
            var systemTasks = new List<ScheduledTask>();

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Query /TN \"{_settings.TaskNamePrefix}*\" /FO LIST /V",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var tasks = ParseSchTasksOutput(output);
                systemTasks.AddRange(tasks);
            }
            catch { }

            return systemTasks;
        }

        public async Task<bool> SyncWithSystemAsync()
        {
            try
            {
                var systemTasks = await GetSystemTasksAsync();
                
                lock (_lock)
                {
                    foreach (var sysTask in systemTasks)
                    {
                        var localTask = _tasks.FirstOrDefault(t => t.Name.Equals(sysTask.Name, StringComparison.OrdinalIgnoreCase));
                        if (localTask != null)
                        {
                            localTask.LastRunTime = sysTask.LastRunTime;
                            localTask.NextRunTime = sysTask.NextRunTime;
                            localTask.RunCount = sysTask.RunCount;
                            localTask.LastRunSuccess = sysTask.LastRunSuccess;
                        }
                    }
                }

                await SaveTasksAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RegisterSystemTaskAsync(ScheduledTask task)
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return false;

                var taskName = $"{_settings.TaskNamePrefix}{task.Name}";
                var args = task.GetCommandLineArgs();

                var schArgs = $"/Create /TN \"{taskName}\" /TR \"\\\"{exePath}\\\" {args}\" /RL HIGHEST";

                switch (task.TriggerType)
                {
                    case ScheduleTriggerType.Daily:
                        schArgs += $"/SC DAILY /ST {task.StartTime:HH:mm}";
                        break;
                    case ScheduleTriggerType.Weekly:
                        schArgs += $"/SC WEEKLY /D {task.DaysOfWeek.ToString().ToUpper()} /ST {task.StartTime:HH:mm}";
                        break;
                    case ScheduleTriggerType.Monthly:
                        schArgs += $"/SC MONTHLY /D {task.DayOfMonth} /ST {task.StartTime:HH:mm}";
                        break;
                    case ScheduleTriggerType.OnIdle:
                        schArgs += $"/SC ONIDLE /I {task.IdleMinutes}";
                        break;
                    case ScheduleTriggerType.OnStartup:
                        schArgs += "/SC ONSTART";
                        break;
                    case ScheduleTriggerType.OnLogon:
                        schArgs += "/SC ONLOGON";
                        break;
                    case ScheduleTriggerType.Once:
                        schArgs += $"/SC ONCE /ST {task.StartTime:HH:mm} /SD {DateTime.Now:yyyy/MM/dd}";
                        break;
                }

                schArgs += $" /RL HIGHEST /F";

                if (task.MaxRunTime > TimeSpan.Zero)
                {
                    schArgs += $" /DU {(int)task.MaxRunTime.TotalHours}:{(int)task.MaxRunTime.Minutes:00}";
                }

                if (task.RunOnlyIfNetworkAvailable)
                {
                    schArgs += " /NP";
                }

                if (_settings.WakeToRun)
                {
                    schArgs += " /W";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = schArgs,
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
                    return proc.ExitCode == 0;
                }
            }
            catch { }

            return false;
        }

        private async Task<bool> UnregisterSystemTaskAsync(string taskName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Delete /TN \"{taskName}\" /F",
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
                    return proc.ExitCode == 0;
                }
            }
            catch { }

            return false;
        }

        private List<ScheduledTask> ParseSchTasksOutput(string output)
        {
            var tasks = new List<ScheduledTask>();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            ScheduledTask? currentTask = null;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("TaskName:"))
                {
                    if (currentTask != null)
                        tasks.Add(currentTask);

                    var name = trimmed.Substring("TaskName:".Length).Trim();
                    if (name.StartsWith(_settings.TaskNamePrefix))
                    {
                        currentTask = new ScheduledTask
                        {
                            Name = name.Substring(_settings.TaskNamePrefix.Length),
                            Id = Guid.NewGuid().ToString()
                        };
                    }
                    else
                    {
                        currentTask = null;
                    }
                }
                else if (currentTask != null)
                {
                    if (trimmed.StartsWith("Next Run Time:"))
                    {
                        var timeStr = trimmed.Substring("Next Run Time:".Length).Trim();
                        if (DateTime.TryParse(timeStr, out var nextRun))
                            currentTask.NextRunTime = nextRun;
                    }
                    else if (trimmed.StartsWith("Last Run Time:"))
                    {
                        var timeStr = trimmed.Substring("Last Run Time:".Length).Trim();
                        if (DateTime.TryParse(timeStr, out var lastRun))
                            currentTask.LastRunTime = lastRun;
                    }
                    else if (trimmed.StartsWith("Last Result:"))
                    {
                        var resultStr = trimmed.Substring("Last Result:".Length).Trim();
                        if (int.TryParse(resultStr, out var result))
                            currentTask.LastRunSuccess = result == 0;
                    }
                }
            }

            if (currentTask != null)
                tasks.Add(currentTask);

            return tasks;
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