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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            _host = CreateHostBuilder().Build();
            await _host.StartAsync();

            // Initialize theme service before creating window
            var themeService = _host.Services.GetRequiredService<IThemeService>();
            await themeService.InitializeAsync();

            var shellWindow = _host.Services.GetRequiredService<ShellWindow>();
            shellWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
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

                    // HttpClient for Winapp2 downloads
                    services.AddHttpClient<IWinapp2Service, Winapp2Service>();

                    // Services
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<ISystemScanner, SystemScanner>();
                    services.AddSingleton<ICleanerService, CleanerService>();
                    services.AddSingleton<IWinapp2Service, Winapp2Service>();
                    services.AddSingleton<IWinapp2ToCleanItemConverter, Winapp2ToCleanItemConverter>();
                    services.AddSingleton<ISecureDeleteService, SecureDeleteService>();
                    services.AddSingleton<ICustomRuleService, CustomRuleService>();
                    services.AddSingleton<ISchedulerService, SchedulerService>();
                    services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();
                    services.AddSingleton<ICookieService, CookieService>();
                    services.AddSingleton<ISecureDeleteService, SecureDeleteService>();
                    services.AddSingleton<IAppxService, AppxService>();
                    services.AddSingleton<IExplorerIntegrationService, ExplorerIntegrationService>();
                    services.AddSingleton<IWindowStateService, WindowStateService>();
                    services.AddSingleton<WinCleaner.Services.IExtensionManager, ExtensionManager>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();
                    services.AddSingleton<IThemeService, ThemeService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<CleanerViewModel>();
                    services.AddTransient<ToolsViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<ShellViewModel>();
                    services.AddTransient<SchedulerTabViewModel>();
                    services.AddTransient<CookieTabViewModel>();
                    services.AddTransient<ShredTabViewModel>();
                    services.AddTransient<AppXTabViewModel>();

                    // Views
                    services.AddTransient<ShellWindow>(provider => 
                    {
                        var vm = provider.GetRequiredService<ShellViewModel>();
                        var windowStateService = provider.GetRequiredService<IWindowStateService>();
                        return new ShellWindow(windowStateService) { DataContext = vm };
                    });
                });
        }
    }
}