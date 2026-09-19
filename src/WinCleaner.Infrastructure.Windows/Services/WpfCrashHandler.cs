using System;
using System.Runtime.Versioning;
using System.Windows;
using Microsoft.Extensions.Logging;
using WinCleaner.Services;

namespace WinCleaner.Services
{
    /// <summary>
    /// WPF-specific crash handling: hooks the Dispatcher unhandled exception event.
    /// Core classifies/logs via IResilienceService; this class only wires UI-layer events.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WpfCrashHandler
    {
        private readonly IResilienceService _resilienceService;
        private readonly ILogger<WpfCrashHandler> _logger;

        public WpfCrashHandler(IResilienceService resilienceService, ILogger<WpfCrashHandler> logger)
        {
            _resilienceService = resilienceService;
            _logger = logger;
        }

        /// <summary>
        /// Attach the dispatcher unhandled exception handler. Call once after the Application is created.
        /// </summary>
        public void Register()
        {
            if (System.Windows.Application.Current == null)
            {
                _logger.LogWarning("No WPF Application present; dispatcher crash handling not registered");
                return;
            }

            System.Windows.Application.Current.DispatcherUnhandledException += (sender, e) =>
            {
                e.Handled = _resilienceService.HandleUiException(e.Exception, "DispatcherUnhandledException");
            };

            _logger.LogInformation("WPF dispatcher crash handling registered");
        }
    }
}
