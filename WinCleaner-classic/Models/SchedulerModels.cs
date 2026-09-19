using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum ScheduleTriggerType
    {
        Daily,
        Weekly,
        Monthly,
        OnIdle,
        OnStartup,
        OnLogon,
        Once
    }

    public enum ScheduleDayOfWeek
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }

    public enum ScheduleAction
    {
        Scan,
        Clean,
        Shred
    }

    public class ScheduledTask : ObservableObject
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "";
        private string _description = "";
        private ScheduleTriggerType _triggerType = ScheduleTriggerType.Daily;
        private TimeSpan _startTime = new TimeSpan(2, 0, 0); // 2 AM default
        private DayOfWeek _daysOfWeek = DayOfWeek.Sunday;
        private int _dayOfMonth = 1;
        private int _idleMinutes = 10;
        private ScheduleAction _action = ScheduleAction.Clean;
        private CleanProfile _profile = CleanProfile.Safe;
        private string[] _databases = new[] { "all" };
        private bool _enabled = true;
        private bool _runOnlyIfNetworkAvailable = false;
        private bool _startWhenAvailable = true;
        private bool _allowHardTerminate = true;
        private TimeSpan _maxRunTime = TimeSpan.FromHours(2);
        private DateTime _createdDate = DateTime.Now;
        private DateTime _modifiedDate = DateTime.Now;
        private DateTime? _lastRunTime = null;
        private DateTime? _nextRunTime = null;
        private int _runCount = 0;
        private bool _lastRunSuccess = true;
        private string _lastRunMessage = "";

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ScheduleTriggerType TriggerType
        {
            get => _triggerType;
            set => SetProperty(ref _triggerType, value);
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DayOfWeek DaysOfWeek
        {
            get => _daysOfWeek;
            set => SetProperty(ref _daysOfWeek, value);
        }

        public int DayOfMonth
        {
            get => _dayOfMonth;
            set => SetProperty(ref _dayOfMonth, Math.Clamp(value, 1, 31));
        }

        public int IdleMinutes
        {
            get => _idleMinutes;
            set => SetProperty(ref _idleMinutes, Math.Max(value, 1));
        }

        public ScheduleAction Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }

        public CleanProfile Profile
        {
            get => _profile;
            set => SetProperty(ref _profile, value);
        }

        public string[] Databases
        {
            get => _databases;
            set => SetProperty(ref _databases, value ?? new[] { "all" });
        }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public bool RunOnlyIfNetworkAvailable
        {
            get => _runOnlyIfNetworkAvailable;
            set => SetProperty(ref _runOnlyIfNetworkAvailable, value);
        }

        public bool StartWhenAvailable
        {
            get => _startWhenAvailable;
            set => SetProperty(ref _startWhenAvailable, value);
        }

        public bool AllowHardTerminate
        {
            get => _allowHardTerminate;
            set => SetProperty(ref _allowHardTerminate, value);
        }

        public TimeSpan MaxRunTime
        {
            get => _maxRunTime;
            set => SetProperty(ref _maxRunTime, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate;
            set => SetProperty(ref _modifiedDate, value);
        }

        public DateTime? LastRunTime
        {
            get => _lastRunTime;
            set => SetProperty(ref _lastRunTime, value);
        }

        public DateTime? NextRunTime
        {
            get => _nextRunTime;
            set => SetProperty(ref _nextRunTime, value);
        }

        public int RunCount
        {
            get => _runCount;
            set => SetProperty(ref _runCount, value);
        }

        public bool LastRunSuccess
        {
            get => _lastRunSuccess;
            set => SetProperty(ref _lastRunSuccess, value);
        }

        public string LastRunMessage
        {
            get => _lastRunMessage;
            set => SetProperty(ref _lastRunMessage, value);
        }

        public string TriggerDescription => GetTriggerDescription();
        public string ActionDescription => GetActionDescription();

        private string GetTriggerDescription()
        {
            return TriggerType switch
            {
                ScheduleTriggerType.Daily => $"Daily at {StartTime:hh\\:mm}",
                ScheduleTriggerType.Weekly => $"Weekly on {DaysOfWeek} at {StartTime:hh\\:mm}",
                ScheduleTriggerType.Monthly => $"Monthly on day {DayOfMonth} at {StartTime:hh\\:mm}",
                ScheduleTriggerType.OnIdle => $"After {IdleMinutes} minutes idle",
                ScheduleTriggerType.OnStartup => "At system startup",
                ScheduleTriggerType.OnLogon => "At user logon",
                ScheduleTriggerType.Once => "One time only",
                _ => TriggerType.ToString()
            };
        }

        private string GetActionDescription()
        {
            return $"{Action} with {Profile} profile";
        }

        public string GetCommandLineArgs()
        {
            var args = $"{Action.ToString().ToLower()} --profile {Profile} --auto";
            
            if (Databases != null && Databases.Length > 0 && !Databases.Contains("all"))
            {
                args += $" --database {string.Join(",", Databases)}";
            }

            if (Action == ScheduleAction.Clean)
            {
                args += " --shutdown"; // Optional: could be configurable
            }

            return args;
        }
    }

    public class SchedulerSettings : ObservableObject
    {
        private bool _enabled = true;
        private bool _requireAdmin = true;
        private bool _wakeToRun = false;
        private bool _startWhenAvailable = true;
        private int _maxConcurrentTasks = 1;
        private TimeSpan _defaultMaxRunTime = TimeSpan.FromHours(2);
        private bool _createRestorePointBeforeClean = true;
        private bool _notifyOnCompletion = true;
        private bool _notifyOnFailure = true;
        private string _taskNamePrefix = "WinCleaner_";

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public bool RequireAdmin
        {
            get => _requireAdmin;
            set => SetProperty(ref _requireAdmin, value);
        }

        public bool WakeToRun
        {
            get => _wakeToRun;
            set => SetProperty(ref _wakeToRun, value);
        }

        public bool StartWhenAvailable
        {
            get => _startWhenAvailable;
            set => SetProperty(ref _startWhenAvailable, value);
        }

        public int MaxConcurrentTasks
        {
            get => _maxConcurrentTasks;
            set => SetProperty(ref _maxConcurrentTasks, Math.Clamp(value, 1, 10));
        }

        public TimeSpan DefaultMaxRunTime
        {
            get => _defaultMaxRunTime;
            set => SetProperty(ref _defaultMaxRunTime, value);
        }

        public bool CreateRestorePointBeforeClean
        {
            get => _createRestorePointBeforeClean;
            set => SetProperty(ref _createRestorePointBeforeClean, value);
        }

        public bool NotifyOnCompletion
        {
            get => _notifyOnCompletion;
            set => SetProperty(ref _notifyOnCompletion, value);
        }

        public bool NotifyOnFailure
        {
            get => _notifyOnFailure;
            set => SetProperty(ref _notifyOnFailure, value);
        }

        public string TaskNamePrefix
        {
            get => _taskNamePrefix;
            set => SetProperty(ref _taskNamePrefix, value);
        }
    }
}