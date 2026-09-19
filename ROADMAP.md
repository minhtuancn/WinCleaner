# WinCleaner v2.0.0 – Roadmap

> **Target**: Production-ready release v2.0.0  
> **Baseline**: .NET 8, WPF + CLI, 48 unit tests passing  
> **Last Updated**: 2026-09-19

---

## Milestone Overview

| Milestone | Target Date | Status | Description |
|---|---|---|---|
| **M1: Core Feature Parity** | 2026-09-26 | 🔄 In Progress | Winapp2 parser, AppxService, CLI silent mode |
| **M2: Repository Hygiene & Docs** | 2026-10-03 | 📋 Planned | Config files, templates, README, CONTRIBUTING |
| **M3: Professional Installer** | 2026-10-17 | 📋 Planned | MSIX packaging, code signing, auto-close |
| **M4: Release Pipeline & Quality** | 2026-10-24 | 📋 Planned | GH Actions release, coverage ≥90%, security scan |
| **M5: Production Release v2.0.0** | 2026-10-31 | 📋 Planned | Checklist, tag, notarize, privacy policy |

---

## M1: Core Feature Parity (Week 1)

### 1.1 Winapp2 Parser Enhancement
- [ ] Add `FileKeyEntry` model with full property support
- [ ] Add `RegKeyEntry` model with full property support  
- [ ] Update `Winapp2Service.ParseIniContent()` to parse FileKey/RegKey sections
- [ ] Add unit tests for new parser functionality
- **Files**: `src/WinCleaner.Core/Models/*.cs`, `src/WinCleaner.Core/Services/Winapp2Service.cs`

### 1.2 AppxService Enhancement
- [ ] Enumerate installed Store apps via `PackageManager`
- [ ] Add cleanable categories (cache, temp, logs) for Store apps
- [ ] Integrate with `ISystemScanner` and `ICleanerService`
- [ ] Add unit tests
- **Files**: `src/WinCleaner.Core/Services/AppxService.cs`

### 1.3 CLI Silent Mode
- [ ] Add `--silent` / `-s` flag to CLI
- [ ] Suppress all console output except errors
- [ ] Return appropriate exit codes (0=success, 1=error, 2=items skipped)
- [ ] Update help text
- **Files**: `src/WinCleaner.Cli/CLI/Program.cs`

---

## M2: Repository Hygiene & Documentation (Week 2)

### 2.1 Repository Config Files
- [ ] `.editorconfig` – consistent formatting across editors
- [ ] `.github/dependabot.yml` – automated dependency updates
- [ ] `renovate.json` – advanced dependency automation
- [ ] `CODEOWNERS` – code ownership for reviews
- [ ] `SECURITY.md` – vulnerability reporting policy
- [ ] `CONTRIBUTING.md` – contribution guidelines

### 2.2 Issue/PR Templates & Labels
- [ ] `.github/ISSUE_TEMPLATE/bug_report.yml`
- [ ] `.github/ISSUE_TEMPLATE/feature_request.yml`
- [ ] `.github/PULL_REQUEST_TEMPLATE.md`
- [ ] Label set: `bug`, `enhancement`, `documentation`, `good first issue`, `help wanted`, `priority:high`, `priority:medium`, `priority:low`, `area:core`, `area:ui`, `area:cli`, `area:installer`, `status:blocked`

### 2.3 Documentation Overhaul
- [ ] `README.md` – professional description, badges, architecture diagram, quick start, contribution guide, license
- [ ] `docs/architecture/README.md` – architecture overview
- [ ] `docs/architecture/adr/` – Architecture Decision Records
- [ ] `docs/api/` – API reference (docfx)
- [ ] `docs/user-guide/` – user documentation

---

## M3: Professional Installer (Week 3)

### 3.1 MSIX Packaging
- [ ] Add `WindowsAppSDK` package reference
- [ ] Create `Package.appxmanifest` with proper capabilities
- [ ] Configure MSIX build in `Directory.Build.props`
- [ ] Test MSIX installation/uninstallation

### 3.2 Code Signing
- [ ] Azure Key Vault / certificate setup (CI secrets)
- [ ] Sign MSIX and MSI artifacts in release workflow
- [ ] Timestamp signing

### 3.3 Auto-Close Running Instance
- [ ] Implement `AppRunningGuard` integration in installer
- [ ] Detect and gracefully close running WinCleaner before install
- [ ] Restart app after installation (optional)

---

## M4: Release Pipeline & Quality Gates (Week 4)

### 4.1 GitHub Actions Release Pipeline
- [ ] Complete `build-release.yml` for MSIX
- [ ] Auto-generate release notes from commit messages
- [ ] Upload signed artifacts to GitHub Releases
- [ ] Publish to Microsoft Store (optional)

### 4.2 Quality Gates
- [ ] Coverage ≥ 90% (coverlet + reportgenerator)
- [ ] Static analysis: SonarCloud / Roslyn analyzers
- [ ] Security scan: CodeQL, dependabot alerts
- [ ] Performance benchmarks (startup time, scan speed)

---

## M5: Production Release v2.0.0 (Week 5)

### 5.1 Release Checklist
- [ ] All tests pass (unit + integration)
- [ ] Code coverage ≥ 90%
- [ ] No critical/high security vulnerabilities
- [ ] MSIX signed and verified
- [ ] Privacy policy published
- [ ] CHANGELOG.md updated
- [ ] Version bumped to `2.0.0` in `version.json` / `Version.props`

### 5.2 Release Tag & Publish
- [ ] Create git tag `v2.0.0`
- [ ] GitHub Release with artifacts
- [ ] Announce / publish

---

## Blocked / Deferred

| Item | Blocker | Resolution Path |
|---|---|---|
| Inspector Blazor WASM | `wasm-tools` workload install fails (disk space) | Free disk space → install workload → fix version conflicts → re-enable project |
| Additional languages (7 more) | Low priority | Add JSON files to `Resources/` when needed |

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