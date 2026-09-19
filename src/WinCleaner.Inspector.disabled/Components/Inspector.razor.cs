using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using WinCleaner.Models;
using WinCleaner.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace WinCleaner.Inspector.Components;

public partial class Inspector
{
    private const long MaxDatabaseSize = 12 * 1024 * 1024;
    private const long MaxSettingsSize = 2 * 1024 * 1024;
    private const int PageSize = 50;

    private bool VersionCheckRequested { get; set; }
    private bool VersionCheckUnavailable { get; set; }
    private bool VersionUpdateAvailable { get; set; }
    private string EditionName { get; set; } = "WinCleaner";
    private string InstalledVersion { get; set; } = "";
    private string LatestVersion { get; set; } = "";
    private string VersionStatusText { get; set; } = "";
    private string VersionDownloadUrl { get; set; } = "https://github.com/minhtuancn/WinCleaner/releases";

    private DatabaseSnapshot? Primary { get; set; }
    private DatabaseSnapshot? Comparison { get; set; }
    private Winapp2Entry? DetailEntry { get; set; }
    private HashSet<string> SavedSelection { get; } = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> SelectedNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    private string SettingsFileName { get; set; } = "settings.json";
    private bool SettingsLoaded { get; set; }
    private string ErrorMessage { get; set; } = "";
    private string HandoffMessage { get; set; } = "";
    private string _searchText = "";
    private string _activeFilter = "all";
    private int CurrentPage { get; set; } = 1;

    [Inject]
    public IWinapp2Service Winapp2Service { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var query = ParseQuery(Navigation.ToAbsoluteUri(Navigation.Uri).Query);
        if (!query.TryGetValue("edition", out var editionValue) ||
            !query.TryGetValue("version", out var versionValue))
            return;

        var edition = editionValue.ToString().Trim().ToLowerInvariant();
        InstalledVersion = versionValue.ToString().Trim();
        if ((edition != "classic" && edition != "modern") || !Version.TryParse(InstalledVersion, out var installed))
            return;

        VersionCheckRequested = true;
        EditionName = edition == "classic" ? "WinCleaner Classic" : "WinCleaner Modern";

        try
        {
            var catalog = await Http.GetFromJsonAsync<Dictionary<string, VersionRelease>>("versions.json");
            if (catalog is null || !catalog.TryGetValue(edition, out var release) ||
                !Version.TryParse(release.Version, out var latest))
                throw new InvalidDataException("The online version catalog is unavailable.");

            LatestVersion = release.Version;
            VersionDownloadUrl = release.DownloadUrl;
            VersionUpdateAvailable = installed < latest;
            VersionStatusText = VersionUpdateAvailable
                ? $"Update available: {LatestVersion}"
                : installed > latest
                    ? "This build is newer than the current public release."
                    : "You are up to date.";
        }
        catch
        {
            VersionCheckUnavailable = true;
            VersionStatusText = "The update status could not be checked right now.";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("inspector.setSelection", SelectedNames.ToArray());
    }

    private string SearchText
    {
        get => _searchText;
        set { _searchText = value; CurrentPage = 1; }
    }

    private string ActiveFilter
    {
        get => _activeFilter;
        set { _activeFilter = value; CurrentPage = 1; }
    }

    private HashSet<string> PrimaryNames => Primary?.Entries
        .Select(entry => entry.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

    private HashSet<string> ComparisonNames => Comparison?.Entries
        .Select(entry => entry.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

    private HashSet<string> AddedNames => ComparisonNames
        .Except(PrimaryNames, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private HashSet<string> RemovedNames => PrimaryNames
        .Except(ComparisonNames, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private HashSet<string> MissingSavedNames => SavedSelection
        .Except(PrimaryNames, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private int SavedMatches => SavedSelection.Count - MissingSavedNames.Count;

    private List<Winapp2Entry> FilteredEntries
    {
        get
        {
            if (Primary is null) return [];
            IEnumerable<Winapp2Entry> query = ActiveFilter == "added" && Comparison is not null
                ? Comparison.Entries.Where(entry => AddedNames.Contains(entry.Name))
                : Primary.Entries;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(entry =>
                    entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (entry.Section?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    entry.FileKeys.Any(rule => rule.Path.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    entry.RegKeys.Any(rule => rule.Key.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            query = ActiveFilter switch
            {
                "selected" => query.Where(entry => SelectedNames.Contains(entry.Name)),
                "saved" => query.Where(entry => SavedSelection.Contains(entry.Name)),
                "warnings" => query.Where(entry => !string.IsNullOrWhiteSpace(entry.Warning)),
                "defaults" => query.Where(entry => entry.Default),
                "duplicates" => query.Where(entry => Primary.DuplicateNames.Contains(entry.Name)),
                "removed" when Comparison is not null => query.Where(entry => RemovedNames.Contains(entry.Name)),
                _ => query
            };

            return query.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredEntries.Count / (double)PageSize));
    private List<Winapp2Entry> PageEntries => FilteredEntries
        .Skip((Math.Min(CurrentPage, TotalPages) - 1) * PageSize).Take(PageSize).ToList();

    private async Task LoadPrimaryAsync(InputFileChangeEventArgs args)
    {
        var snapshot = await ReadDatabaseAsync(args.File);
        if (snapshot is null) return;
        Primary = snapshot;
        DetailEntry = snapshot.Entries.FirstOrDefault();
        var availableNames = PrimaryNames;
        SelectedNames.RemoveWhere(name => !availableNames.Contains(name));
        foreach (var name in SavedSelection.Where(availableNames.Contains))
            SelectedNames.Add(name);
        CurrentPage = 1;
    }

    private async Task LoadComparisonAsync(InputFileChangeEventArgs args)
    {
        var snapshot = await ReadDatabaseAsync(args.File);
        if (snapshot is null) return;
        Comparison = snapshot;
        CurrentPage = 1;
    }

    private async Task<DatabaseSnapshot?> ReadDatabaseAsync(IBrowserFile file)
    {
        ErrorMessage = "";
        try
        {
            if (file.Size <= 0 || file.Size > MaxDatabaseSize)
                throw new InvalidDataException("The database is empty or larger than the 12 MB inspection limit.");

            await using var stream = file.OpenReadStream(MaxDatabaseSize);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var content = await reader.ReadToEndAsync();
            var entries = Winapp2Service.ParseIniContent(content);
            if (entries.Count == 0)
                throw new InvalidDataException("No valid Winapp2 cleaner entries were found in this file.");

            return DatabaseSnapshot.Create(file.Name, content, entries);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    private async Task LoadSettingsAsync(InputFileChangeEventArgs args)
    {
        ErrorMessage = "";
        try
        {
            var file = args.File;
            if (file.Size <= 0 || file.Size > MaxSettingsSize)
                throw new InvalidDataException("The settings file is empty or larger than 2 MB.");

            await using var stream = file.OpenReadStream(MaxSettingsSize);
            using var document = await JsonDocument.ParseAsync(stream);
            var selectedProperty = document.RootElement.EnumerateObject()
                .FirstOrDefault(property => property.Name.Equals("SelectedEntries", StringComparison.OrdinalIgnoreCase));
            if (selectedProperty.Value.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("This JSON file does not contain a SelectedEntries list.");

            SavedSelection.Clear();
            foreach (var item in selectedProperty.Value.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    SavedSelection.Add(item.GetString()!);

            SettingsFileName = file.Name;
            SettingsLoaded = true;
            var availableNames = PrimaryNames;
            foreach (var name in SavedSelection.Where(availableNames.Contains))
                SelectedNames.Add(name);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private void ToggleSelection(string name, ChangeEventArgs args)
    {
        if (args.Value is true) SelectedNames.Add(name);
        else SelectedNames.Remove(name);
    }

    private void SelectVisible()
    {
        foreach (var entry in PageEntries) SelectedNames.Add(entry.Name);
    }

    private void ClearSelection() => SelectedNames.Clear();
    private void PreviousPage() { if (CurrentPage > 1) CurrentPage--; }
    private void NextPage() { if (CurrentPage < TotalPages) CurrentPage++; }

    private void ApplyFilter(string filter)
    {
        if (filter == "added")
        {
            SearchText = "";
            ActiveFilter = "added";
            return;
        }
        if (filter == "missing")
        {
            ErrorMessage = "Saved but unavailable: " + string.Join(", ", MissingSavedNames.Take(12)) +
                (MissingSavedNames.Count > 12 ? " …" : "");
            return;
        }
        ActiveFilter = filter;
    }

    private async Task ExportHandoffAsync()
    {
        if (SelectedNames.Count == 0) return;
        var json = JsonSerializer.Serialize(
            SelectedNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
            new JsonSerializerOptions { WriteIndented = true });
        await JS.InvokeVoidAsync("inspector.downloadText", "wincleaner-selection.json", json, "application/json");
        HandoffMessage = "Downloaded. Use with WinCleaner CLI: --from-selection \"wincleaner-selection.json\".";
    }

    private async Task CopySourceAsync()
    {
        if (DetailEntry is not null)
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", DetailEntry.RawText);
    }

    private static string DisplaySection(Winapp2Entry entry) =>
        string.IsNullOrWhiteSpace(entry.Section) ? "Uncategorized" : entry.Section;

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0) continue;
            var name = Uri.UnescapeDataString(part.Substring(0, separator));
            var value = Uri.UnescapeDataString(part.Substring(separator + 1).Replace("+", " "));
            values[name] = value;
        }
        return values;
    }

    private sealed class DatabaseSnapshot
    {
        public required string FileName { get; init; }
        public required List<Winapp2Entry> Entries { get; init; }
        public required HashSet<string> DuplicateNames { get; init; }
        public int FileRules { get; init; }
        public int RecursiveRules { get; init; }
        public int RegistryRules { get; init; }
        public int WholeRegistryKeys { get; init; }
        public int WarningCount { get; init; }
        public int IgnoredBlocks { get; init; }

        public static DatabaseSnapshot Create(string fileName, string content, List<Winapp2Entry> entries)
        {
            var headers = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("[", StringComparison.Ordinal) &&
                               line.EndsWith("]", StringComparison.Ordinal))
                .ToArray();
            var metadataBlocks = headers.Count(header =>
                header.StartsWith("[Winapp2", StringComparison.OrdinalIgnoreCase) ||
                header.StartsWith("[version", StringComparison.OrdinalIgnoreCase));

            return new DatabaseSnapshot
            {
                FileName = fileName,
                Entries = entries,
                DuplicateNames = entries.GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                FileRules = entries.Sum(entry => entry.FileKeys.Count),
                RecursiveRules = entries.Sum(entry => entry.FileKeys.Count(rule => rule.Recurse)),
                RegistryRules = entries.Sum(entry => entry.RegKeys.Count),
                WholeRegistryKeys = entries.Sum(entry => entry.RegKeys.Count(rule => string.IsNullOrWhiteSpace(rule.ValueName))),
                WarningCount = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.Warning)),
                IgnoredBlocks = Math.Max(0, headers.Length - metadataBlocks - entries.Count)
            };
        }
    }

    public sealed class VersionRelease
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }
}