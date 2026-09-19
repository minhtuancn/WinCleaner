# WinCleaner v2.0.0 Production Release Checklist

> **Target Release**: v2.0.0  
> **Branch**: `main`  
> **Date**: 2026-09-20  
> **Release Manager**: @minhtuancn

---

## Pre-Release Validation

### Code Quality
- [ ] All CI checks pass (build, test, static-analysis, security-scan, quality-gate)
- [ ] Code coverage ≥ 15% (baseline; target 90% for v2.1)
- [ ] No critical/high CodeQL findings
- [ ] No vulnerable NuGet packages (`dotnet list package --vulnerable`)
- [ ] No secrets in codebase (API keys, passwords, tokens)
- [ ] No TODO/FIXME/HACK comments in production code
- [ ] All Roslyn analyzers pass (`RunAnalyzers=true`, `EnforceCodeStyleInBuild=true`)

### Testing
- [ ] All 48 unit tests pass
- [ ] Manual UI smoke test: App launches, navigation works, all 7 views render
- [ ] CLI commands work: `scan`, `clean`, `list`, `database update`, `--silent`
- [ ] Theme switching (Light/Dark/System) works
- [ ] Language switching (6 languages) works
- [ ] Settings persist across restarts
- [ ] Auto-update check works (GitHub API)
- [ ] MSIX package installs/uninstalls correctly

### Build Artifacts
- [ ] Modern MSI: `WinCleaner-{version}-x64.msi` (self-contained)
- [ ] Modern MSIX: `WinCleaner-{version}-x64.msix` (Windows App SDK)
- [ ] Classic MSI: `WinCleaner-Classic-{version}-x64.msi` (optional, if built)
- [ ] SHA256SUMS.txt for all artifacts
- [ ] All artifacts code-signed (cert configured in GitHub Secrets)
- [ ] Version metadata correct in all artifacts

### Version & Metadata
- [ ] `version.json` updated with correct version, build number, release date, channel
- [ ] `Version.props` synchronized
- [ ] `CHANGELOG.md` updated with v2.0.0 changes
- [ ] Git tag `v2.0.0` created and pushed
- [ ] GitHub Release created with artifacts and auto-generated notes

### Security & Compliance
- [ ] Code signing certificate valid (not expired)
- [ ] Timestamp server configured for signing
- [ ] Privacy policy published (required for MSIX/Store)
- [ ] No GPL/non-permissive licenses in dependencies
- [ ] SBOM generated (optional but recommended)

### Documentation
- [ ] README.md reflects v2.0.0 features
- [ ] CONTRIBUTING.md up to date
- [ ] SECURITY.md has correct contact
- [ ] ROADMAP.md updated post-release

### Deployment
- [ ] GitHub Release published (draft → published)
- [ ] Artifacts uploaded to Release (MSI, MSIX, SHA256SUMS)
- [ ] Release notes auto-generated + manually reviewed
- [ ] Pre-release checkbox set correctly (false for stable)

### Post-Release
- [ ] Monitor GitHub Issues for regressions (first 48h)
- [ ] Verify auto-update triggers for existing users
- [ ] Announce on relevant channels (GitHub Discussions, etc.)
- [ ] Plan v2.0.1 hotfix window if needed

---

## Signing Configuration (GitHub Secrets Required)

| Secret | Description | Source |
|--------|-------------|--------|
| `SIGNING_CERT` | Base64-encoded .pfx certificate | Azure Key Vault / DigiCert |
| `SIGNING_CERT_PASSWORD` | Certificate password | Azure Key Vault |
| `NUGET_API_KEY` | For NuGet publishing | nuget.org |

---

## Rollback Plan

If critical issue found post-release:
1. Create hotfix branch: `hotfix/v2.0.1-critical-fix`
2. Fix, test, build
3. Tag `v2.0.1`, create Release
4. Users auto-update within 24h (check interval)

---

## Contact

- **Release Manager**: @minhtuancn
- **Security**: security@minhtuancn.github.io
- **Issues**: https://github.com/minhtuancn/WinCleaner/issues

---

*Checklist version: 1.0 | Last updated: 2026-09-20*