# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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