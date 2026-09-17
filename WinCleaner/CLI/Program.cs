using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinCleaner.Models;
using WinCleaner.Services;
using WinCleaner.ViewModels;

namespace WinCleaner.CLI
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("WinCleaner CLI - Professional System Cleaner for Windows")
            {
                CreateScanCommand(),
                CreateCleanCommand(),
                CreateListCommand(),
                CreateDatabaseCommand(),
                CreateConfigCommand(),
                CreateVersionCommand()
            };

            return await rootCommand.InvokeAsync(args);
        }

        private static Command CreateScanCommand()
        {
            var profileOption = new Option<CleanProfile>(
                aliases: new[] { "--profile", "-p" },
                description: "Cleaning profile to use",
                getDefaultValue: () => CleanProfile.Safe);

            var databaseOption = new Option<string[]>(
                aliases: new[] { "--database", "-d" },
                description: "Databases to use (winapp2, winapp3, custom, all)",
                getDefaultValue: () => new[] { "all" });

            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Preview only, don't actually delete",
                getDefaultValue: () => false);

            var outputOption = new Option<OutputFormat>(
                aliases: new[] { "--output", "-o" },
                description: "Output format",
                getDefaultValue: () => OutputFormat.Text);

            var logOption = new Option<string>(
                aliases: new[] { "--log" },
                description: "Log file path");

            var jsonOption = new Option<bool>(
                aliases: new[] { "--json" },
                description: "Output as JSON",
                getDefaultValue: () => false);

            var command = new Command("scan", "Scan system for cleanable items")
            {
                profileOption,
                databaseOption,
                dryRunOption,
                outputOption,
                logOption,
                jsonOption
            };

            command.SetHandler(async (context) =>
            {
                var profile = context.ParseResult.GetValueForOption(profileOption);
                var databases = context.ParseResult.GetValueForOption(databaseOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var logPath = context.ParseResult.GetValueForOption(logOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var exitCode = await RunScanAsync(profile, databases, dryRun, output, logPath, json);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static Command CreateCleanCommand()
        {
            var profileOption = new Option<CleanProfile>(
                aliases: new[] { "--profile", "-p" },
                description: "Cleaning profile to use",
                getDefaultValue: () => CleanProfile.Safe);

            var databaseOption = new Option<string[]>(
                aliases: new[] { "--database", "-d" },
                description: "Databases to use (winapp2, winapp3, custom, all)",
                getDefaultValue: () => new[] { "all" });

            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Preview only, don't actually delete",
                getDefaultValue: () => false);

            var autoOption = new Option<bool>(
                aliases: new[] { "--auto", "-a" },
                description: "Automatic mode - no prompts",
                getDefaultValue: () => false);

            var shutdownOption = new Option<bool>(
                aliases: new[] { "--shutdown" },
                description: "Shutdown after cleaning",
                getDefaultValue: () => false);

            var outputOption = new Option<OutputFormat>(
                aliases: new[] { "--output", "-o" },
                description: "Output format",
                getDefaultValue: () => OutputFormat.Text);

            var logOption = new Option<string>(
                aliases: new[] { "--log" },
                description: "Log file path");

            var jsonOption = new Option<bool>(
                aliases: new[] { "--json" },
                description: "Output as JSON",
                getDefaultValue: () => false);

            var command = new Command("clean", "Clean system junk files")
            {
                profileOption,
                databaseOption,
                dryRunOption,
                autoOption,
                shutdownOption,
                outputOption,
                logOption,
                jsonOption
            };

            command.SetHandler(async (context) =>
            {
                var profile = context.ParseResult.GetValueForOption(profileOption);
                var databases = context.ParseResult.GetValueForOption(databaseOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                var auto = context.ParseResult.GetValueForOption(autoOption);
                var shutdown = context.ParseResult.GetValueForOption(shutdownOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var logPath = context.ParseResult.GetValueForOption(logOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var exitCode = await RunCleanAsync(profile, databases, dryRun, auto, shutdown, output, logPath);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static Command CreateListCommand()
        {
            var profileOption = new Option<CleanProfile>(
                aliases: new[] { "--profile", "-p" },
                description: "Filter by profile",
                getDefaultValue: () => CleanProfile.Safe);

            var databaseOption = new Option<string[]>(
                aliases: new[] { "--database", "-d" },
                description: "Databases to list",
                getDefaultValue: () => new[] { "all" });

            var outputOption = new Option<OutputFormat>(
                aliases: new[] { "--output", "-o" },
                description: "Output format",
                getDefaultValue: () => OutputFormat.Text);

            var jsonOption = new Option<bool>(
                aliases: new[] { "--json" },
                description: "Output as JSON",
                getDefaultValue: () => false);

            var command = new Command("list", "List available cleaners")
            {
                profileOption,
                databaseOption,
                outputOption,
                jsonOption
            };

            command.SetHandler(async (context) =>
            {
                var profile = context.ParseResult.GetValueForOption(profileOption);
                var databases = context.ParseResult.GetValueForOption(databaseOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var exitCode = await RunListAsync(profile, databases, output, json);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static Command CreateDatabaseCommand()
        {
            var updateCommand = new Command("update", "Update cleaning databases");
            var updateDbOption = new Option<string[]>(
                aliases: new[] { "--database", "-d" },
                description: "Databases to update (winapp2, winapp3, all)",
                getDefaultValue: () => new[] { "all" });
            updateCommand.AddOption(updateDbOption);
            updateCommand.SetHandler(async (context) =>
            {
                var databases = context.ParseResult.GetValueForOption(updateDbOption);
                var exitCode = await RunDatabaseUpdateAsync(databases);
                context.ExitCode = exitCode;
            });

            var listCommand = new Command("list", "List available databases");
            listCommand.SetHandler(async (context) =>
            {
                var exitCode = await RunDatabaseListAsync();
                context.ExitCode = exitCode;
            });

            var addCommand = new Command("add", "Add custom database");
            var pathArgument = new Argument<string>("path", "Path to .ini file");
            var nameOption = new Option<string>("--name", "Custom name for database");
            addCommand.AddArgument(pathArgument);
            addCommand.AddOption(nameOption);
            addCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(pathArgument);
                var name = context.ParseResult.GetValueForOption(nameOption);
                var exitCode = await RunDatabaseAddAsync(path, name);
                context.ExitCode = exitCode;
            });

            var databaseCommand = new Command("database", "Manage cleaning databases")
            {
                updateCommand,
                listCommand,
                addCommand
            };

            return databaseCommand;
        }

        private static Command CreateConfigCommand()
        {
            var exportCommand = new Command("export", "Export settings to file");
            var exportPathArg = new Argument<string>("path", "Output file path");
            exportCommand.AddArgument(exportPathArg);
            exportCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(exportPathArg);
                var exitCode = await RunConfigExportAsync(path);
                context.ExitCode = exitCode;
            });

            var importCommand = new Command("import", "Import settings from file");
            var importPathArg = new Argument<string>("path", "Input file path");
            importCommand.AddArgument(importPathArg);
            importCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(importPathArg);
                var exitCode = await RunConfigImportAsync(path);
                context.ExitCode = exitCode;
            });

            var configCommand = new Command("config", "Manage configuration")
            {
                exportCommand,
                importCommand
            };

            return configCommand;
        }

        private static Command CreateVersionCommand()
        {
            var command = new Command("version", "Show version information");
            command.SetHandler(() =>
            {
                Console.WriteLine("WinCleaner 1.0.0");
                Console.WriteLine("Professional System Cleaner for Windows");
                Console.WriteLine("Copyright (c) 2026 Minh Tuấn");
                Console.WriteLine("MIT License - https://github.com/minhtuancn/WinCleaner");
            });
            return command;
        }

        private static async Task<int> RunScanAsync(
            CleanProfile profile,
            string[] databases,
            bool dryRun,
            OutputFormat output,
            string? logPath,
            bool json)
        {
            try
            {
                using var host = CreateHost();
                var scanner = host.Services.GetRequiredService<ISystemScanner>();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();

                // Filter databases
                var dbs = await winapp2Service.GetAllDatabasesAsync();
                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                Console.WriteLine($"Scanning with profile: {profile}");
                Console.WriteLine($"Databases: {string.Join(", ", dbs.Select(db => db.Name))}");
                if (dryRun) Console.WriteLine("DRY RUN MODE - No files will be deleted");

                var progress = new Progress<string>(msg => Console.WriteLine($"[SCAN] {msg}"));
                var groups = await scanner.ScanAsync(profile, progress);

                var totalItems = groups.Sum(g => g.ItemCount);
                var totalSize = groups.Sum(g => g.TotalSize);

                if (json)
                {
                    var result = new
                    {
                        Profile = profile.ToString(),
                        Databases = dbs.Select(db => db.Name).ToArray(),
                        TotalItems = totalItems,
                        TotalSize = totalSize,
                        TotalSizeFormatted = FormatBytes(totalSize),
                        Groups = groups.Select(g => new
                        {
                            Name = g.Name,
                            Category = g.Category.ToString(),
                            ItemCount = g.ItemCount,
                            TotalSize = g.TotalSize,
                            TotalSizeFormatted = FormatBytes(g.TotalSize),
                            Items = g.Items.Select(i => new
                            {
                                Name = i.Name,
                                Size = i.SizeBytes,
                                SizeFormatted = i.SizeFormatted,
                                RiskLevel = i.RiskLevel.ToString(),
                                Category = i.Category.ToString(),
                                Path = i.Path,
                                Description = i.Description
                            }).ToArray()
                        }).ToArray()
                    };
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"\nScan Complete:");
                    Console.WriteLine($"  Total Items: {totalItems}");
                    Console.WriteLine($"  Total Size: {FormatBytes(totalSize)}");
                    Console.WriteLine($"  Groups: {groups.Count}");

                    foreach (var group in groups.Where(g => g.ItemCount > 0))
                    {
                        Console.WriteLine($"\n  {group.Name} ({group.ItemCount} items, {FormatBytes(group.TotalSize)})");
                        foreach (var item in group.Items.Where(i => i.SizeBytes > 0).OrderByDescending(i => i.SizeBytes).Take(5))
                        {
                            Console.WriteLine($"    - {item.Name}: {item.SizeFormatted} [{item.RiskLevel}]");
                        }
                        if (group.Items.Count(i => i.SizeBytes > 0) > 5)
                        {
                            Console.WriteLine($"    ... and {group.Items.Count(i => i.SizeBytes > 0) - 5} more");
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunCleanAsync(
            CleanProfile profile,
            string[] databases,
            bool dryRun,
            bool auto,
            bool shutdown,
            OutputFormat output,
            string? logPath)
        {
            try
            {
                using var host = CreateHost();
                var scanner = host.Services.GetRequiredService<ISystemScanner>();
                var cleaner = host.Services.GetRequiredService<ICleanerService>();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();

                var dbs = await winapp2Service.GetAllDatabasesAsync();
                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                cleaner.DryRunMode = dryRun;

                Console.WriteLine($"Cleaning with profile: {profile}");
                Console.WriteLine($"Databases: {string.Join(", ", dbs.Select(db => db.Name))}");
                if (dryRun) Console.WriteLine("DRY RUN MODE - No files will be deleted");
                if (!auto && !dryRun)
                {
                    Console.Write("Continue? (y/N): ");
                    var input = Console.ReadLine();
                    if (!input?.Equals("y", StringComparison.OrdinalIgnoreCase) ?? true)
                    {
                        Console.WriteLine("Cancelled.");
                        return 0;
                    }
                }

                var progress = new Progress<string>(msg => Console.WriteLine($"[CLEAN] {msg}"));
                var logProgress = new Progress<LogEntry>(entry => 
                {
                    Console.WriteLine($"[{entry.FormattedTime}] [{entry.Level}] {entry.Message}");
                });

                var groups = await scanner.ScanAsync(profile, new Progress<string>(msg => Console.WriteLine($"[SCAN] {msg}")));
                var result = await cleaner.CleanAsync(groups, progress, logProgress);

                Console.WriteLine($"\nClean Complete:");
                Console.WriteLine($"  Items Cleaned: {result.CleanedItems}/{result.TotalItems}");
                Console.WriteLine($"  Space Freed: {FormatBytes(result.TotalCleanedSize)}");
                Console.WriteLine($"  Duration: {result.Duration:mm\\:ss\\.ff}");
                if (result.FailedItems > 0)
                    Console.WriteLine($"  Failed: {result.FailedItems}");

                if (shutdown && !dryRun)
                {
                    Console.WriteLine("Shutting down...");
                    System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                }

                return result.FailedItems > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunListAsync(
            CleanProfile profile,
            string[] databases,
            OutputFormat output,
            bool json)
        {
            try
            {
                using var host = CreateHost();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();
                var dbs = await winapp2Service.GetAllDatabasesAsync();

                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                if (json)
                {
                    var result = dbs.Select(db => new
                    {
                        Name = db.Name,
                        Version = db.Version,
                        EntryCount = db.EntryCount,
                        LastUpdated = db.LastUpdated,
                        IsEnabled = db.IsEnabled,
                        Path = db.LocalPath
                    }).ToArray();
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Available Databases:");
                    foreach (var db in dbs)
                    {
                        Console.WriteLine($"  {db.Name} v{db.Version ?? "unknown"} - {db.EntryCount} entries - {(db.IsEnabled ? "Enabled" : "Disabled")} - {db.LocalPath}");
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunDatabaseUpdateAsync(string[] databases)
        {
            try
            {
                using var host = CreateHost();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();

                var dbs = await winapp2Service.GetAllDatabasesAsync();
                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                foreach (var db in dbs)
                {
                    Console.WriteLine($"Updating {db.Name}...");
                    var success = await winapp2Service.UpdateDatabaseAsync(db);
                    Console.WriteLine($"  {(success ? "Success" : "Failed")}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunDatabaseListAsync()
        {
            try
            {
                using var host = CreateHost();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();
                var dbs = await winapp2Service.GetAllDatabasesAsync();

                Console.WriteLine("Available Databases:");
                foreach (var db in dbs)
                {
                    Console.WriteLine($"  {db.Name} v{db.Version ?? "unknown"} - {db.EntryCount} entries - {(db.IsEnabled ? "Enabled" : "Disabled")}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunDatabaseAddAsync(string path, string? name)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"File not found: {path}");
                    return 1;
                }

                var customDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinCleaner", "Databases", "Custom");
                Directory.CreateDirectory(customDir);

                var fileName = name ?? Path.GetFileNameWithoutExtension(path);
                var destPath = Path.Combine(customDir, $"{fileName}.ini");
                
                File.Copy(path, destPath, true);
                Console.WriteLine($"Added custom database: {fileName} -> {destPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunConfigExportAsync(string path)
        {
            try
            {
                using var host = CreateHost();
                var settingsService = host.Services.GetRequiredService<ISettingsService>();
                var settings = await settingsService.LoadAsync();

                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(path, json);
                Console.WriteLine($"Settings exported to: {path}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunConfigImportAsync(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"File not found: {path}");
                    return 1;
                }

                using var host = CreateHost();
                var settingsService = host.Services.GetRequiredService<ISettingsService>();
                
                var json = await File.ReadAllTextAsync(path);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                
                if (settings != null)
                {
                    await settingsService.SaveAsync(settings);
                    Console.WriteLine($"Settings imported from: {path}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static IHost CreateHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                    });

                    services.AddHttpClient<IWinapp2Service, Winapp2Service>();

                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<ISystemScanner, SystemScanner>();
                    services.AddSingleton<ICleanerService, CleanerService>();
                    services.AddSingleton<IWinapp2Service, Winapp2Service>();
                    services.AddSingleton<IWinapp2ToCleanItemConverter, Winapp2ToCleanItemConverter>();
                })
                .Build();
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;
            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }
            return $"{dblBytes:0.##} {suffixes[i]}";
        }
    }

    public enum OutputFormat
    {
        Text,
        Json,
        Csv
    }
}