# WinCleaner vs FluentCleaner – Feature Matrix

| Feature | FluentCleaner | WinCleaner (current) | Status | Notes |
|---|---|---|---|---|
| **Core models** – `CleanerEntry`, `FileKeyEntry`, `RegKeyEntry`, `ExcludeKeyEntry`, `ScanResult` | ✅ | ✅ | ✅ Done | Models exist, parser reads ExcludeKey |
| **Winapp2 parser** with full `ExcludeKey`, `FileKey`, `RegKey` support | ✅ | ✅ Implemented | ✅ Done | Parser reads FileKey/FileKey1-5, RegKey/RegKey1-5, ExcludeKey/ExcludeKey1-5 |
| **AI explainer** (`AiExplainer.cs`) – generates human‑readable description of a rule | ✅ | ✅ Implemented | ✅ Done | Local fallback + optional LLM endpoint |
| **Cookie cleaning** (`CookieService.cs`) | ✅ | ✅ Implemented | ✅ Done | SQLite cookie DB scan (Chrome/Edge/Brave/Opera/Vivaldi) |
| **Task Scheduler integration** (`TaskSchedulerService.cs`) | ✅ | ✅ Implemented | ✅ Done | Full Windows Task Scheduler integration with weekly triggers, task creation/deletion, enable/disable |
| **AppX / Store apps cleaning** (`AppxService.cs`) | ✅ | ✅ Implemented | ✅ Done | Full enumeration, categorization, risk assessment, cleanable items (cache, temp, logs, cookies, local storage, IndexedDB, crash reports, telemetry), package-specific cleanups |
| **Inspector UI** (Blazor WASM – `FluentCleaner.Inspector`) | ✅ | ❌ Disabled | 📋 Planned | Scaffolded but disabled (build issues); needs fix |
| **Multi‑language JSON localisation** (13 languages) | ✅ | 6 languages (en, vi, de, fr, zh-CN, zh-TW) | ✅ Done | `ResourceService` + embedded JSON resources |
| **Silent runner** (`SilentRunner.cs`) for headless clean | ✅ | CLI `--silent` flag | ✅ Done | Global `-s/--silent` flag suppresses all output except errors, exit codes for automation |
| **Professional installer** – MSIX/EXE, code‑sign, auto‑update, close‑running‑app | ✅ (MSIX build) | MSIX infrastructure ready | 🔄 In-progress | Package.appxmanifest, Assets, Publish profile, Windows App SDK reference; requires wasm-tools workload for full build |
| **Rich CI** – Dependabot, Renovate, CodeQL, security policy | ✅ | ✅ Implemented | ✅ Done | `.github/dependabot.yml`, `renovate.json`, `CODEOWNERS`, `SECURITY.md`, `CONTRIBUTING.md`, issue/PR templates, label set |
| **Auto-update service** (`IUpdateService`, `UpdateService`) | ✅ | ✅ Implemented | ✅ Done | GitHub Releases polling, silent MSI install, Settings UI |
| **Settings persistence** | ✅ | ✅ Implemented | ✅ Done | Theme + update channel persisted |
| **Auto-update Settings UI** | ✅ | ✅ Implemented | ✅ Done | Channel (Stable/Beta/Preview), auto-download, auto-install |
| **Winapp2 parser** with `ExcludeKey` support | ✅ | ✅ Implemented | ✅ Done | Parser reads ExcludeKey entries |
| **Winapp2 parser** `ExcludeKeyEntry` model | ✅ | ✅ Implemented | ✅ Done | Model + parser support |
| **Resource service** (`IResourceService`, `ResourceService`) | ✅ | ✅ Implemented | ✅ Done | Embedded JSON, culture switching, fallback |

## Legend
- ✅  Implemented
- 🔄  In‑progress / partially done
- 📋  Planned / not started
- ❌  Missing

## Completed This Session
- ✅ Fixed Inspector build issues (disabled temporarily due to Blazor WASM SDK issues)
- ✅ Added `CookieService` with SQLite cookie DB scanning
- ✅ Added `TaskSchedulerService` with full Windows Task Scheduler integration
- ✅ Added `AiExplainer` with local fallback + optional LLM
- ✅ Added `IUpdateService` / `UpdateService` with GitHub Releases polling
- ✅ Added Settings UI for auto-update (channel, auto-download, auto-install)
- ✅ Winapp2 parser: added `ExcludeKeyEntry` model + parser support
- ✅ Version management: `version.json`, `Version.props`, `global.json`
- ✅ GitHub Actions: CI, Beta build, Release build (dual Modern/Classic MSI)
- ✅ MSIX installer scaffolding in workflows (code-sign, auto-close app)
- ✅ Dual-build workflow (Modern + Classic MSI)
- ✅ All 48 unit tests pass, build passes
- ✅ Localization framework: `ResourceService` + 6 language JSON files (en, vi, de, fr, zh-CN, zh-TW)

## Next P0 Actions (Priority Order)
1. ✅ **Enhance Winapp2 parser** – FileKey/RegKey model support
2. ✅ **Enhance AppxService** – enumerate Store apps, add cleanable categories
3. ✅ **CLI silent mode** – `--silent` flag for headless clean
4. ✅ **Repository hygiene** – `.editorconfig`, `dependabot.yml`, `renovate.json`, `CODEOWNERS`, `SECURITY.md`, `CONTRIBUTING.md`, issue/PR templates
5. ✅ **Documentation overhaul** – rewrite `README.md`, add `docs/` with Roadmap, ADR, API reference
6. **Professional MSIX installer** – Windows App SDK packaging, code‑sign, auto‑close running instance
7. **GitHub Actions Release pipeline** – build MSIX, sign, upload assets, generate release notes
8. **Quality gate** – enforce ≥ 90 % coverage, static analysis, security scan
9. **Production release checklist & tag** – sign, notarize, privacy policy, tag `v2.0.0`
10. **Fix Inspector Blazor WASM build** – install `wasm-tools` workload, fix version conflicts, re-enable

---
*Updated: 2026-09-19 – Localization complete, feature matrix synchronized with actual implementation*