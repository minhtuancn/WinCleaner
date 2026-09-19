using System;
using System.IO;
using System.Net.Http;
using System.Windows;  // WPF Application
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;
using WinCleaner.Services;
using WinCleaner.ViewModels;
using WinCleaner.Views;

namespace WinCleaner
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;

        public static IServiceProvider Services { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            _host = CreateHostBuilder().Build();
            Services = _host.Services;
            await _host.StartAsync();

            var shellWindow = _host.Services.GetRequiredService<ShellWindow>();
            shellWindow.Show();

            // Initialize resilience service (registers global exception handlers)
            var resilience = _host.Services.GetRequiredService<IResilienceService>();
            _ = resilience; // Touch to ensure initialization

            // Wire WPF dispatcher crash handling
            _host.Services.GetRequiredService<WpfCrashHandler>().Register();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Clear session marker on clean exit
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string markerFile = Path.Combine(localAppData, "WinCleaner", ".session-active");
                if (File.Exists(markerFile))
                    File.Delete(markerFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear session marker: {ex.Message}");
            }

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddDebug();
                        builder.AddConsole();
                    });

                    // Options
                    services.Configure<StructuredLoggingOptions>(context.Configuration.GetSection("StructuredLogging"));

                    // HttpClient for Winapp2 downloads
                    services.AddHttpClient<IWinapp2Service, Winapp2Service>();

                    // Core Services
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<ISystemScanner, SystemScanner>();
                    services.AddSingleton<ICleanerService, CleanerService>();
                    services.AddSingleton<IWinapp2Service, Winapp2Service>();
                    services.AddSingleton<IWinapp2ToCleanItemConverter, Winapp2ToCleanItemConverter>();
                    services.AddSingleton<ISecureDeleteService, SecureDeleteService>();
                    services.AddSingleton<ICustomRuleService, CustomRuleService>();
                    services.AddSingleton<ISchedulerService, SchedulerService>();
                    services.AddSingleton<IAppxService, AppxService>();
                    services.AddSingleton<IExplorerIntegrationService, ExplorerIntegrationService>();
                    services.AddSingleton<IWindowStateService, WindowStateService>();
                    services.AddSingleton<WinCleaner.Services.IExtensionManager, ExtensionManager>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();

                    // Resilience / Production Services (Issue #21)
                    services.AddSingleton<IStructuredLogger, StructuredLogger>();
                    services.AddSingleton<IResilienceService, ResilienceService>();
                    services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
                    services.AddSingleton<WpfCrashHandler>();

                    // Theme services - separated configuration from WPF application
                    services.AddSingleton<IThemeConfigurationStore, ThemeConfigurationStore>();
                    services.AddSingleton<IWpfThemeApplicator, WpfThemeApplicator>();
                    services.AddSingleton<IThemeService, ThemeService>();

                    // Safety / CleanupPlan Services (Issue #3)
                    services.AddSingleton<IPathSafetyValidator, PathSafetyValidator>();
                    services.AddSingleton<IAppRunningGuard, AppRunningGuard>();
                    services.AddSingleton<ICleanupPlanService, CleanupPlanService>();

                    // ViewModels - App Shell pattern
                    services.AddTransient<ShellViewModel>();
                    services.AddTransient<HealthCheckViewModel>();
                    services.AddTransient<AdvancedCleanViewModel>();
                    services.AddTransient<StorageViewModel>();
                    services.AddTransient<ManualCleanupViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<DiagnosticsViewModel>();
                    services.AddTransient<LiveTimelineViewModel>();

                    // Views
                    services.AddTransient<ShellWindow>(provider => 
                    {
                        var vm = provider.GetRequiredService<ShellViewModel>();
                        return new ShellWindow { DataContext = vm };
                    });
                });
        }
    }
}