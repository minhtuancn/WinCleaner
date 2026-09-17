using System;
using System.IO;
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

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
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

                    // Services
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<ISystemScanner, SystemScanner>();
                    services.AddSingleton<ICleanerService, CleanerService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();

                    // Views
                    services.AddTransient<MainWindow>(provider => 
                    {
                        var vm = provider.GetRequiredService<MainViewModel>();
                        return new MainWindow { DataContext = vm };
                    });
                });
        }
    }
}