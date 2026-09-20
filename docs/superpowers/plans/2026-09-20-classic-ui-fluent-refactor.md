# Classic UI Fluent Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform WinCleaner-classic from a single-window legacy UI to a modern Fluent-style application with sidebar navigation, card-based dashboard, virtualized tree views, and consistent design system – while reusing existing ViewModels and Core services.

**Architecture:** 
- Keep existing `MainViewModel` as Dashboard view model; extract `CleanerViewModel`, `ToolsViewModel`, `SettingsViewModel` from its responsibilities.
- Introduce a `ShellViewModel` + `ShellWindow` (mirroring Modern edition) for navigation.
- Centralize design tokens in `WinCleaner-classic/Design/` mirroring `src/WinCleaner.App/Design/`.
- Use `DynamicResource` for theme-aware resources; merge dictionaries in correct order (Shared → Theme → Styles).
- All XAML changes only; no Core/Infrastructure modifications.

**Tech Stack:** .NET 8, WPF (net8.0-windows), CommunityToolkit.Mvvm, Microsoft.Win32.TaskScheduler, MaterialDesignThemes (optional for ColorPicker), Fluent icons (vector Geometry).

**Spec:** This plan derives from the FluentCleaner reference (`.references/FluentCleaner/`) and the current Classic codebase in `WinCleaner-classic/`.

## Global Constraints

- Target Framework: `net8.0-windows`
- Warnings as errors except whitelisted: `CS1591;CS1998;CS8600;CS8601;CS8603;CS8604;CS8618;CA1416;VSTHRD200;VSTHRD103`
- No emoji – all icons from `Design/Icons.cs` (vector Geometry)
- UI contract: compact professional dashboard, no fake progress, no empty `catch { }`
- Version single source: `Version.props` + `version.json` + `global.json`
- All new XAML must reference resources via `DynamicResource` for Margin/Padding/Brushes
- Existing `MainViewModel` stays but refactored to implement `INavigableViewModel` (Title, Icon, IsSelected)

## Review Focus

1. **Theme switch at runtime** – resources must resolve after Light↔Dark toggle without restart.
2. **Virtualized TreeView with 10k+ nodes** – UI must stay responsive; use `VirtualizingStackPanel`.
3. **ColorPicker replacement** – remove WinForms `ColorDialog`; use WPF-native picker.
4. **Localization strings** – all hard-coded Vietnamese text moved to `IResourceService`.
5. **Navigation state persistence** – `WindowStateService` must save/restore selected tab, sidebar width, window size.

---

### Task 1: Design System Tokens

**Files:**
- Create: `WinCleaner-classic/Design/ColorPalette.cs`
- Create: `WinCleaner-classic/Design/Typography.cs`
- Create: `WinCleaner-classic/Design/Spacing.cs`
- Create: `WinCleaner-classic/Design/Icons.cs` (vector Geometry from FluentCleaner)
- Modify: `WinCleaner-classic/WinCleaner.csproj` (add Design folder as compile)

**Interfaces:**
- Produces: static classes with `public static readonly` fields used by ResourceDictionaries.

- [ ] **Step 1: Write failing test** – create a test that loads the assembly and verifies each token class exists and has expected members.

```csharp
[Fact]
public void DesignTokens_Exist()
{
    var asm = Assembly.Load("WinCleaner");
    Assert.NotNull(asm.GetType("WinCleaner.Design.ColorPalette"));
    Assert.NotNull(asm.GetType("WinCleaner.Design.Typography"));
    Assert.NotNull(asm.GetType("WinCleaner.Design.Spacing"));
    Assert.NotNull(asm.GetType("WinCleaner.Design.Icons"));
}
```

- [ ] **Step 2: Run test to verify it fails**  
  `dotnet test tests/WinCleaner.Tests.Unit --filter "DesignTokens_Exist"` → FAIL (types missing)

- [ ] **Step 3: Implement token classes** (copy from Modern `src/WinCleaner.App/Design/` and adapt namespace to `WinCleaner.Design`)

```csharp
// ColorPalette.cs
namespace WinCleaner.Design;
public static class ColorPalette
{
    public static readonly Color Primary = Color.FromRgb(0x00, 0x78, 0xD4);
    public static readonly Color PrimaryDark = Color.FromRgb(0x00, 0x5A, 0x9E);
    // ... all Fluent colors
}
```

- [ ] **Step 4: Run test to verify it passes** → PASS

- [ ] **Step 5: Commit**  
  `git add WinCleaner-classic/Design/ tests/WinCleaner.Tests.Unit/DesignTokensTests.cs`  
  `git commit -m "feat: add Fluent design system tokens for Classic"`

---

### Task 2: Shared Theme Resources (Common Margins, Padding, CornerRadius, Shadows)

**Files:**
- Create: `WinCleaner-classic/Resources/SharedThemeResources.xaml`
- Modify: `WinCleaner-classic/App.xaml` (insert SharedThemeResources before Styles.xaml)

**Interfaces:**
- Produces: ResourceDictionary with keys `MarginXs`, `MarginSm`, `MarginMd`, `MarginLg`, `MarginXl`, `PaddingXs`…`CornerRadiusSmall`…`ShadowSmall`…

- [ ] **Step 1: Write failing test** – load Application resources and assert keys exist.

```csharp
[Fact]
public void SharedThemeResources_ContainsRequiredKeys()
{
    var app = new App();
    app.InitializeComponent();
    var rd = app.Resources.MergedDictionaries
        .First(d => d.Source?.OriginalString.Contains("SharedThemeResources") == true);
    Assert.Contains("MarginXs", rd.Keys.Cast<string>());
    Assert.Contains("CornerRadiusNormal", rd.Keys.Cast<string>());
    Assert.Contains("ShadowMedium", rd.Keys.Cast<string>());
}
```

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement SharedThemeResources.xaml** (copy from Modern, ensure all keys used in Styles.xaml)

```xml
<ResourceDictionary ...>
  <Thickness x:Key="MarginXs">4</Thickness>
  <Thickness x:Key="MarginSm">8</Thickness>
  <Thickness x:Key="MarginMd">12</Thickness>
  <Thickness x:Key="MarginLg">16</Thickness>
  <Thickness x:Key="MarginXl">24</Thickness>
  <CornerRadius x:Key="CornerRadiusSmall">2</CornerRadius>
  <CornerRadius x:Key="CornerRadiusNormal">4</CornerRadius>
  <CornerRadius x:Key="CornerRadiusMedium">6</CornerRadius>
  <CornerRadius x:Key="CornerRadiusLarge">8</CornerRadius>
  <DropShadowEffect x:Key="ShadowSmall" BlurRadius="4" ShadowDepth="1" Opacity="0.1" Color="Black"/>
  <DropShadowEffect x:Key="ShadowMedium" BlurRadius="8" ShadowDepth="2" Opacity="0.15" Color="Black"/>
  <DropShadowEffect x:Key="ShadowLarge" BlurRadius="16" ShadowDepth="4" Opacity="0.2" Color="Black"/>
</ResourceDictionary>
```

- [ ] **Step 4: Update App.xaml merge order**  

```xml
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary Source="Resources/Converters.xaml"/>
  <ResourceDictionary Source="Resources/SharedThemeResources.xaml"/>
  <ResourceDictionary Source="Resources/Styles.xaml"/>
  <ResourceDictionary Source="Resources/Themes/Light.xaml"/>
  <ResourceDictionary Source="Resources/Themes/Dark.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**

---

### Task 3: Light / Dark Theme Dictionaries (Color + Brush only)

**Files:**
- Modify: `WinCleaner-classic/Resources/Themes/Light.xaml`
- Modify: `WinCleaner-classic/Resources/Themes/Dark.xaml`

**Interfaces:**
- Each dict defines only `Color` and `SolidColorBrush` keys (PrimaryColor, BackgroundBrush, TextPrimaryBrush, etc.) – **no** spacing/corner/shadow keys.

- [ ] **Step 1: Write failing test** – verify Light.xaml has `PrimaryColor` and no `MarginXs`.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Strip spacing/corner/shadow from both theme files** (keep only colors/brushes/fonts).

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**

---

### Task 4: Styles.xaml – Switch StaticResource → DynamicResource for themeable values

**Files:**
- Modify: `WinCleaner-classic/Resources/Styles.xaml` (lines referencing Margin, Padding, Brushes)

**Interfaces:**
- Consumes: keys from SharedThemeResources + Light/Dark.
- Produces: styles that update at runtime when theme changes.

- [ ] **Step 1: Write failing test** – create a Window with a styled Button, switch theme via `ThemeService`, assert button Margin updates.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Replace all `StaticResource Margin*` / `Padding*` / `Brush*` with `DynamicResource`** in Styles.xaml (search/replace).

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**

---

### Task 5: ShellViewModel & ShellWindow (Navigation Host)

**Files:**
- Create: `WinCleaner-classic/ViewModels/ShellViewModel.cs`
- Create: `WinCleaner-classic/Views/ShellWindow.xaml` + `.cs`
- Modify: `WinCleaner-classic/App.xaml.cs` (startup → show ShellWindow, init ThemeService)

**Interfaces:**
- `ShellViewModel` exposes `ObservableCollection<NavItem> NavigationItems`, `INavigableViewModel CurrentView`, `ICommand NavigateCommand`, `ICommand ToggleSidebarCommand`, `ICommand ToggleThemeCommand`.
- `NavItem` : `Label`, `Icon` (Geometry), `ViewModelType`.
- `INavigableViewModel` : `string Title { get; }`, `Geometry Icon { get; }`, `bool IsSelected { get; set; }`.

- [ ] **Step 1: Write failing test** – instantiate ShellViewModel, verify NavigationItems count = 4 (Dashboard, Cleaner, Tools, Settings), verify NavigateCommand changes CurrentView.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement ShellViewModel** (mirror Modern `ShellViewModel.cs` but with Classic view models). Register views via `DataTemplate` in ShellWindow.Resources.

- [ ] **Step 4: Implement ShellWindow.xaml** – Grid with Sidebar (ListBox bound to NavigationItems) + ContentControl bound to CurrentView.

- [ ] **Step 5: Update App.xaml.cs** – resolve `IThemeService`, `await InitializeAsync()`, then `new ShellWindow { DataContext = shellVm }.Show();`

- [ ] **Step 6: Run test → PASS**

- [ ] **Step 7: Commit**

---

### Task 6: Dashboard View (MainViewModel refactor)

**Files:**
- Modify: `WinCleaner-classic/ViewModels/MainViewModel.cs` (implement `INavigableViewModel`, keep scan/clean logic)
- Create: `WinCleaner-classic/Views/DashboardView.xaml` (card grid: Health, Disk, Top Junk, Quick Actions)

**Interfaces:**
- `MainViewModel` implements `INavigableViewModel` (Title="Dashboard", Icon=Icons.Dashboard).
- DashboardView binds to MainViewModel properties.

- [ ] **Step 1: Write failing test** – verify MainViewModel.Title == "Dashboard" and has `ScanCommand`.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Add `INavigableViewModel` implementation**; move UI-only properties (Drives, LogEntries) to DashboardView; keep core Scan/Clean logic.

- [ ] **Step 4: Create DashboardView.xaml** with `UniformGrid` of `Card` controls using `DynamicResource` for margins.

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**

---

### Task 7: Cleaner View (Virtualized TreeView)

**Files:**
- Create: `WinCleaner-classic/ViewModels/CleanerViewModel.cs`
- Create: `WinCleaner-classic/Views/CleanerView.xaml`
- Modify: `WinCleaner-classic/ViewModels/MainViewModel.cs` (extract scan/clean logic to CleanerViewModel)

**Interfaces:**
- `CleanerViewModel` : `ObservableCollection<CleanCategoryGroup> Categories`, `CleanProfile SelectedProfile`, `ICommand ScanCommand`, `ICommand CleanCommand`, `ICommand SelectSafeCommand`.
- TreeView uses `VirtualizingStackPanel.IsVirtualizing="True"` and `VirtualizingStackPanel.VirtualizationMode="Recycling"`.

- [ ] **Step 1: Write failing test** – verify CleanerViewModel loads profiles, ScanCommand populates Categories.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement CleanerViewModel** (reuse `ISystemScanner`, `ICleanerService` from DI).

- [ ] **Step 4: Build CleanerView.xaml** – Toolbar (Profile ComboBox, SearchBox, Select buttons) + TreeView with `HierarchicalDataTemplate` for groups/items, checkbox binding, risk badge.

- [ ] **Step 6: Run test → PASS**

- [ ] **Step 7: Commit**

---

### Task 8: Tools View (Scheduler, Cookie, Shred, AppX tabs)

**Files:**
- Create: `WinCleaner-classic/ViewModels/ToolsViewModel.cs`
- Create: `WinCleaner-classic/Views/ToolsView.xaml`
- Create sub-UserControls: `SchedulerTab.xaml`, `CookieTab.xaml`, `ShredTab.xaml`, `AppXTab.xaml`

**Interfaces:**
- ToolsViewModel aggregates four child VMs (SchedulerVM, CookieVM, ShredVM, AppXVM) each implementing `INavigableViewModel` (for tab header).

- [ ] **Step 1: Write failing test** – verify ToolsViewModel exposes four tabs with correct titles/icons.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement each tab VM** (wrap existing services: `TaskSchedulerService`, `CookieService`, `SecureDeleteService`, `AppxService`).

- [ ] **Step 4: Build ToolsView.xaml** – `TabControl` with `ItemTemplate` for icons, content via `ContentTemplateSelector`.

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**

---

### Task 9: Settings View (WPF ColorPicker, Localization, Theme, Updates)

**Files:**
- Create: `WinCleaner-classic/ViewModels/SettingsViewModel.cs` (replace WinForms ColorDialog)
- Create: `WinCleaner-classic/Views/SettingsView.xaml`
- Modify: `WinCleaner-classic/Services/SettingsService.cs` (persist new settings)

**Interfaces:**
- SettingsViewModel: `ICommand PickAccentColorCommand` (opens WPF ColorPicker dialog), `ObservableCollection<string> Languages`, `string SelectedLanguage`, `bool AutoCheckUpdates`, `UpdateChannel SelectedChannel`.

- [ ] **Step 1: Write failing test** – verify SettingsViewModel loads/saves accent color, language, update channel.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Implement SettingsViewModel** – use `Microsoft.Toolkit.Wpf.UI.Controls.ColorPicker` (NuGet) or custom HSV picker.

- [ ] **Step 4: Build SettingsView.xaml** – grouped sections (Appearance, Updates, Advanced) with Expander, bind all to DynamicResources.

- [ ] **Step 5: Run test → PASS**

- [ ] **Step 6: Commit**

---

### Task 10: Animations & Micro-interactions

**Files:**
- Modify: `WinCleaner-classic/Resources/Styles.xaml` (add `Storyboard` resources for FadeIn, SlideIn)
- Modify: ShellWindow.xaml (sidebar slide, content cross-fade)

**Interfaces:**
- Consumes: `EnableAnimations` bool from ThemeService.

- [ ] **Step 1: Write failing test** – UI automation: click nav item, verify content opacity animation runs (use `FlaUI`).

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Define `FadeIn`/`FadeOut` Storyboards** targeting `ContentControl.Opacity`; trigger via `DataTrigger` on `CurrentView` change.

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**

---

### Task 11: Accessibility & Localization Polish

**Files:**
- Modify: all Views (add `AutomationProperties.Name`, `AutomationProperties.HelpText`)
- Modify: ViewModels (move any hard-coded strings to `IResourceService`)

**Interfaces:**
- Consumes: `IResourceService.GetString(key)`.

- [ ] **Step 1: Write failing test** – scan XAML for missing `AutomationProperties.Name` on interactive elements.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Add automation properties**; replace Vietnamese literals with resource keys.

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**

---

### Task 12: Window State Persistence (Sidebar width, selected tab, window size)

**Files:**
- Modify: `WinCleaner-classic/Services/WindowStateService.cs` (extend `WindowStateData`)
- Modify: `ShellWindow.xaml.cs` (save/restore on Loaded/Closing)

**Interfaces:**
- `WindowStateData` adds `double SidebarWidth`, `string SelectedNavItem`, `bool IsSidebarOpen`.

- [ ] **Step 1: Write failing test** – serialize/deserialize WindowStateData, verify round-trip.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Extend WindowStateData & persist in ShellWindow**.

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**

---

### Task 13: Integration Smoke Test (Launch → Scan → Clean)

**Files:**
- Create: `tests/WinCleaner.Tests.Integration/ClassicUISmokeTests.cs`

**Interfaces:**
- Uses `FlaUI` to start Classic exe, click Dashboard → Cleaner tab, run Scan (DryRun), assert tree populated.

- [ ] **Step 1: Write failing test** – automation script.

- [ ] **Step 2: Run test → FAIL**

- [ ] **Step 3: Ensure all previous tasks compile; run test → PASS** (may need minor selector fixes).

- [ ] **Step 4: Commit**

---

### Task 14: Performance Benchmarks (Startup <1s, Scan 10k <2s)

**Files:**
- Create: `tests/WinCleaner.Tests.Integration/PerformanceTests.cs`

**Interfaces:**
- Uses `BenchmarkDotNet` or simple `Stopwatch`.

- [ ] **Step 1: Write failing test** – measure startup time, scan time.

- [ ] **Step 2: Run test → FAIL (if > thresholds)**

- [ ] **Step 3: Optimize** (enable UI virtualization, reduce layout passes).

- [ ] **Step 4: Run test → PASS**

- [ ] **Step 5: Commit**

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-09-20-classic-ui-fluent-refactor.md`. Please review the plan. Which execution approach would you prefer?

- **Subagent-driven** – A fresh subagent implements each task and a fresh reviewer checks it before the next one starts, then a whole-branch review at the end. Most thorough; costs a fresh context per task and per review.
- **Native** – I implement every task myself in this session, the way this harness runs work, then one fresh reviewer on the most capable model checks the whole branch. Cheapest and fastest; no independent review until the end.

For this plan I recommend **Subagent-driven**, because the tasks have clear, independent interfaces (Design tokens → Resources → Shell → Views) and a mistake in early token definitions would cascade through all later UI work. Does the plan capture what you want, and which approach should we use?