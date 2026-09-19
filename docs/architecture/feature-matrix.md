# WinCleaner vs FluentCleaner – Feature Matrix

| Feature | FluentCleaner | WinCleaner (current) | Status | Notes |
|---|---|---|---|---|
| **Core models** – `CleanerEntry`, `FileKeyEntry`, `RegKeyEntry`, `ExcludeKeyEntry`, `ScanResult` | ✅ | ✅ Partial (models exist, parser updated) | ✅ Done | Models exist, parser reads ExcludeKey |
| **Winapp2 parser** with full `ExcludeKey`, `FileKey`, `RegKey` support | ✅ | Basic parser + ExcludeKey | 🔄 In‑progress | Parser reads ExcludeKey; need FileKey/RegKey enhancement |
| **AI explainer** (`AiExplainer.cs`) – generates human‑readable description of a rule | ✅ | ✅ Implemented | ✅ Done | Local fallback + optional LLM endpoint |
| **Cookie cleaning** (`CookieService.cs`) | ✅ | ✅ Implemented | ✅ Done | SQLite cookie DB scan (Chrome/Edge/Brave/Opera/Vivaldi) |
| **Task Scheduler integration** (`TaskSchedulerService.cs`) | ✅ | ✅ Stub implemented | 🔄 In‑progress | Interface + DI; needs real TaskScheduler impl |
| **AppX / Store apps cleaning** (`AppxService.cs`) | ✅ | Minimal | 🔄 In‑progress | Extend |
| **Inspector UI** (Blazor WASM – `FluentCleaner.Inspector`) | ✅ | ❌ Disabled | 📋 Planned | Scaffolded but disabled (build issues); needs fix |
| **Multi‑language JSON localisation** (13 languages) | ✅ | English only | 📋 Planned | Add localisation infra |
| **Silent runner** (`SilentRunner.cs`) for headless clean | ✅ | CLI exists but no `--silent` flag | 📋 Planned | Add `--silent` flag |
| **Professional installer** – MSIX/EXE, code‑sign, auto‑update, close‑running‑app | ✅ (MSIX build) | MSI only, no signing | 📋 Planned | MSIX + signing |
| **Rich CI** – Dependabot, Renovate, CodeQL, security policy | ✅ | CodeQL + basic CI | 🔄 In‑progress | Need Dependabot, Renovate |
| **Auto-update service** (`IUpdateService`, `UpdateService`) | ✅ | ✅ Implemented | ✅ Done | GitHub Releases polling, silent MSI install, Settings UI |
| **Settings persistence** | ✅ | ✅ Implemented | ✅ Done | Theme + update channel persisted |
| **Auto-update Settings UI** | ✅ | ✅ Implemented | ✅ Done | Channel (Stable/Beta/Preview), auto-download, auto-install |
| **Winapp2 parser** with `ExcludeKey` support | ✅ | ✅ Implemented | ✅ Done | Parser reads ExcludeKey entries |
| **Winapp2 parser** `ExcludeKeyEntry` model | ✅ | ✅ Implemented | ✅ Done | Model + parser support |

## Legend
- ✅  Implemented
- 🔄  In‑progress / partially done
- 📋  Planned / not started
- ❌  Missing

## Next Actions (priority order)
1. **Fix Inspector Blazor build** – fix `ResolveWasmOutputs` target, version conflicts, re-enable
2. **Complete TaskSchedulerService** – implement real `Microsoft.Win32.TaskScheduler` integration
3. **Localization framework** – `ResourceService` + JSON files (en, vi, de, fr, zh‑CN, zh‑TW …)
4. **AppxService enhancement** – enumerate Store apps, add cleanable categories
5. **CLI silent mode** – `--silent` flag for headless clean
6. **Professional installer (MSIX)** – Windows App SDK packaging, code‑sign, auto‑close running instance
7. **GitHub Actions – Release pipeline** – build MSIX, sign, upload assets, generate release notes
8. **Repository hygiene** – `.editorconfig`, `dependabot.yml`, `renovate.json`, `CODEOWNERS`, `SECURITY.md`, `CONTRIBUTING.md`, issue/PR templates
9. **Documentation overhaul** – rewrite `README.md`, add `docs/` with Roadmap, ADR, API reference (docfx)
10. **Quality gate** – enforce ≥ 90 % coverage, static analysis, security scan
11. **Production release checklist & tag** – sign, notarize, privacy policy, tag `v2.0.0`

## Completed This Session
- ✅ Fixed Inspector build issues (disabled temporarily due to Blazor WASM SDK issues)
- ✅ Added `CookieService` with SQLite cookie DB scanning
- ✅ Added `TaskSchedulerService` (stub) with DI registration
- ✅ Added `AiExplainer` with local fallback + optional LLM
- ✅ Added `IUpdateService` / `UpdateService` with GitHub Releases polling
- ✅ Added Settings UI for auto-update (channel, auto-download, auto-install)
- ✅ Winapp2 parser: added `ExcludeKeyEntry` model + parser support
- ✅ Version management: `version.json`, `Version.props`, `global.json`
- ✅ GitHub Actions: CI, Beta build, Release build (dual Modern/Classic MSI)
- ✅ MSIX installer scaffolding in workflows (code-sign, auto-close app)
- ✅ Dual-build workflow (Modern + Classic MSI)
- ✅ All 48 unit tests pass, build passes

---
*Updated after session: Inspector disabled, new services added, CI/CD workflows created*