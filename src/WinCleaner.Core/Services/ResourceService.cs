using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WinCleaner.Core.Services;

public interface IResourceService
{
    CultureInfo CurrentCulture { get; }
    event EventHandler<CultureInfo>? CultureChanged;
    Task InitializeAsync();
    string GetString(string key, params object[] args);
    string GetString(string key, CultureInfo culture, params object[] args);
    void SetCulture(CultureInfo culture);
    IEnumerable<CultureInfo> GetSupportedCultures();
}

public sealed class ResourceService : IResourceService, IDisposable
{
    private readonly ILogger<ResourceService> _logger;
    private readonly ConcurrentDictionary<string, JsonElement> _resources = new();
    private readonly ConcurrentDictionary<string, JsonElement> _fallbackResources = new();
    private CultureInfo _currentCulture = CultureInfo.GetCultureInfo("en");
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _disposed;

    public CultureInfo CurrentCulture => _currentCulture;
    public event EventHandler<CultureInfo>? CultureChanged;

    public ResourceService(ILogger<ResourceService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var culture = CultureInfo.CurrentUICulture;
        if (!GetSupportedCultures().Any(c => c.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
        {
            culture = culture.Parent;
            if (!GetSupportedCultures().Any(c => c.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
            {
                culture = CultureInfo.GetCultureInfo("en");
            }
        }
        await LoadResourcesAsync(culture.Name);
        SetCulture(culture);
    }

    public async Task LoadResourcesAsync(string cultureName)
    {
        await _loadLock.WaitAsync();
        try
        {
            if (_resources.ContainsKey(cultureName))
                return;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"WinCleaner.Core.Resources.Resources.{cultureName}.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Resource file not found: {ResourceName}, falling back to English", resourceName);
                if (cultureName != "en")
                {
                    await LoadResourcesAsync("en");
                }
                return;
            }

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var doc = JsonDocument.Parse(json);
            _resources[cultureName] = doc.RootElement.Clone();

            if (cultureName == "en")
            {
                _fallbackResources["en"] = doc.RootElement.Clone();
            }

            _logger.LogInformation("Loaded resources for culture: {Culture}", cultureName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load resources for culture: {Culture}", cultureName);
            if (cultureName != "en")
            {
                await LoadResourcesAsync("en");
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public string GetString(string key, params object[] args)
    {
        return GetString(key, _currentCulture, args);
    }

    public string GetString(string key, CultureInfo culture, params object[] args)
    {
        var cultureName = culture.Name;
        if (!_resources.TryGetValue(cultureName, out var resources))
        {
            if (!_resources.TryGetValue("en", out resources))
            {
                return key;
            }
            cultureName = "en";
        }

        var value = GetValue(resources, key) ?? GetValue(_fallbackResources.GetValueOrDefault("en"), key);
        if (value == null)
        {
            _logger.LogWarning("Resource key not found: {Key} in culture: {Culture}", key, cultureName);
            return key;
        }

        try
        {
            return args.Length > 0 ? string.Format(value, args) : value;
        }
        catch (FormatException)
        {
            _logger.LogWarning("Format error for key: {Key} with args: {Args}", key, args);
            return value;
        }
    }

    private static string? GetValue(JsonElement element, string key)
    {
        var parts = key.Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.TryGetProperty(part, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    public void SetCulture(CultureInfo culture)
    {
        if (culture == null)
            throw new ArgumentNullException(nameof(culture));

        if (_currentCulture.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
            return;

        var oldCulture = _currentCulture;
        _currentCulture = culture;

        _ = LoadResourcesAsync(culture.Name);

        _logger.LogInformation("Culture changed from {OldCulture} to {NewCulture}", oldCulture.Name, culture.Name);
        CultureChanged?.Invoke(this, culture);
    }

    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        yield return CultureInfo.GetCultureInfo("en");
        yield return CultureInfo.GetCultureInfo("vi");
        yield return CultureInfo.GetCultureInfo("de");
        yield return CultureInfo.GetCultureInfo("fr");
        yield return CultureInfo.GetCultureInfo("zh-CN");
        yield return CultureInfo.GetCultureInfo("zh-TW");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _loadLock.Dispose();
        _disposed = true;
    }
}