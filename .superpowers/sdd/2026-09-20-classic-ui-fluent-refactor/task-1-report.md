# Task 1: Design System Tokens - Implementation Report

## Summary
Successfully implemented Fluent design system tokens for the Classic edition by creating four token classes in the `WinCleaner.Design` namespace.

## Implementation Details

### Files Created
1. **WinCleaner-classic/Design/ColorPalette.cs** - Complete color palette with light/dark theme support (Primary, Semantic status, Neutral grays, Surface, Text, Border, Shadow colors + DarkColorPalette nested class)
2. **WinCleaner-classic/Design/Typography.cs** - Typography scale with font families, display/heading/title/body/label sizes, font weights, line heights, and CreateTextStyle helper
3. **WinCleaner-classic/Design/Spacing.cs** - 4px base unit spacing scale (Xs through Xxxl), component-specific spacing, border radius, and shadow values
4. **WinCleaner-classic/Design/Icons.cs** - Updated to match Modern edition with 60+ vector Geometry icons (Navigation, Actions, Status, App-specific, Categories, Hardware)

### Files Modified
1. **WinCleaner-classic/WinCleaner.csproj** - No changes needed (SDK auto-includes .cs files)
2. **tests/WinCleaner.Tests.Unit/WinCleaner.Tests.Unit.csproj** - Added Classic project reference with `ReferenceOutputAssembly="false"` and build target to copy assembly for test discovery

### Test Created
- **tests/WinCleaner.Tests.Unit/DesignTokensTests.cs** - Verifies all four token classes exist in the WinCleaner assembly

## TDD Evidence

### RED Phase
```bash
dotnet test WinCleaner/tests/WinCleaner.Tests.Unit --filter "DesignTokens_Exist" --configuration Release
```
**Result:** FAIL - `System.IO.FileNotFoundException: Could not load file or assembly 'WinCleaner'`
- Expected failure because token classes didn't exist yet and assembly wasn't discoverable

### GREEN Phase
After implementing all four token classes and fixing test project configuration:
```bash
dotnet test WinCleaner/tests/WinCleaner.Tests.Unit --filter "DesignTokens_Exist" --configuration Release
```
**Result:** PASS - `Passed: 1, Failed: 0`

Full test suite: `49/49 passing, output pristine`

## Self-Review Findings

**Completeness:** ✅ All four token classes implemented matching Modern edition exactly
**Quality:** ✅ Clean code, proper namespace (`WinCleaner.Design`), frozen geometries for performance
**Discipline:** ✅ No overbuilding - only implemented what was specified
**Testing:** ✅ TDD followed - test written first, failed, then implementation made it pass

## Concerns
- The test project required a workaround (ReferenceOutputAssembly="false" + copy target) to avoid type conflicts between WinCleaner.Core and WinCleaner-classic (both have duplicate models like ItemRiskLevel). This is a pre-existing architectural issue, not introduced by this task.
- Icons.cs was updated to match Modern edition exactly (added missing Hardware icons: Cpu, Memory, Gpu, Disk; Categories: Windows, Browser, Code, Game, TrashBin; fixed Upload/Download paths)

## Report Path
D:\dev\setup-ai\WinCleaner\.superpowers\sdd\2026-09-20-classic-ui-fluent-refactor\task-1-report.md