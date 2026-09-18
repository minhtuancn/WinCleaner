using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WinCleaner.Models;

namespace WinCleaner.Services
{
    public interface IExtensionManager
    {
        Task<List<ExtensionInfo>> GetExtensionsAsync();
        Task<ExtensionInfo?> GetExtensionAsync(string id);
        Task<bool> LoadExtensionAsync(string assemblyPath);
        Task<bool> UnloadExtensionAsync(string id);
        Task<bool> EnableExtensionAsync(string id, bool enabled);
        Task<bool> InstallExtensionAsync(string packagePath);
        Task<bool> UninstallExtensionAsync(string id);
        Task<List<ExtensionInfo>> DiscoverExtensionsAsync(string directory);
        Task<bool> ReloadExtensionAsync(string id);
        event EventHandler<ExtensionLoadedEventArgs> ExtensionLoaded;
        event EventHandler<ExtensionUnloadedEventArgs> ExtensionUnloaded;
    }

    [SupportedOSPlatform("windows")]
    public class ExtensionManager : IExtensionManager
    {
        private readonly string _extensionsDirectory;
        private readonly object _lock = new();
        private readonly Dictionary<string, ExtensionInfo> _extensions = new();
        private readonly Dictionary<string, AssemblyLoadContext> _loadContexts = new();
        private readonly Dictionary<string, IExtension> _instances = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExtensionManager> _logger;

        public event EventHandler<ExtensionLoadedEventArgs> ExtensionLoaded;
        public event EventHandler<ExtensionUnloadedEventArgs> ExtensionUnloaded;

        public ExtensionManager(IServiceProvider serviceProvider, ILogger<ExtensionManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _extensionsDirectory = Path.Combine(appData, "WinCleaner", "Extensions");
            Directory.CreateDirectory(_extensionsDirectory);

            _ = DiscoverExtensionsAsync(_extensionsDirectory);
        }

        public async Task<List<ExtensionInfo>> GetExtensionsAsync()
        {
            lock (_lock)
            {
                return _extensions.Values.OrderBy(e => e.Name).ToList();
            }
        }

        public async Task<ExtensionInfo?> GetExtensionAsync(string id)
        {
            lock (_lock)
            {
                return _extensions.TryGetValue(id, out var ext) ? ext : null;
            }
        }

        public async Task<bool> LoadExtensionAsync(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                return false;

            try
            {
                var loadContext = new AssemblyLoadContext(assemblyPath);
                var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

                var extensionTypes = assembly.GetTypes()
                    .Where(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (!extensionTypes.Any())
                {
                    loadContext.Unload();
                    return false;
                }

                foreach (var extType in extensionTypes)
                {
                    var instance = (IExtension)ActivatorUtilities.CreateInstance(_serviceProvider, extType);
                    var metadata = instance.GetMetadata();

                    var extInfo = new ExtensionInfo
                    {
                        Id = metadata.Id,
                        Name = metadata.Name,
                        Version = metadata.Version,
                        Author = metadata.Author,
                        Description = metadata.Description,
                        Type = metadata.Type,
                        AssemblyPath = assemblyPath,
                        Metadata = metadata,
                        IsEnabled = true,
                        IsLoaded = true,
                        LoadedAt = DateTime.Now
                    };

                    var initSuccess = await instance.InitializeAsync(_serviceProvider);
                    if (!initSuccess)
                    {
                        _logger.LogWarning($"Extension {metadata.Id} failed to initialize");
                        continue;
                    }

                    lock (_lock)
                    {
                        _extensions[extInfo.Id] = extInfo;
                        _loadContexts[extInfo.Id] = loadContext;
                        _instances[extInfo.Id] = instance;
                    }

                    ExtensionLoaded?.Invoke(this, new ExtensionLoadedEventArgs
                    {
                        ExtensionId = extInfo.Id,
                        ExtensionName = extInfo.Name
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load extension from {assemblyPath}");
                return false;
            }
        }

        public async Task<bool> UnloadExtensionAsync(string id)
        {
            lock (_lock)
            {
                if (!_extensions.TryGetValue(id, out var extInfo))
                    return false;

                if (!_instances.TryGetValue(id, out var instance))
                    return false;

                if (!_loadContexts.TryGetValue(id, out var loadContext))
                    return false;

                try
                {
                    var shutdownTask = instance.ShutdownAsync();
                    shutdownTask.Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error shutting down extension {id}");
                }

                _instances.Remove(id);
                _loadContexts.Remove(id);
                _extensions.Remove(id);

                loadContext.Unload();

                ExtensionUnloaded?.Invoke(this, new ExtensionUnloadedEventArgs
                {
                    ExtensionId = id,
                    ExtensionName = extInfo.Name
                });

                return true;
            }
        }

        public async Task<bool> EnableExtensionAsync(string id, bool enabled)
        {
            lock (_lock)
            {
                if (!_extensions.TryGetValue(id, out var extInfo))
                    return false;

                extInfo.IsEnabled = enabled;
                extInfo.ErrorMessage = "";
                return true;
            }
        }

        public async Task<bool> InstallExtensionAsync(string packagePath)
        {
            if (!File.Exists(packagePath))
                return false;

            try
            {
                var extDirName = Path.GetFileNameWithoutExtension(packagePath);
                var extDir = Path.Combine(_extensionsDirectory, extDirName);
                Directory.CreateDirectory(extDir);

                if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(packagePath, extDir, true);
                }
                else if (packagePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    var destPath = Path.Combine(extDir, Path.GetFileName(packagePath));
                    File.Copy(packagePath, destPath, true);
                }
                else
                {
                    return false;
                }

                var dllFiles = Directory.GetFiles(extDir, "*.dll", SearchOption.AllDirectories);
                foreach (var dll in dllFiles)
                {
                    await LoadExtensionAsync(dll);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to install extension from {packagePath}");
                return false;
            }
        }

        public async Task<bool> UninstallExtensionAsync(string id)
        {
            await UnloadExtensionAsync(id);

            lock (_lock)
            {
                if (_extensions.TryGetValue(id, out var extInfo))
                {
                    try
                    {
                        var extDir = Path.Combine(_extensionsDirectory, extInfo.Id);
                        if (Directory.Exists(extDir))
                        {
                            Directory.Delete(extDir, true);
                        }
                    }
                    catch { }
                }
            }

            return true;
        }

        public async Task<List<ExtensionInfo>> DiscoverExtensionsAsync(string directory)
        {
            var found = new List<ExtensionInfo>();

            if (!Directory.Exists(directory))
                return found;

            var dllFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dllFiles)
            {
                try
                {
                    var loadContext = new AssemblyLoadContext(dll);
                    var assembly = loadContext.LoadFromAssemblyPath(dll);

                    var extensionTypes = assembly.GetTypes()
                        .Where(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .ToList();

                    foreach (var extType in extensionTypes)
                    {
                        var instance = (IExtension)ActivatorUtilities.CreateInstance(_serviceProvider, extType);
                        var metadata = instance.GetMetadata();

                        var extInfo = new ExtensionInfo
                        {
                            Id = metadata.Id,
                            Name = metadata.Name,
                            Version = metadata.Version,
                            Author = metadata.Author,
                            Description = metadata.Description,
                            Type = metadata.Type,
                            AssemblyPath = dll,
                            Metadata = metadata,
                            IsEnabled = false,
                            IsLoaded = false
                        };

                        found.Add(extInfo);
                    }

                    loadContext.Unload();
                }
                catch { }
            }

            lock (_lock)
            {
                foreach (var ext in found)
                {
                    if (!_extensions.ContainsKey(ext.Id))
                    {
                        _extensions[ext.Id] = ext;
                    }
                }
            }

            return found;
        }

        public async Task<bool> ReloadExtensionAsync(string id)
        {
            var extInfo = await GetExtensionAsync(id);
            if (extInfo == null) return false;

            var path = extInfo.AssemblyPath;
            await UnloadExtensionAsync(id);
            return await LoadExtensionAsync(path);
        }
    }
}