# WinCleaner vs FluentCleaner – Feature Matrix

| Feature | FluentCleaner | WinCleaner (current) | Status | Notes |
|---|---|---|---|---|
| **Core models** – `CleanerEntry`, `FileKeyEntry`, `RegKeyEntry`, `ExcludeKeyEntry`, `ScanResult` | ✅ | Partial (only basic `CleanItem`) | 🔄 In‑progress | Need richer rule model |
| **Winapp2 parser** with full `ExcludeKey`, `FileKey`, `RegKey` support | ✅ | Basic parser only | 🔄 In‑progress | Need full parser |
| **AI explainer** (`AiExplainer.cs`) – generates human‑readable description of a rule | ✅ | ❌ | 📋 Planned | Add AI/LLM integration |
| **Cookie cleaning** (`CookieService.cs`) | ✅ | ❌ | 📋 Planned | Add cookie DB scanning |
| **Task Scheduler integration** (`TaskSchedulerService.cs`) | ✅ | ❌ | 📋 Planned | Schedule clean runs |
| **AppX / Store apps cleaning** (`AppxService.cs`) | ✅ | Minimal | 🔄 In‑progress | Extend |
| **Inspector UI** (Blazor WASM – `FluentCleaner.Inspector`) | ✅ | ❌ | 📋 Planned | Rule‑inspector web view |
| **Multi‑language JSON localisation** (13 languages) | ✅ | English only | 📋 Planned | Add localisation infra |
| **Silent runner** (`SilentRunner.cs`) for headless clean | ✅ | CLI exists but no silent‑mode flag | 📋 Planned | Add `--silent` |
| **Professional installer** – MSIX/EXE, code‑sign, auto‑update, close‑running‑app | ✅ (MSIX build) | MSI only, no signing | 📋 Planned | MSIX + signing |
| **Rich CI** – Dependabot, Renovate, CodeQL, security policy | ✅ | Basic CI only | 📋 Planned | Add tooling |

## Legend
- ✅  Implemented
- 🔄  In‑progress / partially done
- 📋  Planned / not started
- ❌  Missing

## Next Actions (priority order)
1. **Feature matrix & ADRs** – document decisions (this file).
2. **Upgrade Winapp2 parser** – add `ExcludeKeyEntry`, `FileKeyEntry`, `RegKeyEntry` models and extend parser.
3. **Core services** – `CookieService`, `TaskSchedulerService`, `AiExplainer`, enhance `AppxService`, add `SilentRunner` flag.
4. **Inspector UI** – scaffold Blazor WASM project (`WinCleaner.Inspector`).
5. **Localization framework** – `ResourceService` + JSON files (en, vi, de, fr, zh‑CN, zh‑TW …).
6. **Professional installer** – MSIX package + optional EXE bootstrapper, code‑sign, auto‑close running instance.
7. **GitHub Actions – Release pipeline** – build MSIX, sign, upload, generate release notes.
8. **Repository hygiene** – `.editorconfig`, `dependabot.yml`, `renovate.json`, `CODEOWNERS`, `SECURITY.md`, `CONTRIBUTING.md`, issue/PR templates.
9. **Documentation overhaul** – rewrite `README.md`, add `docs/` with Roadmap, ADR, API reference.
10. **Quality gate** – full test suite, coverage ≥ 90 %, static analysis, security scan.
11. **Production release checklist & tag** – sign, notarize, privacy policy, tag `v2.0.0`.

---
*Generated as part of WinCleaner v2.0 roadmap alignment with FluentCleaner reference implementation.*