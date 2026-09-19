using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ITaskSchedulerService
    {
        Task<bool> CreateCleanupTaskAsync(string taskName, CleanProfile profile, TimeSpan triggerTime, DayOfWeek[] days, bool enabled = true, CancellationToken cancellationToken = default);
        Task<bool> DeleteTaskAsync(string taskName, CancellationToken cancellationToken = default);
        Task<List<TaskInfo>> GetTasksAsync(CancellationToken cancellationToken = default);
        Task<bool> EnableTaskAsync(string taskName, bool enable, CancellationToken cancellationToken = default);
    }

    public class TaskInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public DateTime? NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string State { get; set; } = string.Empty;
    }

    public class TaskSchedulerService : ITaskSchedulerService
    {
        // Stub implementation – replace with real Task Scheduler integration later
        public Task<bool> CreateCleanupTaskAsync(string taskName, CleanProfile profile, TimeSpan triggerTime, DayOfWeek[] days, bool enabled = true, CancellationToken cancellationToken = default)
        {
            // TODO: implement using Microsoft.Win32.TaskScheduler
            return Task.FromResult(false);
        }

        public Task<bool> DeleteTaskAsync(string taskName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<List<TaskInfo>> GetTasksAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<TaskInfo>());
        }

        public Task<bool> EnableTaskAsync(string taskName, bool enable, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}