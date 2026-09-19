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

namespace WinCleaner.CLI
{
    public class Program
    {
        private static readonly Option<bool> SilentOption = new(
            aliases: new[] { "--silent", "-s" },
            description: "Suppress all output except errors",
            getDefaultValue: () => false);

        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("WinCleaner CLI - Professional System Cleaner for Windows")
            {
                CreateScanCommand(),
                CreateCleanCommand(),
                CreateShredCommand(),
                CreateScheduleCommand(),
                CreateExtensionCommand(),
                CreateLanguageCommand(),
                CreateListCommand(),
                CreateDatabaseCommand(),
                CreateConfigCommand(),
                CreateThemeCommand(),
                CreateVersionCommand()
            };

            rootCommand.AddGlobalOption(SilentOption);

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
                var silent = context.ParseResult.GetValueForOption(SilentOption);

                var exitCode = await RunScanAsync(profile, databases, dryRun, output, logPath, json, silent);
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
                var silent = context.ParseResult.GetValueForOption(SilentOption);

                var exitCode = await RunCleanAsync(profile, databases, dryRun, auto, shutdown, output, logPath, silent);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static Command CreateShredCommand()
        {
            var pathArgument = new Argument<string>("path", "File or directory path to shred");
            
            var algorithmOption = new Option<ShredAlgorithm>(
                aliases: new[] { "--algorithm", "-a" },
                description: "Shredding algorithm",
                getDefaultValue: () => ShredAlgorithm.DoD_5220_22_M);

            var recursiveOption = new Option<bool>(
                aliases: new[] { "--recursive", "-r" },
                description: "Shred directories recursively",
                getDefaultValue: () => true);

            var verifyOption = new Option<bool>(
                aliases: new[] { "--verify" },
                description: "Verify after shredding",
                getDefaultValue: () => true);

            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Preview only, don't actually shred",
                getDefaultValue: () => false);

            var outputOption = new Option<OutputFormat>(
                aliases: new[] { "--output", "-o" },
                description: "Output format",
                getDefaultValue: () => OutputFormat.Text);

            var jsonOption = new Option<bool>(
                aliases: new[] { "--json" },
                description: "Output as JSON",
                getDefaultValue: () => false);

            var command = new Command("shred", "Securely delete files/directories (file shredding)")
            {
                pathArgument,
                algorithmOption,
                recursiveOption,
                verifyOption,
                dryRunOption,
                outputOption,
                jsonOption
            };

            command.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(pathArgument);
                var algorithm = context.ParseResult.GetValueForOption(algorithmOption);
                var recursive = context.ParseResult.GetValueForOption(recursiveOption);
                var verify = context.ParseResult.GetValueForOption(verifyOption);
                var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var exitCode = await RunShredAsync(path, algorithm, recursive, verify, dryRun, output, json);
                context.ExitCode = exitCode;
            });

            return command;
        }

        private static async Task<int> RunShredAsync(
            string path,
            ShredAlgorithm algorithm,
            bool recursive,
            bool verify,
            bool dryRun,
            OutputFormat output,
            bool json)
        {
            try
            {
                using var host = CreateHost();
                var shredder = host.Services.GetRequiredService<ISecureDeleteService>();

                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    Console.Error.WriteLine($"Error: Path not found: {path}");
                    return 1;
                }

                Console.WriteLine($"Shredding: {path}");
                Console.WriteLine($"Algorithm: {algorithm}");
                Console.WriteLine($"Recursive: {recursive}");
                if (dryRun) Console.WriteLine("DRY RUN MODE - No files will be shredded");

                var progress = new Progress<string>(msg => Console.WriteLine($"[SHRED] {msg}"));

                bool success;
                if (File.Exists(path))
                {
                    if (dryRun)
                    {
                        Console.WriteLine($"[DRY RUN] Would shred file: {path} ({FormatBytes(new FileInfo(path).Length)}) with {algorithm}");
                        return 0;
                    }
                    success = await host.Services.GetRequiredService<ISecureDeleteService>().ShredFileAsync(path, algorithm, progress);
                }
                else if (Directory.Exists(path))
                {
                    if (!recursive)
                    {
                        Console.Error.WriteLine("Error: Directory specified but --recursive not set");
                        return 1;
                    }
                    if (dryRun)
                    {
                        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        long totalSize = files.Sum(f => new FileInfo(f).Length);
                        Console.WriteLine($"[DRY RUN] Would shred directory: {path} ({files.Length} files, {FormatBytes(totalSize)}) with {algorithm}");
                        return 0;
                    }
                    success = await host.Services.GetRequiredService<ISecureDeleteService>().ShredDirectoryAsync(path, algorithm, progress);
                }
                else
                {
                    Console.Error.WriteLine($"Error: Path not found: {path}");
                    return 1;
                }

                if (verify && !dryRun)
                {
                    Console.WriteLine("Verifying deletion...");
                    if (File.Exists(path) || Directory.Exists(path))
                    {
                        Console.WriteLine("WARNING: Path still exists after shredding!");
                        success = false;
                    }
                    else
                    {
                        Console.WriteLine("Verification passed: Path no longer exists");
                    }
                }

                if (json)
                {
                    var result = new
                    {
                        Path = path,
                        Algorithm = algorithm.ToString(),
                        Recursive = recursive,
                        Success = success,
                        Verified = verify && success && !File.Exists(path) && !Directory.Exists(path)
                    };
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"\nShred Complete:");
                    Console.WriteLine($"  Path: {path}");
                    Console.WriteLine($"  Algorithm: {algorithm}");
                    Console.WriteLine($"  Success: {success}");
                }

                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
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

        private static Command CreateScheduleCommand()
        {
            var listCommand = new Command("list", "List scheduled tasks");
            var listJsonOption = new Option<bool>(
                aliases: new[] { "--json" },
                description: "Output as JSON",
                getDefaultValue: () => false);
            listCommand.AddOption(listJsonOption);
            listCommand.SetHandler(async (context) =>
            {
                var json = context.ParseResult.GetValueForOption(listJsonOption);
                var exitCode = await RunScheduleListAsync(json);
                context.ExitCode = exitCode;
            });

            var createCommand = new Command("create", "Create scheduled task");
            var createNameArg = new Argument<string>("name", "Task name");
            var createActionOption = new Option<ScheduleAction>("--action", getDefaultValue: () => ScheduleAction.Clean);
            var createProfileOption = new Option<CleanProfile>("--profile", getDefaultValue: () => CleanProfile.Safe);
            var createTriggerOption = new Option<ScheduleTriggerType>("--trigger", getDefaultValue: () => ScheduleTriggerType.Daily);
            var createTimeOption = new Option<TimeSpan>("--time", getDefaultValue: () => new TimeSpan(2, 0, 0));
            var createDaysOption = new Option<DayOfWeek>("--days", getDefaultValue: () => DayOfWeek.Sunday);
            var createDayOfMonthOption = new Option<int>("--day", getDefaultValue: () => 1);
            var createIdleOption = new Option<int>("--idle", getDefaultValue: () => 10);
            var createDatabasesOption = new Option<string[]>("--databases", getDefaultValue: () => new[] { "all" });
            var createDescriptionOption = new Option<string>("--description", "Task description");
            var createEnabledOption = new Option<bool>("--enabled", getDefaultValue: () => true);
            var createMaxRunOption = new Option<TimeSpan>("--max-run", getDefaultValue: () => TimeSpan.FromHours(2));
            createCommand.AddArgument(createNameArg);
            createCommand.AddOption(createActionOption);
            createCommand.AddOption(createProfileOption);
            createCommand.AddOption(createTriggerOption);
            createCommand.AddOption(createTimeOption);
            createCommand.AddOption(createDaysOption);
            createCommand.AddOption(createDayOfMonthOption);
            createCommand.AddOption(createIdleOption);
            createCommand.AddOption(createDatabasesOption);
            createCommand.AddOption(createDescriptionOption);
            createCommand.AddOption(createEnabledOption);
            createCommand.AddOption(createMaxRunOption);
            createCommand.SetHandler(async (context) =>
            {
                var name = context.ParseResult.GetValueForArgument(createNameArg);
                var action = context.ParseResult.GetValueForOption(createActionOption);
                var profile = context.ParseResult.GetValueForOption(createProfileOption);
                var trigger = context.ParseResult.GetValueForOption(createTriggerOption);
                var time = context.ParseResult.GetValueForOption(createTimeOption);
                var days = context.ParseResult.GetValueForOption(createDaysOption);
                var dayOfMonth = context.ParseResult.GetValueForOption(createDayOfMonthOption);
                var idle = context.ParseResult.GetValueForOption(createIdleOption);
                var databases = context.ParseResult.GetValueForOption(createDatabasesOption);
                var description = context.ParseResult.GetValueForOption(createDescriptionOption);
                var enabled = context.ParseResult.GetValueForOption(createEnabledOption);
                var maxRun = context.ParseResult.GetValueForOption(createMaxRunOption);
                var exitCode = await RunScheduleCreateAsync(name, action, profile, trigger, time, days, dayOfMonth, idle, databases, description, enabled);
                context.ExitCode = exitCode;
            });

            var removeCommand = new Command("remove", "Remove scheduled task");
            var removeIdArg = new Argument<string>("id", "Task ID");
            removeCommand.AddArgument(removeIdArg);
            removeCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(removeIdArg);
                var exitCode = await RunScheduleRemoveAsync(id);
                context.ExitCode = exitCode;
            });

            var enableCommand = new Command("enable", "Enable/disable scheduled task");
            var enableIdArg = new Argument<string>("id", "Task ID");
            var enableStateArg = new Argument<bool>("state", "Enable (true) or disable (false)");
            enableCommand.AddArgument(enableIdArg);
            enableCommand.AddArgument(enableStateArg);
            enableCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(enableIdArg);
                var state = context.ParseResult.GetValueForArgument(enableStateArg);
                var exitCode = await RunScheduleEnableAsync(id, state);
                context.ExitCode = exitCode;
            });

            var runCommand = new Command("run", "Run scheduled task immediately");
            var runIdArg = new Argument<string>("id", "Task ID");
            runCommand.AddArgument(runIdArg);
            runCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(runIdArg);
                var exitCode = await RunScheduleRunAsync(id);
                context.ExitCode = exitCode;
            });

            var syncCommand = new Command("sync", "Sync with Windows Task Scheduler");
            syncCommand.SetHandler(async (context) =>
            {
                var exitCode = await RunScheduleSyncAsync();
                context.ExitCode = exitCode;
            });

            var scheduleCommand = new Command("schedule", "Manage scheduled tasks")
            {
                listCommand,
                createCommand,
                removeCommand,
                enableCommand,
                runCommand,
                syncCommand
            };

            return scheduleCommand;
        }

        private static async Task<int> RunScheduleListAsync(bool json)
        {
            try
            {
                using var host = CreateHost();
                var scheduler = host.Services.GetRequiredService<ISchedulerService>();
                var tasks = await host.Services.GetRequiredService<ISchedulerService>().GetTasksAsync();

                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(tasks, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"Scheduled Tasks ({tasks.Count}):");
                    foreach (var task in tasks.OrderBy(t => t.Name))
                    {
                        Console.WriteLine($"  [{task.Id.Substring(0, 8)}] {task.Name} - {(task.Enabled ? "Enabled" : "Disabled")}");
                        Console.WriteLine($"    Trigger: {task.TriggerDescription}");
                        Console.WriteLine($"    Action: {task.ActionDescription}");
                        Console.WriteLine($"    Profile: {task.Profile} | Databases: {string.Join(", ", task.Databases)}");
                        if (task.LastRunTime.HasValue)
                            Console.WriteLine($"    Last Run: {task.LastRunTime:yyyy-MM-dd HH:mm} ({task.LastRunMessage})");
                        if (task.NextRunTime.HasValue)
                            Console.WriteLine($"    Next Run: {task.NextRunTime:yyyy-MM-dd HH:mm}");
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

        private static async Task<int> RunScheduleCreateAsync(
            string name, ScheduleAction action, CleanProfile profile, ScheduleTriggerType trigger,
            TimeSpan time, DayOfWeek days, int dayOfMonth, int idle,
            string[] databases, string description, bool enabled)
        {
            try
            {
                using var host = CreateHost();
                var scheduler = host.Services.GetRequiredService<ISchedulerService>();

                var task = new ScheduledTask
                {
                    Name = name,
                    Description = description,
                    Action = action,
                    Profile = profile,
                    TriggerType = trigger,
                    StartTime = time,
                    DaysOfWeek = days,
                    DayOfMonth = dayOfMonth,
                    IdleMinutes = idle,
                    Databases = databases,
                    Enabled = enabled,
                    MaxRunTime = TimeSpan.FromHours(2)
                };

                var success = await host.Services.GetRequiredService<ISchedulerService>().CreateTaskAsync(task);
                Console.WriteLine(success ? $"Task created: {name} (ID: {task.Id.Substring(0, 8)})" : "Failed to create task");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunScheduleRemoveAsync(string id)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<ISchedulerService>().DeleteTaskAsync(id);
                Console.WriteLine(success ? "Task removed" : "Task not found");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunScheduleEnableAsync(string id, bool state)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<ISchedulerService>().EnableTaskAsync(id, state);
                Console.WriteLine(success ? $"Task {(state ? "enabled" : "disabled")}" : "Task not found");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunScheduleRunAsync(string id)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<ISchedulerService>().RunTaskAsync(id);
                Console.WriteLine(success ? "Task completed successfully" : "Task failed");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunScheduleSyncAsync()
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<ISchedulerService>().SyncWithSystemAsync();
                Console.WriteLine(success ? "Synced with Windows Task Scheduler" : "Sync failed");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static Command CreateExtensionCommand()
        {
            var listCommand = new Command("list", "List installed extensions");
            var listJsonOption = new Option<bool>("--json", "Output as JSON");
            listCommand.AddOption(listJsonOption);
            listCommand.SetHandler(async (context) =>
            {
                var json = context.ParseResult.GetValueForOption(listJsonOption);
                var exitCode = await RunExtensionListAsync(json);
                context.ExitCode = exitCode;
            });

            var loadCommand = new Command("load", "Load extension from assembly");
            var loadPathArg = new Argument<string>("path", "Path to extension assembly (.dll)");
            loadCommand.AddArgument(loadPathArg);
            loadCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(loadPathArg);
                var exitCode = await RunExtensionLoadAsync(path);
                context.ExitCode = exitCode;
            });

            var unloadCommand = new Command("unload", "Unload extension");
            var unloadIdArg = new Argument<string>("id", "Extension ID");
            unloadCommand.AddArgument(unloadIdArg);
            unloadCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(unloadIdArg);
                var exitCode = await RunExtensionUnloadAsync(id);
                context.ExitCode = exitCode;
            });

            var enableCommand = new Command("enable", "Enable/disable extension");
            var enableIdArg = new Argument<string>("id", "Extension ID");
            var enableStateArg = new Argument<bool>("state");
            enableStateArg.Description = "Enable (true) or disable (false)";
            enableCommand.AddArgument(enableIdArg);
            enableCommand.AddArgument(enableStateArg);
            enableCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(enableIdArg);
                var state = context.ParseResult.GetValueForArgument(enableStateArg);
                var exitCode = await RunExtensionEnableAsync(id, state);
                context.ExitCode = exitCode;
            });

            var installCommand = new Command("install", "Install extension from package");
            var installPathArg = new Argument<string>("path", "Path to extension package (.zip or .dll)");
            installCommand.AddArgument(installPathArg);
            installCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(installPathArg);
                var exitCode = await RunExtensionInstallAsync(path);
                context.ExitCode = exitCode;
            });

            var uninstallCommand = new Command("uninstall", "Uninstall extension");
            var uninstallIdArg = new Argument<string>("id", "Extension ID");
            uninstallCommand.AddArgument(uninstallIdArg);
            uninstallCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(uninstallIdArg);
                var exitCode = await RunExtensionUninstallAsync(id);
                context.ExitCode = exitCode;
            });

            var reloadCommand = new Command("reload", "Reload extension");
            var reloadIdArg = new Argument<string>("id", "Extension ID");
            reloadCommand.AddArgument(reloadIdArg);
            reloadCommand.SetHandler(async (context) =>
            {
                var id = context.ParseResult.GetValueForArgument(reloadIdArg);
                var exitCode = await RunExtensionReloadAsync(id);
                context.ExitCode = exitCode;
            });

            var discoverCommand = new Command("discover", "Discover extensions in directory");
            var discoverPathArg = new Argument<string>("path", "Directory to search for extensions");
            discoverCommand.AddArgument(discoverPathArg);
            discoverCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(discoverPathArg);
                var exitCode = await RunExtensionDiscoverAsync(path);
                context.ExitCode = exitCode;
            });

            var extensionCommand = new Command("extension", "Manage extensions/plugins")
            {
                listCommand,
                loadCommand,
                unloadCommand,
                enableCommand,
                installCommand,
                uninstallCommand,
                reloadCommand,
                discoverCommand
            };

            return extensionCommand;
        }

        private static async Task<int> RunExtensionListAsync(bool json)
        {
            try
            {
                using var host = CreateHost();
                var extManager = host.Services.GetRequiredService<IExtensionManager>();
                var extensions = await host.Services.GetRequiredService<IExtensionManager>().GetExtensionsAsync();

                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(extensions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"Extensions ({extensions.Count}):");
                    foreach (var ext in extensions.OrderBy(e => e.Name))
                    {
                        Console.WriteLine($"  [{ext.Id.Substring(0, Math.Min(8, ext.Id.Length))}] {ext.Name} v{ext.Version} by {ext.Author}");
                        Console.WriteLine($"    Type: {ext.Type} | {(ext.IsEnabled ? "Enabled" : "Disabled")} | {(ext.IsLoaded ? "Loaded" : "Not Loaded")}");
                        if (!string.IsNullOrEmpty(ext.ErrorMessage))
                            Console.WriteLine($"    Error: {ext.ErrorMessage}");
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

        private static async Task<int> RunExtensionLoadAsync(string path)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IExtensionManager>().LoadExtensionAsync(path);
                Console.WriteLine(success ? $"Extension loaded from: {path}" : "Failed to load extension");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunExtensionUnloadAsync(string id)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IExtensionManager>().UnloadExtensionAsync(id);
                Console.WriteLine(success ? "Extension unloaded" : "Extension not found");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunExtensionEnableAsync(string id, bool state)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IExtensionManager>().EnableExtensionAsync(id, state);
                Console.WriteLine(success ? $"Extension {(state ? "enabled" : "disabled")}" : "Extension not found");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunExtensionInstallAsync(string path)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IExtensionManager>().InstallExtensionAsync(path);
                Console.WriteLine(success ? $"Extension installed from: {path}" : "Install failed");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunExtensionUninstallAsync(string id)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IExtensionManager>().UninstallExtensionAsync(id);
                Console.WriteLine(success ? "Extension uninstalled" : "Extension not found");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunExtensionReloadAsync(string id)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IExtensionManager>().ReloadExtensionAsync(id);
                Console.WriteLine(success ? "Extension reloaded" : "Reload failed");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunExtensionDiscoverAsync(string path)
        {
            try
            {
                using var host = CreateHost();
                var extensions = await host.Services.GetRequiredService<IExtensionManager>().DiscoverExtensionsAsync(path);

                Console.WriteLine($"Discovered {extensions.Count} extensions in {path}:");
                foreach (var ext in extensions)
                {
                    Console.WriteLine($"  {ext.Name} v{ext.Version} by {ext.Author} ({ext.Type})");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static Command CreateLanguageCommand()
        {
            var listCommand = new Command("list", "List available languages");
            var listJsonOption = new Option<bool>("--json", "Output as JSON");
            listCommand.AddOption(listJsonOption);
            listCommand.SetHandler(async (context) =>
            {
                var json = context.ParseResult.GetValueForOption(listJsonOption);
                var exitCode = await RunLanguageListAsync(json);
                context.ExitCode = exitCode;
            });

            var setCommand = new Command("set", "Set current language");
            var setLangArg = new Argument<string>("language", "Language code (e.g., en-US, vi-VN)");
            setCommand.AddArgument(setLangArg);
            setCommand.SetHandler(async (context) =>
            {
                var langCode = context.ParseResult.GetValueForArgument(setLangArg);
                var exitCode = await RunLanguageSetAsync(langCode);
                context.ExitCode = exitCode;
            });

            var currentCommand = new Command("current", "Show current language");
            currentCommand.SetHandler(async (context) =>
            {
                var exitCode = await RunLanguageCurrentAsync();
                context.ExitCode = exitCode;
            });

            var importCommand = new Command("import", "Import language pack from file");
            var importPathArg = new Argument<string>("path", "Path to language pack file");
            importCommand.AddArgument(importPathArg);
            importCommand.SetHandler(async (context) =>
            {
                var path = context.ParseResult.GetValueForArgument(importPathArg);
                var exitCode = await RunLanguageImportAsync(path);
                context.ExitCode = exitCode;
            });

            var exportCommand = new Command("export", "Export language pack to file");
            var exportLangArg = new Argument<string>("language", "Language code to export");
            var exportPathArg = new Argument<string>("path", "Output file path");
            exportCommand.AddArgument(exportLangArg);
            exportCommand.AddArgument(exportPathArg);
            exportCommand.SetHandler(async (context) =>
            {
                var lang = context.ParseResult.GetValueForArgument(exportLangArg);
                var path = context.ParseResult.GetValueForArgument(exportPathArg);
                var exitCode = await RunLanguageExportAsync(lang, path);
                context.ExitCode = exitCode;
            });

            var missingCommand = new Command("missing", "Show missing translations for a language");
            var missingLangArg = new Argument<string>("language", "Language code");
            var missingJsonOption = new Option<bool>("--json", "Output as JSON");
            missingCommand.AddArgument(missingLangArg);
            missingCommand.AddOption(missingJsonOption);
            missingCommand.SetHandler(async (context) =>
            {
                var lang = context.ParseResult.GetValueForArgument(missingLangArg);
                var json = context.ParseResult.GetValueForOption(missingJsonOption);
                var exitCode = await RunLanguageMissingAsync(lang, json);
                context.ExitCode = exitCode;
            });

            var languageCommand = new Command("language", "Manage languages and translations")
            {
                listCommand,
                setCommand,
                currentCommand,
                importCommand,
                exportCommand,
                missingCommand
            };

            return languageCommand;
        }

        private static async Task<int> RunLanguageListAsync(bool json)
        {
            try
            {
                using var host = CreateHost();
                var localization = host.Services.GetRequiredService<ILocalizationService>();
                var languages = await host.Services.GetRequiredService<ILocalizationService>().GetAvailableLanguagesAsync();

                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(languages, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Available Languages:");
                    foreach (var lang in languages)
                    {
                        var name = lang.ToString();
                        Console.WriteLine($"  {name}");
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

        private static async Task<int> RunLanguageSetAsync(string langCode)
        {
            try
            {
                using var host = CreateHost();
                var localization = host.Services.GetRequiredService<ILocalizationService>();
                
                if (!Enum.TryParse<SupportedLanguage>(langCode, true, out var language))
                {
                    Console.Error.WriteLine($"Invalid language code: {langCode}");
                    return 1;
                }

                var success = await host.Services.GetRequiredService<ILocalizationService>().SetLanguageAsync(language);
                Console.WriteLine(success ? $"Language set to: {language}" : $"Failed to set language: {language}");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunLanguageCurrentAsync()
        {
            try
            {
                using var host = CreateHost();
                var localization = host.Services.GetRequiredService<ILocalizationService>();
                Console.WriteLine($"Current language: {localization.CurrentLanguage}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunLanguageImportAsync(string path)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<ILocalizationService>().LoadCustomLanguagePackAsync(path);
                Console.WriteLine(success ? $"Language pack imported from: {path}" : "Import failed");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunLanguageExportAsync(string language, string path)
        {
            try
            {
                using var host = CreateHost();
                
                if (!Enum.TryParse<SupportedLanguage>(language, true, out var lang))
                {
                    Console.Error.WriteLine($"Invalid language code: {language}");
                    return 1;
                }

                var success = await host.Services.GetRequiredService<ILocalizationService>().ExportLanguagePackAsync(lang, path);
                Console.WriteLine(success ? $"Language pack exported to: {path}" : "Export failed");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunLanguageMissingAsync(string language, bool json)
        {
            try
            {
                using var host = CreateHost();
                
                if (!Enum.TryParse<SupportedLanguage>(language, true, out var lang))
                {
                    Console.Error.WriteLine($"Invalid language code: {language}");
                    return 1;
                }

                var missing = await host.Services.GetRequiredService<ILocalizationService>().GetMissingTranslationsAsync(lang);

                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(missing, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"Missing translations for {language} ({missing.Count} keys):");
                    foreach (var kvp in missing.Take(50))
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    if (missing.Count > 50)
                        Console.WriteLine($"  ... and {missing.Count - 50} more");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static Command CreateThemeCommand()
        {
            var themeCommand = new Command("theme", "Manage UI theme");

            var listCommand = new Command("list", "List available themes");
            var listJsonOption = new Option<bool>(aliases: new[] { "--json" }, description: "Output as JSON", getDefaultValue: () => false);
            listCommand.AddOption(listJsonOption);
            listCommand.SetHandler(async (context) =>
            {
                var json = context.ParseResult.GetValueForOption(listJsonOption);
                var exitCode = await RunThemeListAsync(json);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(listCommand);

            var setCommand = new Command("set", "Set theme");
            var setThemeArg = new Argument<string>("theme", "Theme (Light, Dark, System)");
            setCommand.AddArgument(setThemeArg);
            setCommand.SetHandler(async (context) =>
            {
                var theme = context.ParseResult.GetValueForArgument(setThemeArg);
                var exitCode = await RunThemeSetAsync(theme);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(setCommand);

            var currentCommand = new Command("current", "Show current theme");
            currentCommand.SetHandler(async (context) =>
            {
                var exitCode = await RunThemeCurrentAsync();
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(currentCommand);

            var toggleCommand = new Command("toggle", "Toggle between Light and Dark theme");
            toggleCommand.SetHandler(async (context) =>
            {
                var exitCode = await RunThemeToggleAsync();
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(toggleCommand);

            var accentCommand = new Command("accent", "Set accent color");
            var accentColorArg = new Argument<string>("color", "Hex color code (e.g., #0078D4)");
            accentCommand.AddArgument(accentColorArg);
            accentCommand.SetHandler(async (context) =>
            {
                var color = context.ParseResult.GetValueForArgument(accentColorArg);
                var exitCode = await RunThemeAccentAsync(color);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(accentCommand);

            var animCommand = new Command("animations", "Enable/disable animations");
            var animStateArg = new Argument<bool>("state", "Enable (true) or disable (false)");
            animCommand.AddArgument(animStateArg);
            animCommand.SetHandler(async (context) =>
            {
                var state = context.ParseResult.GetValueForArgument(animStateArg);
                var exitCode = await RunThemeAnimationsAsync(state);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(animCommand);

            var transCommand = new Command("transparency", "Enable/disable transparency effects");
            var transStateArg = new Argument<bool>("state", "Enable (true) or disable (false)");
            transCommand.AddArgument(transStateArg);
            transCommand.SetHandler(async (context) =>
            {
                var state = context.ParseResult.GetValueForArgument(transStateArg);
                var exitCode = await RunThemeTransparencyAsync(state);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(transCommand);

            var systemCommand = new Command("system", "Use system theme");
            var sysStateArg = new Argument<bool>("state", "Enable (true) or disable (false)");
            systemCommand.AddArgument(sysStateArg);
            systemCommand.SetHandler(async (context) =>
            {
                var state = context.ParseResult.GetValueForArgument(sysStateArg);
                var exitCode = await RunThemeSystemAsync(state);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(systemCommand);

            var scaleCommand = new Command("scale", "Set UI scale");
            var scaleArg = new Argument<double>("scale", "Scale factor (0.5 - 2.0)");
            scaleCommand.AddArgument(scaleArg);
            scaleCommand.SetHandler(async (context) =>
            {
                var scale = context.ParseResult.GetValueForArgument(scaleArg);
                var exitCode = await RunThemeScaleAsync(scale);
                context.ExitCode = exitCode;
            });
            themeCommand.AddCommand(scaleCommand);

            return themeCommand;
        }

        private static async Task<int> RunThemeListAsync(bool json)
        {
            try
            {
                using var host = CreateHost();
                var themes = Enum.GetValues<AppTheme>().Cast<AppTheme>().ToList();
                var current = host.Services.GetRequiredService<IThemeConfigurationStore>().Settings.CurrentTheme;

                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { Themes = themes.Select(t => t.ToString()).ToArray(), Current = current.ToString() }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"Available Themes ({themes.Count}):");
                    foreach (var theme in themes)
                    {
                        var marker = theme == current ? " (current)" : "";
                        Console.WriteLine($"  {theme}{marker}");
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

        private static async Task<int> RunThemeSetAsync(string theme)
        {
            try
            {
                using var host = CreateHost();
                if (!Enum.TryParse<AppTheme>(theme, true, out var themeEnum))
                {
                    Console.Error.WriteLine($"Invalid theme: {theme}. Use Light, Dark, or System");
                    return 1;
                }

                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().SetThemeAsync(themeEnum);
                Console.WriteLine(success ? $"Theme set to: {themeEnum}" : $"Failed to set theme: {themeEnum}");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeCurrentAsync()
        {
            try
            {
                using var host = CreateHost();
                var theme = host.Services.GetRequiredService<IThemeConfigurationStore>().Settings.CurrentTheme;
                Console.WriteLine($"Current theme: {theme}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeToggleAsync()
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().ToggleThemeAsync();
                Console.WriteLine(success ? "Theme toggled" : "Failed to toggle theme");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeAccentAsync(string color)
        {
            try
            {
                using var host = CreateHost();
                if (!color.StartsWith("#") || color.Length != 7)
                {
                    Console.Error.WriteLine("Invalid color format. Use #RRGGBB format.");
                    return 1;
                }

                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().SetAccentColorAsync(color);
                Console.WriteLine(success ? $"Accent color set to: {color}" : "Failed to set accent color");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeAnimationsAsync(bool state)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().SetAnimationsEnabledAsync(state);
                Console.WriteLine(success ? $"Animations {(state ? "enabled" : "disabled")}" : "Failed to set animations");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeTransparencyAsync(bool state)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().SetTransparencyEnabledAsync(state);
                Console.WriteLine(success ? $"Transparency {(state ? "enabled" : "disabled")}" : "Failed to set transparency");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeSystemAsync(bool state)
        {
            try
            {
                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().SetUseSystemThemeAsync(state);
                Console.WriteLine(success ? $"System theme {(state ? "enabled" : "disabled")}" : "Failed to set system theme");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static async Task<int> RunThemeScaleAsync(double scale)
        {
            try
            {
                if (scale < 0.5 || scale > 2.0)
                {
                    Console.Error.WriteLine("Scale must be between 0.5 and 2.0");
                    return 1;
                }

                using var host = CreateHost();
                var success = await host.Services.GetRequiredService<IThemeConfigurationStore>().SetUiScaleAsync(scale);
                Console.WriteLine(success ? $"UI scale set to: {scale}" : "Failed to set UI scale");
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
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
            bool json,
            bool silent)
        {
            try
            {
                using var host = CreateHost();
                var scanner = host.Services.GetRequiredService<ISystemScanner>();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();

                // Filter databases
                var dbs = await host.Services.GetRequiredService<IWinapp2Service>().GetAllDatabasesAsync();
                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                if (!silent)
                {
                    Console.WriteLine($"Scanning with profile: {profile}");
                    Console.WriteLine($"Databases: {string.Join(", ", dbs.Select(db => db.Name))}");
                    if (dryRun) Console.WriteLine("DRY RUN MODE - No files will be deleted");
                }

                var progress = new Progress<string>(msg => { if (!silent) Console.WriteLine($"[SCAN] {msg}"); });
                var groups = await host.Services.GetRequiredService<ISystemScanner>().ScanAsync(profile, progress);

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
                else if (!silent)
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
            string? logPath,
            bool silent)
        {
            try
            {
                using var host = CreateHost();
                var scanner = host.Services.GetRequiredService<ISystemScanner>();
                var cleaner = host.Services.GetRequiredService<ICleanerService>();
                var winapp2Service = host.Services.GetRequiredService<IWinapp2Service>();

                var dbs = await host.Services.GetRequiredService<IWinapp2Service>().GetAllDatabasesAsync();
                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                cleaner.DryRunMode = dryRun;

                if (!silent)
                {
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
                }

                var progress = new Progress<string>(msg => { if (!silent) Console.WriteLine($"[CLEAN] {msg}"); });
                var logProgress = new Progress<LogEntry>(entry =>
                {
                    if (!silent)
                        Console.WriteLine($"[{entry.FormattedTime}] [{entry.Level}] {entry.Message}");
                });

                var groups = await host.Services.GetRequiredService<ISystemScanner>().ScanAsync(profile, new Progress<string>(msg => { if (!silent) Console.WriteLine($"[SCAN] {msg}"); }));
                var result = await cleaner.CleanAsync(groups, progress, logProgress);

                if (!silent)
                {
                    Console.WriteLine($"\nClean Complete:");
                    Console.WriteLine($"  Items Cleaned: {result.CleanedItems}/{result.TotalItems}");
                    Console.WriteLine($"  Space Freed: {FormatBytes(result.TotalCleanedSize)}");
                    Console.WriteLine($"  Duration: {result.Duration:mm\\:ss\\.ff}");
                    if (result.FailedItems > 0)
                        Console.WriteLine($"  Failed: {result.FailedItems}");
                }

                if (shutdown && !dryRun)
                {
                    if (!silent) Console.WriteLine("Shutting down...");
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
                var dbs = await host.Services.GetRequiredService<IWinapp2Service>().GetAllDatabasesAsync();

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

                var dbs = await host.Services.GetRequiredService<IWinapp2Service>().GetAllDatabasesAsync();
                if (!databases.Contains("all"))
                {
                    dbs = dbs.Where(db => databases.Contains(db.Name.ToLowerInvariant())).ToList();
                }

                foreach (var db in dbs)
                {
                    Console.WriteLine($"Updating {db.Name}...");
                    var success = await host.Services.GetRequiredService<IWinapp2Service>().UpdateDatabaseAsync(db);
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
                var dbs = await host.Services.GetRequiredService<IWinapp2Service>().GetAllDatabasesAsync();

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
                Console.WriteLine($"Added custom database: {name ?? Path.GetFileNameWithoutExtension(path)} -> {destPath}");
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
                var settings = await host.Services.GetRequiredService<ISettingsService>().LoadAsync();

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
                    await host.Services.GetRequiredService<ISettingsService>().SaveAsync(settings);
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
                    services.AddSingleton<ISecureDeleteService, SecureDeleteService>();
                    services.AddSingleton<ICustomRuleService, CustomRuleService>();
                    services.AddSingleton<ISchedulerService, SchedulerService>();
                    services.AddSingleton<IAppxService, AppxService>();
                    services.AddSingleton<IExplorerIntegrationService, ExplorerIntegrationService>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();

                    // New services
                    services.AddSingleton<ICookieService, CookieService>();
                    services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();
                    services.AddSingleton<IAiExplainer, AiExplainer>();

                    // Theme configuration store (no WPF dependencies)
                    services.AddSingleton<IThemeConfigurationStore, ThemeConfigurationStore>();
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