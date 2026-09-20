# Task 3: Light / Dark Theme Dictionaries - Report

## What Was Implemented

1. **Stripped CornerRadius keys from Light.xaml and Dark.xaml** - Both theme dictionaries now contain ONLY:
   - Color keys (PrimaryColor, BackgroundColor, TextPrimaryColor, etc.)
   - SolidColorBrush keys (PrimaryBrush, BackgroundBrush, TextPrimaryBrush, etc.)
   - Font keys (DefaultFont, MonospaceFont, FontSizeSmall through FontSizeTitle)

2. **Added ThemeDictionaryTests** - New test verifies:
   - Light.xaml contains `PrimaryColor` and `BackgroundBrush` (color/brush keys)
   - Light.xaml does NOT contain `MarginXs`, `CornerRadiusNormal`, or `ShadowMedium` (spacing/corner/shadow keys)

3. **Added WpfTestFixture** - Collection fixture for sharing a single Application instance across WPF tests, fixing the "Cannot create more than one System.Windows.Application instance" error when running tests together.

4. **Updated SharedThemeResourcesTests** - Modified to use the new WpfTestFixture collection.

## TDD Evidence

### RED Phase
```bash
dotnet test tests/WinCleaner.Tests.Unit --filter "FullyQualifiedName~ThemeDictionaryTests" --no-build
```
**Result:** FAILED - Test found `CornerRadiusNormal` in Light.xaml keys (pos 45)

### GREEN Phase
After removing CornerRadius keys from both theme files:
```bash
dotnet test tests/WinCleaner.Tests.Unit --filter "FullyQualifiedName~ThemeDictionaryTests" --no-build
```
**Result:** PASSED - Light theme contains color/brush keys, no spacing/corner/shadow keys

Full test suite: **51/51 passing**

## Files Changed

| File | Change |
|------|--------|
| `WinCleaner-classic/Resources/Themes/Light.xaml` | Removed CornerRadius section (4 keys) |
| `WinCleaner-classic/Resources/Themes/Dark.xaml` | Removed CornerRadius section (4 keys) |
| `tests/WinCleaner.Tests.Unit/SharedThemeResourcesTests.cs` | Updated to use WpfTestFixture collection |
| `tests/WinCleaner.Tests.Unit/ThemeDictionaryTests.cs` | New test file (TDD) |
| `tests/WinCleaner.Tests.Unit/WpfTestFixture.cs` | New fixture for shared Application instance |

## Commit

- **ae2359c** - feat(theme): strip spacing/corner/shadow from Light/Dark theme dicts

## Self-Review Findings

**Completeness:** ✅ All requirements met - theme dicts contain only Color, SolidColorBrush, and Font keys.

**Quality:** ✅ Clean separation - spacing/corner/shadow keys remain only in SharedThemeResources.xaml as intended for runtime theme switching.

**Testing:** ✅ TDD followed - wrote failing test first, then fixed implementation. All 51 tests pass.

**Concerns:** None. The implementation follows the plan exactly and enables proper runtime theme switching by keeping theme-specific resources (colors/brushes) in swappable dictionaries while shared resources (spacing/corner/shadow) stay in the always-loaded SharedThemeResources.