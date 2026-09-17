# WinCleaner - Comprehensive Enhancement Plan

**Version:** 2.0 Planning  
**Date:** 2026-09-17  
**Author:** Minh Tuấn

---

## 🎯 Vision

Build **the best open-source Windows system cleaner** that combines:
- **Modern native UI** (WinUI 3 / WPF with Fluent Design)
- **Community-powered cleaning rules** (Winapp2.ini compatibility)
- **Enterprise-grade features** (CLI, scheduling, secure deletion)
- **Zero bloat, zero tracking, zero upsells**
- **Full transparency** - user sees exactly what will be deleted

---

## 📊 Competitive Analysis

### Top Competitors

| Feature | CCleaner | BleachBit | Fluent Cleaner | Wise Disk Cleaner | **WinCleaner (Target)** |
|---------|----------|-----------|----------------|-------------------|------------------------|
| **License** | Freemium (Pro) | GPLv3 (Free) | MIT (Free) | Freemium | **MIT (Free)** |
| **UI Framework** | Win32 | GTK/Win32 | **WinUI 3** | Win32 | **WPF + Fluent** |
| **Winapp2.ini** | ❌ Dropped | ❌ Custom | ✅ Native Parser | ❌ | ✅ **Full Support** |
| **CLI/Terminal** | ❌ | ✅ | ✅ | ❌ | ✅ **Full Support** |
| **Secure Delete** | ❌ | ✅ (DoD/Gutmann) | ❌ | ❌ | ✅ **Multiple Standards** |
| **Custom Rules** | ❌ | ✅ CleanerML | ❌ | ❌ | ✅ **Custom Engine** |
| **Scheduled Clean** | Pro only | CLI + Cron | ✅ /AUTO flag | Pro only | ✅ **Built-in Scheduler** |
| **AppX Debloat** | ❌ | ❌ | ✅ Winappx.ini | ❌ | ✅ **Native Module** |
| **Registry Clean** | ✅ (Risky) | ❌ (Safe) | ❌ (Safe) | ✅ | ⚠️ **Optional + Warnings** |
| **Extensions/Plugins** | ❌ | ❌ | ✅ | ❌ | ✅ **Plugin System** |
| **Multi-Database** | ❌ | ❌ | ✅ (3 DBs) | ❌ | ✅ **Unlimited** |
| **Explorer Integration** | ❌ | ❌ | ✅ | ❌ | ✅ **Open in Explorer** |
| **Window State Persistence** | ❌ | ❌ | ✅ | ❌ | ✅ **Full Restore** |
| **Localization** | ✅ | ✅ | ✅ JSON | ✅ | ✅ **JSON Runtime** |
| **Theme Support** | ❌ | ❌ | System only | ❌ | ✅ **Light/Dark/System** |
| **Portable Version** | ❌ | ✅ | ✅ | ❌ | ✅ **Self-contained** |
| **Telemetry/Ads** | ✅ (Heavy) | ❌ | ❌ | ❌ | ❌ **Zero** |
| **System Restore Point** | ✅ | ❌ | ❌ | ❌ | ✅ **Auto-create** |

---

## 🚀 Phase 1: Core Stability & Current Fixes (Week 1-2)

### 1.1 Fix Current Issues
- [ ] Fix `ExpanderToggleButtonStyle` StaticResource ordering in Styles.xaml
- [ ] Fix MainWindow binding issues (Drives[0] null check)
- [ ] Verify Admin elevation works correctly
- [ ] Fix Scan command not triggering properly
- [ ] Fix CleanAction delegate signature (Async support)
- [ ] Add proper error handling & user feedback

### 1.2 Architecture Improvements
- [ ] Migrate to **WinUI 3** for true native Windows 11 look (or enhance WPF Fluent)
- [ ] Implement **Settings Service** with proper JSON serialization
- [ ] Add **ILogger** integration throughout
- [ ] Implement **Unit Test** project (xUnit)
- [ ] Add **CI/CD Pipeline** (GitHub Actions)

---

## 🚀 Phase 2: Winapp2.ini Integration (Week 2-3)

### 2.1 Winapp2.ini Parser
```csharp
// Core models
public class Winapp2Entry {
    public string Name { get; set; }
    public string Section { get; set; }  // [SectionName]
    public string Detect { get; set; }   // Detect=HKCU\Software\...
    public string DetectFile { get; set; }
    public string DetectOS { get; set; }
    public List<FileKey> FileKeys { get; set; }
    public List<RegKey> RegKeys { get; set; }
    public string Warning { get; set; }
    public string Default { get; set; }  // True/False
    public int RebootOk { get; set; }
}

public class FileKey {
    public string Path { get; set; }      // %AppData%\Mozilla\Firefox\Profiles\*\cache2
    public string Pattern { get; set; }   // *.tmp
    public bool Recurse { get; set; }
    public string Exclude { get; set; }
}
```

### 2.2 Features
- [ ] Download latest Winapp2.ini from GitHub (MoscaDotTo/Winapp2)
- [ ] Parse INI with full syntax support (Detect, DetectFile, DetectOS, FileKey, RegKey, Exclude)
- [ ] Environment variable expansion (%AppData%, %LocalAppData%, %ProgramFiles%, %SystemDrive%, etc.)
- [ ] Recursive directory scanning with pattern matching
- [ ] Registry key scanning (read-only, no deletion by default)
- [ ] **Multi-database**: Winapp2 + Winapp3 (modern apps) + Winappx (AppX bloatware) + Custom
- [ ] Deduplication across databases (first match wins)
- [ ] Custom database support (user can add .ini files)
- [ ] Auto-update database (weekly check)

### 2.3 Integration
- [ ] Replace hardcoded clean items with Winapp2 entries
- [ ] Keep built-in profiles as fallback
- [ ] Show source database for each entry
- [ ] Allow enabling/disabling per database

---

## 🚀 Phase 3: CLI/Terminal Mode (Week 3-4)

### 3.1 Command Structure
```bash
WinCleaner.exe [command] [options]

Commands:
  scan           Scan system for junk
  clean          Clean selected items
  list           List available cleaners
  databases      Manage cleaning databases
  schedule       Manage scheduled tasks
  config         Manage settings

Options:
  --profile <Safe|Deep|Custom|Nuclear>
  --database <winapp2|winapp3|winappx|custom|all>
  --dry-run      Preview only
  --auto         Silent mode (no UI)
  --shutdown     Shutdown after clean
  --output <json|text|csv>
  --log <path>   Log file path
  --help         Show help
```

### 3.2 Implementation
- [ ] `Program.cs` entry point with command parsing (System.CommandLine)
- [ ] Headless mode (no WPF window creation)
- [ ] JSON output for automation/integration
- [ ] Exit codes: 0=success, 1=error, 2=cancelled, 3=partial
- [ ] PowerShell completion script generation
- [ ] Task Scheduler integration (`/AUTO` flag)

### 3.3 Automation
- [ ] Windows Task Scheduler wrapper
- [ ] `/AUTO` = silent clean with saved profile
- [ ] `/AUTO /SHUTDOWN` = clean then shutdown
- [ ] Auto-log to `%AppData%\WinCleaner\auto.log`
- [ ] Event Log integration

---

## 🚀 Phase 4: Secure Deletion (Week 4)

### 4.1 Standards Implementation
```csharp
public enum ShredAlgorithm
{
    Quick,              // 1 pass - zeros
    DoD_5220_22_M,      // 3 passes - DoD 5220.22-M
    DoD_5220_22_M_ECE,  // 7 passes - DoD 5220.22-M ECE
    Gutmann,            // 35 passes - Gutmann
    RCMP_TSSIT_OPS_II,  // 7 passes - RCMP TSSIT OPS-II
    VSITR,              // 7 passes - VSITR
    HMG_IS5_Baseline,   // 1 pass - HMG IS5 Baseline
    HMG_IS5_Enhanced,   // 3 passes - HMG IS5 Enhanced
    Schneier,           // 7 passes - Bruce Schneier
    PFITzer,            // 35 passes - PFitzner
    Random,             // 1 pass - random data
    Custom              // User-defined passes
}
```

### 4.2 Features
- [ ] Implement all standard algorithms
- [ ] Per-file or per-folder shredding
- [ ] Progress reporting per file
- [ ] Verify deletion (read back)
- [ ] MFT entry wiping (NTFS)
- [ ] Free space wiping (wipe unused clusters)
- [ ] Integration with clean operations (optional)
- [ ] Right-click context menu "Shred with WinCleaner"

---

## 🚀 Phase 5: Custom Cleaner Rules Engine (Week 5)

### 5.1 Rule Definition
```json
{
  "name": "My Custom Cleaner",
  "description": "Cleans temporary files from MyApp",
  "category": "Applications",
  "riskLevel": "Safe",
  "rules": [
    {
      "type": "File",
      "path": "%LocalAppData%\\MyApp\\Cache",
      "pattern": "*.tmp",
      "recursive": true,
      "exclude": "*.important"
    },
    {
      "type": "Registry",
      "key": "HKCU\\Software\\MyApp\\RecentFiles",
      "action": "DeleteValues"
    }
  ],
  "conditions": {
    "detectFile": "%LocalAppData%\\MyApp\\MyApp.exe",
    "detectRegistry": "HKCU\\Software\\MyApp",
    "osVersion": ">=10.0.17763"
  }
}
```

### 5.2 Features
- [ ] JSON-based rule format (easier than INI)
- [ ] Visual rule editor in UI
- [ ] Import/Export rules
- [ ] Rule validation & testing
- [ ] Community rule sharing (GitHub Gists)
- [ ] Built-in rule templates

---

## 🚀 Phase 6: Advanced Features (Week 6-8)

### 6.1 Scheduled/Auto Clean
- [ ] Built-in scheduler UI (not just Task Scheduler wrapper)
- [ ] Multiple schedules (daily, weekly, monthly, on idle, on startup)
- [ ] Per-profile scheduling
- [ ] Notification before clean (toast)
- [ ] Post-clean summary notification
- [ ] Run on idle detection
- [ ] Battery-aware (skip on battery)

### 6.2 AppX/Debloat Module
- [ ] Parse Winappx.ini format
- [ ] List all installed AppX packages
- [ ] Categorize: Microsoft, Office, Gaming, Social, Media
- [ ] Safe/Unsafe classification
- [ ] Bulk remove with confirmation
- [ ] PowerShell backend for removal
- [ ] Restore capability (reinstall from Store)

### 6.3 Explorer Integration
- [ ] "Open in Explorer" for scan results (file & folder)
- [ ] "Open in Registry Editor" for registry entries
- [ ] Copy path to clipboard
- [ ] Properties dialog
- [ ] Shell extension (right-click → Clean with WinCleaner)

### 6.4 Window State Persistence
- [ ] Window position, size, maximized state
- [ ] TreeView expanded/collapsed state
- [ ] Selected profile, columns, sort order
- [ ] Log window scroll position
- [ ] Per-user settings in `%AppData%\WinCleaner\windowstate.json`

---

## 🚀 Phase 7: Multi-Database & Extensions (Week 8-10)

### 7.1 Database Management
- [ ] **Winapp2.ini** - Community classic (1000s of apps)
- [ ] **Winapp3.ini** - Modern/UWP apps
- [ ] **Winappx.ini** - AppX bloatware packages
- [ ] **Custom.ini** - User-defined
- [ ] **Embedded** - Built-in fallback rules
- [ ] Database priority order (first match wins)
- [ ] Enable/disable per database
- [ ] Auto-update with version checking
- [ ] Offline mode (bundled databases)

### 7.2 Extensions/Plugin System
```csharp
public interface ICleanerExtension
{
    string Name { get; }
    string Version { get; }
    string Author { get; }
    Task<IEnumerable<CleanerEntry>> GetEntriesAsync();
    Task<bool> CleanAsync(CleanerEntry entry, IProgress<string> progress);
    Task<bool> ValidateAsync();
}
```

- [ ] Plugin discovery (`%AppData%\WinCleaner\Extensions\`)
- [ ] .NET assembly loading (DLL plugins)
- [ ] PowerShell script plugins
- [ ] Python script plugins (if Python installed)
- [ ] Extension marketplace (GitHub-based)
- [ ] Sandboxed execution (limited permissions)

---

## 🚀 Phase 8: UX & Polish (Week 10-12)

### 8.1 Settings Management
- [ ] Export settings to JSON file
- [ ] Import settings from JSON
- [ ] Reset to defaults
- [ ] Settings profiles (Work/Home/Dev)
- [ ] Sync via cloud (optional, user-controlled)

### 8.2 Localization
- [ ] JSON-based translations
- [ ] Runtime language switching
- [ ] Community translations (Crowdin/GitHub)
- [ ] RTL support
- [ ] Pluralization support

### 8.3 Theme Support
- [ ] Light / Dark / System
- [ ] Accent color (Windows accent or custom)
- [ ] High contrast support
- [ ] Custom theme JSON

### 8.4 System Restore Point
- [ ] Auto-create before Deep/Custom/Nuclear clean
- [ ] Manual "Create Restore Point" button
- [ ] Show existing restore points
- [ ] COM API (SRSetRestorePoint)

---

## 🚀 Phase 9: Additional Tools (Week 12-16)

### 9.1 Analysis Tools
| Tool | Description |
|------|-------------|
| **Duplicate Finder** | Hash-based (MD5/SHA256) duplicate detection |
| **Large File Finder** | Top 100 largest files, treemap visualization |
| **Empty Folder Cleaner** | Recursive empty directory removal |
| **Broken Shortcut Fixer** | Fix/remove invalid .lnk files |
| **Registry Defrag** | Compact registry hives (optional) |

### 9.2 Management Tools
| Tool | Description |
|------|-------------|
| **Uninstaller** | Bulk uninstall, forced uninstall, leftover scan |
| **Startup Manager** | Enable/disable/remove startup entries |
| **Service Manager** | View/manage Windows services |
| **Driver Cleaner** | Remove old driver versions (DriverStore) |
| **Update Cleaner** | Remove Windows Update backup files |

### 9.3 Privacy Tools
| Tool | Description |
|------|-------------|
| **Browser Privacy** | Cookies, history, passwords, form data |
| **Recent Files Cleaner** | Clear Jump Lists, Recent Items, Office MRU |
| **Thumbnail Cache** | Clear icon/thumbnail caches |
| **DNS/ARP Cache** | Flush network caches |

---

## 🚀 Phase 10: Distribution & Packaging (Week 16-18)

### 10.1 Build Variants
| Variant | Size | Runtime | Target |
|---------|------|---------|--------|
| **Portable (Self-contained)** | ~80MB | Bundled | USB, no-admin |
| **Installer (MSIX)** | ~5MB | Framework | Store, Enterprise |
| **Installer (EXE/NSIS)** | ~10MB | Framework | General |
| **Portable (Framework-dependent)** | ~5MB | System | Tech-savvy |
| **CLI Only** | ~5MB | Bundled | Servers, Automation |

### 10.2 Distribution
- [ ] GitHub Releases (auto on tag)
- [ ] Microsoft Store (MSIX)
- [ ] Winget manifest
- [ ] Chocolatey package
- [ ] Scoop bucket
- [ ] Homebrew (if cross-platform)

### 10.3 Security
- [ ] Authenticode signing
- [ ] Reproducible builds
- [ ] SBOM generation
- [ ] Dependency vulnerability scanning
- [ ] CodeQL analysis

---

## 📋 Technical Debt & Quality

### Code Quality
- [ ] **SonarCloud** integration
- [ ] **StyleCop** / **EditorConfig** enforcement
- [ ] **Nullable** reference types everywhere
- [ ] **Async** suffix convention
- [ ] **CancellationToken** propagation
- [ ] **IDisposable** pattern for resources

### Testing
- [ ] **Unit Tests** (>80% coverage)
- [ ] **Integration Tests** (scan/clean workflows)
- [ ] **UI Tests** (Playwright/WPF)
- [ ] **Property-based Tests** (FsCheck)
- [ ] **Mutation Testing** (Stryker.NET)

### Documentation
- [ ] **Architecture Decision Records** (ADRs)
- [ ] **API Documentation** (DocFX)
- [ ] **User Guide** (MkDocs)
- [ ] **Developer Guide**
- [ ] **Video Tutorials**

---

## 🎯 Success Metrics

| Metric | Target |
|--------|--------|
| **GitHub Stars** | 1,000+ in 6 months |
| **Downloads/Release** | 10,000+ |
| **Contributors** | 10+ active |
| **Issue Resolution** | <7 days average |
| **Test Coverage** | >85% |
| **Build Time** | <5 minutes |
| **App Size (Portable)** | <100MB |
| **Scan Time (Full)** | <30 seconds |
| **Memory Usage** | <150MB |
| **Crash Rate** | <0.1% |

---

## 🤝 Community Building

- [ ] **Discord/Slack** community
- [ ] **GitHub Discussions** for Q&A
- [ ] **Monthly Dev Blog** posts
- [ ] **Hacktoberfest** participation
- [ ] **Bug Bounty** program
- [ ] **Translator** recognition program

---

## 📅 Timeline Summary

| Phase | Weeks | Focus |
|-------|-------|-------|
| 1 | 1-2 | Core Stability |
| 2 | 2-3 | Winapp2.ini Parser |
| 3 | 3-4 | CLI/Terminal |
| 4 | 4 | Secure Deletion |
| 5 | 5 | Custom Rules |
| 6 | 6-8 | Advanced Features |
| 7 | 8-10 | Multi-DB & Extensions |
| 8 | 10-12 | UX & Polish |
| 9 | 12-16 | Additional Tools |
| 10 | 16-18 | Distribution |

**Total: ~18 weeks (4.5 months) to v2.0**

---

## 💡 Quick Wins (Can do this week)

1. ✅ Fix current UI bugs
2. ✅ Add Winapp2.ini downloader & parser (core)
3. ✅ Add CLI entry point with `/AUTO` flag
4. ✅ Add Secure Delete (Quick/DoD)
5. ✅ Add Window state persistence
6. ✅ Add Settings Export/Import
7. ✅ Add Theme support (Light/Dark/System)
8. ✅ Create portable build script
9. ✅ GitHub Actions CI/CD
10. ✅ Unit test project

---

*This plan is a living document. Priorities may shift based on community feedback and real-world usage.*