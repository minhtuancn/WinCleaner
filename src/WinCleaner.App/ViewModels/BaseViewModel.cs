using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private bool _isProgressIndeterminate;

        internal Action<string>? LogMessage;

        protected void Log(string message, LogLevel level = LogLevel.Info)
        {
            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
        }

        public void SubscribeToLog(Action<string> handler)
        {
            LogMessage += handler;
        }

        public void UnsubscribeFromLog(Action<string> handler)
        {
            LogMessage -= handler;
        }

        protected void LogInfo(string message) => Log(message, LogLevel.Info);
        protected void LogSuccess(string message) => Log(message, LogLevel.Success);
        protected void LogWarning(string message) => Log(message, LogLevel.Warning);
        protected void LogError(string message) => Log(message, LogLevel.Error);
        protected void LogDebug(string message) => Log(message, LogLevel.Debug);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success
    }
}