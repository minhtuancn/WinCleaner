# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-09-20

### Added
- **Modern WPF Shell Edition** — 7-page navigation (Health Check, Advanced Clean, Storage, Manual Cleanup, Live Timeline, Settings, Diagnostics)
- **Classic Single-Window Edition** — Lightweight alternative with full feature parity
- **Cookie Cleaning** — Chrome/Edge/Brave/Opera/Vivaldi SQLite database scanning with secure deletion
- **Store Apps (AppX) Cleaning** — Full enumeration via PackageManager with cache/temp/logs/cookies categories
- **Task Scheduler Integration** — Native Windows Task Scheduler for automated cleaning (Microsoft.Win32.TaskScheduler)
- **AI Explainer** — Human-readable rule descriptions with local templates + optional LLM enrichment
- **Auto-Update System** — GitHub Releases polling, silent MSI install, Stable/Beta channel selection
- **Full Localization** — 6 languages (English, Vietnamese, German, French, Chinese Simplified/Traditional)
- **CLI Silent Mode** — Global `--silent` flag suppressing all output except errors
- **Docker Support** — Multi-stage Dockerfile for containerized builds and headless execution
- **WiX MSI Installers** — Modern (~59 MB) and Classic (~59 MB) with auto-close running app, shortcuts, registry
- **MSIX Scaffolding** — Package.appxmanifest, WindowsAppSDK, asset PNGs, publish profile (needs code signing)
- **Repository Hygiene** — .editorconfig, dependabot.yml, renovate.json, CODEOWNERS, SECURITY.md, CONTRIBUTING.md
- **Issue/PR Templates** — Bug report, feature request, PR template, standardized labels
- **CI/CD Pipelines** — ci.yml (quality gates), build-beta.yml (prerelease), build-release.yml (production)

### Changed
- **Architecture Refactor** — Clean separation: Core (net8.0) / Infrastructure.Windows (net8.0-windows) / App / CLI
- **Design System** — Centralized ColorPalette, Typography, Spacing, Icons (vector geometries, no emoji)
- **Theme Service** — Proper initialization before window creation, DynamicResource for runtime theme switching
- **Shared Theme Resources** — Common margin/padding/shadow/corner-radius keys always loaded
- **Security** — PathSafetyValidator, AppRunningGuard, CleanupPlanService, System Restore Points
- **Resilience** — IResilienceService with retry policies, crash reporting, structured logging

### Deprecated
- Legacy `WinCleaner_Old/` directory (kept for reference, not in solution)

### Removed
- Empty catch blocks in Core services (replaced with proper logging)

### Fixed
- **Critical**: SQLite heap buffer overflow (GHSA-2m69-gcr7-jv3q) — upgraded SQLitePCLRaw.lib.e_sqlite3 to 2.1.12
- Classic edition launch crash — theme initialization order + resource resolution
- CI Docker build failure — added multi-stage Dockerfile
- MSI UpgradeCode placeholders — generated real GUIDs for both editions
- WinForms ColorDialog in WPF — flagged for replacement (uses System.Windows.Forms)

### Security
- No critical/high vulnerabilities after SQLite fix
- Secret detection in CI (truffleHog)
- CodeQL static analysis enabled
- Dependabot automated dependency updates

## [Unreleased]

### Added
- Initial project structure with WPF MVVM architecture
- 4 Cleaning profiles: Safe, Deep, Custom, Nuclear
- Multi-drive, multi-user, multi-OS support
- Real-time console logging with color-coded levels
- Dry-run mode for safe preview
- 50+ cleanable item types detection

### Changed
- N/A

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [1.0.0] - 2026-09-17

### Added
- **Core Features**
  - Professional WPF application with Fluent Design
  - Dependency Injection với Microsoft.Extensions.Hosting
  - MVVM pattern với CommunityToolkit.Mvvm
  - Settings persistence (JSON)

- **Scanning Engine**
  - SystemScanner: Multi-drive detection (Win32_Volume)
  - User profile enumeration (Win32_UserProfile)
  - System info collection (WMI)
  - 20+ CleanCategory groups
  - 50+ CleanItem definitions với risk levels

- **Cleaning Profiles**
  - **Safe**: System temp, Windows Update, Browser caches, Dev tools, User temp, Recycle bin
  - **Deep**: + DriverStore, Discord old versions, Playwright, Minecraft temp, Updater caches
  - **Custom**: + Game data (Roblox, Minecraft), User Programs, Other users
  - **Nuclear**: + CompactOS, Hibernation, System Restore, Windows.old

- **Supported Clean Targets**
  - System: Windows Temp, Prefetch, SoftwareDistribution, Logs, DriverStore
  - Browsers: Chrome, Edge, Firefox, Brave, Opera, Vivaldi
  - Dev Tools: npm, Yarn, uv, pip, NuGet, Go, Flutter, .NET, Gradle, Maven, Cargo, Playwright, Cypress
  - Apps: Discord, Electron, VS Code, Cursor, Codex, Copilot, Roblox, Minecraft, Zalo, Steam
  - Games: Roblox, RobloxPCGDK, Lunar Client, CurseForge, Steam
  - System: Recycle Bin, CompactOS, Hibernation, System Restore, Windows.old

- **UI/UX**
  - Modern WPF UI với Fluent Design System
  - TreeView categories collapsible với checkbox
  - Real-time console log (Debug/Info/Warning/Error/Success)
  - Progress bars per item và overall
  - Risk level badges (Safe/Low/Medium/High/Critical)
  - Dry-run toggle
  - Keyboard shortcuts (F5, Ctrl+A, Ctrl+D, Ctrl+S, Ctrl+E, Esc)
  - Auto-scroll log
  - Export log to file
  - Status bar với drive info, user info, selection summary

- **Architecture**
  - Clean separation: Models, ViewModels, Views, Services
  - Interface-based: ISystemScanner, ICleanerService, ISettingsService
  - Async/await throughout
  - CancellationToken support
  - Progress<IProgress<T>> reporting
  - Structured logging

- **Compatibility**
  - Windows 10 (1809+) / Windows 11
  - x64 / ARM64
  - .NET 8.0
  - Per-monitor DPI v2 awareness
  - Admin manifest (requireAdministrator optional)

- **Documentation**
  - Comprehensive README.md
  - Detailed disk analysis report
  - Architecture documentation
  - Contributing guide
  - Code of Conduct
  - Security policy
  - MIT License

### Changed
- N/A (Initial release)

### Fixed
- N/A (Initial release)

---

## Version History Format

```
## [VERSION] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security improvements
```