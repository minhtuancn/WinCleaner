using System;

namespace WinCleaner.Models
{
    /// <summary>
    /// Update information model for auto-update functionality
    /// </summary>
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public bool IsPrerelease { get; set; }
        public bool IsMandatory { get; set; }
        public long SizeBytes { get; set; }
    }

    /// <summary>
    /// Update status enumeration
    /// </summary>
    public enum UpdateStatus
    {
        Idle,
        Checking,
        Available,
        Downloading,
        Installing,
        Completed,
        Failed,
        UpToDate
    }

    /// <summary>
    /// Event arguments for update status changes
    /// </summary>
    public class UpdateStatusChangedEventArgs : EventArgs
    {
        public UpdateStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public double Progress { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
    }

    /// <summary>
    /// Interface for update service
    /// </summary>
    public interface IUpdateService
    {
        Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
        Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo update, IProgress<double> progress, CancellationToken cancellationToken = default);
        event EventHandler<UpdateStatusChangedEventArgs> UpdateStatusChanged;
    }
}