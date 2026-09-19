using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using TaskSchedulerTask = Microsoft.Win32.TaskScheduler.Task;
using TaskDefinition = Microsoft.Win32.TaskScheduler.TaskDefinition;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface ITaskSchedulerService
    {
        System.Threading.Tasks.Task<bool> CreateCleanupTaskAsync(string taskName, CleanProfile profile, TimeSpan triggerTime, DayOfWeek[] days, bool enabled = true, CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<bool> DeleteTaskAsync(string taskName, CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<List<TaskInfo>> GetTasksAsync(CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<bool> EnableTaskAsync(string taskName, bool enable, CancellationToken cancellationToken = default);
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
        private readonly TaskService _taskService;
        private const string TaskFolderName = "WinCleaner";

        public TaskSchedulerService()
        {
            _taskService = new TaskService();
            EnsureTaskFolderExists();
        }

        private void EnsureTaskFolderExists()
        {
            try
            {
                if (!_taskService.RootFolder.SubFolders.Exists(TaskFolderName))
                {
                    _taskService.RootFolder.CreateFolder(TaskFolderName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create task folder: {ex.Message}");
            }
        }

        public async System.Threading.Tasks.Task<bool> CreateCleanupTaskAsync(string taskName, CleanProfile profile, TimeSpan triggerTime, DayOfWeek[] days, bool enabled = true, CancellationToken cancellationToken = default)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var taskFolder = _taskService.RootFolder.SubFolders[TaskFolderName];
                    if (taskFolder == null)
                    {
                        return false;
                    }

                    // Remove existing task if it exists
                    if (taskFolder.Tasks.Exists(taskName))
                    {
                        taskFolder.DeleteTask(taskName, false);
                    }

                    var taskDefinition = _taskService.NewTask();
                    taskDefinition.RegistrationInfo.Description = $"WinCleaner scheduled cleanup - {profile}";
                    taskDefinition.Settings.AllowDemandStart = true;
                    taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                    taskDefinition.Settings.StopIfGoingOnBatteries = false;
                    taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromHours(2);
                    taskDefinition.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    taskDefinition.Settings.RunOnlyIfNetworkAvailable = false;

                    // Trigger
                    var weeklyTrigger = new WeeklyTrigger
                    {
                        DaysOfWeek = DaysOfWeekToTaskScheduler(days),
                        StartBoundary = DateTime.Today + triggerTime,
                        Enabled = enabled
                    };
                    taskDefinition.Triggers.Add(weeklyTrigger);

                    // Action: run WinCleaner CLI with clean command
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "WinCleaner.Cli.exe";
                    var action = new ExecAction(exePath, $"clean --profile {profile} --auto", null);
                    taskDefinition.Actions.Add(action);

                    taskFolder.RegisterTaskDefinition(taskName, taskDefinition, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken, null);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create scheduled task: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        public async System.Threading.Tasks.Task<bool> DeleteTaskAsync(string taskName, CancellationToken cancellationToken = default)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var taskFolder = _taskService.RootFolder.SubFolders[TaskFolderName];
                    if (taskFolder == null)
                    {
                        return true; // Folder doesn't exist, task doesn't exist
                    }

                    if (taskFolder.Tasks.Exists(taskName))
                    {
                        taskFolder.DeleteTask(taskName, false);
                    }
                    return true;
                }
                catch (FileNotFoundException)
                {
                    return true; // Task doesn't exist
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete task: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        public async System.Threading.Tasks.Task<List<TaskInfo>> GetTasksAsync(CancellationToken cancellationToken = default)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                var result = new List<TaskInfo>();
                try
                {
                    var taskFolder = _taskService.RootFolder.SubFolders[TaskFolderName];
                    if (taskFolder == null)
                    {
                        return result;
                    }

                    foreach (var task in taskFolder.Tasks)
                    {
                        if (task.Name.StartsWith("WinCleaner", StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(new TaskInfo
                            {
                                Name = task.Name,
                                Description = task.Definition.RegistrationInfo.Description,
                                Enabled = task.Enabled,
                                NextRunTime = task.NextRunTime,
                                LastRunTime = task.LastRunTime,
                                State = task.State.ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to get tasks: {ex.Message}");
                }
                return result;
            }, cancellationToken);
        }

        public async System.Threading.Tasks.Task<bool> EnableTaskAsync(string taskName, bool enable, CancellationToken cancellationToken = default)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var taskFolder = _taskService.RootFolder.SubFolders[TaskFolderName];
                    if (taskFolder == null)
                    {
                        return false;
                    }

                    var task = taskFolder.Tasks[taskName];
                    if (task != null)
                    {
                        task.Enabled = enable;
                        taskFolder.RegisterTaskDefinition(taskName, task.Definition, TaskCreation.Update, null, null, TaskLogonType.InteractiveToken, null);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to enable/disable task: {ex.Message}");
                    return false;
                }
            }, cancellationToken);
        }

        private Microsoft.Win32.TaskScheduler.DaysOfTheWeek DaysOfWeekToTaskScheduler(DayOfWeek[] days)
        {
            Microsoft.Win32.TaskScheduler.DaysOfTheWeek result = 0;
            foreach (var d in days)
            {
                result |= d switch
                {
                    DayOfWeek.Sunday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Sunday,
                    DayOfWeek.Monday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Monday,
                    DayOfWeek.Tuesday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Tuesday,
                    DayOfWeek.Wednesday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Wednesday,
                    DayOfWeek.Thursday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Thursday,
                    DayOfWeek.Friday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Friday,
                    DayOfWeek.Saturday => Microsoft.Win32.TaskScheduler.DaysOfTheWeek.Saturday,
                    _ => 0
                };
            }
            return result;
        }
    }
}