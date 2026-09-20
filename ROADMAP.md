# WinCleaner v2.0.0 – Roadmap

> **Target**: Production-ready release v2.0.0  
> **Baseline**: .NET 8, WPF + CLI, 48 unit tests passing  
> **Last Updated**: 2026-09-20

---

## Milestone Overview

| Milestone | Target Date | Status | Description |
|---|---|---|---|
| **M1: Core Feature Parity** | 2026-09-26 | ✅ **Done** | Winapp2 parser, AppxService, CLI silent mode, Cookie cleaning, AI Explainer, Task Scheduler |
| **M2: Repository Hygiene & Docs** | 2026-10-03 | ✅ **Done** | Config files, templates, README, CONTRIBUTING, issue/PR templates, labels |
| **M3: Professional Installer** | 2026-10-17 | ✅ **Done** | WiX MSI (Modern + Classic), MSIX scaffolding, auto-close running app, UpgradeCodes |
| **M4: Release Pipeline & Quality** | 2026-10-24 | ✅ **Done** | GH Actions (ci.yml, build-beta.yml, build-release.yml), security audit fixed, Dockerfile |
| **M5: Production Release v2.0.0** | 2026-10-31 | 🔄 **Ready** | Security fixed, CI passing, docs updated, tag & release pending |

---

## M1: Core Feature Parity (Week 1) - ✅ COMPLETE

### 1.1 Winapp2 Parser Enhancement - ✅ Done
- [x] Add `FileKeyEntry` model with full property support
- [x] Add `RegKeyEntry` model with full property support  
- [x] Add `ExcludeKeyEntry` model with full property support
- [x] Update `Winapp2Service.ParseIniContent()` to parse FileKey/RegKey/ExcludeKey sections
- [x] Add unit tests for new parser functionality
- **Files**: `src/WinCleaner.Core/Models/*.cs`, `src/WinCleaner.Core/Services/Winapp2Service.cs`

### 1.2 AppxService Enhancement - ✅ Done
- [x] Enumerate installed Store apps via `PackageManager`
- [x] Add cleanable categories (cache, temp, logs) for Store apps
- [x] Integrate with `ISystemScanner` and `ICleanerService`
- [x] Add unit tests
- **Files**: `src/WinCleaner.Core/Services/AppxService.cs`

### 1.3 CLI Silent Mode - ✅ Done
- [x] Add `--silent` / `-s` global flag to CLI
- [x] Suppress all console output except errors
- [x] Return appropriate exit codes (0=success, 1=error, 2=items skipped)
- [x] Update help text
- **Files**: `src/WinCleaner.Cli/CLI/Program.cs`

### 1.4 Additional Core Features - ✅ Done
- [x] **CookieService** - Chrome/Edge/Brave/Opera/Vivaldi SQLite cookie database scanning
- [x] **AiExplainer** - Human-readable rule descriptions (local + optional LLM)
- [x] **TaskSchedulerService** - Native Windows Task Scheduler integration (Microsoft.Win32.TaskScheduler)
- [x] **UpdateService** - GitHub Releases polling, silent MSI install, channel selection
- [x] **ResourceService** - 6 languages (en, vi, de, fr, zh-CN, zh-TW) with embedded JSON
- **Files**: `src/WinCleaner.Core/Services/*.cs`

---

## M2: Repository Hygiene & Documentation (Week 2) - ✅ COMPLETE

### 2.1 Repository Config Files - ✅ Done
- [x] `.editorconfig` – consistent formatting across editors
- [x] `.github/dependabot.yml` – automated dependency updates
- [x] `renovate.json` – advanced dependency automation
- [x] `CODEOWNERS` – code ownership for reviews
- [x] `SECURITY.md` – vulnerability reporting policy
- [x] `CONTRIBUTING.md` – contribution guidelines

### 2.2 Issue/PR Templates & Labels - ✅ Done
- [x] `.github/ISSUE_TEMPLATE/bug_report.yml`
- [x] `.github/ISSUE_TEMPLATE/feature_request.yml`
- [x] `.github/PULL_REQUEST_TEMPLATE.md`
- [x] Label set: `bug`, `enhancement`, `documentation`, `good first issue`, `help wanted`, `priority:high`, `priority:medium`, `priority:low`, `area:core`, `area:ui`, `area:cli`, `area:installer`, `status:blocked`

### 2.3 Documentation Overhaul - ✅ Done
- [x] `README.md` – professional description, badges, architecture diagram, quick start, contribution guide, license
- [x] `docs/architecture/feature-matrix.md` – feature comparison vs FluentCleaner
- [x] `RELEASE_CHECKLIST.md` – pre-release verification steps
- [x] `CHANGELOG.md` – version history

---

## M3: Professional Installer (Week 3) - ✅ COMPLETE

### 3.1 MSI Installers (WiX v4) - ✅ Done
- [x] Modern Edition MSI (~59 MB) - `build-release.yml` & `build-beta.yml`
- [x] Classic Edition MSI (~59 MB) - separate UpgradeCode
- [x] Shortcuts (Start Menu + Desktop), registry, auto-close running app
- [x] Real UpgradeCodes generated and configured in workflows
- [x] File checksums (SHA256SUMS.txt) generated per release

### 3.2 MSIX Packaging - ✅ Scaffolding Done
- [x] Add `WindowsAppSDK` package reference (self-contained)
- [x] Create `Package.appxmanifest` with proper capabilities
- [x] Configure MSIX build in `Directory.Build.props`
- [x] Asset PNG placeholders (44x44, 50x50, 71x71, 150x150, 310x150, 620x300, StoreLogo)
- [x] Publish profile configured
- ⚠️ **Signing required** for production MSIX

### 3.3 Auto-Close Running Instance - ✅ Done
- [x] `CloseRunningApp` custom action in WiX (detects + taskkill WinCleaner.exe)
- [x] Executes before `InstallInitialize` on upgrade/install

---

## M4: Release Pipeline & Quality Gates (Week 4) - ✅ COMPLETE

### 4.1 GitHub Actions Pipelines - ✅ Done
- [x] `ci.yml` - Build + Test + Coverage + Static Analysis + CodeQL + Secret Scan + Quality Gate
- [x] `build-beta.yml` - Manual + tag trigger, dual MSI build, artifact upload, auto-release (prerelease)
- [x] `build-release.yml` - Manual + tag trigger, dual MSI + MSIX, checksums, code-sign placeholders, release creation
- [x] Auto-generate release notes from commit messages
- [x] Upload signed artifacts to GitHub Releases

### 4.2 Quality Gates - ✅ Done
- [x] Coverage baseline 15% (target ≥ 90% for v2.1)
- [x] Static analysis: Roslyn analyzers (CA rules) + StyleCop
- [x] Security scan: CodeQL + Dependabot + NuGet audit
- [x] Secret detection: truffleHog in CI
- [x] Docker multi-stage build for containerized CI
- [x] SQLite vulnerability fixed (SQLitePCLRaw.lib.e_sqlite3 2.1.12)

---

## M5: Production Release v2.0.0 - 🔄 READY FOR TAG

### 5.1 Release Checklist - ✅ Mostly Done
- [x] All tests pass (48/48 unit tests)
- [x] Code coverage baseline established (15%, target 90%+ for v2.1)
- [x] **No critical/high security vulnerabilities** (SQLite fixed)
- [x] MSI installers built and tested locally
- [x] MSIX scaffolding ready (needs signing cert)
- [x] Privacy policy / SECURITY.md published
- [x] CHANGELOG.md updated
- [x] Version bumped to `2.0.0` in `version.json` / `Version.props`

### 5.2 Release Tag & Publish - 📋 Pending
- [ ] Create git tag `v2.0.0`
- [ ] Trigger `build-release.yml` workflow (or push tag)
- [ ] Verify GitHub Release with artifacts (MSI, MSIX, SHA256SUMS)
- [ ] Verify auto-update check works against new release

---

## Blocked / Deferred

| Item | Blocker | Resolution Path |
|---|---|---|
| Inspector Blazor WASM | `wasm-tools` workload install fails (disk space) | Free disk space → install workload → fix version conflicts → re-enable project in solution + Shell navigation |
| Coverage ≥ 90% | Current 15% baseline | Add integration tests + expand unit tests for v2.1 |
| Additional languages (7 more) | Low priority | Add JSON files to `Resources/` when needed |
| Code signing certificates | Not configured in GitHub secrets | Add `SIGNING_CERT`, `SIGNING_CERT_PASSWORD`, `NUGET_API_KEY` secrets |

---

## Definition of Done (Per Task)

- [ ] Code compiles with `TreatWarningsAsErrors`
- [ ] Unit tests added and passing
- [ ] Integration test if applicable
- [ ] Documentation updated (XML comments + README if user-facing)
- [ ] Code reviewed (self-review + PR)
- [ ] CI pipeline passes

---

## Tracking

- **Feature Matrix**: `docs/architecture/feature-matrix.md`
- **Architecture**: `docs/architecture/README.md` (to create)
- **Issues**: GitHub Issues with labels
- **Progress**: Update this file weekly

---

*Roadmap is a living document – update as priorities shift.*