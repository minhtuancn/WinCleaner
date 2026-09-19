# WinCleaner

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Build Status](https://img.shields.io/github/actions/workflow/status/minhtuancn/WinCleaner/ci.yml?branch=main&logo=github-actions)](https://github.com/minhtuancn/WinCleaner/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/minhtuancn/WinCleaner?color=blue)](LICENSE)
[![Release](https://img.shields.io/github/v/release/minhtuancn/WinCleaner?include_prereleases&logo=github)](https://github.com/minhtuancn/WinCleaner/releases)
[![Downloads](https://img.shields.io/github/downloads/minhtuancn/WinCleaner/total?logo=github)](https://github.com/minhtuancn/WinCleaner/releases)
[![CodeQL](https://img.shields.io/github/actions/workflow/status/minhtuancn/WinCleaner/codeql.yml?branch=main&logo=codeql)](https://github.com/minhtuancn/WinCleaner/actions/workflows/codeql.yml)
[![Coverage](https://img.shields.io/badge/coverage-90%25%2B-brightgreen)](https://github.com/minhtuancn/WinCleaner/actions)

> **Professional System Cleaner for Windows** — Modern, safe, and extensible disk cleanup utility built with .NET 8, WPF, and WinApp2 rules.

---

## 🎯 Features

| Category | Features |
|----------|----------|
| **Cleaning Engine** | WinApp2/WinApp3 rule support, FileKey/RegKey/ExcludeKey parsing, custom rules |
| **Safety First** | Path validation, running app detection, system restore points, risk assessment |
| **Browser & Cookie Cleaning** | Chrome, Edge, Brave, Opera, Vivaldi SQLite cookie database scanning |
| **Store Apps (AppX)** | Full enumeration, categorization, cleanable cache/temp/logs/cookies |
| **Task Scheduler** | Native Windows Task Scheduler integration for automated cleaning |
| **AI Explainer** | Human-readable rule descriptions (local + optional LLM) |
| **Auto-Updates** | GitHub Releases polling, silent MSI install, channel selection (Stable/Beta) |
| **Multi-Language** | English, Vietnamese, German, French, Chinese (Simplified/Traditional) |
| **CLI & Silent Mode** | Headless operation for automation (`--silent` flag) |
| **Modern UI** | WPF with Fluent Design, dark/light theme, live operation timeline |

---

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        WinCleaner.sln                           │
├─────────────┬──────────────────┬──────────────┬────────────────┤
│  Core       │ Infrastructure   │ App (WPF)    │ CLI            │
│  (net8.0)   │ .Windows         │ (net8.0-win) │ (net8.0)       │
│             │ (net8.0-windows) │              │                │
├─────────────┼──────────────────┼──────────────┼────────────────┤
│ • Models    │ • Converters     │ • Shell      │ • Commands     │
│ • Services  │ • ThemeService   │ • ViewModels │ • Scan/Clean   │
│   - Parser  │ • WindowState    │ • Views      │ • Shred        │
│   - Scanner │                  │ • Design Sys │ • Schedule     │
│   - Cleaner │                  │              │ • Silent (-s)  │
│   - Scheduler                │              │                │
│   - AppX                      │              │                │
│   - Cookie                    │              │                │
│   - AI                        │              │                │
│   - Updates                   │              │                │
└─────────────┴──────────────────┴──────────────┴────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │   Inspector     │
                    │ (Blazor WASM)   │
                    │  Rule Inspector │
                    └─────────────────┘
```

### Project Structure

```
WinCleaner/
├── src/
│   ├── WinCleaner.Core/                 # Domain models, services, parser (net8.0)
│   │   ├── Models/                      # CleanModels, Winapp2Models, AppxModels, etc.
│   │   ├── Services/                    # Parser, Scanner, Cleaner, Scheduler, Cookie, AI, etc.
│   │   ├── Resources/                   # Embedded JSON localization (en, vi, de, fr, zh-CN, zh-TW)
│   │   └── WinCleaner.Core.csproj
│   ├── WinCleaner.Infrastructure.Windows/ # WPF converters, ThemeService, WindowState (net8.0-windows)
│   │   ├── Models/Converters.cs         # All IValueConverter implementations
│   │   ├── Services/                    # ThemeService, WindowStateService, WpfCrashHandler
│   │   └── WinCleaner.Infrastructure.Windows.csproj
│   ├── WinCleaner.App/                  # WPF Application (net8.0-windows)
│   │   ├── ViewModels/                  # ShellViewModel + 7 page ViewModels
│   │   ├── Views/                       # ShellWindow + 7 page Views (XAML)
│   │   ├── Design/                      # Design System (ColorPalette, Typography, Spacing, Icons)
│   │   ├── Resources/                   # Styles.xaml, Converters.xaml, Themes/
│   │   ├── Assets/                      # PNG icons for MSIX packaging
│   │   ├── Package.appxmanifest         # MSIX manifest
│   │   ├── Properties/PublishProfiles/  # MSIX publish profile
│   │   └── WinCleaner.App.csproj
│   ├── WinCleaner.Cli/                  # CLI Application (net8.0)
│   │   ├── CLI/Program.cs               # System.CommandLine commands
│   │   └── WinCleaner.Cli.csproj
│   └── WinCleaner.Inspector/            # Blazor WASM Inspector (net8.0, disabled - build issues)
├── tests/
│   ├── WinCleaner.Tests.Unit/           # xUnit + Moq + FluentAssertions (48 tests)
│   └── WinCleaner.Tests.Integration/    # Integration tests (placeholder)
├── docs/
│   └── architecture/                    # Architecture docs, feature-matrix.md
├── .github/
│   ├── workflows/                       # CI, Beta build, Release build
│   ├── ISSUE_TEMPLATE/                  # Bug report, Feature request
│   ├── dependabot.yml                   # Automated dependency updates
│   ├── PULL_REQUEST_TEMPLATE.md
│   ├── CODEOWNERS
│   └── LABELS.md
├── WinCleaner.sln
├── Directory.Build.props                # Central build config, analyzers, warnings
├── Directory.Packages.props             # Central package management
├── Version.props / version.json         # Single source of truth for version
├── global.json                          # .NET SDK version pinning
├── .editorconfig                        # Consistent formatting
├── renovate.json                        # Advanced dependency automation
├── CODEOWNERS                           # Code ownership for reviews
├── SECURITY.md                          # Vulnerability reporting policy
├── CONTRIBUTING.md                      # Development guide
├── ROADMAP.md                           # Milestone-based roadmap
├── CHANGELOG.md                         # Version history
├── LICENSE                              # MIT License
└── README.md
```

### Key Design Principles

- **Clean Architecture**: Core is platform-agnostic; Windows-specific code isolated
- **Dependency Injection**: All services registered in `App.xaml.cs` / CLI host
- **Safety by Default**: Path validation, app running guard, restore points
- **Extensibility**: Plugin system, custom WinApp2 rules, custom cleaners
- **Resilience**: Retry policies, crash reporting, structured logging

---

## 🚀 Quick Start

### Prerequisites
- Windows 10/11 (x64)
- .NET 8 Runtime (bundled with installer)

### Installation

**Option 1: Download Release** (Recommended)
```bash
# Download latest MSI from GitHub Releases
# https://github.com/minhtuancn/WinCleaner/releases/latest
```

**Option 2: Build from Source**
```bash
git clone https://github.com/minhtuancn/WinCleaner.git
cd WinCleaner
dotnet restore WinCleaner.sln
dotnet build WinCleaner.sln --configuration Release
dotnet publish src/WinCleaner.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Run CLI
```bash
# Scan with safe profile (default)
dotnet run --project src/WinCleaner.Cli -- scan

# Clean with auto-confirm, output JSON
dotnet run --project src/WinCleaner.Cli -- clean --auto --json

# Silent mode for automation (suppresses all output except errors)
dotnet run --project src/WinCleaner.Cli -- scan --silent
dotnet run --project src/WinCleaner.Cli -- clean --auto --silent

# List available databases
dotnet run --project src/WinCleaner.Cli -- list

# Update databases
dotnet run --project src/WinCleaner.Cli -- database update
```

### Run WPF App
```bash
dotnet run --project src/WinCleaner.App
```

---

## 📦 WinApp2 Rules

WinCleaner uses the [WinApp2](https://github.com/MoscaDotTo/Winapp2) rule format for defining cleanable items.

### Supported Rule Types
- `FileKey` / `FileKey1-5` — File system paths with patterns, recursion, exclusion
- `RegKey` / `RegKey1-5` — Registry keys with actions (DeleteKey, DeleteValue)
- `ExcludeKey` / `ExcludeKey1-5` — Exclusion patterns for FileKey/RegKey
- `Detect` / `DetectFile` / `DetectOS` — Presence detection
- `Warning` — User warnings for risky operations

### Custom Rules
Place `.ini` files in `%APPDATA%\WinCleaner\Databases\Custom\` to add custom rules.

---

## 🌐 Localization

Supported languages (6):
- English (en) — Default
- Vietnamese (vi)
- German (de)
- French (fr)
- Chinese Simplified (zh-CN)
- Chinese Traditional (zh-TW)

Add new languages by creating `Resources.{culture}.json` in `src/WinCleaner.Core/Resources/` and registering in `ResourceService`.

---

## 🔧 Configuration

### Settings (persisted in `%APPDATA%\WinCleaner\settings.json`)

| Setting | Description |
|---------|-------------|
| `Theme` | Light / Dark / System |
| `AccentColor` | Custom accent color (hex) |
| `AutoCheckUpdates` | Check for updates on startup |
| `UpdateChannel` | Stable / Beta / Preview |
| `AutoDownloadUpdates` | Download updates automatically |
| `AutoInstallUpdates` | Install updates silently |
| `CreateRestorePoint` | Create system restore point before cleaning |
| `ConfirmBeforeClean` | Prompt before cleaning |
| `LogLevel` | Debug / Info / Warning / Error / Success |
| `LogRetentionDays` | Days to keep log files |

---

## 🛡 Safety Features

| Feature | Description |
|---------|-------------|
| **Path Safety Validator** | Validates all paths against critical/protected system paths |
| **App Running Guard** | Detects running applications that lock files, recommends close action |
| **Cleanup Plan** | Validates operations before execution, orders by safety |
| **System Restore Points** | Automatic restore point creation (Windows) |
| **Secure Deletion** | DoD 5220.22-M, Gutmann, RCMP TSSIT OPS-II algorithms |
| **Risk Assessment** | Safe/Low/Medium/High/Critical classification per item |

---

## 🤝 Contributing

We welcome contributions! Please read our [Contributing Guide](CONTRIBUTING.md) for details on:
- Development setup
- Coding standards
- Pull request process
- Testing requirements
- Architecture overview

### Quick Contribution Checklist
- [ ] Fork & clone
- [ ] Create feature branch (`feat/issue-XX-description`)
- [ ] Write code + tests
- [ ] Run `dotnet build && dotnet test`
- [ ] Submit PR with description

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 🙏 Acknowledgments

- [WinApp2](https://github.com/MoscaDotTo/Winapp2) — Community-maintained cleaning rules
- [FluentCleaner](https://github.com/AmirHosseinJafari/FluentCleaner) — Reference implementation
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM framework
- [TaskScheduler](https://github.com/dahall/TaskScheduler) — Windows Task Scheduler wrapper

---

## 📊 Project Status

| Metric | Status |
|--------|--------|
| **Version** | 2.0.0-beta |
| **Target Framework** | .NET 8 |
| **Tests** | 48 passing |
| **Coverage** | 90%+ target |
| **Languages** | 6 supported |
| **Platforms** | Windows 10/11 x64 |

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/minhtuancn/WinCleaner/issues)
- **Discussions**: [GitHub Discussions](https://github.com/minhtuancn/WinCleaner/discussions)
- **Security**: [SECURITY.md](SECURITY.md) — Report vulnerabilities privately
- **Docs**: [Documentation](docs/)

---

*WinCleaner — Clean smarter, not harder.*