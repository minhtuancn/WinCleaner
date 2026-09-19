# AGENTS.md - WinCleaner Development Guide

## Quick Commands

```bash
# Build & Test (local mirrors CI)
dotnet restore WinCleaner.sln
dotnet build WinCleaner.sln --configuration Release --no-restore
dotnet test WinCleaner.sln --configuration Release --no-build

# Run single test project
dotnet test tests/WinCleaner.Tests.Unit --configuration Release

# Run app (WPF)
dotnet run --project src/WinCleaner.App

# Run CLI
dotnet run --project src/WinCleaner.Cli -- --help

# Security audit
dotnet list WinCleaner.sln package --vulnerable --include-transitive
```

## Architecture

| Project | Purpose | Target |
|---------|---------|--------|
| `WinCleaner.Core` | Domain models, services, parser, rules engine | net8.0 |
| `WinCleaner.Infrastructure.Windows` | WPF converters, ThemeService, WindowStateService | net8.0-windows |
| `WinCleaner.App` | WPF UI (Shell + Views + ViewModels) | net8.0-windows |
| `WinCleaner.Cli` | Headless CLI | net8.0 |
| `WinCleaner.Inspector` | Blazor WASM Winapp2 rule inspector | net8.0 (WASM) |
| `WinCleaner.Tests.Unit` | xUnit + Moq + FluentAssertions | net8.0-windows |

Legacy code lives in `WinCleaner_Old/` (not in solution).

## Build & Version

- **Central version**: `version.json` + `Version.props` + `Directory.Build.props`
- **Target**: `.NET 8` (not .NET 10 despite legacy issue title)
- **Warnings as errors** except: `CS1591` (missing XML docs), `CS1573/1572`
- **Warnings as errors on test projects**: disabled
- **Central package management** enabled (`Directory.Packages.props` implied)

## CI Pipeline (`.github/workflows/ci.yml`)

Order: `restore` → `build` → `test` → `security-scan` → `docker-build`
- `dotnet restore WinCleaner.sln`
- `dotnet build --configuration Release --no-restore`
- `dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"`
- `dotnet list package --vulnerable`
- CodeQL analysis

## Key Architecture Patterns

- **DI**: `App.xaml.cs` registers all services; `IServiceProvider` exposed via `App.Services`
- **App Shell**: `ShellViewModel` + `ShellWindow` with navigation items → ViewModels resolved via DI
- **Theme**: `IThemeConfigurationStore` (headless) + `WpfThemeApplicator` (WPF) + `ThemeService`
- **Resilience**: `IResilienceService` (retry, isolation, crash reporting) - Issue #21
- **Safety**: `CleanupPlanService` + `PathSafetyValidator` + `AppRunningGuard` (Issue #3)
- **Winapp2 Parser**: `Winapp2Service.ParseIniContent()` (now public for Inspector)

## Testing

- Framework: xUnit + Moq + FluentAssertions
- Coverage: `coverlet.collector` with `XPlat Code Coverage`
- Unit tests: `tests/WinCleaner.Tests.Unit` (48 tests passing)
- Integration tests: placeholder project, no tests yet

## Common Gotchas

| Issue | Resolution |
|-------|------------|
| `Microsoft.Web.WebView2` version | Use `1.0.3124.44` (exact in csproj) |
| `TaskScheduler` package | Use `TaskScheduler` (dahall) 2.11.0, namespace `Microsoft.Win32.TaskScheduler` |
| SQLite for cookies | `Microsoft.Data.Sqlite` 9.0.0 in Core |
| Inspector Blazor needs Core reference | Add `<ProjectReference Include="..\WinCleaner.Core\WinCleaner.Core.csproj" />` |
| Inspector wwwroot copy | `CopyInspectorWwwroot` target in App csproj copies `wwwroot` to output |
| WPF ampersands in XAML | Must be `&` not `&` |
| `StringFormat` in XAML | Use `{}{0:F1}` escape prefix |

## Branch & Release Flow

```bash
# Start work
git checkout main && git pull --ff-only origin main
git checkout -b feat/issue-XX-description

# After implementation
dotnet restore && dotnet build && dotnet test
git add -A && git commit -m "feat(scope): description (#XX)"
git push -u origin branch

# PR targets main, squash merge preferred
# After merge: verify on main, delete branch
```

## Current Branch State

- **Default branch**: `main` (was `master`, migrated)
- **Release workflows**: `build-beta.yml` (manual), `build-release.yml` (tag `v*`)
- Dual build: Modern (`WinCleaner.App`) + Classic (`WinCleaner_Old`) → separate MSIs

## Key Files for Context

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Central build config, analyzers, warnings |
| `Version.props` / `version.json` | Version single source of truth |
| `src/WinCleaner.App/App.xaml.cs` | DI container, service registration |
| `src/WinCleaner.Core/Services/Winapp2Service.cs` | Winapp2 parser (public `ParseIniContent`) |
| `src/WinCleaner.App/ViewModels/ShellViewModel.cs` | Navigation + view switching |
| `docs/architecture/feature-matrix.md` | Live feature vs FluentCleaner comparison |

## Current Focus Areas (P0)

- #29 Settings persistence end-to-end
- #5 Winapp2 rule engine hardening  
- #6 Cleaner coverage (browsers, apps, dev caches, cookie keeper)
- #27 Dashboard redesign
- #19 CI hardening + security scanning

## References

- FluentCleaner (reference only): `.references/FluentCleaner/` (gitignored)
- Issue tracker: GitHub Issues on `minhtuancn/WinCleaner`